# PHASE 2 IMMEDIATE ACTION PLAN - Post Critical Fixes

## 🎯 Mission: 22.6% → 80% Test Coverage in 2 Weeks

**Status:** ✅ All critical blockers removed  
**Foundation:** Secure, stable, multilingual, OAuth-ready  
**Goal:** Systematic test expansion to achieve 80%+ coverage

---

## 📊 Coverage Analysis & Strategy

### Current Distribution (22.6% overall)
```
Core.Application:     77.9% ✅ (maintain)
Core.Domain:          53.7% ✅ (expand to 65%)
Desktop.Avalonia:     12.5% ⚠️ (expand to 70%) ← PRIMARY TARGET
Infrastructure.Data:  ~18%  ⚠️ (expand to 60%)
Infrastructure.Identity: ~20% ⚠️ (expand to 65%)
```

### Impact Potential
**Desktop.Avalonia expansion (12.5% → 70%):**
- ~300 classes/components in Desktop layer
- Adding 120 tests → +40 percentage points overall coverage
- **Biggest bang for buck**

**Infrastructure expansion (18% → 60%):**
- Repository tests
- Service integration tests
- OAuth flow tests (some already added)
- +15 percentage points overall

**Domain expansion (53.7% → 65%):**
- Entity validation tests
- Business logic tests
- Domain event tests
- +8 percentage points overall

**Total potential:** 22.6% + 40 + 15 + 8 = **85.6% coverage**

---

## WEEK 1: DESKTOP UI BLITZ (Days 1-7)

### Target: Desktop 12.5% → 70% (+57.5 points)

Your Desktop layer has the **lowest coverage** but **highest impact potential**.

### Day 1: NotificationCenterViewModel (Monday)

**File: `MIC.Tests.Unit/Desktop/ViewModels/NotificationCenterViewModelTests.cs`**

**Required Tests (20 tests minimum):**

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Core.Application.Notifications;

namespace MIC.Tests.Unit.Desktop.ViewModels;

public class NotificationCenterViewModelTests
{
    private readonly INotificationService _mockNotificationService;
    private readonly IUiDispatcher _mockDispatcher;
    private readonly NotificationCenterViewModel _sut;

    public NotificationCenterViewModelTests()
    {
        _mockNotificationService = Substitute.For<INotificationService>();
        _mockDispatcher = Substitute.For<IUiDispatcher>();
        
        // Configure immediate dispatcher for synchronous testing
        _mockDispatcher.InvokeAsync(Arg.Any<Action>())
            .Returns(ci => { ci.Arg<Action>()(); return Task.CompletedTask; });
        
        _sut = new NotificationCenterViewModel(_mockNotificationService, _mockDispatcher);
    }

