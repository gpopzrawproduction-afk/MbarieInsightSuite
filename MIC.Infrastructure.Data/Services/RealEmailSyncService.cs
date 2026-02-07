using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MIC.Core.Domain.Entities;
using MIC.Core.Domain.Exceptions;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Infrastructure.Identity.Services;
using MIC.Infrastructure.AI.Services;
using MIC.Infrastructure.Data.Resilience;
using Polly.Retry;
using DomainEmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;

namespace MIC.Infrastructure.Data.Services;

public partial class RealEmailSyncService : IEmailSyncService
{
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IEmailOAuth2Service _oauth2Service;
    private readonly IEmailAnalysisService _analysisService;
    private readonly IAttachmentStorageService _attachmentStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RealEmailSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy _mailConnectivityPolicy;
    private readonly AsyncRetryPolicy _mailFetchPolicy;

    public RealEmailSyncService(
        IEmailRepository emailRepository,
        IEmailAccountRepository emailAccountRepository,
        IEmailOAuth2Service oauth2Service,
        IEmailAnalysisService analysisService,
        IAttachmentStorageService attachmentStorage,
        IUnitOfWork unitOfWork,
        ILogger<RealEmailSyncService> logger,
        IConfiguration configuration)
    {
        _emailRepository = emailRepository;
        _emailAccountRepository = emailAccountRepository;
        _oauth2Service = oauth2Service;
        _analysisService = analysisService;
        _attachmentStorage = attachmentStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _mailConnectivityPolicy = RetryPolicies.CreateMailConnectivityPolicy(_logger, "IMAP connectivity");
        _mailFetchPolicy = RetryPolicies.CreateStandardPolicy(_logger, "IMAP message fetch");
    }

    // Implement historical sync required by interface. This aggregates per-account SyncAccountAsync results
    // and reports progress via the provided IProgress instance.
    public async Task<MIC.Core.Application.Common.Interfaces.IEmailSyncService.HistoricalSyncResult> SyncHistoricalEmailsAsync(
        Guid userId,
        MIC.Core.Domain.Settings.EmailSyncSettings settings,
        IProgress<MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new MIC.Core.Application.Common.Interfaces.IEmailSyncService.HistoricalSyncResult
        {
            UserId = userId,
            StartTime = DateTimeOffset.UtcNow,
            Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.NotStarted
        };

        try
        {
            var accounts = await _emailAccountRepository.GetByUserIdAsync(userId, cancellationToken);
            if (accounts == null || accounts.Count == 0)
            {
                result.Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.NoAccountsConfigured;
                result.EndTime = DateTimeOffset.UtcNow;
                return result;
            }

            result.Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.InProgress;

            var historyStart = settings.HistoryMonths <= 0
                ? DateTime.UtcNow.AddYears(-10)
                : DateTime.UtcNow.AddMonths(-settings.HistoryMonths);

            if (historyStart > DateTime.UtcNow)
            {
                historyStart = DateTime.UtcNow;
            }

            foreach (var account in accounts)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.Failed;
                    break;
                }

                // Report starting account
                progress?.Report(new MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncProgress
                {
                    UserId = userId,
                    AccountEmail = account.EmailAddress,
                    TotalFound = 0,
                    Processed = 0,
                    Message = $"Starting sync for {account.EmailAddress} (since {historyStart:u})"
                });

                var syncResult = await SyncAccountAsync(
                    account,
                    cancellationToken,
                    historyStart,
                    settings.DownloadAttachments,
                    settings.IncludeSentFolder,
                    settings.IncludeDraftsFolder,
                    settings.IncludeArchiveFolder);

                result.TotalEmailsFound += syncResult.TotalEmailsChecked;
                result.EmailsSynced += syncResult.NewEmailsCount;

                progress?.Report(new MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncProgress
                {
                    UserId = userId,
                    AccountEmail = account.EmailAddress,
                    TotalFound = syncResult.TotalEmailsChecked,
                    Processed = syncResult.NewEmailsCount,
                    Message = $"Completed sync for {account.EmailAddress}: {syncResult.NewEmailsCount} new of {syncResult.TotalEmailsChecked} checked"
                });
            }

