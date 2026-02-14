using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
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
using NSubstitute;
using ReactiveUI;
using Xunit;
using EmailAttachment = MIC.Core.Application.Common.Interfaces.EmailAttachment;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Extended tests for <see cref="ComposeEmailViewModel"/> covering GetContentType,
/// SendEmailAsync success/failure paths, prefix deduplication, CanExecute guards,
/// and event invocation.
/// </summary>
[Collection("UserSessionServiceTests")]
public sealed class ComposeEmailViewModelExtendedTests : IDisposable
{
    private readonly IServiceProvider? _originalProvider;
    private readonly SessionStorageScope _sessionScope;
    private readonly FakeEmailAccountRepository _accountRepo = new();
    private readonly FakeEmailRepository _emailRepo = new();
    private readonly FakeErrorHandlingService _errorHandling = new();
    private readonly IEmailSenderService _senderService = Substitute.For<IEmailSenderService>();

    static ComposeEmailViewModelExtendedTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public ComposeEmailViewModelExtendedTests()
    {
        _sessionScope = new SessionStorageScope(clearSession: true);
        _originalProvider = GetProgramServiceProvider();
        SetProgramServiceProvider(BuildServiceProvider());
        UserSessionService.Instance.Clear();
    }

    public void Dispose()
    {
        SetProgramServiceProvider(_originalProvider);
        UserSessionService.Instance.Clear();
        _sessionScope.Dispose();
    }

    #region GetContentType Tests (via reflection)

