using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReactiveUI;
using Xunit;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

public sealed class EmailInboxViewModelTests : IDisposable
{
    private readonly Mock<IErrorHandlingService> _errorHandlingServiceMock = new();
    private readonly Mock<IEmailRepository> _emailRepositoryMock = new();
    private readonly TestNotificationService _notificationService = new();
    private readonly IServiceProvider? _originalProvider;
    private readonly SessionStorageScope _sessionScope;

    static EmailInboxViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public EmailInboxViewModelTests()
    {
        _originalProvider = GetProgramServiceProvider();
        _sessionScope = new SessionStorageScope();
        UserSessionService.Instance.Clear();
    }

    [Fact]
    public async Task RefreshCommand_WhenMediatorMissing_SetsUnavailableStatus()
    {
        _notificationService.Reset();
        SetProgramServiceProvider(BuildServiceProvider(null));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.RefreshCommand.Execute().ToTask();

        viewModel.StatusText.Should().Be("Email data service is not available.");
        viewModel.Emails.Should().BeEmpty();
        viewModel.TotalEmails.Should().Be(0);
        viewModel.UnreadCount.Should().Be(0);
        viewModel.RequiresResponseCount.Should().Be(0);
    }

    [Fact]
    public async Task RefreshCommand_WhenMediatorReturnsEmails_PopulatesCollectionAndCounts()
    {
        _notificationService.Reset();
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var emails = new List<EmailDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Subject = "Unread",
                IsRead = false,
                RequiresResponse = true,
                Folder = EmailFolder.Inbox
            },
            new()
            {
                Id = Guid.NewGuid(),
                Subject = "Read",
                IsRead = true,
                RequiresResponse = false,
                Folder = EmailFolder.Inbox
            }
        };

        var mediator = new StubMediator((query, _) => Task.FromResult<ErrorOr<IReadOnlyList<EmailDto>>>(emails));

        SetProgramServiceProvider(BuildServiceProvider(mediator));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.RefreshCommand.Execute().ToTask();

        viewModel.TotalEmails.Should().Be(2);
        viewModel.UnreadCount.Should().Be(1);
        viewModel.RequiresResponseCount.Should().Be(1);
        viewModel.Emails.Should().HaveCount(2);

    }

    [Fact]
    public async Task RefreshCommand_WhenMediatorThrows_HandlesErrorAndUpdatesStatus()
    {
        _notificationService.Reset();
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var mediator = new StubMediator((_, _) => throw new InvalidOperationException("Mediator failure"));

        SetProgramServiceProvider(BuildServiceProvider(mediator));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.RefreshCommand.Execute().ToTask();

        viewModel.StatusText.Should().Be("Failed to load emails.");
        _errorHandlingServiceMock.Verify(
            e => e.HandleException(It.IsAny<Exception>(), "Load Emails", false),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task MarkAsReadCommand_WhenEmailExists_MarksEntityAndPersists()
    {
        _emailRepositoryMock.Reset();
        _notificationService.Reset();

        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var entity = new EmailMessage(
            "message-1",
            "Quarterly Update",
            "from@example.com",
            "Finance",
            "user@example.com",
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(-1),
            "Body",
            userId,
            Guid.NewGuid());

        var dto = new EmailDto
        {
            Id = entity.Id,
            Subject = entity.Subject,
            Folder = entity.Folder,
            IsRead = entity.IsRead
        };

        _emailRepositoryMock
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _emailRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediator = new StubMediator((_, _) => Task.FromResult<ErrorOr<IReadOnlyList<EmailDto>>>(new List<EmailDto>()));

        SetProgramServiceProvider(BuildServiceProvider(mediator, _emailRepositoryMock.Object));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.MarkAsReadCommand.Execute(dto).ToTask();

        entity.IsRead.Should().BeTrue();
        _emailRepositoryMock.Verify(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailRepositoryMock.Verify(r => r.UpdateAsync(It.Is<EmailMessage>(m => m.Id == entity.Id && m.IsRead), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleFlagCommand_WhenEmailExists_TogglesFlagAndPersists()
    {
        _emailRepositoryMock.Reset();
        _notificationService.Reset();

        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var entity = new EmailMessage(
            "message-2",
            "Incident Report",
            "from@example.com",
            "Operations",
            "user@example.com",
            DateTime.UtcNow.AddHours(-3),
            DateTime.UtcNow.AddHours(-2),
            "Body",
            userId,
            Guid.NewGuid());

        var dto = new EmailDto
        {
            Id = entity.Id,
            Subject = entity.Subject,
            Folder = entity.Folder,
            IsFlagged = entity.IsFlagged
        };

        _emailRepositoryMock
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _emailRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediator = new StubMediator((_, _) => Task.FromResult<ErrorOr<IReadOnlyList<EmailDto>>>(new List<EmailDto>()));

        SetProgramServiceProvider(BuildServiceProvider(mediator, _emailRepositoryMock.Object));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.ToggleFlagCommand.Execute(dto).ToTask();

        entity.IsFlagged.Should().BeTrue();
        _emailRepositoryMock.Verify(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailRepositoryMock.Verify(r => r.UpdateAsync(It.Is<EmailMessage>(m => m.Id == entity.Id && m.IsFlagged), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveCommand_WhenEmailExists_MovesEmailToArchive()
    {
        _emailRepositoryMock.Reset();
        _notificationService.Reset();

        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var entity = new EmailMessage(
            "message-3",
            "Weekly Digest",
            "from@example.com",
            "Insights",
            "user@example.com",
            DateTime.UtcNow.AddHours(-5),
            DateTime.UtcNow.AddHours(-4),
            "Body",
            userId,
            Guid.NewGuid());

        var dto = new EmailDto
        {
            Id = entity.Id,
            Subject = entity.Subject,
            Folder = entity.Folder
        };

        _emailRepositoryMock
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _emailRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediator = new StubMediator((_, _) => Task.FromResult<ErrorOr<IReadOnlyList<EmailDto>>>(new List<EmailDto>()));

        SetProgramServiceProvider(BuildServiceProvider(mediator, _emailRepositoryMock.Object));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.ArchiveCommand.Execute(dto).ToTask();

        entity.Folder.Should().Be(EmailFolder.Archive);
        _emailRepositoryMock.Verify(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailRepositoryMock.Verify(r => r.UpdateAsync(It.Is<EmailMessage>(m => m.Id == entity.Id && m.Folder == EmailFolder.Archive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenEmailExists_MovesEmailToTrashAndEmitsNotification()
    {
        _emailRepositoryMock.Reset();
        _notificationService.Reset();

        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@example.com", "User", token: "token");

        var entity = new EmailMessage(
            "message-4",
            "Old Announcement",
            "from@example.com",
            "Communications",
            "user@example.com",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1),
            "Body",
            userId,
            Guid.NewGuid());

        var dto = new EmailDto
        {
            Id = entity.Id,
            Subject = entity.Subject,
            Folder = entity.Folder
        };

        var initialNotifications = _notificationService.Notifications.Count;

        _emailRepositoryMock
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _emailRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediator = new StubMediator((_, _) => Task.FromResult<ErrorOr<IReadOnlyList<EmailDto>>>(new List<EmailDto>()));

        SetProgramServiceProvider(BuildServiceProvider(mediator, _emailRepositoryMock.Object));

        var viewModel = new EmailInboxViewModel(notificationService: _notificationService);

        await viewModel.DeleteCommand.Execute(dto).ToTask();

        entity.Folder.Should().Be(EmailFolder.Trash);
        _emailRepositoryMock.Verify(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailRepositoryMock.Verify(r => r.UpdateAsync(It.Is<EmailMessage>(m => m.Id == entity.Id && m.Folder == EmailFolder.Trash), It.IsAny<CancellationToken>()), Times.Once);

        var notified = SpinWait.SpinUntil(() => _notificationService.Notifications.Count > initialNotifications, TimeSpan.FromSeconds(1));
        notified.Should().BeTrue("delete command should raise a user notification");

        var toast = _notificationService.Notifications.First();
        toast.Message.Should().Be($"Deleted: {entity.Subject}");
        toast.Category.Should().Be("Email");

        _notificationService.DismissAll();
    }

    public void Dispose()
    {
        SetProgramServiceProvider(_originalProvider);
        UserSessionService.Instance.Clear();
        _sessionScope.Dispose();
    }

    private IServiceProvider BuildServiceProvider(IMediator? mediator, IEmailRepository? emailRepository = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IErrorHandlingService>(_errorHandlingServiceMock.Object);
        if (emailRepository != null)
        {
            services.AddSingleton<IEmailRepository>(emailRepository);
        }
        if (mediator != null)
        {
            services.AddSingleton(mediator);
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

    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope()
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
                File.Delete(_sessionPath);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath is not null)
                {
                    File.Copy(_backupPath, _sessionPath, overwrite: true);
                    File.Delete(_backupPath);
                }
                else if (File.Exists(_sessionPath))
                {
                    File.Delete(_sessionPath);
                }
            }
            catch
            {
                // Ignore cleanup failures to avoid masking test results.
            }
        }
    }

    private sealed class TestNotificationService : INotificationService
    {
        public ObservableCollection<ToastNotification> Notifications { get; } = new();
        private readonly ObservableCollection<NotificationEntry> _history = new();
        public ReadOnlyObservableCollection<NotificationEntry> NotificationHistory { get; }
        public event EventHandler? HistoryChanged;

        public TestNotificationService()
        {
            NotificationHistory = new ReadOnlyObservableCollection<NotificationEntry>(_history);
        }

        public void ShowSuccess(string message, string? title = null, string? category = null) => AddNotification(ToastType.Success, message, title, category);
        public void ShowError(string message, string? title = null, string? category = null) => AddNotification(ToastType.Error, message, title, category);
        public void ShowWarning(string message, string? title = null, string? category = null) => AddNotification(ToastType.Warning, message, title, category);
        public void ShowInfo(string message, string? title = null, string? category = null) => AddNotification(ToastType.Info, message, title, category);

        public void Dismiss(ToastNotification notification) => Notifications.Remove(notification);
        public void DismissAll() => Notifications.Clear();
        public void MarkAsRead(Guid notificationId)
        {
            if (_history.FirstOrDefault(h => h.Id == notificationId) is { } entry)
            {
                entry.IsRead = true;
            }
        }

        public void MarkAllAsRead()
        {
            foreach (var entry in _history)
            {
                entry.IsRead = true;
            }
        }

        public void Remove(Guid notificationId)
        {
            if (_history.FirstOrDefault(h => h.Id == notificationId) is { } entry)
            {
                _history.Remove(entry);
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        public int UnreadCount => _history.Count(entry => !entry.IsRead);

        public void Reset()
        {
            Notifications.Clear();
            _history.Clear();
        }

        private void AddNotification(ToastType type, string message, string? title, string? category)
        {
            var toast = new ToastNotification
            {
                Type = type,
                Message = message,
                Title = title ?? type.ToString(),
                Category = category ?? "General"
            };

            Notifications.Insert(0, toast);

            var entry = new NotificationEntry
            {
                Id = toast.Id,
                Title = toast.Title,
                Message = toast.Message,
                Category = toast.Category,
                Type = toast.Type,
                Icon = toast.Icon,
                CreatedAt = toast.CreatedAt
            };

            _history.Insert(0, entry);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class StubMediator : IMediator
    {
        private readonly Func<GetEmailsQuery, CancellationToken, Task<ErrorOr<IReadOnlyList<EmailDto>>>> _handler;

        public StubMediator(Func<GetEmailsQuery, CancellationToken, Task<ErrorOr<IReadOnlyList<EmailDto>>>> handler)
        {
            _handler = handler;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            if (request is GetEmailsQuery query)
            {
                return (Task<TResponse>)(object)_handler(query, cancellationToken);
            }

            throw new NotSupportedException("StubMediator only supports GetEmailsQuery.");
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
        {
            if (request is GetEmailsQuery query)
            {
                return (Task<TResponse>)(object)_handler(query, cancellationToken);
            }

            throw new NotSupportedException("StubMediator only supports GetEmailsQuery.");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken) where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotSupportedException();
    }
}