            result.Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.Completed;
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Historical sync failed for user {UserId}", userId);
            result.Status = MIC.Core.Application.Common.Interfaces.IEmailSyncService.SyncStatus.Failed;
            result.Errors.Add(ex.Message);
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }
    }

    public async Task<EmailSyncResult> SyncAccountAsync(
        EmailAccount account,
        CancellationToken ct = default,
        DateTime? resumeFrom = null,
        bool? syncAttachmentsOverride = null,
        bool includeSentFolder = false,
        bool includeDraftsFolder = false,
        bool includeArchiveFolder = false)
    {
        _logger.LogInformation(
            "Starting sync for account: {Email} (Provider: {Provider})",
            account.EmailAddress,
            account.Provider);

        try
        {
            using var client = new ImapClient();

            var (host, port) = GetImapSettings(account);
            var secureSocketOptions = GetSecureSocketOptions(account);
            await _mailConnectivityPolicy.ExecuteAsync(
                token => client.ConnectAsync(host, port, secureSocketOptions, token),
                ct).ConfigureAwait(false);

            if (account.Provider == EmailProvider.IMAP)
            {
                if (string.IsNullOrEmpty(account.PasswordEncrypted))
                {
                    throw new EmailSyncException("Password is required for IMAP provider");
                }

                var password = account.PasswordEncrypted;
                await _mailConnectivityPolicy.ExecuteAsync(
                    token => client.AuthenticateAsync(account.EmailAddress, password, token),
                    ct).ConfigureAwait(false);
            }
            else
            {
                var accessToken = await GetAccessTokenAsync(account, ct).ConfigureAwait(false);
                var oauth2 = new SaslMechanismOAuth2(account.EmailAddress, accessToken);
                await _mailConnectivityPolicy.ExecuteAsync(
                    token => client.AuthenticateAsync(oauth2, token),
                    ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Connected to IMAP server for {Email}", account.EmailAddress);

            var monthSetting = _configuration["EmailSync:InitialSyncMonths"];
            var defaultMonths = int.TryParse(monthSetting, out var months) ? months : 3;
            var initialSyncDays = account.InitialSyncDays > 0
                ? account.InitialSyncDays
                : defaultMonths * 30;

            var shouldSyncAttachments = syncAttachmentsOverride ?? account.SyncAttachments;

            var baseline = resumeFrom ?? DateTime.UtcNow.AddDays(-initialSyncDays);
            var computedResume = resumeFrom ?? (account.LastSyncedAt?.AddMinutes(-5) ?? baseline);

            if (!resumeFrom.HasValue && computedResume < baseline)
            {
                computedResume = baseline;
            }

            if (computedResume > DateTime.UtcNow)
            {
                computedResume = DateTime.UtcNow;
            }

            var folderResults = new List<FolderSyncResult>();

            var inboxResult = await SyncFolderAsync(
                client.Inbox,
                EmailFolder.Inbox,
                account,
                computedResume,
                shouldSyncAttachments,
                ct).ConfigureAwait(false);
            folderResults.Add(inboxResult);

            if (includeSentFolder)
            {
                var sentFolder = TryGetFolder(client, SpecialFolder.Sent);
                if (sentFolder != null)
                {
                    var sentResult = await SyncFolderAsync(
                        sentFolder,
                        EmailFolder.Sent,
                        account,
                        computedResume,
                        shouldSyncAttachments,
                        ct).ConfigureAwait(false);
                    folderResults.Add(sentResult);
                }
            }

            if (includeDraftsFolder)
            {
                var draftsFolder = TryGetFolder(client, SpecialFolder.Drafts);
                if (draftsFolder != null)
                {
                    var draftsResult = await SyncFolderAsync(
                        draftsFolder,
                        EmailFolder.Drafts,
                        account,
                        computedResume,
                        shouldSyncAttachments,
                        ct).ConfigureAwait(false);
                    folderResults.Add(draftsResult);
                }
            }

            if (includeArchiveFolder)
            {
                var archiveFolder = TryGetFolder(client, SpecialFolder.Archive) ?? TryGetFolder(client, SpecialFolder.All);
                if (archiveFolder != null)
                {
                    var archiveResult = await SyncFolderAsync(
                        archiveFolder,
                        EmailFolder.Archive,
                        account,
                        computedResume,
                        shouldSyncAttachments,
                        ct).ConfigureAwait(false);
                    folderResults.Add(archiveResult);
                }
            }

            var totalChecked = folderResults.Sum(r => r.TotalFound);
            var newCount = folderResults.Sum(r => r.NewEmails);
            var attachmentsStored = folderResults.Sum(r => r.AttachmentsStored);

            var latestReceivedUtc = folderResults
                .Where(r => r.LatestReceivedUtc.HasValue)
                .Select(r => r.LatestReceivedUtc!.Value)
                .DefaultIfEmpty(account.LastSyncedAt ?? DateTime.MinValue)
                .Max();

            var finalSyncedThrough = latestReceivedUtc == DateTime.MinValue
                ? account.LastSyncedAt
                : latestReceivedUtc;

            account.UpdateSyncStatus(
                SyncStatus.Completed,
                newCount,
                attachmentsStored,
                finalSyncedThrough);

            await _emailAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            await client.DisconnectAsync(true, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Sync completed for {Email}. New emails: {New}, Attachments stored: {Attachments}, Folders processed: {Folders}",
                account.EmailAddress,
                newCount,
                attachmentsStored,
                folderResults.Count);

            return new EmailSyncResult
            {
                Success = true,
                NewEmailsCount = newCount,
                TotalEmailsChecked = totalChecked,
                SyncedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sync failed for {Email}", account.EmailAddress);
            account.SetSyncFailed(ex.Message);
            await _emailAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return new EmailSyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    private IMailFolder? TryGetFolder(ImapClient client, SpecialFolder folder)
    {
        try
        {
            return client.GetFolder(folder);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Provider does not expose {Folder}", folder);
            return null;
        }
    }

    private async Task<FolderSyncResult> SyncFolderAsync(
        IMailFolder folder,
        EmailFolder folderKind,
        EmailAccount account,
        DateTime resumeFrom,
        bool shouldSyncAttachments,
        CancellationToken ct)
    {
        var folderName = folder.FullName ?? folder.Name ?? folderKind.ToString();

        await folder.OpenAsync(FolderAccess.ReadOnly, ct).ConfigureAwait(false);

        var query = SearchQuery.DeliveredAfter(resumeFrom);
        var uids = await folder.SearchAsync(query, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Folder {Folder}: {Count} emails to evaluate since {Since}",
            folderName,
            uids.Count,
            resumeFrom);

        var newCount = 0;
        var processedCount = 0;
        var attachmentsStored = 0;
        var latestReceivedUtc = DateTime.MinValue;

        foreach (var uid in uids)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                MimeMessage message = null!;
                await _mailFetchPolicy.ExecuteAsync(async token =>
                    {
                        message = await folder.GetMessageAsync(uid, token).ConfigureAwait(false);
                    },
                    ct).ConfigureAwait(false);
                var messageId = message.MessageId;

                if (!string.IsNullOrWhiteSpace(messageId))
                {
                    if (await _emailRepository.ExistsAsync(messageId, ct).ConfigureAwait(false))
                    {
                        continue;
                    }
                }

                var emailMessage = ConvertToEntity(message, account, folderKind);
                var attachmentCount = await ProcessAttachmentsAsync(
                        message,
                        emailMessage,
                        account,
                        shouldSyncAttachments,
                        ct)
                    .ConfigureAwait(false);
                attachmentsStored += attachmentCount;

                try
                {
                    var analysis = await _analysisService.AnalyzeEmailAsync(emailMessage, ct).ConfigureAwait(false);
                    emailMessage.SetInboxFlags(analysis.Priority, analysis.IsUrgent, false, false, analysis.ActionItems.Any());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI analysis failed for folder {Folder}; using defaults", folderName);
                }

                await _emailRepository.AddAsync(emailMessage, ct).ConfigureAwait(false);

                var receivedUtc = message.Date.UtcDateTime;
                if (receivedUtc > latestReceivedUtc)
                {
                    latestReceivedUtc = receivedUtc;
                }

                newCount++;
                processedCount++;

                if (processedCount % 25 == 0)
                {
                    _logger.LogInformation(
                        "Folder {Folder}: processed {Processed}/{Total}",
                        folderName,
                        processedCount,
                        uids.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Folder {Folder}: failed to process email UID {Uid}", folderName, uid);
            }
        }

        var latest = latestReceivedUtc == DateTime.MinValue ? (DateTime?)null : latestReceivedUtc;

        return new FolderSyncResult(folderName, uids.Count, newCount, attachmentsStored, latest);
    }

    private sealed record FolderSyncResult(
        string FolderName,
        int TotalFound,
        int NewEmails,
        int AttachmentsStored,
        DateTime? LatestReceivedUtc);

    private EmailMessage ConvertToEntity(MimeMessage message, EmailAccount account, EmailFolder folder)
    {
        var messageId = string.IsNullOrWhiteSpace(message.MessageId)
            ? Guid.NewGuid().ToString("N")
            : message.MessageId;

        var fromMailbox = message.From.Mailboxes.FirstOrDefault();
        var sentUtc = message.Date.UtcDateTime == DateTime.MinValue
            ? DateTime.UtcNow
            : message.Date.UtcDateTime;
        var receivedUtc = sentUtc;

        var toRecipients = message.To.Mailboxes.Any()
            ? string.Join("; ", message.To.Mailboxes.Select(m => m.Address))
            : account.EmailAddress;
        var ccRecipients = message.Cc.Mailboxes.Any()
            ? string.Join("; ", message.Cc.Mailboxes.Select(m => m.Address))
            : null;
        var bccRecipients = message.Bcc.Mailboxes.Any()
            ? string.Join("; ", message.Bcc.Mailboxes.Select(m => m.Address))
            : null;

        var emailMessage = new EmailMessage(
            messageId,
            message.Subject ?? "(No Subject)",
            fromMailbox?.Address ?? account.EmailAddress,
            fromMailbox?.Name ?? fromMailbox?.Address ?? account.EmailAddress,
            toRecipients,
            sentUtc,
            receivedUtc,
            message.TextBody ?? message.HtmlBody ?? string.Empty,
            account.UserId,
            account.Id,
            folder);

        emailMessage.SetHtmlBody(message.HtmlBody);

        var conversationId = message.Headers["Thread-Index"]
            ?? message.Headers["References"]
            ?? messageId;
        emailMessage.SetThreadInfo(conversationId, message.InReplyTo);

        emailMessage.SetCopyRecipients(ccRecipients, bccRecipients);

        return emailMessage;
    }

    private async Task<int> ProcessAttachmentsAsync(
        MimeMessage message,
        EmailMessage emailEntity,
        EmailAccount account,
        bool shouldSyncAttachments,
        CancellationToken ct)
    {
        if (!shouldSyncAttachments)
        {
            return 0;
        }

        var attachments = message.Attachments
            .OfType<MimePart>()
            .ToList();

        if (attachments.Count == 0)
        {
            return 0;
        }

        var storedCount = 0;

        foreach (var attachment in attachments)
        {
            ct.ThrowIfCancellationRequested();

            await using var ms = new MemoryStream();
            await attachment.Content.DecodeToAsync(ms, ct).ConfigureAwait(false);
            var data = ms.ToArray();

            if (data.Length == 0)
            {
                continue;
            }

            var fileName = string.IsNullOrWhiteSpace(attachment.FileName)
                ? $"attachment-{Guid.NewGuid():N}"
                : attachment.FileName;
            var contentType = attachment.ContentType?.MimeType ?? "application/octet-stream";

            if (account.MaxAttachmentSizeMB > 0)
            {
                var maxBytes = account.MaxAttachmentSizeMB * 1024L * 1024L;
                if (data.LongLength > maxBytes)
                {
                    _logger.LogInformation(
                        "Skipping attachment {FileName} for {AccountEmail} because size {SizeBytes} exceeds limit {LimitBytes}",
                        fileName,
                        account.EmailAddress,
                        data.LongLength,
                        maxBytes);
                    continue;
                }
            }

            try
            {
                var storeResult = await _attachmentStorage
                    .StoreAsync(fileName, contentType, data, ct)
                    .ConfigureAwait(false);

                var emailAttachment = new DomainEmailAttachment(
                    fileName,
                    contentType,
                    data.LongLength,
                    storeResult.StoragePath,
                    emailEntity.Id,
                    attachment.ContentId,
                    storeResult.ContentHash);

                emailEntity.AddAttachment(emailAttachment);
                storedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist attachment {FileName} for message {MessageId}", fileName, emailEntity.MessageId);
            }
        }

        return storedCount;
    }

    private async Task<string> GetAccessTokenAsync(EmailAccount account, CancellationToken ct)
    {
        return account.Provider switch
        {
            EmailProvider.Gmail => await _oauth2Service.GetGmailAccessTokenAsync(account.EmailAddress, ct),
            EmailProvider.Outlook => await _oauth2Service.GetOutlookAccessTokenAsync(account.EmailAddress, ct),
            _ => throw new NotSupportedException($"Provider {account.Provider} not supported")
        };
    }

    private (string host, int port) GetImapSettings(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            if (string.IsNullOrEmpty(account.ImapServer))
            {
                throw new InvalidOperationException("IMAP server is not configured for this account");
            }
            
            return (account.ImapServer, account.ImapPort > 0 ? account.ImapPort : 993);
        }
        
        // For OAuth providers, use predefined settings
        return account.Provider switch
        {
            EmailProvider.Gmail => ("imap.gmail.com", 993),
            EmailProvider.Outlook => ("outlook.office365.com", 993),
            EmailProvider.Exchange => throw new NotSupportedException("Exchange provider not yet implemented"),
            _ => throw new NotSupportedException($"Provider {account.Provider} not supported")
        };
    }

    private SecureSocketOptions GetSecureSocketOptions(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            return account.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
        }
        
        // OAuth providers always use SSL
        return SecureSocketOptions.SslOnConnect;
    }
}
