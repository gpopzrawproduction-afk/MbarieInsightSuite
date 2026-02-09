# PHASE 2: COVERAGE COMPLETION & PRODUCTION READINESS

## 🎯 Mission: Desktop UI Coverage + Release Infrastructure

**Current State:** ✅ Core/Application layer excellent (77.9%), Desktop layer needs work (12.5%)  
**Phase 2 Goal:** 🚀 Desktop coverage to 70%+, overall to 80%+, production-ready release

---

## 📊 Phase 1 Achievement Analysis

### Coverage by Assembly (Current State)

| Assembly | Line Coverage | Branch Coverage | Status | Phase 2 Target |
|----------|--------------|-----------------|--------|----------------|
| **MIC.Core.Application** | 77.9% | ~70% | ✅ Excellent | Maintain 75%+ |
| **MIC.Core.Domain** | 53.7% | ~48% | ✅ Good | Increase to 65%+ |
| **MIC.Desktop.Avalonia** | 12.5% | ~10% | ⚠️ Critical Gap | **Target: 70%+** |
| **MIC.Infrastructure.Data** | ~15% | ~12% | 🟡 Needs Work | Target: 60%+ |
| **MIC.Infrastructure.Identity** | ~20% | ~15% | 🟡 Needs Work | Target: 65%+ |
| **Overall Combined** | 22.6% | 20.3% | 🟡 Progress | **Target: 80%+** |

### Critical Gaps Identified

**High Priority (Must Fix):**
1. ❌ **NotificationCenterViewModel** (0% coverage)
2. ❌ **Desktop ViewModels** (Email, Chat flows)
3. ❌ **View Converters** (Avalonia value converters)
4. ❌ **Intelligence Services** (prediction scenarios limited)
 
**Medium Priority:**
5. 🟡 **Infrastructure layers** (Data, Identity)
6. 🟡 **Domain model validation**
7. 🟡 **Error handling paths**

---

## 📋 PHASE 2 WORK PACKAGES (3-4 Weeks)

## WP-2.1: Desktop UI Coverage (Week 1) - CRITICAL PRIORITY

### Goal: Desktop.Avalonia from 12.5% to 70%+

Your analysis correctly identified: *"MIC.Desktop.Avalonia still heavily uncovered (view models, converters)"*

#### Task 2.1.1: NotificationCenterViewModel Tests (Days 1-2)

**File: `MIC.Tests.Unit/Desktop/ViewModels/NotificationCenterViewModelTests.cs`**
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
        _mockDispatcher = new ImmediateUiDispatcher(); // Use your immediate dispatcher
        _sut = new NotificationCenterViewModel(_mockNotificationService, _mockDispatcher);
    }

    [Fact]
    public async Task Initialize_LoadsNotifications()
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
    public async Task MarkAsRead_UpdatesNotificationAndCount()
    {
        // Arrange
        var notification = new Notification { Id = 1, IsRead = false };
        _mockNotificationService.MarkAsReadAsync(1).Returns(Task.CompletedTask);
        
        // Act
        await _sut.MarkAsReadAsync(notification);
        
        // Assert
        await _mockNotificationService.Received(1).MarkAsReadAsync(1);
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAllNotifications()
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
        await _sut.MarkAllAsReadCommand.ExecuteAsync(null);
        
        // Assert
        await _mockNotificationService.Received(1).MarkAllAsReadAsync();
        _sut.Notifications.All(n => n.IsRead).Should().BeTrue();
        _sut.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task ClearNotification_RemovesFromList()
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
        
        // Act
        await _sut.ClearNotificationCommand.ExecuteAsync(notifications[0]);
        
        // Assert
        _sut.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void FilterByType_FiltersNotifications()
    {
        // Test notification filtering by type
    }

    [Fact]
    public async Task RefreshCommand_ReloadsNotifications()
    {
        // Test refresh functionality
    }

    [Theory]
    [InlineData(NotificationPriority.Low)]
    [InlineData(NotificationPriority.Normal)]
    [InlineData(NotificationPriority.High)]
    [InlineData(NotificationPriority.Urgent)]
    public void FilterByPriority_ShowsCorrectNotifications(NotificationPriority priority)
    {
        // Test priority-based filtering
    }
}
```

**Deliverable:** NotificationCenterViewModel covered to 80%+

#### Task 2.1.2: Email ViewModel Tests (Days 3-4)

**File: `MIC.Tests.Unit/Desktop/ViewModels/EmailDetailViewModelTests.cs`**
```csharp
public class EmailDetailViewModelTests
{
    private readonly IEmailService _mockEmailService;
    private readonly INotificationService _mockNotificationService;
    private readonly IUiDispatcher _mockDispatcher;
    private readonly EmailDetailViewModel _sut;

