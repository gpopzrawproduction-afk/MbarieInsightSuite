# REVISED EMERGENCY RECOVERY PLAN - Based on Actual Project Scan

## 🎯 Current Reality (Validated)

**Baseline:** 19.3% coverage, 353 tests passing, 6.8s execution  
**Target:** 80% coverage  
**Gap:** 60.7 percentage points  
**Tests Needed:** ~800-900 additional tests  
**Timeline:** 3 weeks (revised from 4)

---

## 📊 ACCURATE COVERAGE BREAKDOWN

### Layer-by-Layer Analysis

| Layer | Current | Target | Gap | Impact | Priority |
|-------|---------|--------|-----|--------|----------|
| **Desktop.Avalonia** | 7.7% | 70% | 62.3% | +40 pts | 🔴 CRITICAL |
| **Infrastructure.AI** | 0% | 60% | 60% | +10 pts | 🔴 CRITICAL |
| **Infrastructure.Monitoring** | 0% | 60% | 60% | +7 pts | 🔴 HIGH |
| **Infrastructure.Data** | 20.6% | 60% | 39.4% | +8 pts | 🟡 MEDIUM |
| **Infrastructure.Identity** | 39.5% | 65% | 25.5% | +5 pts | 🟡 MEDIUM |
| **Core.Domain** | 55.2% | 68% | 12.8% | +4 pts | 🟢 LOW |
| **Core.Application** | 73.7% | 75% | 1.3% | +1 pt | ✅ MAINTAIN |

**Total Potential:** 19.3% + 75 points = **94.3%** ✅

---

## 🚀 3-WEEK RECOVERY TIMELINE

### WEEK 1 REMAINING (Days 5-7) - Desktop UI Focus

**You're on Day 4 of Week 1**. Continue momentum!

#### Day 5 (Friday): Desktop Converters & Helpers

**You have 21 new ViewModel test suites**. Now add converter tests.

**File: `MIC.Tests.Unit/Desktop/Converters/AllConverterTests.cs`**

```csharp
using Xunit;
using FluentAssertions;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MIC.Tests.Unit.Desktop.Converters;

public class BooleanConverterTests
{
    private readonly BooleanToVisibilityConverter _visibilityConverter = new();
    private readonly InverseBooleanConverter _inverseConverter = new();

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void BooleanToVisibility_Convert_ReturnsCorrectVisibility(bool input, bool expected)
    {
        var result = _visibilityConverter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void BooleanToVisibility_ConvertBack_HandlesGracefully()
    {
        var result = _visibilityConverter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InverseBoolean_Convert_InvertsValue(bool input, bool expected)
    {
        var result = _inverseConverter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void InverseBoolean_ConvertBack_InvertsBack()
    {
        var result = _inverseConverter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }
}

public class DateTimeConverterTests
{
    private readonly DateTimeToStringConverter _dateConverter = new();
    private readonly TimeSpanToStringConverter _timeConverter = new();

    [Fact]
    public void DateTimeToString_ValidDateTime_FormatsCorrectly()
    {
        var date = new DateTime(2026, 2, 10, 14, 30, 0);
        var result = _dateConverter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().NotBeNull();
    }

    [Fact]
    public void DateTimeToString_NullValue_ReturnsEmptyString()
    {
        var result = _dateConverter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void TimeSpanToString_ValidTimeSpan_FormatsCorrectly()
    {
        var timeSpan = TimeSpan.FromMinutes(90);
        var result = _timeConverter.Convert(timeSpan, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Contain("1h 30m");
    }
}

public class NumericConverterTests
{
    private readonly ByteSizeConverter _sizeConverter = new();
    private readonly PercentageConverter _percentConverter = new();

    [Theory]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void ByteSize_Convert_FormatsCorrectly(long bytes, string expected)
    {
        var result = _sizeConverter.Convert(bytes, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.5, "50%")]
    [InlineData(0.753, "75.3%")]
    [InlineData(1.0, "100%")]
    public void Percentage_Convert_FormatsCorrectly(double value, string expected)
    {
        var result = _percentConverter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }
}

public class StatusConverterTests
{
    private readonly StatusToColorConverter _colorConverter = new();
    private readonly StatusToIconConverter _iconConverter = new();

    [Theory]
    [InlineData(AlertSeverity.Low, "#4CAF50")]      // Green
    [InlineData(AlertSeverity.Medium, "#FF9800")]   // Orange
    [InlineData(AlertSeverity.High, "#F44336")]     // Red
    [InlineData(AlertSeverity.Critical, "#9C27B0")] // Purple
    public void StatusToColor_Convert_ReturnsCorrectColor(AlertSeverity severity, string expectedColor)
    {
        var result = _colorConverter.Convert(severity, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData(AlertSeverity.Low, "✓")]
    [InlineData(AlertSeverity.High, "⚠")]
    [InlineData(AlertSeverity.Critical, "❌")]
    public void StatusToIcon_Convert_ReturnsCorrectIcon(AlertSeverity severity, string expectedIcon)
    {
        var result = _iconConverter.Convert(severity, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Contain(expectedIcon);
    }
}

// Continue for ALL converters found in project scan
```

