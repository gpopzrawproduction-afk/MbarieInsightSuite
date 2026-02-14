using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using ReactiveUI;
using Xunit;
using EmailAttachment = MIC.Core.Application.Common.Interfaces.EmailAttachment;

namespace MIC.Tests.Unit.ViewModels;

[CollectionDefinition("ComposeEmailViewModelTests", DisableParallelization = true)]
public sealed class ComposeEmailViewModelTestsCollectionDefinition
{
}

[Collection("UserSessionServiceTests")]
public sealed class ComposeEmailViewModelTests : IDisposable
{
    private readonly FakeEmailAccountRepository _accountRepository = new();
    private readonly FakeEmailRepository _emailRepository = new();
    private readonly FakeErrorHandlingService _errorHandling = new();
    private readonly SessionStorageScope _sessionScope;
    private readonly IServiceProvider? _originalProvider;

    static ComposeEmailViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public ComposeEmailViewModelTests()
    {
        _sessionScope = new SessionStorageScope(clearSession: true);
        _originalProvider = GetProgramServiceProvider();
        SetProgramServiceProvider(BuildServiceProvider());
        UserSessionService.Instance.Clear();
    }

    [Fact]
    public void Constructor_LoadsActiveAccounts_SelectsFirstActive()
    {
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var primary = new EmailAccount("user@example.com", EmailProvider.Gmail, userId);
        var secondary = new EmailAccount("team@example.com", EmailProvider.Outlook, userId);
        secondary.RemovePrimary();
        var inactive = new EmailAccount("inactive@example.com", EmailProvider.Gmail, userId);
        inactive.RemovePrimary();
        inactive.Deactivate();

        _accountRepository.SetAccounts(new[] { inactive, primary, secondary });

        var viewModel = CreateViewModel();

        WaitUntil(() => viewModel.EmailAccounts.Count == 2)
            .Should().BeTrue("active accounts should load after construction");

        viewModel.EmailAccounts.Select(a => a.EmailAddress)
            .Should().BeEquivalentTo(new[] { "user@example.com", "team@example.com" });
        viewModel.SelectedAccount.Should().Be(primary);
    }

    [Fact]
    public void ReplyToEmailId_WhenSet_PopulatesReplyTemplate()
    {
        var (viewModel, message) = CreateModelWithSampleEmail();

        viewModel.ReplyToEmailId = message.Id;

        WaitUntil(() => viewModel.Mode == "reply")
            .Should().BeTrue("reply template should populate asynchronously");

        viewModel.Subject.Should().Be($"Re: {message.Subject}");
        viewModel.Body.Should().Contain("--- Original Message ---");
        viewModel.Body.Should().Contain(message.BodyText);
        viewModel.To.Should().Be(message.FromAddress);
    }

    [Fact]
    public void ReplyToAll_WhenTrue_UsesAllRecipientsExceptCurrent()
    {
        var (viewModel, message) = CreateModelWithSampleEmail(staticRecipients: true);
        viewModel.ReplyToAll = true;

        viewModel.ReplyToEmailId = message.Id;

        WaitUntil(() => viewModel.Mode == "reply")
            .Should().BeTrue();

        var recipients = viewModel.To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        recipients.Should().Contain(message.FromAddress);
        recipients.Should().Contain("teammate@example.com");
        recipients.Should().NotContain(viewModel.SelectedAccount!.EmailAddress);
    }

    [Fact]
    public void ForwardEmailId_WhenSet_PopulatesForwardTemplate()
    {
        var (viewModel, message) = CreateModelWithSampleEmail();
        viewModel.AdditionalMessage = "See below";

        viewModel.ForwardEmailId = message.Id;

        WaitUntil(() => viewModel.Mode == "forward")
            .Should().BeTrue();

        viewModel.Subject.Should().Be($"Fwd: {message.Subject}");
        viewModel.Body.Should().Contain("--- Forwarded Message ---");
        viewModel.Body.Should().Contain("See below");
        viewModel.Body.Should().Contain(message.BodyText);
    }