    [Fact]
    public void Constructor_WithValidDependencies_InitializesProperties()
    {
        // Assert
        _sut.Should().NotBeNull();
        _sut.Notifications.Should().NotBeNull();
        _sut.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task InitializeAsync_LoadsNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Title = "Test 1", Message = "Message 1", IsRead = false },
            new() { Id = 2, Title = "Test 2", Message = "Message 2", IsRead = true }
        };
        
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(notifications);
        
        // Act
        await _sut.InitializeAsync();
        
        // Assert
        _sut.Notifications.Should().HaveCount(2);
        _sut.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyNotifications_HandlesGracefully()
    {
        // Arrange
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(new List<Notification>());
        
        // Act
        await _sut.InitializeAsync();
        
        // Assert
        _sut.Notifications.Should().BeEmpty();
        _sut.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceFails_HandlesError()
    {
        // Arrange
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(Task.FromException<List<Notification>>(new Exception("Service error")));
        
        // Act
        var act = () => _sut.InitializeAsync();
        
        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Service error");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithUnreadNotification_UpdatesNotification()
    {
        // Arrange
        var notification = new Notification { Id = 1, IsRead = false };
        _mockNotificationService.MarkAsReadAsync(1).Returns(Task.CompletedTask);
        
        // Act
        await _sut.MarkAsReadAsync(notification);
        
        // Assert
        notification.IsRead.Should().BeTrue();
        await _mockNotificationService.Received(1).MarkAsReadAsync(1);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithAlreadyReadNotification_DoesNothing()
    {
        // Arrange
        var notification = new Notification { Id = 1, IsRead = true };
        
        // Act
        await _sut.MarkAsReadAsync(notification);
        
        // Assert
        await _mockNotificationService.DidNotReceive().MarkAsReadAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNullNotification_ThrowsException()
    {
        // Act
        var act = () => _sut.MarkAsReadAsync(null);
        
        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MarkAllAsReadCommand_UpdatesAllUnreadNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, IsRead = false },
            new() { Id = 2, IsRead = false },
            new() { Id = 3, IsRead = true }
        };
        
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(notifications);
        await _sut.InitializeAsync();
        
        _mockNotificationService.MarkAllAsReadAsync().Returns(Task.CompletedTask);
        
        // Act
        if (_sut.MarkAllAsReadCommand.CanExecute(null))
            await _sut.MarkAllAsReadCommand.ExecuteAsync(null);
        
        // Assert
        await _mockNotificationService.Received(1).MarkAllAsReadAsync();
    }

    [Fact]
    public async Task ClearNotificationCommand_RemovesNotificationFromList()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Title = "Test" }
        };
        
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(notifications);
        await _sut.InitializeAsync();
        
        _mockNotificationService.DeleteNotificationAsync(1).Returns(Task.CompletedTask);
        
        var notification = _sut.Notifications[0];
        
        // Act
        if (_sut.ClearNotificationCommand.CanExecute(notification))
            await _sut.ClearNotificationCommand.ExecuteAsync(notification);
        
        // Assert
        _sut.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void FilterByType_Email_ShowsOnlyEmailNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Type = NotificationType.Email },
            new() { Id = 2, Type = NotificationType.Alert },
            new() { Id = 3, Type = NotificationType.Email }
        };
        
        // Act
        _sut.FilterByType(NotificationType.Email);
        
        // Assert
        var filtered = _sut.Notifications.Where(n => n.Type == NotificationType.Email);
        filtered.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(NotificationPriority.Low)]
    [InlineData(NotificationPriority.Normal)]
    [InlineData(NotificationPriority.High)]
    [InlineData(NotificationPriority.Urgent)]
    public void FilterByPriority_FiltersCorrectly(NotificationPriority priority)
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Priority = NotificationPriority.Low },
            new() { Id = 2, Priority = NotificationPriority.High },
            new() { Id = 3, Priority = priority }
        };
        
        // Act
        _sut.FilterByPriority(priority);
        
        // Assert
        var filtered = _sut.Notifications.Where(n => n.Priority == priority);
        filtered.Should().ContainSingle();
    }

    [Fact]
    public async Task RefreshCommand_ReloadsNotifications()
    {
        // Arrange
        var initialNotifications = new List<Notification>
        {
            new() { Id = 1, Title = "Old" }
        };
        
        var refreshedNotifications = new List<Notification>
        {
            new() { Id = 1, Title = "Old" },
            new() { Id = 2, Title = "New" }
        };
        
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(initialNotifications, refreshedNotifications);
        
        await _sut.InitializeAsync();
        
        // Act
        if (_sut.RefreshCommand.CanExecute(null))
            await _sut.RefreshCommand.ExecuteAsync(null);
        
        // Assert
        _sut.Notifications.Should().HaveCount(2);
    }

    [Fact]
    public void NotificationCount_ReturnsCorrectTotal()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1 },
            new() { Id = 2 },
            new() { Id = 3 }
        };
        
        // Assuming notifications are loaded
        _sut.Notifications.Clear();
        foreach (var n in notifications)
            _sut.Notifications.Add(n);
        
        // Assert
        _sut.NotificationCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchNotifications_WithQuery_FiltersResults()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Title = "Email from John", Message = "Meeting tomorrow" },
            new() { Id = 2, Title = "Alert: Server down", Message = "System offline" },
            new() { Id = 3, Title = "Email from Jane", Message = "Project update" }
        };
        
        _mockNotificationService.GetRecentNotificationsAsync(Arg.Any<int>())
            .Returns(notifications);
        await _sut.InitializeAsync();
        
        // Act
        _sut.SearchQuery = "Email";
        
        // Assert
        var filtered = _sut.Notifications.Where(n => n.Title.Contains("Email", StringComparison.OrdinalIgnoreCase));
        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void ClearFilters_RemovesAllFilters()
    {
        // Arrange
        _sut.FilterByType(NotificationType.Email);
        _sut.SearchQuery = "test";
        
        // Act
        _sut.ClearFilters();
        
        // Assert
        _sut.SearchQuery.Should().BeNullOrEmpty();
        _sut.Notifications.Should().NotBeFiltered();
    }

    [Fact]
    public async Task NotificationReceived_Event_AddsToList()
    {
        // Arrange
        var newNotification = new Notification
        {
            Id = 999,
            Title = "New notification",
            IsRead = false
        };
        
        // Simulate event
        _mockNotificationService.NotificationReceived += Raise.Event<EventHandler<Notification>>(this, newNotification);
        
        // Assert
        _sut.Notifications.Should().Contain(n => n.Id == 999);
        _sut.UnreadCount.Should().BeGreaterThan(0);
    }
}
```

**Day 1 Target:** 20 tests for NotificationCenterViewModel  
**Expected Coverage Increase:** +3-4 percentage points

---

### Day 2: EmailInboxViewModel (Tuesday)

**File: `MIC.Tests.Unit/Desktop/ViewModels/EmailInboxViewModelTests.cs`**

**Note:** You mentioned some ViewModel tests were disabled due to implementation mismatches. Let's fix those.

**Strategy:**
1. Review disabled tests
2. Update tests to match actual ViewModel implementation
3. Add missing tests for uncovered scenarios

**Required Tests (25 tests minimum):**
- Constructor initialization
- LoadEmails command
- RefreshEmails command
- MarkAsRead command
- ToggleFlag command
- Archive command
- Delete command
- SelectEmail command
- Search functionality
- Filter by folder
- Sort options
- Error handling
- Loading states
- Empty state handling
- Multi-select operations

**Day 2 Target:** 25 tests for EmailInboxViewModel  
**Expected Coverage Increase:** +4-5 percentage points

---

### Day 3: EmailDetailViewModel & EmailComposeViewModel (Wednesday)

**File: `MIC.Tests.Unit/Desktop/ViewModels/EmailDetailViewModelTests.cs`**

**Required Tests (20 tests):**

```csharp
public class EmailDetailViewModelTests
{
    [Fact]
    public async Task LoadEmail_WithValidId_DisplaysEmailDetails()
    {
        // Test email loading
    }