    public EmailDetailViewModelTests()
    {
        _mockEmailService = Substitute.For<IEmailService>();
        _mockNotificationService = Substitute.For<INotificationService>();
        _mockDispatcher = new ImmediateUiDispatcher();
        _sut = new EmailDetailViewModel(_mockEmailService, _mockNotificationService, _mockDispatcher);
    }

    [Fact]
    public async Task LoadEmail_DisplaysEmailDetails()
    {
        // Arrange
        var email = new Email
        {
            Id = 1,
            Subject = "Test Subject",
            From = "sender@example.com",
            To = "recipient@example.com",
            Body = "Test body",
            ReceivedDate = DateTime.UtcNow
        };
        
        _mockEmailService.GetEmailByIdAsync(1).Returns(email);
        
        // Act
        await _sut.LoadEmailAsync(1);
        
        // Assert
        _sut.Subject.Should().Be("Test Subject");
        _sut.From.Should().Be("sender@example.com");
        _sut.Body.Should().Be("Test body");
    }

    [Fact]
    public async Task ReplyCommand_OpensReplyComposer()
    {
        // Test reply functionality
    }

    [Fact]
    public async Task ForwardCommand_OpensForwardComposer()
    {
        // Test forward functionality
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
    public async Task DeleteCommand_DeletesEmail()
    {
        // Test delete with confirmation
    }

    [Fact]
    public async Task DownloadAttachment_SavesFile()
    {
        // Test attachment download
    }
}
```

**Also create:**
- `EmailComposeViewModelTests.cs` (compose, send, draft)
- `EmailListViewModelTests.cs` (list, filter, search)
- `EmailFolderViewModelTests.cs` (folder navigation)

**Deliverable:** Email ViewModels covered to 75%+

**WP-2.1 Summary:**
- Add ~80 unit tests for Desktop layer
- Cover ViewModels: Notification, Email, Chat, Settings, Dashboard
- Cover Converters: All value converters
- Cover Services: Navigation, Dialogs, UI utilities
- **Target:** Desktop.Avalonia from 12.5% to 70%+ coverage

---

## 📊 Phase 2 Success Metrics

| Metric | Current | Target | Priority |
|--------|---------|--------|----------|
| **Desktop.Avalonia** | 12.5% | **70%+** | 🔴 Critical |
| **Core.Application** | 77.9% | 75%+ | ✅ Maintain |
| **Core.Domain** | 53.7% | 65%+ | 🟡 Important |
| **Infrastructure** | ~15-20% | 60%+ | 🟡 Important |
| **Overall** | 22.6% | **80%+** | 🔴 Critical |
| **Total Tests** | 163+ | 300+ | 🟡 Target |

---

## 🎯 Week-by-Week Execution Plan

### Week 1: Desktop UI Blitz
**Days 1-2:** NotificationCenterViewModel (80+ lines, 20 tests)  
**Days 3-4:** Email ViewModels (150+ lines, 30 tests)  
**Day 5:** Chat ViewModels (80+ lines, 15 tests)  
**Day 6:** Value Converters (100+ lines, 20 tests)  
**Day 7:** Desktop Services (50+ lines, 10 tests)  
**Target:** Desktop 12.5% → 70%+ (95 tests added)

### Week 2: Intelligence & Infrastructure
**Days 1-3:** Intelligence/Prediction Services (60 tests)  
**Days 4-5:** Infrastructure.Data expansion (40 tests)  
**Target:** Infrastructure to 60%+, Intelligence to 60%+

### Week 3: Domain & CI
**Days 1-3:** Domain model validation (50 tests)  
**Days 4-5:** HTML coverage CI setup  
**Target:** Domain to 65%+, automated reports

### Week 4: Release Prep
**Days 1-3:** Release scripts, MSIX packaging  
**Days 4-5:** Final validation, documentation  
**Target:** Production-ready release

---

## 🚀 Quick Start - Monday Tasks

**Morning (3 hours):**
```powershell
# 1. Create NotificationCenterViewModelTests.cs
# 2. Write 10 tests for notification operations
# 3. Run tests: dotnet test MIC.Tests.Unit
# 4. Run coverage: dotnet test --collect:"XPlat Code Coverage"
# 5. Measure improvement
```

**Afternoon (3 hours):**
```powershell
# 1. Complete remaining 10 notification tests
# 2. Start EmailDetailViewModelTests.cs
# 3. Write 5 basic email detail tests
# 4. Run full suite
# 5. Update PHASE_2_PROGRESS.md
```

**End of Day Target:**
- 25 new tests added
- Desktop coverage: 12.5% → 18-20%
- All tests passing

---

**You've got this! Start with NotificationCenterViewModel tests today and build momentum! 🚀**