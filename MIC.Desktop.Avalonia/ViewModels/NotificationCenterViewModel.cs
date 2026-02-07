using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// View model backing the notification center panel.
/// </summary>
public class NotificationCenterViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;
    private readonly CompositeDisposable _subscriptions = new();

    public ReadOnlyObservableCollection<NotificationEntry> Notifications { get; }
    public ObservableCollection<NotificationEntry> FilteredNotifications { get; } = new();
    public ObservableCollection<NotificationFilterOption<string?>> CategoryFilters { get; } = new();
    public ObservableCollection<NotificationFilterOption<ToastType?>> SeverityFilters { get; } = new();

    private NotificationFilterOption<string?> _selectedCategory;
    public NotificationFilterOption<string?> SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    private NotificationFilterOption<ToastType?> _selectedSeverity;
    public NotificationFilterOption<ToastType?> SelectedSeverity
    {
        get => _selectedSeverity;
        set => this.RaiseAndSetIfChanged(ref _selectedSeverity, value);
    }

    private bool _showUnreadOnly;
    public bool ShowUnreadOnly
    {
        get => _showUnreadOnly;
        set => this.RaiseAndSetIfChanged(ref _showUnreadOnly, value);
    }

    public ReactiveCommand<NotificationEntry?, Unit> MarkAsReadCommand { get; }
    public ReactiveCommand<NotificationEntry?, Unit> DismissCommand { get; }
    public ReactiveCommand<Unit, Unit> MarkAllAsReadCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    public NotificationCenterViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        Notifications = _notificationService.NotificationHistory;

        BuildFilterOptions();
        ApplyFilters();

        _notificationService.HistoryChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(UnreadCount));
            BuildFilterOptions();
            ApplyFilters();
        };

        _subscriptions.Add(
            this.WhenAnyValue(x => x.SelectedCategory, x => x.SelectedSeverity, x => x.ShowUnreadOnly)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => ApplyFilters())
        );

        MarkAsReadCommand = ReactiveCommand.Create<NotificationEntry?>(entry =>
        {
            if (entry is null)
            {
                return;
            }

            _notificationService.MarkAsRead(entry.Id);
        });

        DismissCommand = ReactiveCommand.Create<NotificationEntry?>(entry =>
        {
            if (entry is null)
            {
                return;
            }

            _notificationService.Remove(entry.Id);
        });

        MarkAllAsReadCommand = ReactiveCommand.Create(() => _notificationService.MarkAllAsRead());
        ClearCommand = ReactiveCommand.Create(() => _notificationService.ClearHistory());
    }

    public int UnreadCount => _notificationService.UnreadCount;
    public bool HasNotifications => FilteredNotifications.Any();
    public bool HasNoNotifications => !HasNotifications;

    private void BuildFilterOptions()
    {
        var categories = Notifications
            .Select(n => n.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var previousCategory = SelectedCategory?.Value;
        CategoryFilters.Clear();
        CategoryFilters.Add(new NotificationFilterOption<string?>("All", null));
        foreach (var category in categories)
        {
            CategoryFilters.Add(new NotificationFilterOption<string?>(category, category));
        }
        SelectedCategory = CategoryFilters.FirstOrDefault(option => option.Value == previousCategory)
                           ?? CategoryFilters.First();

        if (!SeverityFilters.Any())
        {
            SeverityFilters.Add(new NotificationFilterOption<ToastType?>("All", null));
            SeverityFilters.Add(new NotificationFilterOption<ToastType?>("Information", ToastType.Info));
            SeverityFilters.Add(new NotificationFilterOption<ToastType?>("Success", ToastType.Success));
            SeverityFilters.Add(new NotificationFilterOption<ToastType?>("Warning", ToastType.Warning));
            SeverityFilters.Add(new NotificationFilterOption<ToastType?>("Error", ToastType.Error));
            SelectedSeverity = SeverityFilters.First();
        }
    }

    private void ApplyFilters()
    {
        FilteredNotifications.Clear();

        var category = SelectedCategory?.Value;
        var severity = SelectedSeverity?.Value;
        var unreadOnly = ShowUnreadOnly;

        foreach (var notification in Notifications)
        {
            if (category != null && !string.Equals(notification.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (severity.HasValue && notification.Type != severity.Value)
            {
                continue;
            }

            if (unreadOnly && notification.IsRead)
            {
                continue;
            }

            FilteredNotifications.Add(notification);
        }

        this.RaisePropertyChanged(nameof(HasNotifications));
        this.RaisePropertyChanged(nameof(HasNoNotifications));
    }
}

public sealed record NotificationFilterOption<T>(string Label, T? Value);