**Converters to Test (Based on Avalonia Project Scan):**
- BooleanToVisibilityConverter (4 tests)
- InverseBooleanConverter (3 tests)
- NullToVisibilityConverter (4 tests)
- DateTimeToStringConverter (5 tests)
- TimeSpanToStringConverter (4 tests)
- ByteSizeConverter (6 tests)
- PercentageConverter (4 tests)
- StatusToColorConverter (5 tests)
- StatusToIconConverter (5 tests)
- EmailStatusConverter (4 tests)
- PriorityConverter (4 tests)
- CollectionToVisibilityConverter (3 tests)
- StringToVisibilityConverter (3 tests)
- NumberToVisibilityConverter (3 tests)

**Day 5 Target:** 60 converter tests  
**Expected Coverage:** 19.3% → 23% (+3.7%)

---

#### Days 6-7 (Weekend): Desktop Services + Navigation

**File: `MIC.Tests.Unit/Desktop/Services/NavigationServiceExpandedTests.cs`**

```csharp
public class NavigationServiceExpandedTests
{
    private readonly INavigationService _sut;
    private readonly IServiceProvider _mockServiceProvider;

    public NavigationServiceExpandedTests()
    {
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _sut = new NavigationService(_mockServiceProvider);
    }

    [Fact]
    public async Task NavigateTo_ValidViewModel_NavigatesSuccessfully()
    {
        // Arrange
        var viewModel = new DashboardViewModel();
        _mockServiceProvider.GetService(typeof(DashboardViewModel))
            .Returns(viewModel);

        // Act
        await _sut.NavigateToAsync<DashboardViewModel>();

        // Assert
        _sut.CurrentViewModel.Should().Be(viewModel);
    }

    [Fact]
    public async Task NavigateTo_WithParameters_PassesParameters()
    {
        // Test parameter passing
    }

    [Fact]
    public async Task GoBack_WithHistory_NavigatesBack()
    {
        // Test back navigation
    }

    [Fact]
    public async Task GoBack_WithoutHistory_DoesNothing()
    {
        // Test edge case
    }

    [Fact]
    public void CanGoBack_WithHistory_ReturnsTrue()
    {
        // Test history check
    }

    [Fact]
    public void CanGoBack_WithoutHistory_ReturnsFalse()
    {
        // Test edge case
    }

    [Fact]
    public async Task ClearNavigationStack_RemovesHistory()
    {
        // Test stack clearing
    }

    [Fact]
    public async Task NavigateTo_InvalidViewModel_ThrowsException()
    {
        // Test error handling
    }

    [Fact]
    public void NavigationHistory_MaintainsCorrectOrder()
    {
        // Test history order
    }

    [Fact]
    public async Task NavigateTo_CancellationRequested_CancelsNavigation()
    {
        // Test cancellation
    }
}
```

**Desktop Services to Test:**
- NavigationService (10 tests) - expand existing
- DialogService (8 tests)
- NotificationService (10 tests) - expand
- ThemeService (6 tests)
- LocalizationService (8 tests) - you have some, expand
- KeyboardShortcutService (6 tests) - expand existing
- ExportService (8 tests) - expand existing
- SettingsService (8 tests) - expand existing

**Days 6-7 Target:** 64 desktop service tests  
**Expected Coverage:** 23% → 27% (+4%)

---

### WEEK 1 SUMMARY (Days 1-7)

**Tests at Week Start:** 353  
**Tests Added Week 1:**
- Days 1-4 (already done): +128 tests ✅
- Day 5: +60 tests (converters)
- Days 6-7: +64 tests (services)
**Week 1 Total:** 353 + 128 + 60 + 64 = **605 tests**  
**Week 1 Coverage:** 19.3% → **~32%** (+12.7 points) ✅

---

## WEEK 2: INFRASTRUCTURE BLITZ (Days 8-14)

### Goal: 32% → 55% Coverage

#### Days 8-9: Infrastructure.AI Testing (CRITICAL - Currently 0%)

**The AI layer is COMPLETELY UNTESTED.**

**File: `MIC.Tests.Unit/Infrastructure/AI/ChatServiceTests.cs`**

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.SemanticKernel;
using MIC.Infrastructure.AI.Services;

namespace MIC.Tests.Unit.Infrastructure.AI;

