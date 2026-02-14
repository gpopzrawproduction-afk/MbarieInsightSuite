using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for NotificationCenterViewModel covering initialization, commands, and filtering.
/// Target: 20 tests for notification center functionality coverage
/// </summary>
public class NotificationCenterViewModelTests
{
    private readonly INotificationService _notificationService;

    public NotificationCenterViewModelTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        
        // Set up basic notification service with empty history
        var history = new ObservableCollection<NotificationEntry>();
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        _notificationService.UnreadCount.Returns(0);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Assert
        viewModel.Notifications.Should().NotBeNull();
        viewModel.FilteredNotifications.Should().NotBeNull();
        viewModel.CategoryFilters.Should().NotBeNull().And.ContainSingle(); // "All" option
        viewModel.SeverityFilters.Should().NotBeNull().And.HaveCount(5); // All + 4 severity types
        viewModel.SelectedCategory.Should().NotBeNull();
        viewModel.SelectedSeverity.Should().NotBeNull();
        viewModel.ShowUnreadOnly.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Assert
        viewModel.MarkAsReadCommand.Should().NotBeNull();
        viewModel.DismissCommand.Should().NotBeNull();
        viewModel.MarkAllAsReadCommand.Should().NotBeNull();
        viewModel.ClearCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnNullNotificationService()
    {
        // Act
        var act = () => new NotificationCenterViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_InitializesSeverityFilters()
    {
        // Act
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Assert
        viewModel.SeverityFilters.Should().HaveCount(5);
        viewModel.SeverityFilters[0].Label.Should().Be("All");
        viewModel.SeverityFilters[0].Value.Should().BeNull();
        viewModel.SeverityFilters[1].Label.Should().Be("Information");
        viewModel.SeverityFilters[1].Value.Should().Be(ToastType.Info);
        viewModel.SeverityFilters[2].Label.Should().Be("Success");
        viewModel.SeverityFilters[2].Value.Should().Be(ToastType.Success);
    }

    [Fact]
    public void Constructor_InitializesCategoryFiltersWithAllOption()
    {
        // Act
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Assert
        viewModel.CategoryFilters.Should().ContainSingle();
        viewModel.CategoryFilters[0].Label.Should().Be("All");
        viewModel.CategoryFilters[0].Value.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void SelectedCategory_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedCategory))
                propertyChangedCount++;
        };
        var newCategory = new NotificationFilterOption<string?>("Test", "Test");

        // Act
        viewModel.SelectedCategory = newCategory;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SelectedCategory.Should().Be(newCategory);
    }

    [Fact]
    public void SelectedSeverity_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedSeverity))
                propertyChangedCount++;
        };
        var newSeverity = new NotificationFilterOption<ToastType?>("Error", ToastType.Error);

        // Act
        viewModel.SelectedSeverity = newSeverity;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SelectedSeverity.Should().Be(newSeverity);
    }

    [Fact]
    public void ShowUnreadOnly_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ShowUnreadOnly))
                propertyChangedCount++;
        };

        // Act
        viewModel.ShowUnreadOnly = true;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.ShowUnreadOnly.Should().BeTrue();
    }

    [Fact]
    public void UnreadCount_ReturnsServiceUnreadCount()
    {
        // Arrange
        _notificationService.UnreadCount.Returns(5);
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        var count = viewModel.UnreadCount;

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public void HasNotifications_WhenFilteredListEmpty_ReturnsFalse()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        var hasNotifications = viewModel.HasNotifications;

        // Assert
        hasNotifications.Should().BeFalse();
    }

    [Fact]
    public void HasNoNotifications_WhenFilteredListEmpty_ReturnsTrue()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        var hasNoNotifications = viewModel.HasNoNotifications;

        // Assert
        hasNoNotifications.Should().BeTrue();
    }

    #endregion

    #region Command Tests

    [Fact]
    public void MarkAsReadCommand_WithValidEntry_CallsServiceMarkAsRead()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);
        var entryId = Guid.NewGuid();
        var entry = new NotificationEntry
        {
            Id = entryId,
            Title = "Test",
            Message = "Message",
            Type = ToastType.Info,
            CreatedAt = DateTime.Now,
            Category = "Category"
        };

        // Act
        viewModel.MarkAsReadCommand.Execute(entry).Subscribe();

        // Assert
        _notificationService.Received(1).MarkAsRead(entryId);
    }

    [Fact]
    public void MarkAsReadCommand_WithNullEntry_DoesNotCallService()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        viewModel.MarkAsReadCommand.Execute(null).Subscribe();

        // Assert
        _notificationService.DidNotReceive().MarkAsRead(Arg.Any<Guid>());
    }

    [Fact]
    public void DismissCommand_WithValidEntry_CallsServiceRemove()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);
        var entryId = Guid.NewGuid();
        var entry = new NotificationEntry
        {
            Id = entryId,
            Title = "Test",
            Message = "Message",
            Type = ToastType.Info,
            CreatedAt = DateTime.Now,
            Category = "Category"
        };

        // Act
        viewModel.DismissCommand.Execute(entry).Subscribe();

        // Assert
        _notificationService.Received(1).Remove(entryId);
    }

    [Fact]
    public void DismissCommand_WithNullEntry_DoesNotCallService()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        viewModel.DismissCommand.Execute(null).Subscribe();

        // Assert
        _notificationService.DidNotReceive().Remove(Arg.Any<Guid>());
    }

    [Fact]
    public void MarkAllAsReadCommand_CallsServiceMarkAllAsRead()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        viewModel.MarkAllAsReadCommand.Execute().Subscribe();

        // Assert
        _notificationService.Received(1).MarkAllAsRead();
    }

    [Fact]
    public void ClearCommand_CallsServiceClearHistory()
    {
        // Arrange
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        viewModel.ClearCommand.Execute().Subscribe();

        // Assert
        _notificationService.Received(1).ClearHistory();
    }

    #endregion

    #region Filter Tests

    [Fact]
    public void FilteredNotifications_WithNoFilters_ShowsAllNotifications()
    {
        // Arrange
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "Title1", Message = "Message1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "Title2", Message = "Message2", Type = ToastType.Success, CreatedAt = DateTime.Now, Category = "Cat2", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "Title3", Message = "Message3", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Cat1", IsRead = true }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));

        // Act
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Assert
        viewModel.FilteredNotifications.Should().HaveCount(3);
    }

    [Fact]
    public void FilteredNotifications_WithShowUnreadOnly_FiltersReadNotifications()
    {
        // Arrange
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "Title1", Message = "Message1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "Title2", Message = "Message2", Type = ToastType.Success, CreatedAt = DateTime.Now, Category = "Cat2", IsRead = true },
            new() { Id = Guid.NewGuid(), Title = "Title3", Message = "Message3", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Cat1", IsRead = false }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Act
        viewModel.ShowUnreadOnly = true;

        // Assert - wait a bit for throttle
        System.Threading.Thread.Sleep(200);
        viewModel.FilteredNotifications.Should().HaveCount(2);
        viewModel.FilteredNotifications.Should().NotContain(n => n.IsRead);
    }

    [Fact]
    public void FilteredNotifications_WithCategoryFilter_FiltersByCategory()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Security" },
            new() { Id = Guid.NewGuid(), Title = "T2", Message = "M2", Type = ToastType.Success, CreatedAt = DateTime.Now, Category = "Email" },
            new() { Id = Guid.NewGuid(), Title = "T3", Message = "M3", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Security" }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Select "Security" category filter
        var securityFilter = viewModel.CategoryFilters.FirstOrDefault(c => c.Value == "Security");
        securityFilter.Should().NotBeNull();
        viewModel.SelectedCategory = securityFilter!;

        System.Threading.Thread.Sleep(200);
        viewModel.FilteredNotifications.Should().HaveCount(2);
        viewModel.FilteredNotifications.Should().OnlyContain(n => n.Category == "Security");
    }

    [Fact]
    public void FilteredNotifications_WithSeverityFilter_FiltersBySeverity()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1" },
            new() { Id = Guid.NewGuid(), Title = "T2", Message = "M2", Type = ToastType.Error, CreatedAt = DateTime.Now, Category = "Cat2" },
            new() { Id = Guid.NewGuid(), Title = "T3", Message = "M3", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1" }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        var errorFilter = viewModel.SeverityFilters.FirstOrDefault(s => s.Value == ToastType.Error);
        errorFilter.Should().NotBeNull();
        viewModel.SelectedSeverity = errorFilter!;

        System.Threading.Thread.Sleep(200);
        viewModel.FilteredNotifications.Should().HaveCount(1);
        viewModel.FilteredNotifications[0].Type.Should().Be(ToastType.Error);
    }

    [Fact]
    public void FilteredNotifications_CombinedFilters_AppliesBoth()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Security", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "T2", Message = "M2", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Email", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "T3", Message = "M3", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Security", IsRead = false },
            new() { Id = Guid.NewGuid(), Title = "T4", Message = "M4", Type = ToastType.Warning, CreatedAt = DateTime.Now, Category = "Security", IsRead = true }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        var securityFilter = viewModel.CategoryFilters.FirstOrDefault(c => c.Value == "Security");
        var warningFilter = viewModel.SeverityFilters.FirstOrDefault(s => s.Value == ToastType.Warning);
        viewModel.SelectedCategory = securityFilter!;
        viewModel.SelectedSeverity = warningFilter!;
        viewModel.ShowUnreadOnly = true;

        System.Threading.Thread.Sleep(200);
        viewModel.FilteredNotifications.Should().HaveCount(1);
        viewModel.FilteredNotifications[0].Title.Should().Be("T1");
    }

    [Fact]
    public void HasNotifications_WhenEntriesExist_ReturnsTrue()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1" }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        viewModel.HasNotifications.Should().BeTrue();
        viewModel.HasNoNotifications.Should().BeFalse();
    }

    [Fact]
    public void CategoryFilters_WithNotifications_IncludesDistinctCategories()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Alpha" },
            new() { Id = Guid.NewGuid(), Title = "T2", Message = "M2", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Beta" },
            new() { Id = Guid.NewGuid(), Title = "T3", Message = "M3", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Alpha" }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        viewModel.CategoryFilters.Should().HaveCount(3); // "All" + "Alpha" + "Beta"
        viewModel.CategoryFilters.Should().Contain(c => c.Label == "All");
        viewModel.CategoryFilters.Should().Contain(c => c.Value == "Alpha");
        viewModel.CategoryFilters.Should().Contain(c => c.Value == "Beta");
    }

    [Fact]
    public void SeverityFilters_OnlyBuiltOnce()
    {
        var history = new ObservableCollection<NotificationEntry>
        {
            new() { Id = Guid.NewGuid(), Title = "T1", Message = "M1", Type = ToastType.Info, CreatedAt = DateTime.Now, Category = "Cat1" }
        };
        _notificationService.NotificationHistory.Returns(new ReadOnlyObservableCollection<NotificationEntry>(history));
        var viewModel = new NotificationCenterViewModel(_notificationService);

        // Severity filters should always be 5 (All + 4 types)
        viewModel.SeverityFilters.Should().HaveCount(5);
    }

    #endregion
}
