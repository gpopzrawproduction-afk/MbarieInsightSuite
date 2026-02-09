using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

public sealed class NotificationCenterViewModelTests
{
    [Fact]
    public void Constructor_PopulatesFiltersAndNotifications()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        service.AddEntry("Ops", ToastType.Error, isRead: true);

        var vm = CreateViewModel(service);

        vm.FilteredNotifications.Should().HaveCount(2);
        vm.CategoryFilters.Select(option => option.Label).Should().Contain(new[] { "All", "General", "Ops" });
        vm.SeverityFilters.Select(option => option.Value).Should().Contain(new ToastType?[] { null, ToastType.Info, ToastType.Success, ToastType.Warning, ToastType.Error });
        vm.SelectedCategory.Label.Should().Be("All");
        vm.SelectedSeverity.Label.Should().Be("All");
        vm.HasNotifications.Should().BeTrue();
        vm.HasNoNotifications.Should().BeFalse();
    }

    [Fact]
    public void ShowUnreadOnly_WhenTrue_ExcludesReadEntries()
    {
        var service = new FakeNotificationService();
        var unread = service.AddEntry("General", ToastType.Info, isRead: false);
        service.AddEntry("Ops", ToastType.Warning, isRead: true);
        var vm = CreateViewModel(service);

        vm.ShowUnreadOnly = true;
        InvokeApplyFilters(vm);

        vm.FilteredNotifications.Should().ContainSingle().Which.Should().Be(unread);
    }

    [Fact]
    public void SelectedCategory_WhenChanged_FiltersNotifications()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var expected = service.AddEntry("Ops", ToastType.Warning, isRead: false);
        var vm = CreateViewModel(service);

        vm.SelectedCategory = vm.CategoryFilters.First(option => option.Value == "Ops");
        InvokeApplyFilters(vm);

        vm.FilteredNotifications.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public void SelectedSeverity_WhenChanged_FiltersNotifications()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var expected = service.AddEntry("Ops", ToastType.Error, isRead: false);
        var vm = CreateViewModel(service);

        vm.SelectedSeverity = vm.SeverityFilters.First(option => option.Value == ToastType.Error);
        InvokeApplyFilters(vm);

        vm.FilteredNotifications.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public async Task MarkAsReadCommand_WhenEntryProvided_DelegatesToService()
    {
        var service = new FakeNotificationService();
        var entry = service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.MarkAsReadCommand.Execute(entry).ToTask();

        service.MarkedAsRead.Should().Be(entry.Id);
        entry.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadCommand_WhenEntryNull_DoesNothing()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.MarkAsReadCommand.Execute(null).ToTask();

        service.MarkedAsRead.Should().BeNull();
        service.NotificationHistory.Should().ContainSingle(entry => !entry.IsRead);
    }

    [Fact]
    public async Task DismissCommand_WhenEntryProvided_RemovesEntry()
    {
        var service = new FakeNotificationService();
        var entry = service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.DismissCommand.Execute(entry).ToTask();

        service.RemovedEntries.Should().Contain(entry.Id);
        vm.FilteredNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task DismissCommand_WhenEntryNull_DoesNothing()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.DismissCommand.Execute(null).ToTask();

        service.RemovedEntries.Should().BeEmpty();
        vm.FilteredNotifications.Should().HaveCount(1);
    }

    [Fact]
    public async Task MarkAllAsReadCommand_DelegatesToService()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.MarkAllAsReadCommand.Execute().ToTask();

        service.MarkAllAsReadCount.Should().Be(1);
        service.NotificationHistory.Should().OnlyContain(entry => entry.IsRead);
    }

    [Fact]
    public async Task ClearCommand_DelegatesToService()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        await vm.ClearCommand.Execute().ToTask();

        service.ClearHistoryCount.Should().Be(1);
        vm.FilteredNotifications.Should().BeEmpty();
        vm.HasNotifications.Should().BeFalse();
        vm.HasNoNotifications.Should().BeTrue();
    }

    [Fact]
    public void HistoryChanged_RebuildsFiltersAndAppliesChanges()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var vm = CreateViewModel(service);

        var newEntry = service.AddEntry("Security", ToastType.Warning, isRead: false);
        service.RaiseHistoryChanged();

        vm.CategoryFilters.Select(option => option.Label).Should().Contain("Security");
        vm.UnreadCount.Should().Be(2);
        vm.FilteredNotifications.Should().Contain(newEntry);
    }

    [Fact]
    public void HistoryChanged_PreservesSelectedCategoryWhenAvailable()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        service.AddEntry("Ops", ToastType.Warning, isRead: false);
        var vm = CreateViewModel(service);

        vm.SelectedCategory = vm.CategoryFilters.First(option => option.Value == "Ops");
        service.AddEntry("Security", ToastType.Warning, isRead: false);
        service.RaiseHistoryChanged();

        vm.SelectedCategory.Value.Should().Be("Ops");
    }

    [Fact]
    public void ShowUnreadOnly_WhenDisabled_AllNotificationsVisible()
    {
        var service = new FakeNotificationService();
        service.AddEntry("General", ToastType.Info, isRead: false);
        var read = service.AddEntry("Ops", ToastType.Warning, isRead: true);
        var vm = CreateViewModel(service);

        vm.ShowUnreadOnly = true;
        InvokeApplyFilters(vm);
        vm.ShowUnreadOnly = false;
        InvokeApplyFilters(vm);

        vm.FilteredNotifications.Should().Contain(read);
    }

    private static NotificationCenterViewModel CreateViewModel(FakeNotificationService service)
        => new(service);

    private static void InvokeApplyFilters(NotificationCenterViewModel vm)
    {
        var method = typeof(NotificationCenterViewModel).GetMethod("ApplyFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(vm, null);
    }

    private sealed class FakeNotificationService : INotificationService
    {
        private readonly ObservableCollection<ToastNotification> _notifications = new();
        private readonly ObservableCollection<NotificationEntry> _history = new();

        public FakeNotificationService()
        {
            NotificationHistory = new ReadOnlyObservableCollection<NotificationEntry>(_history);
        }

        public ObservableCollection<ToastNotification> Notifications => _notifications;
        public ReadOnlyObservableCollection<NotificationEntry> NotificationHistory { get; }
        public event EventHandler? HistoryChanged;

        public Guid? MarkedAsRead { get; private set; }
        public int MarkAllAsReadCount { get; private set; }
        public int ClearHistoryCount { get; private set; }
        public List<Guid> RemovedEntries { get; } = new();

        public void ShowSuccess(string message, string? title = null, string? category = null) => AddToast(ToastType.Success, message, title, category);
        public void ShowError(string message, string? title = null, string? category = null) => AddToast(ToastType.Error, message, title, category);
        public void ShowWarning(string message, string? title = null, string? category = null) => AddToast(ToastType.Warning, message, title, category);
        public void ShowInfo(string message, string? title = null, string? category = null) => AddToast(ToastType.Info, message, title, category);

        public void Dismiss(ToastNotification notification)
        {
            if (_notifications.Contains(notification))
            {
                _notifications.Remove(notification);
            }
        }

        public void DismissAll() => _notifications.Clear();

        public void MarkAsRead(Guid notificationId)
        {
            MarkedAsRead = notificationId;
            if (_history.FirstOrDefault(entry => entry.Id == notificationId) is { } entry)
            {
                entry.IsRead = true;
            }
        }

        public void MarkAllAsRead()
        {
            MarkAllAsReadCount++;
            foreach (var entry in _history)
            {
                entry.IsRead = true;
            }
        }

        public void Remove(Guid notificationId)
        {
            if (_history.FirstOrDefault(entry => entry.Id == notificationId) is { } entry)
            {
                _history.Remove(entry);
                RemovedEntries.Add(notificationId);
                RaiseHistoryChanged();
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
            ClearHistoryCount++;
            RaiseHistoryChanged();
        }

        public int UnreadCount => _history.Count(entry => !entry.IsRead);

        public NotificationEntry AddEntry(string category, ToastType severity, bool isRead)
        {
            var entry = new NotificationEntry
            {
                Id = Guid.NewGuid(),
                Title = category,
                Message = $"{category} message",
                Category = category,
                Type = severity,
                Icon = "ℹ",
                CreatedAt = DateTime.UtcNow,
                IsRead = isRead
            };

            _history.Add(entry);
            return entry;
        }

        public void RaiseHistoryChanged() => HistoryChanged?.Invoke(this, EventArgs.Empty);

        private void AddToast(ToastType type, string message, string? title, string? category)
        {
            _notifications.Add(new ToastNotification
            {
                Type = type,
                Message = message,
                Title = title ?? string.Empty,
                Category = category ?? string.Empty
            });
        }
    }
}