public class ChatServiceTests
{
    private readonly IKernel _mockKernel;
    private readonly IOpenAIService _mockOpenAI;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _mockKernel = Substitute.For<IKernel>();
        _mockOpenAI = Substitute.For<IOpenAIService>();
        _sut = new ChatService(_mockKernel, _mockOpenAI);
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ReturnsResponse()
    {
        // Arrange
        var message = "Hello, AI!";
        var expectedResponse = "Hello! How can I help you?";
        
        _mockOpenAI.GetCompletionAsync(message)
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SendMessageAsync(message);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyMessage_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.SendMessageAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendMessageAsync_WithContext_UsesContext()
    {
        // Test context passing
    }

    [Fact]
    public async Task SendMessageAsync_APIFailure_HandlesGracefully()
    {
        // Test error handling
    }

    [Fact]
    public async Task GetChatHistoryAsync_ReturnsHistory()
    {
        // Test history retrieval
    }

    [Fact]
    public async Task ClearChatHistoryAsync_ClearsHistory()
    {
        // Test history clearing
    }

    [Fact]
    public async Task StreamResponseAsync_StreamsInChunks()
    {
        // Test streaming responses
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task SendMessageAsync_WithMaxTokens_RespectsLimit(int maxTokens)
    {
        // Test token limits
    }

    [Fact]
    public async Task SendMessageAsync_WithPlugins_UsesPlugins()
    {
        // Test plugin integration
    }

    [Fact]
    public async Task SendMessageAsync_RateLimited_RetriesWithBackoff()
    {
        // Test rate limiting
    }
}

public class InsightGeneratorServiceTests
{
    [Fact]
    public async Task GenerateEmailInsightsAsync_ValidEmails_ReturnsInsights()
    {
        // Test insight generation
    }

    [Fact]
    public async Task GenerateAlertInsightsAsync_ValidAlerts_ReturnsInsights()
    {
        // Test alert insights
    }

    [Fact]
    public async Task GeneratePredictionsAsync_ValidData_ReturnsPredictions()
    {
        // Test predictions
    }

    // Add 7 more tests
}

public class AlertsPluginTests
{
    [Fact]
    public async Task GetRecentAlerts_ReturnsAlerts()
    {
        // Test alert plugin
    }

    // Add 5 more tests
}

public class MetricsPluginTests
{
    [Fact]
    public async Task GetMetrics_ReturnsMetrics()
    {
        // Test metrics plugin
    }

    // Add 5 more tests
}
```

**AI Services to Test:**
- ChatService (10 tests)
- InsightGeneratorService (10 tests)
- AlertsPlugin (6 tests)
- MetricsPlugin (6 tests)
- EmailAnalysisService (8 tests)
- PredictionService (8 tests)
- SemanticKernelConfiguration (6 tests)

**Days 8-9 Target:** 54 AI tests (0% → 60%)  
**Expected Coverage Impact:** +10 points → 32% → **42%**

---

#### Days 10-11: Infrastructure.Monitoring Testing (Currently 0%)

**File: `MIC.Tests.Unit/Infrastructure/Monitoring/TelemetryServiceTests.cs`**

```csharp
public class TelemetryServiceTests
{
    [Fact]
    public void TrackEvent_ValidEvent_LogsEvent()
    {
        // Test event tracking
    }

    [Fact]
    public void TrackException_ValidException_LogsException()
    {
        // Test exception tracking
    }

    [Fact]
    public void TrackMetric_ValidMetric_LogsMetric()
    {
        // Test metric tracking
    }

    // Add 7 more tests
}

public class PerformanceMonitorTests
{
    [Fact]
    public void MeasureOperation_TimesCorrectly()
    {
        // Test performance measurement
    }

    // Add 7 more tests
}

public class HealthCheckServiceTests
{
    [Fact]
    public async Task CheckHealth_AllHealthy_ReturnsHealthy()
    {
        // Test health checks
    }

    // Add 7 more tests
}
```

**Monitoring Services to Test:**
- TelemetryService (10 tests)
- PerformanceMonitor (8 tests)
- HealthCheckService (8 tests)
- LoggingService (8 tests)
- DiagnosticService (6 tests)

**Days 10-11 Target:** 40 monitoring tests (0% → 60%)  
**Expected Coverage Impact:** +7 points → 42% → **49%**

---

#### Days 12-14: Infrastructure Completion

**Infrastructure.Data expansion (20.6% → 60%):**
- Complex query tests (15 tests)
- Transaction tests (10 tests)
- Migration tests (8 tests)

**Infrastructure.Identity expansion (39.5% → 65%):**
- OAuth edge cases (10 tests)
- Token management (8 tests)
- Permission tests (7 tests)

**Days 12-14 Target:** 58 infrastructure tests  
**Expected Coverage Impact:** +6 points → 49% → **55%**

---

### WEEK 2 SUMMARY (Days 8-14)

**Tests Added:**
- Days 8-9: +54 tests (AI)
- Days 10-11: +40 tests (Monitoring)
- Days 12-14: +58 tests (Infrastructure)
**Week 2 Total:** +152 tests  
**Week 2 Coverage:** 32% → **55%** (+23 points) ✅

---

## WEEK 3: FINAL PUSH (Days 15-21)

### Goal: 55% → 82% Coverage

#### Days 15-17: Integration & E2E Tests

**Currently: 14 integration tests, 0 E2E tests**

**Integration Tests (add 30):**
- Email workflows (10 tests)
- Alert workflows (8 tests)
- Authentication flows (6 tests)
- Settings persistence (6 tests)

**E2E Tests (add 15):**
- User registration → login → usage (5 scenarios)
- Email workflows (5 scenarios)
- Dashboard workflows (5 scenarios)

**Days 15-17 Target:** 45 integration/E2E tests  
**Expected Coverage:** +10 points → 55% → **65%**

---

#### Days 18-21: Gap Filling & Polish

**Systematic gap filling:**
- Domain layer: 55.2% → 70% (30 tests)
- Infrastructure.Data: 60% → 70% (20 tests)
- Desktop.Avalonia: 50% → 75% (40 tests)
- Edge cases across all layers (30 tests)

**Days 18-21 Target:** 120 tests  
**Expected Coverage:** +17 points → 65% → **82%** ✅

---

### WEEK 3 SUMMARY (Days 15-21)

**Tests Added:** 165 tests  
**Week 3 Coverage:** 55% → **82%** (+27 points) ✅

---

## 📊 FINAL PROJECTED STATE

**Starting Point:** 353 tests, 19.3% coverage  
**After 3 Weeks:**
- Week 1: +252 tests → 605 tests, 32% coverage
- Week 2: +152 tests → 757 tests, 55% coverage
- Week 3: +165 tests → **922 tests, 82% coverage** ✅

**Target Exceeded:** 82% > 80% ✅

---

## 🎯 DAILY EXECUTION CHECKLIST

### Each Day:

**Morning (2 hours):**
- [ ] Review day's target area
- [ ] Create test files
- [ ] Write 50% of day's tests
- [ ] Run tests continuously

**Afternoon (3 hours):**
- [ ] Complete remaining tests
- [ ] Run full test suite
- [ ] Collect coverage
- [ ] Document progress

**End of Day:**
```powershell
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport"
# Verify improvement
# Update daily log
# Commit changes
```

---

## 📝 PROGRESS TRACKING

**Create: `WEEK_2_PROGRESS.md`**

```markdown
# Week 2 Progress - Infrastructure Blitz

## Day 8 - [Date] - Infrastructure.AI (Part 1)
**Target:** 27 tests (ChatService, InsightGenerator)
**Actual:** [count]
**Coverage:** 32% → [measure]% (+[delta]%)
**Status:** ✅ / 🟡 / ❌

## Day 9 - [Date] - Infrastructure.AI (Part 2)
**Target:** 27 tests (Plugins, Analysis)
**Actual:** [count]
**Coverage:** [previous]% → [measure]% (+[delta]%)
**Status:** [update]

[Continue for each day...]

## Week 2 Totals
- Tests Added: [total] / 152 target
- Coverage: 32% → [end]% / 55% target
- Status: [assessment]
```

---

## ✅ SUCCESS METRICS

**After 3 Weeks, you should have:**

- [ ] 920+ total tests (from 353)
- [ ] 82%+ line coverage (from 19.3%)
- [ ] Desktop.Avalonia: 75%+ (from 7.7%)
- [ ] Infrastructure.AI: 60%+ (from 0%)
- [ ] Infrastructure.Monitoring: 60%+ (from 0%)
- [ ] All tests passing
- [ ] Test execution < 30 seconds
- [ ] Production-ready quality

---

## 🚀 IMMEDIATE NEXT STEPS (DAY 5 - FRIDAY)

**This Friday (Day 5 of Week 1):**

1. **Create converter tests file** (30 min)
2. **Implement 60 converter tests** (4 hours)
3. **Run coverage analysis** (15 min)
4. **Verify 19.3% → 23%** (15 min)
5. **Commit progress** (15 min)

**Saturday-Sunday (Days 6-7):**

1. **Desktop services tests** (6-8 hours total)
2. **64 tests for navigation, dialogs, etc.**
3. **Coverage: 23% → 27%**
4. **Week 1 complete:** 605 tests, 32% coverage ✅

---

**You're already making excellent progress! You added 128 tests in 4 days. Keep this momentum through Week 1, then tackle Infrastructure in Week 2. You'll hit 82% by Day 21! 🚀**