    [Fact]
    public async Task SendCommand_WhenServiceMissing_SetsErrorMessage()
    {
        var (viewModel, _) = CreateModelWithSampleEmail();

        viewModel.To = "recipient@example.com";
        viewModel.Subject = "Subject";
        viewModel.Body = "Body";

        var canExecute = await viewModel.SendCommand.CanExecute.FirstAsync();
        canExecute.Should().BeTrue();

        await viewModel.SendCommand.Execute().ToTask();

        viewModel.ErrorMessage.Should().Be("Email service or account not available");
    }

    [Fact]
    public async Task RemoveAttachmentCommand_RemovesAttachment()
    {
        var (viewModel, _) = CreateModelWithSampleEmail();

        var attachment = new EmailAttachment
        {
            FileName = "report.pdf",
            Content = new byte[] { 0x1, 0x2 },
            ContentType = "application/pdf"
        };

        viewModel.Attachments.Add(attachment);
        viewModel.Attachments.Should().HaveCount(1);

        await viewModel.RemoveAttachmentCommand.Execute(attachment).ToTask();

        viewModel.Attachments.Should().BeEmpty();
    }

    public void Dispose()
    {
        SetProgramServiceProvider(_originalProvider);
        UserSessionService.Instance.Clear();
        _accountRepository.Reset();
        _emailRepository.Clear();
        _sessionScope.Dispose();
    }

    private ComposeEmailViewModel CreateViewModel()
        => new();

    private (ComposeEmailViewModel ViewModel, EmailMessage Message) CreateModelWithSampleEmail(bool staticRecipients = false)
    {
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var primary = new EmailAccount("user@example.com", EmailProvider.Gmail, userId);
        var secondary = new EmailAccount("backup@example.com", EmailProvider.Outlook, userId);
        secondary.RemovePrimary();

        _accountRepository.SetAccounts(new[] { primary, secondary });

        var message = new EmailMessage(
            "message-1",
            "Quarterly Update",
            "finance@example.com",
            "Finance",
            staticRecipients ? "user@example.com; teammate@example.com" : "user@example.com",
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(-1),
            "Body content",
            userId,
            primary.Id);

        if (staticRecipients)
        {
            message.SetCopyRecipients("cc@example.com", null);
        }

        _emailRepository.Set(message);

        var viewModel = CreateViewModel();

        WaitUntil(() => viewModel.SelectedAccount is not null)
            .Should().BeTrue();

        return (viewModel, message);
    }

    private IServiceProvider BuildServiceProvider(IEmailSenderService? sender = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IErrorHandlingService>(_errorHandling);
        services.AddSingleton<IEmailAccountRepository>(_accountRepository);
        services.AddSingleton<IEmailRepository>(_emailRepository);
        if (sender != null)
        {
            services.AddSingleton(sender);
        }
        return services.BuildServiceProvider();
    }

    private static void SetProgramServiceProvider(IServiceProvider? provider)
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        if (programType == null)
        {
            throw new InvalidOperationException("Unable to locate Program type for service provider injection.");
        }

        var property = programType.GetProperty("ServiceProvider", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
        {
            throw new InvalidOperationException("Program.ServiceProvider property not found.");
        }

        property.SetValue(null, provider);
    }