    [Fact]
    public async Task ReplyCommand_OpensReplyComposer()
    {
        // Test reply functionality
    }

    [Fact]
    public async Task ReplyAllCommand_IncludesAllRecipients()
    {
        // Test reply all
    }

    [Fact]
    public async Task ForwardCommand_OpensForwardComposer()
    {
        // Test forward
    }

    [Fact]
    public async Task DownloadAttachment_SavesFile()
    {
        // Test attachment download
    }

    [Fact]
    public async Task MarkAsReadCommand_UpdatesEmailStatus()
    {
        // Test mark as read
    }

    [Fact]
    public async Task ArchiveCommand_MovesEmailToArchive()
    {
        // Test archive
    }

    [Fact]
    public async Task DeleteCommand_ShowsConfirmation()
    {
        // Test delete with confirmation
    }

    // Add 12 more tests for edge cases
}
```

**File: `MIC.Tests.Unit/Desktop/ViewModels/EmailComposeViewModelTests.cs`**

**Required Tests (20 tests):**

```csharp
public class EmailComposeViewModelTests
{
    [Fact]
    public async Task SendCommand_WithValidData_SendsEmail()
    {
        // Test email sending
    }

    [Fact]
    public void ValidateRecipients_WithInvalidEmail_ShowsError()
    {
        // Test recipient validation
    }

    [Fact]
    public async Task SaveDraft_PreservesEmailContent()
    {
        // Test draft saving
    }

    [Fact]
    public async Task AddAttachment_AddsFileToList()
    {
        // Test attachment addition
    }

    [Fact]
    public void RemoveAttachment_RemovesFromList()
    {
        // Test attachment removal
    }

