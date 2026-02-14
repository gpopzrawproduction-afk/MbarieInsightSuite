using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for EmailInboxViewModel covering property initialization, command availability, and email operations.
/// </summary>
public class EmailInboxViewModelTests : IDisposable
{
    private readonly IMediator _mediator;
    private readonly IEmailSyncService _emailSyncService;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailOAuth2Service _gmailOAuthService;
    private readonly IEmailOAuth2Service _outlookOAuthService;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly INotificationService _notificationService;

    static EmailInboxViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public EmailInboxViewModelTests()
    {
        _mediator = Substitute.For<IMediator>();
        _emailSyncService = Substitute.For<IEmailSyncService>();
        _emailAccountRepository = Substitute.For<IEmailAccountRepository>();
        _emailRepository = Substitute.For<IEmailRepository>();
        _gmailOAuthService = Substitute.For<IEmailOAuth2Service>();
        _outlookOAuthService = Substitute.For<IEmailOAuth2Service>();
        _errorHandlingService = Substitute.For<IErrorHandlingService>();
        _notificationService = Substitute.For<INotificationService>();
    }

    public void Dispose() { }

    private EmailInboxViewModel CreateViewModel()
    {
        return new EmailInboxViewModel(
            _mediator,
            _emailSyncService,
            _emailAccountRepository,
            _emailRepository,
            _gmailOAuthService,
            _outlookOAuthService,
            _errorHandlingService,
            _notificationService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesDefaultProperties()
    {
        var vm = CreateViewModel();

        vm.IsLoading.Should().BeFalse();
        vm.IsSyncing.Should().BeFalse();
        vm.SyncStatus.Should().BeEmpty();
        vm.SelectedFolder.Should().Be(EmailFolder.Inbox);
        vm.ShowUnreadOnly.Should().BeFalse();
        vm.ShowRequiresResponseOnly.Should().BeFalse();
        vm.SearchText.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        var vm = CreateViewModel();

        vm.RefreshCommand.Should().NotBeNull();
        vm.SyncCommand.Should().NotBeNull();
        vm.MarkAsReadCommand.Should().NotBeNull();
        vm.ToggleFlagCommand.Should().NotBeNull();
        vm.ArchiveCommand.Should().NotBeNull();
        vm.DeleteCommand.Should().NotBeNull();
        vm.AddGmailAccountCommand.Should().NotBeNull();
        vm.AddOutlookAccountCommand.Should().NotBeNull();
        vm.AddDirectEmailAccountCommand.Should().NotBeNull();
        vm.ExportCommand.Should().NotBeNull();
        vm.ComposeCommand.Should().NotBeNull();
        vm.ReplyCommand.Should().NotBeNull();
        vm.ForwardCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesFolders()
    {
        var vm = CreateViewModel();

        vm.Folders.Should().HaveCount(5);
    }

    [Fact]
    public void Constructor_InitializesCategories()
    {
        var vm = CreateViewModel();

        vm.Categories.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Constructor_InitializesPriorities()
    {
        var vm = CreateViewModel();

        vm.Priorities.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Constructor_InitializesEmailsCollection()
    {
        var vm = CreateViewModel();

        vm.Emails.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullMediator_SetsStatusText()
    {
        // When mediator is null, LoadEmailsAsync sets a message
        var vm = new EmailInboxViewModel(
            mediator: null,
            emailSyncService: _emailSyncService,
            emailAccountRepository: _emailAccountRepository,
            emailRepository: _emailRepository,
            gmailOAuthService: _gmailOAuthService,
            outlookOAuthService: _outlookOAuthService,
            errorHandlingService: _errorHandlingService,
            notificationService: _notificationService);

        vm.StatusText.Should().Contain("not available");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var vm = CreateViewModel();

        vm.IsLoading = true;
        vm.IsSyncing = true;
        vm.SyncStatus = "Syncing...";
        vm.StatusText = "Loading emails";
        vm.SearchText = "test query";
        vm.ShowUnreadOnly = true;
        vm.ShowRequiresResponseOnly = true;
        vm.TotalEmails = 42;
        vm.UnreadCount = 10;
        vm.RequiresResponseCount = 5;

        vm.IsLoading.Should().BeTrue();
        vm.IsSyncing.Should().BeTrue();
        vm.SyncStatus.Should().Be("Syncing...");
        vm.StatusText.Should().Be("Loading emails");
        vm.SearchText.Should().Be("test query");
        vm.ShowUnreadOnly.Should().BeTrue();
        vm.ShowRequiresResponseOnly.Should().BeTrue();
        vm.TotalEmails.Should().Be(42);
        vm.UnreadCount.Should().Be(10);
        vm.RequiresResponseCount.Should().Be(5);
    }

    [Fact]
    public void SelectedFolder_CanBeChanged()
    {
        var vm = CreateViewModel();

        vm.SelectedFolder = EmailFolder.Sent;

        vm.SelectedFolder.Should().Be(EmailFolder.Sent);
    }

    [Fact]
    public void SelectedEmail_CanBeSet()
    {
        var vm = CreateViewModel();
        var email = new EmailDto { Subject = "Test Email" };

        vm.SelectedEmail = email;

        vm.SelectedEmail.Should().BeSameAs(email);
    }

    [Fact]
    public void SelectedEmail_CanBeNull()
    {
        var vm = CreateViewModel();

        vm.SelectedEmail = null;

        vm.SelectedEmail.Should().BeNull();
    }

    #endregion

    #region Category/Priority Options Tests

    [Fact]
    public void Categories_ContainsAllCategoryOption()
    {
        var vm = CreateViewModel();

        var allOption = vm.Categories[0];
        allOption.Category.Should().BeNull();
    }

    [Fact]
    public void Priorities_ContainsAllPriorityOption()
    {
        var vm = CreateViewModel();

        var allOption = vm.Priorities[0];
        allOption.Priority.Should().BeNull();
    }

    [Fact]
    public void Folders_ContainsInbox()
    {
        var vm = CreateViewModel();

        vm.Folders.Should().Contain(f => f.Folder == EmailFolder.Inbox);
    }

    [Fact]
    public void Folders_ContainsSent()
    {
        var vm = CreateViewModel();

        vm.Folders.Should().Contain(f => f.Folder == EmailFolder.Sent);
    }

    [Fact]
    public void Folders_ContainsDrafts()
    {
        var vm = CreateViewModel();

        vm.Folders.Should().Contain(f => f.Folder == EmailFolder.Drafts);
    }

    #endregion

    #region Command Execution Tests

    private static EmailMessage CreateEmailEntity(Guid? userId = null)
    {
        return new EmailMessage(
            "msg-test",
            "Test Subject",
            "from@test.com",
            "From Name",
            "to@test.com",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Body",
            userId ?? Guid.NewGuid(),
            Guid.NewGuid());
    }

    [Fact]
    public async Task MarkAsReadCommand_WithNullEmail_CompletesWithoutError()
    {
        var vm = CreateViewModel();

        var act = () => vm.MarkAsReadCommand.Execute(null!).ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkAsReadCommand_NullRepository_ShowsError()
    {
        var vm = new EmailInboxViewModel(
            mediator: _mediator,
            emailSyncService: _emailSyncService,
            emailAccountRepository: _emailAccountRepository,
            emailRepository: null,
            gmailOAuthService: _gmailOAuthService,
            outlookOAuthService: _outlookOAuthService,
            errorHandlingService: _errorHandlingService,
            notificationService: _notificationService);

        var email = new EmailDto { Id = Guid.NewGuid(), Subject = "Test" };

        await vm.MarkAsReadCommand.Execute(email).ToTask();

        _notificationService.Received(1).ShowError(
            Arg.Is<string>(s => s.Contains("not available")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task MarkAsReadCommand_EntityNotFound_ShowsError()
    {
        var emailId = Guid.NewGuid();
        _emailRepository.GetByIdAsync(emailId).Returns((EmailMessage?)null);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Test" };

        await vm.MarkAsReadCommand.Execute(email).ToTask();

        _notificationService.Received(1).ShowError(
            Arg.Is<string>(s => s.Contains("not found")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task MarkAsReadCommand_Success_CallsUpdateAsync()
    {
        var emailId = Guid.NewGuid();
        var entity = CreateEmailEntity();
        _emailRepository.GetByIdAsync(emailId).Returns(entity);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Test Subject" };

        await vm.MarkAsReadCommand.Execute(email).ToTask();

        await _emailRepository.Received(1).UpdateAsync(entity);
        _notificationService.Received(1).ShowInfo(
            Arg.Is<string>(s => s.Contains("Marked as read")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task ToggleFlagCommand_WithNullEmail_CompletesWithoutError()
    {
        var vm = CreateViewModel();

        var act = () => vm.ToggleFlagCommand.Execute(null!).ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ToggleFlagCommand_EntityNotFound_ShowsError()
    {
        var emailId = Guid.NewGuid();
        _emailRepository.GetByIdAsync(emailId).Returns((EmailMessage?)null);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Flag Test" };

        await vm.ToggleFlagCommand.Execute(email).ToTask();

        _notificationService.Received(1).ShowError(
            Arg.Is<string>(s => s.Contains("not found")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task ToggleFlagCommand_Success_CallsUpdateAsync()
    {
        var emailId = Guid.NewGuid();
        var entity = CreateEmailEntity();
        _emailRepository.GetByIdAsync(emailId).Returns(entity);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Flag Test" };

        await vm.ToggleFlagCommand.Execute(email).ToTask();

        await _emailRepository.Received(1).UpdateAsync(entity);
    }

    [Fact]
    public async Task ArchiveCommand_WithNullEmail_CompletesWithoutError()
    {
        var vm = CreateViewModel();

        var act = () => vm.ArchiveCommand.Execute(null!).ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ArchiveCommand_Success_CallsUpdateAndNotifies()
    {
        var emailId = Guid.NewGuid();
        var entity = CreateEmailEntity();
        _emailRepository.GetByIdAsync(emailId).Returns(entity);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Archive Test" };

        await vm.ArchiveCommand.Execute(email).ToTask();

        await _emailRepository.Received(1).UpdateAsync(entity);
        _notificationService.Received(1).ShowSuccess(
            Arg.Is<string>(s => s.Contains("Archived")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task DeleteCommand_WithNullEmail_CompletesWithoutError()
    {
        var vm = CreateViewModel();

        var act = () => vm.DeleteCommand.Execute(null!).ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteCommand_Success_CallsUpdateAndNotifies()
    {
        var emailId = Guid.NewGuid();
        var entity = CreateEmailEntity();
        _emailRepository.GetByIdAsync(emailId).Returns(entity);
        var vm = CreateViewModel();
        var email = new EmailDto { Id = emailId, Subject = "Delete Test" };

        await vm.DeleteCommand.Execute(email).ToTask();

        await _emailRepository.Received(1).UpdateAsync(entity);
        _notificationService.Received(1).ShowInfo(
            Arg.Is<string>(s => s.Contains("Deleted")),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task ExportCommand_CompletesWithoutError()
    {
        var vm = CreateViewModel();

        var act = () => vm.ExportCommand.Execute().ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncCommand_NullSyncService_ShowsError()
    {
        var vm = new EmailInboxViewModel(
            mediator: _mediator,
            emailSyncService: null,
            emailAccountRepository: _emailAccountRepository,
            emailRepository: _emailRepository,
            gmailOAuthService: _gmailOAuthService,
            outlookOAuthService: _outlookOAuthService,
            errorHandlingService: _errorHandlingService,
            notificationService: _notificationService);

        await vm.SyncCommand.Execute().ToTask();

        vm.SyncStatus.Should().Contain("not available");
    }

    [Fact]
    public async Task SyncCommand_NullAccountRepo_ShowsError()
    {
        var vm = new EmailInboxViewModel(
            mediator: _mediator,
            emailSyncService: _emailSyncService,
            emailAccountRepository: null,
            emailRepository: _emailRepository,
            gmailOAuthService: _gmailOAuthService,
            outlookOAuthService: _outlookOAuthService,
            errorHandlingService: _errorHandlingService,
            notificationService: _notificationService);

        await vm.SyncCommand.Execute().ToTask();

        vm.SyncStatus.Should().Contain("not available");
    }

    #endregion
}