    private static IServiceProvider? GetProgramServiceProvider()
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(null) as IServiceProvider;
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMilliseconds = 2000, int pollMilliseconds = 20)
    {
        var waited = 0;
        while (!condition())
        {
            if (waited >= timeoutMilliseconds)
            {
                return false;
            }

            Thread.Sleep(pollMilliseconds);
            waited += pollMilliseconds;
        }

        return true;
    }

    private sealed class FakeEmailAccountRepository : IEmailAccountRepository
    {
        private readonly List<EmailAccount> _accounts = new();

        public void SetAccounts(IEnumerable<EmailAccount> accounts)
        {
            _accounts.Clear();
            _accounts.AddRange(accounts);
        }

        public void Reset() => _accounts.Clear();

        public Task AddAsync(EmailAccount entity, CancellationToken cancellationToken = default)
        {
            _accounts.Add(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailAccount entity, CancellationToken cancellationToken = default)
        {
            _accounts.RemoveAll(a => a.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailAccount>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<EmailAccount>>(_accounts.ToList());

        public Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<EmailAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var result = _accounts.Where(a => a.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<EmailAccount>>(result);
        }

        public Task<EmailAccount?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.UserId == userId && a.IsPrimary));

        public Task<IReadOnlyList<EmailAccount>> GetAccountsNeedingSyncAsync(CancellationToken cancellationToken = default)
        {
            var result = _accounts.Where(a => a.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<EmailAccount>>(result);
        }

        public Task<bool> IsEmailConnectedAsync(string emailAddress, Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(_accounts.Any(a => a.UserId == userId && a.EmailAddress.Equals(emailAddress, StringComparison.OrdinalIgnoreCase)));

        public Task UpdateAsync(EmailAccount entity, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailRepository : IEmailRepository
    {
        private readonly Dictionary<Guid, EmailMessage> _messages = new();

        public void Set(EmailMessage message)
        {
            _messages[message.Id] = message;
        }

        public void Clear() => _messages.Clear();

        public Task AddAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _messages[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _messages.Remove(entity.Id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailMessage>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<EmailMessage>>(_messages.Values.ToList());

        public Task<EmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_messages.TryGetValue(id, out var message) ? message : null);

        public Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(Guid userId, Guid? emailAccountId = null, EmailFolder? folder = null, bool? isUnread = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var query = _messages.Values.Where(m => m.UserId == userId);
            if (emailAccountId.HasValue)
            {
                query = query.Where(m => m.EmailAccountId == emailAccountId.Value);
            }
            if (folder.HasValue)
            {
                query = query.Where(m => m.Folder == folder.Value);
            }
            if (isUnread.HasValue)
            {
                query = query.Where(m => m.IsRead != isUnread.Value);
            }

            var result = query.Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<EmailMessage>>(result);
        }

        public Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
            => Task.FromResult(_messages.Values.FirstOrDefault(m => m.MessageId == messageId));

        public Task<IReadOnlyList<EmailMessage>> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            var result = _messages.Values.Where(m => string.Equals(m.ConversationId, conversationId, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IReadOnlyList<EmailMessage>>(result);
        }

        public Task<int> GetUnreadCountAsync(Guid userId, Guid? emailAccountId = null, CancellationToken cancellationToken = default)
        {
            var count = _messages.Values.Count(m => m.UserId == userId && !m.IsRead);
            return Task.FromResult(count);
        }

        public Task<int> GetRequiresResponseCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var count = _messages.Values.Count(m => m.UserId == userId && m.RequiresResponse);
            return Task.FromResult(count);
        }

        public Task MarkAsReadAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default)
        {
            foreach (var id in emailIds)
            {
                if (_messages.TryGetValue(id, out var message))
                {
                    message.MarkAsRead();
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
            => Task.FromResult(_messages.Values.Any(m => m.MessageId == messageId));

        public Task UpdateAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _messages[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeErrorHandlingService : IErrorHandlingService
    {
        public event Action<ErrorContext>? OnError;
        public event Action<ErrorContext>? OnCriticalError;

        public void HandleException(Exception exception, string? context = null, bool isCritical = false)
        {
            var errorContext = new ErrorContext
            {
                Exception = exception,
                Context = context ?? string.Empty,
                IsCritical = isCritical
            };

            if (isCritical)
            {
                OnCriticalError?.Invoke(errorContext);
            }
            else
            {
                OnError?.Invoke(errorContext);
            }
        }

        public async Task<T?> SafeExecuteAsync<T>(Func<Task<T>> operation, string context, T? defaultValue = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return await operation().ConfigureAwait(false);
        }

        public Task SafeExecuteAsync(Func<Task> operation, string context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation();
        }

        public T? SafeExecute<T>(Func<T> operation, string context, T? defaultValue = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation();
        }
    }

    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope(bool clearSession)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MIC");
            Directory.CreateDirectory(directory);

            _sessionPath = Path.Combine(directory, "session.json");

            if (File.Exists(_sessionPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;
            }

            if (clearSession && File.Exists(_sessionPath))
            {
                File.Delete(_sessionPath);
            }
        }

        public void Dispose()
        {
            if (File.Exists(_sessionPath))
            {
                File.Delete(_sessionPath);
            }

            if (_hadExisting && _backupPath != null && File.Exists(_backupPath))
            {
                File.Copy(_backupPath, _sessionPath, overwrite: true);
                File.Delete(_backupPath);
            }
        }
    }
}