    // Add 15 more tests
}
```

**Day 3 Target:** 40 tests (20 + 20)  
**Expected Coverage Increase:** +5-6 percentage points

---

### Day 4: ChatViewModel & SettingsViewModel (Thursday)

**Required Tests (30 tests total):**

**ChatViewModel (15 tests):**
- Send message
- Load history
- Clear session
- Streaming responses
- Error handling
- Message validation
- Command states

**SettingsViewModel (15 tests):**
- Load settings
- Save settings
- Language selection (using your LocalizationService)
- Theme selection
- Account management
- Notification preferences
- Validation

**Day 4 Target:** 30 tests  
**Expected Coverage Increase:** +4-5 percentage points

---

### Day 5: Value Converters (Friday)

**All converters need comprehensive testing.**

**File: `MIC.Tests.Unit/Desktop/Converters/ConverterTests.cs`**

**Pattern for each converter:**

```csharp
public class BooleanToVisibilityConverterTests
{
    private readonly BooleanToVisibilityConverter _sut = new();

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Convert_BooleanValue_ReturnsCorrectVisibility(bool input, bool expected)
    {
        // Act
        var result = _sut.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);
        
        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_NullValue_ReturnsDefaultVisibility()
    {
        // Test null handling
    }

    [Fact]
    public void ConvertBack_Visibility_ReturnsBool()
    {
        // Test reverse conversion (you've implemented this!)
    }

    [Fact]
    public void Convert_InvalidType_ReturnsDefault()
    {
        // Test type validation
    }
}
```

**Converters to test (~15 converters × 4 tests = 60 tests):**
- BooleanToVisibilityConverter
- DateTimeToStringConverter
- EmailStatusToColorConverter
- PriorityToIconConverter
- AttachmentSizeConverter
- NullToVisibilityConverter
- InverseBooleanConverter
- StringEmptyToVisibilityConverter
- etc.

**Day 5 Target:** 60 tests for all converters  
**Expected Coverage Increase:** +8-10 percentage points

---

### Days 6-7: Desktop Services & Remaining Components (Weekend)

**Navigation Service Tests (10 tests):**
- Navigate to view model
- Go back
- Go forward
- Clear navigation stack
- Navigation with parameters
- Error handling

**Dialog Service Tests (10 tests):**
- Show message dialog
- Show confirmation dialog
- Show error dialog
- Show input dialog
- Modal results

**Other Desktop Components (20 tests):**
- Command implementations
- Helper classes
- Extension methods
- Utilities

**Days 6-7 Target:** 40 tests  
**Expected Coverage Increase:** +5-7 percentage points

---

### Week 1 Summary

**Total Tests Added:** ~230 tests  
**Coverage Increase:** +35-45 percentage points  
**Expected End State:** 22.6% + 40% = **62-67% overall coverage**  
**Desktop Layer:** 12.5% → **70%+** ✅

---

## WEEK 2: INFRASTRUCTURE & DOMAIN (Days 8-14)

### Days 8-10: Infrastructure Tests (Mon-Wed)

**Repository Tests (30 tests):**
- CRUD operations for all repositories
- Complex queries
- Transactions
- Error handling
- Concurrency

**Identity Service Tests (20 tests):**
- Authentication flows
- Token management
- Password hashing validation
- Session management
- OAuth integration tests (expand on what you've done)

**Data Layer Tests (20 tests):**
- DbContext operations
- Migrations
- Seed data
- Query optimization

**Days 8-10 Target:** 70 tests  
**Expected Coverage Increase:** +12-15 percentage points

---

### Days 11-12: Domain Tests (Thu-Fri)

**Entity Tests (30 tests):**
- Entity validation
- Business rules
- State transitions
- Domain events

**Value Object Tests (20 tests):**
- Value object equality
- Validation
- Immutability

**Days 11-12 Target:** 50 tests  
**Expected Coverage Increase:** +8-10 percentage points

---

### Days 13-14: Integration & E2E (Weekend)

**Integration Tests (20 tests):**
- End-to-end workflows
- Multi-service coordination
- Database integration
- OAuth flows (expand existing)

**E2E Scenarios (10 tests):**
- User registration → login → usage
- Email workflows
- Settings persistence
- Multilingual UI

**Days 13-14 Target:** 30 tests  
**Expected Coverage Increase:** +3-5 percentage points

---

### Week 2 Summary

**Total Tests Added:** ~150 tests  
**Coverage Increase:** +23-30 percentage points  
**Expected End State:** 62-67% + 25% = **87-92% overall coverage** ✅

---

## 📊 FINAL METRICS (After 2 Weeks)

| Metric | Before | After Week 1 | After Week 2 | Target |
|--------|--------|--------------|--------------|--------|
| **Total Tests** | 263 | ~490 | ~640 | 600+ |
| **Line Coverage** | 22.6% | ~62% | ~87% | 80% |
| **Desktop Coverage** | 12.5% | ~70% | ~75% | 70% |
| **Infrastructure** | ~18% | ~25% | ~60% | 60% |
| **Domain** | 53.7% | ~57% | ~68% | 65% |

---

## 🎯 DAILY EXECUTION CHECKLIST

### Each Day:

**Morning (9 AM - 12 PM):**
- [ ] Review day's test targets
- [ ] Create test file(s)
- [ ] Write 50% of day's tests
- [ ] Run tests continuously
- [ ] Fix any failures immediately

**Afternoon (1 PM - 5 PM):**
- [ ] Write remaining 50% of tests
- [ ] Run full test suite
- [ ] Collect coverage data
- [ ] Document progress
- [ ] Commit changes

**End of Day:**
```powershell
# Run full suite
dotnet test MIC.slnx

