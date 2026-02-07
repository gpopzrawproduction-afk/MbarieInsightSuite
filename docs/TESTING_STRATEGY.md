# Mbarie Intelligence Console - Testing Strategy

## Current Test Coverage Analysis (2026-02-07)

| Suite | Scope | Pass Rate | Notes |
|-------|-------|----------|-------|
| MIC.Tests.Unit | View models, services, CQRS handlers, domain logic | 100% (52/52) | Expanded coverage for error handling and knowledge base flows |
| MIC.Tests.Integration | Persistence + infrastructure via PostgreSQL Testcontainers | 100% (28/28) | Validates migrations, DbInitializer resilience, and email sync wiring |
| MIC.Tests.E2E | Desktop smoke scenarios | 100% (15/15) | Light smoke checks; full UI automation backlog remains |

Cobertura results from `dotnet test --collect:"XPlat Code Coverage"`:

| Suite | Line Coverage | Branch Coverage |
|-------|---------------|-----------------|
| MIC.Tests.Unit | 8.62% (1915/22212) | 5.74% (246/4282) |
| MIC.Tests.Integration | 8.04% (833/10350) | 2.56% (47/1832) |
| MIC.Tests.E2E | n/a | n/a |
| **Combined** | **8.44%** (2748/32562) | **5.19%** (293/6114) |

> **Action**: Close the coverage gap to ≥ 80% before Phase 1. Prioritize CQRS handlers, infrastructure services, and high-traffic view models.

---

## Phase 0 Gap Analysis

### Tier 1 – Critical Coverage

| Component | Gap | Recommended Tests |
|-----------|-----|-------------------|
| Authentication (Login, Register, Refresh) | Partial | Add negative-path tests, token expiry scenarios |
| Knowledge Base CQRS + Services | Minimal | Exercise search/index flows, error handling contracts |
| Email Sync & Retry Policies | Minimal | Simulate MailKit failures, ensure Polly wraps all calls |
| DbInitializer / DatabaseSettings | Minimal | Verify exception translation + migration guards |

### Tier 2 – High Value

| Component | Gap | Recommended Tests |
|-----------|-----|-------------------|
| View Models (Dashboard, Metrics, Alerts) | Sparse | Cover UI state transitions, loading indicators, filter logic |
| Notification Pipeline | Sparse | Validate toast routing, critical notification handling |
| ExportService | Partial | Validate CSV/PDF generation success + error paths |

### Tier 3 – Supporting

| Component | Gap | Recommended Tests |
|-----------|-----|-------------------|
| Settings Handlers | Outdated | Refresh tests for DatabaseSettings-driven behaviors |
| Predictions Service | Placeholder | Add stubs or integration tests once models finalize |
| Auxiliary Utilities | Mixed | Cover extension methods, comparers, value converters |

---

## Test Implementation Guidelines

### Unit Test Naming Convention
```
[MethodName]_[Scenario]_[ExpectedResult]
```

Examples:
- `Handle_WithValidCommand_ReturnsAlertId`
- `Handle_WithNonexistentAlert_ReturnsNotFoundError`
- `Handle_WhenRepositoryThrows_ReturnsError`

### AAA Pattern (Arrange, Act, Assert)
```csharp
[Fact]
public async Task Handle_WithValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new UpdateAlertCommand(...);
    _repository.GetByIdAsync(...).Returns(existingAlert);

    // Act
    var result = await _sut.Handle(command, CancellationToken.None);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Should().Be(expectedId);
}
```

### Mocking Strategy
- Use **Moq** for repositories/services and **Testcontainers** for integration boundaries
- Prefer **FluentAssertions** for expressive assertions
- Do not mock the System Under Test; isolate external dependencies only

### Test Data Strategy
1. **Test Builders** - For complex entity creation
2. **Fixtures** - Shared setup for related tests
3. **Inline Data** - Simple, test-specific values

---

## Coverage Targets

| Layer | Target | Current | Notes |
|-------|--------|---------|-------|
| Application (Commands/Queries) | 80% | ~12% | Focus on MediatR handlers and validators |
| Domain (Aggregates/Entities) | 60% | ~5% | Use builder utilities + invariants |
| Infrastructure (Services, Resilience) | 70% | ~10% | Cover retry policies, external adapters |
| Presentation (View Models) | 70% | ~15% | Exercise error states + command pipelines |

**Overall Phase 0 Goal:** ≥ 80% combined line coverage. Track progress in `TestResults/coverage-summary.txt`.

---

## CI/CD Pipeline Requirements

1. **Pull Requests**
    - Execute unit + integration suites (Docker available in CI)
    - Fail build if coverage delta lowers overall percentage
    - Publish Cobertura report for build comparisons

2. **Main Branch**
    - Run full test matrix (unit, integration, E2E smoke)
    - Aggregate coverage reports and upload to artifacts
    - Trigger documentation badge updates (future)

3. **Nightly / Scheduled Jobs**
    - Execute long-running E2E/regression suites
    - Run performance/stress scenarios (placeholder)
    - Post coverage delta summary to project dashboard

---

## Test Project Structure

```
MIC.Tests.Unit/
??? Features/
?   ??? Alerts/
?   ?   ??? CreateAlertCommandHandlerTests.cs ?
?   ?   ??? UpdateAlertCommandHandlerTests.cs ??
?   ?   ??? DeleteAlertCommandHandlerTests.cs ??
?   ?   ??? GetAllAlertsQueryHandlerTests.cs ?
?   ?   ??? GetAlertByIdQueryHandlerTests.cs ??
?   ??? Auth/
?   ?   ??? AuthenticationServiceTests.cs ?
?   ?   ??? LoginCommandHandlerTests.cs ??
?   ?   ??? RegisterUserCommandHandlerTests.cs ??
?   ??? Metrics/
?   ?   ??? GetMetricsQueryHandlerTests.cs ??
?   ?   ??? GetMetricTrendQueryHandlerTests.cs ??
?   ??? Chat/
?   ?   ??? SaveChatInteractionCommandHandlerTests.cs ??
?   ?   ??? GetChatHistoryQueryHandlerTests.cs ??
?   ??? Settings/
?       ??? SaveSettingsCommandHandlerTests.cs ??
??? Builders/
?   ??? AlertBuilder.cs ??
?   ??? UserBuilder.cs ??
?   ??? MetricBuilder.cs ??
??? Fixtures/
    ??? TestFixtures.cs ??

MIC.Tests.Integration/
??? Features/
?   ??? Auth/
?       ??? LoginIntegrationTests.cs ?
??? Database/
?   ??? AlertRepositoryTests.cs ??
??? Fixtures/
    ??? DatabaseFixture.cs ??
```

---

## Next Steps

1. Expand unit coverage for authentication, knowledge base, and error handling workflows
2. Add integration tests for retry policies and DatabaseSettings-driven migrations
3. Build Avalonia UI automation harness for high-value E2E flows
4. Enable CI coverage gates targeting incremental progress toward 80%