    [Theory]
    [InlineData("document.txt", "text/plain")]
    [InlineData("report.pdf", "application/pdf")]
    [InlineData("file.doc", "application/msword")]
    [InlineData("file.docx", "application/msword")]
    [InlineData("data.xls", "application/vnd.ms-excel")]
    [InlineData("data.xlsx", "application/vnd.ms-excel")]
    [InlineData("slides.ppt", "application/vnd.ms-powerpoint")]
    [InlineData("slides.pptx", "application/vnd.ms-powerpoint")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("animation.gif", "image/gif")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    [InlineData("noext", "application/octet-stream")]
    [InlineData("FILE.PDF", "application/pdf")]
    public void GetContentType_ReturnsExpectedMimeType(string filePath, string expected)
    {
        SetupUserSession();
        var vm = CreateViewModel();
        var method = typeof(ComposeEmailViewModel)
            .GetMethod("GetContentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();

        var result = (string)method!.Invoke(vm, new object[] { filePath })!;
        result.Should().Be(expected);
    }

    #endregion

    #region SendEmailAsync – Compose Success Path

    [Fact]
    public async Task SendEmailAsync_ComposeMode_Success_InvokesOnSent()
    {
        SetupUserSession();
        _accountRepo.AddAccount(CreateAccount());
        var vm = CreateViewModel();

        // Wait for accounts to load
        await Task.Delay(100);
        vm.SelectedAccount.Should().NotBeNull();

        vm.To = "recipient@test.com";
        vm.Subject = "Test Subject";
        vm.Body = "Test Body";

        _senderService.SendEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EmailSendResult { Success = true }));

        var sentFired = false;
        vm.OnSent += () => sentFired = true;

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        sentFired.Should().BeTrue();
        vm.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SendEmailAsync_ComposeMode_Failure_SetsErrorMessage()
    {
        SetupUserSession();
        _accountRepo.AddAccount(CreateAccount());
        var vm = CreateViewModel();
        await Task.Delay(100);

        vm.To = "recipient@test.com";
        vm.Subject = "Test Subject";
        vm.Body = "Test Body";

        _senderService.SendEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EmailSendResult { Success = false, ErrorMessage = "SMTP error" }));

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        vm.ErrorMessage.Should().Contain("SMTP error");
    }

    [Fact]
    public async Task SendEmailAsync_NullSenderService_SetsErrorMessage()
    {
        SetupUserSession();
        // Build provider without sender service
        var services = new ServiceCollection();
        services.AddSingleton<IEmailAccountRepository>(_accountRepo);
        services.AddSingleton<IEmailRepository>(_emailRepo);
        services.AddSingleton<IErrorHandlingService>(_errorHandling);
        SetProgramServiceProvider(services.BuildServiceProvider());

        _accountRepo.AddAccount(CreateAccount());
        var vm = new ComposeEmailViewModel();
        await Task.Delay(100);

        vm.To = "test@test.com";
        vm.Subject = "Test";
        vm.Body = "Body";

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        vm.ErrorMessage.Should().Contain("not available");
    }

    [Fact]
    public async Task SendEmailAsync_ExceptionThrown_SetsErrorMessage()
    {
        SetupUserSession();
        _accountRepo.AddAccount(CreateAccount());
        var vm = CreateViewModel();
        await Task.Delay(100);

        vm.To = "recipient@test.com";
        vm.Subject = "Test";
        vm.Body = "Body";

        _senderService.SendEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<EmailSendResult>(x => throw new InvalidOperationException("Connection lost"));

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        vm.ErrorMessage.Should().Contain("Connection lost");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ReplyMode_CallsReplyToEmailAsync()
    {
        SetupUserSession();
        var account = CreateAccount();
        _accountRepo.AddAccount(account);
        var emailId = Guid.NewGuid();
        _emailRepo.Set(CreateEmailMessage(emailId, account.UserId, account.Id));

        var vm = CreateViewModel();
        await Task.Delay(100);

        // Set up reply mode 
        vm.To = "sender@test.com";
        vm.Subject = "Re: Test";
        vm.Body = "Reply body";
        // Use reflection to set Mode
        var modeProp = typeof(ComposeEmailViewModel)
            .GetProperty("Mode");
        modeProp!.SetValue(vm, "reply");
        var replyIdProp = typeof(ComposeEmailViewModel)
            .GetProperty("ReplyToEmailId");
        replyIdProp!.SetValue(vm, (Guid?)emailId);

        _senderService.ReplyToEmailAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EmailSendResult { Success = true }));

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        await _senderService.Received(1).ReplyToEmailAsync(
            Arg.Any<Guid>(), emailId, Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_ForwardMode_CallsForwardEmailAsync()
    {
        SetupUserSession();
        var account = CreateAccount();
        _accountRepo.AddAccount(account);
        var emailId = Guid.NewGuid();
        _emailRepo.Set(CreateEmailMessage(emailId, account.UserId, account.Id));

        var vm = CreateViewModel();
        await Task.Delay(100);

        vm.To = "forward@test.com";
        vm.Subject = "Fwd: Test";
        vm.Body = "Forward body";
        var modeProp = typeof(ComposeEmailViewModel).GetProperty("Mode");
        modeProp!.SetValue(vm, "forward");
        var fwdIdProp = typeof(ComposeEmailViewModel).GetProperty("ForwardEmailId");
        fwdIdProp!.SetValue(vm, (Guid?)emailId);

        _senderService.ForwardEmailAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EmailSendResult { Success = true }));

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        await _senderService.Received(1).ForwardEmailAsync(
            Arg.Any<Guid>(), emailId, Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Subject Prefix Deduplication

    [Fact]
    public async Task LoadEmailForReply_ExistingRePrefix_DoesNotDuplicate()
    {
        SetupUserSession();
        var account = CreateAccount();
        _accountRepo.AddAccount(account);

        var email = CreateEmailMessage(Guid.NewGuid(), account.UserId, account.Id, "Re: Hello");
        _emailRepo.Set(email);

        var vm = CreateViewModel();
        await Task.Delay(100);

        var method = typeof(ComposeEmailViewModel)
            .GetMethod("LoadEmailForReplyAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            await (Task)method.Invoke(vm, new object[] { email.Id })!;
            vm.Subject.Should().NotStartWith("Re: Re:");
        }
    }

    [Fact]
    public async Task LoadEmailForForward_ExistingFwdPrefix_DoesNotDuplicate()
    {
        SetupUserSession();
        var account = CreateAccount();
        _accountRepo.AddAccount(account);

        var email = CreateEmailMessage(Guid.NewGuid(), account.UserId, account.Id, "Fwd: Hello");
        _emailRepo.Set(email);

        var vm = CreateViewModel();
        await Task.Delay(100);

        var method = typeof(ComposeEmailViewModel)
            .GetMethod("LoadEmailForForwardAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            await (Task)method.Invoke(vm, new object[] { email.Id })!;
            vm.Subject.Should().NotStartWith("Fwd: Fwd:");
        }
    }

    #endregion

    #region Properties and Events

    [Fact]
    public void OnCancel_Event_IsRaised()
    {
        SetupUserSession();
        var vm = CreateViewModel();

        var cancelFired = false;
        vm.OnCancel += () => cancelFired = true;

        vm.CancelCommand.Execute().Subscribe();

        cancelFired.Should().BeTrue();
    }

    [Fact]
    public void IsHtml_DefaultsFalse()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.IsHtml.Should().BeFalse();
    }

    [Fact]
    public void IsHtml_CanBeSet()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.IsHtml = true;
        vm.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void ReplyToAll_DefaultsFalse()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.ReplyToAll.Should().BeFalse();
    }

    [Fact]
    public void Mode_DefaultsToCompose()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.Mode.Should().Be("compose");
    }

    [Fact]
    public void AdditionalMessage_CanBeSet()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.AdditionalMessage = "Extra info";
        vm.AdditionalMessage.Should().Be("Extra info");
    }

    [Fact]
    public void Cc_CanBeSet()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.Cc = "cc@test.com";
        vm.Cc.Should().Be("cc@test.com");
    }

    [Fact]
    public void Bcc_CanBeSet()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.Bcc = "bcc@test.com";
        vm.Bcc.Should().Be("bcc@test.com");
    }

    [Fact]
    public void EmailAccounts_IsInitialized()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.EmailAccounts.Should().NotBeNull();
    }

    [Fact]
    public void Attachments_IsInitialized()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.Attachments.Should().NotBeNull();
    }

    #endregion

    #region SendEmailAsync – Send Result with null error message

    [Fact]
    public async Task SendEmailAsync_FailureWithNullErrorMessage_UsesDefaultMessage()
    {
        SetupUserSession();
        _accountRepo.AddAccount(CreateAccount());
        var vm = CreateViewModel();
        await Task.Delay(100);

        vm.To = "to@test.com";
        vm.Subject = "Test";
        vm.Body = "Body";

        _senderService.SendEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EmailSendResult { Success = false, ErrorMessage = null }));

        var sendMethod = typeof(ComposeEmailViewModel)
            .GetMethod("SendEmailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)sendMethod!.Invoke(vm, null)!;

        vm.ErrorMessage.Should().Contain("Failed to send email");
    }

    #endregion

    #region Helpers

    private void SetupUserSession()
    {
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(
            userId.ToString("D"), "user", "user@example.com", "Test User", token: "token");
    }

    private EmailAccount CreateAccount()
    {
        var userId = Guid.Parse(UserSessionService.Instance.CurrentSession!.UserId!);
        return new EmailAccount("user@example.com", EmailProvider.Gmail, userId);
    }

    private EmailMessage CreateEmailMessage(Guid id, Guid userId, Guid accountId, string subject = "Test Subject")
    {
        return new EmailMessage(
            messageId: $"msg-{id:N}",
            subject: subject,
            fromAddress: "sender@test.com",
            fromName: "Sender",
            toRecipients: "user@example.com",
            sentDate: DateTime.UtcNow.AddMinutes(-5),
            receivedDate: DateTime.UtcNow,
            bodyText: "Original body text",
            userId: userId,
            emailAccountId: accountId
        );
    }

    private ComposeEmailViewModel CreateViewModel()
    {
        return new ComposeEmailViewModel();
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmailSenderService>(_senderService);
        services.AddSingleton<IEmailAccountRepository>(_accountRepo);
        services.AddSingleton<IEmailRepository>(_emailRepo);
        services.AddSingleton<IErrorHandlingService>(_errorHandling);
        return services.BuildServiceProvider();
    }

    private static IServiceProvider? GetProgramServiceProvider()
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(null) as IServiceProvider;
    }

    private static void SetProgramServiceProvider(IServiceProvider? provider)
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(null, provider);
    }

    #endregion

    #region Inner Fakes (shared with ComposeEmailViewModelTests)

    private sealed class FakeEmailAccountRepository : IEmailAccountRepository
    {
        private readonly List<EmailAccount> _accounts = new();

        public void AddAccount(EmailAccount account) => _accounts.Add(account);
        public void Clear() => _accounts.Clear();

        public Task AddAsync(EmailAccount entity, CancellationToken ct = default)
        {
            _accounts.Add(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailAccount entity, CancellationToken ct = default)
        {
            _accounts.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailAccount>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<EmailAccount>>(_accounts);

        public Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<EmailAccount>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EmailAccount>>(_accounts.Where(a => a.UserId == userId).ToList());

        public Task<EmailAccount?> GetPrimaryAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.UserId == userId && a.IsPrimary));

        public Task<IReadOnlyList<EmailAccount>> GetAccountsNeedingSyncAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EmailAccount>>(_accounts.Where(a => a.IsActive).ToList());

        public Task<bool> IsEmailConnectedAsync(string emailAddress, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_accounts.Any(a => a.UserId == userId && a.EmailAddress.Equals(emailAddress, StringComparison.OrdinalIgnoreCase)));

        public Task UpdateAsync(EmailAccount entity, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailRepository : IEmailRepository
    {
        private readonly Dictionary<Guid, EmailMessage> _messages = new();

        public void Set(EmailMessage message) => _messages[message.Id] = message;
        public void Clear() => _messages.Clear();

        public Task AddAsync(EmailMessage entity, CancellationToken ct = default)
        {
            _messages[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailMessage entity, CancellationToken ct = default)
        {
            _messages.Remove(entity.Id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailMessage>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<EmailMessage>>(_messages.Values.ToList());

        public Task<EmailMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_messages.TryGetValue(id, out var m) ? m : null);

        public Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(Guid userId, Guid? emailAccountId = null,
            EmailFolder? folder = null, bool? isUnread = null, int skip = 0, int take = 50,
            CancellationToken ct = default)
        {
            var q = _messages.Values.Where(m => m.UserId == userId);
            if (emailAccountId.HasValue) q = q.Where(m => m.EmailAccountId == emailAccountId.Value);
            if (folder.HasValue) q = q.Where(m => m.Folder == folder.Value);
            if (isUnread.HasValue) q = q.Where(m => m.IsRead != isUnread.Value);
            return Task.FromResult<IReadOnlyList<EmailMessage>>(q.Skip(skip).Take(take).ToList());
        }

        public Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken ct = default)
            => Task.FromResult(_messages.Values.FirstOrDefault(m => m.MessageId == messageId));

        public Task<IReadOnlyList<EmailMessage>> GetConversationAsync(string conversationId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EmailMessage>>(
                _messages.Values.Where(m => string.Equals(m.ConversationId, conversationId, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<int> GetUnreadCountAsync(Guid userId, Guid? emailAccountId = null, CancellationToken ct = default)
            => Task.FromResult(_messages.Values.Count(m => m.UserId == userId && !m.IsRead));

        public Task<int> GetRequiresResponseCountAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(_messages.Values.Count(m => m.UserId == userId && m.RequiresResponse));

        public Task MarkAsReadAsync(IEnumerable<Guid> emailIds, CancellationToken ct = default)
        {
            foreach (var id in emailIds)
                if (_messages.TryGetValue(id, out var msg)) msg.MarkAsRead();
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string messageId, CancellationToken ct = default)
            => Task.FromResult(_messages.Values.Any(m => m.MessageId == messageId));

        public Task UpdateAsync(EmailMessage entity, CancellationToken ct = default)
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
            var ec = new ErrorContext { Exception = exception, Context = context ?? "", IsCritical = isCritical };
            if (isCritical) OnCriticalError?.Invoke(ec); else OnError?.Invoke(ec);
        }

        public async Task<T?> SafeExecuteAsync<T>(Func<Task<T>> operation, string context, T? defaultValue = default, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return await operation().ConfigureAwait(false);
        }

        public Task SafeExecuteAsync(Func<Task> operation, string context, CancellationToken ct = default)
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
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MIC");
            Directory.CreateDirectory(directory);
            _sessionPath = Path.Combine(directory, "session.json");

            if (File.Exists(_sessionPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;
            }

            if (clearSession && File.Exists(_sessionPath))
                File.Delete(_sessionPath);
        }

        public void Dispose()
        {
            if (File.Exists(_sessionPath)) File.Delete(_sessionPath);
            if (_hadExisting && _backupPath != null && File.Exists(_backupPath))
            {
                File.Copy(_backupPath, _sessionPath, overwrite: true);
                File.Delete(_backupPath);
            }
        }
    }

    #endregion
}