# Collect coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport"

# Check improvement
# Document in daily log
```

---

## 🚀 QUICK START - MONDAY MORNING

**Step 1: Create NotificationCenterViewModelTests.cs (30 min)**
```powershell
cd src\MIC.Tests.Unit\Desktop\ViewModels
New-Item -Name "NotificationCenterViewModelTests.cs" -ItemType File
```

**Step 2: Copy template from above (10 min)**

**Step 3: Implement tests (3 hours)**
- Start with basic tests (constructor, initialize)
- Add command tests (mark read, clear, refresh)
- Add filter/search tests
- Add edge case tests

**Step 4: Run and verify (30 min)**
```powershell
dotnet test --filter "FullyQualifiedName~NotificationCenterViewModelTests"
```

**Step 5: Measure impact (15 min)**
```powershell
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport"
# Check coverage increase
```

**Monday Target:** 20 tests, +3-4% coverage increase

---

## 📝 PROGRESS TRACKING TEMPLATE

Create: `PHASE_2_DAILY_LOG.md`

```markdown
# Phase 2 Daily Progress Log

## Week 1: Desktop UI Coverage

### Day 1 - [Date] - NotificationCenterViewModel
**Target:** 20 tests
**Actual:** [count]
**Coverage:** 22.6% → [measure]% (+[delta]%)
**Time:** [hours]
**Notes:** [any issues or observations]
**Status:** ✅ Complete / 🟡 Partial / ❌ Behind

### Day 2 - [Date] - EmailInboxViewModel
**Target:** 25 tests
**Actual:** [count]
**Coverage:** [previous]% → [measure]% (+[delta]%)
**Status:** [update]

[Continue for each day...]

## Weekly Summary

### Week 1 Total
- Tests Added: [total]
- Coverage Increase: [start]% → [end]% (+[delta]%)
- Desktop Layer: 12.5% → [end]%
- Status: ✅ On Track / 🟡 At Risk / ❌ Behind Schedule

### Week 2 Total
[Fill after week 2]

## Overall Phase 2
- Start: 263 tests, 22.6% coverage
- End: [final] tests, [final]% coverage
- Target Met: ✅ Yes / ❌ No
```

---

## 🎉 SUCCESS CRITERIA

After 2 weeks, you should have:

- [ ] 600+ total tests (from 263)
- [ ] 80%+ line coverage (from 22.6%)
- [ ] 70%+ Desktop coverage (from 12.5%)
- [ ] 60%+ Infrastructure coverage (from ~18%)
- [ ] 65%+ Domain coverage (from 53.7%)
- [ ] All tests passing
- [ ] No flaky tests
- [ ] Production-ready quality

---

**Start Monday morning with NotificationCenterViewModel tests. You've got this! 🚀**