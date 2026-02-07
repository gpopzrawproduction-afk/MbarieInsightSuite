# PHASE 0: FOUNDATION STABILIZATION - Master Instructions

## OBJECTIVE
Stabilize existing codebase, fix critical bugs, complete partial implementations,
and establish testing foundation. NO new features - only completion and fixes.

## STRICT RULES
1. NO mock data - all implementations must use real services/data
2. NO placeholder responses - complete all TODO/NotImplementedException
3. ALL code changes must include unit tests
4. MAINTAIN existing architecture patterns (DI, CQRS, clean architecture)
5. Document all changes in changelog
6. Each task must build and pass tests before moving to next

## WORK PACKAGES

### WP-0.1: OAuth Implementation Completion
**Priority:** CRITICAL
**Files to Fix:**
- MIC.Infrastructure.Services/Email/OutlookOAuthService.cs (lines 214-272)
- MIC.Infrastructure.Services/Email/GmailOAuthService.cs (lines 188-250)

**Requirements:**
1. Implement complete OAuth2 flow for Outlook (MSAL library)
   - Authorization code flow with PKCE
   - Token acquisition and storage in secure credential manager
   - Automatic token refresh with retry logic
   - Error handling for network failures and user cancellation
   
2. Implement complete OAuth2 flow for Gmail (Google.Apis.Auth library)
   - OAuth2 authorization with offline access
   - Token storage encrypted at rest
   - Token refresh before expiration
   - Scope management (Gmail.Readonly, Gmail.Send, Gmail.Modify)

3. Implement token persistence service
   - Use Windows Credential Manager for token storage
   - Encrypt tokens with DPAPI
   - Handle multi-account scenarios
   - Provide token revocation capability

4. Add comprehensive error handling
   - Network timeout scenarios
   - Invalid credentials
   - Expired refresh tokens
   - User denial of permissions

5. Unit tests required (minimum 80% coverage):
   - Token acquisition flow
   - Token refresh logic
   - Error scenarios
   - Multi-account management
   - Token encryption/decryption

**Acceptance Criteria:**
- ✅ User can authenticate with Outlook account
- ✅ User can authenticate with Gmail account
- ✅ Tokens persist across app restarts
- ✅ Tokens auto-refresh before expiration
- ✅ All error scenarios handled gracefully
- ✅ No NotImplementedException thrown
- ✅ All unit tests pass

**Dependencies:**
- NuGet: Microsoft.Identity.Client (MSAL)
- NuGet: Google.Apis.Auth
- NuGet: Google.Apis.Gmail.v1

---

### WP-0.2: Settings Persistence System
**Priority:** CRITICAL
**Scope:** Complete user settings and preferences system

**Requirements:**
1. Implement ISettingsService with real storage
   - User preferences (theme, language, notifications)
   - Application settings (sync intervals, cache size)
   - Account-specific settings per email provider
   - Feature flags for A/B testing
   
2. Storage backend implementation
   - Primary: SQLite database in user AppData
   - Backup: JSON file for settings export/import
   - Schema versioning for migrations
   - Atomic writes with transaction support

3. Settings synchronization
   - Cloud backup option (encrypted Azure Blob Storage)
   - Cross-device sync with conflict resolution
   - Last-write-wins strategy with timestamp
   - Offline queue for sync when network unavailable

4. Settings categories to implement:
   - UserPreferences (UI, behavior, accessibility)
   - EmailSettings (signature, default account, send options)
   - NotificationSettings (channels, priority rules, DND schedules)
   - SecuritySettings (encryption preferences, timeout)
   - AISettings (model preferences, prompt templates)
   - PerformanceSettings (cache size, sync frequency)

5. Migration service for settings schema changes
   - Version tracking in database
   - Automatic migration on app startup
   - Rollback capability for failed migrations
   - Migration history logging

**Database Schema:**
```sql
CREATE TABLE Settings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    Category TEXT NOT NULL,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    ValueType TEXT NOT NULL,
    LastModified DATETIME NOT NULL,
    SyncStatus TEXT,
    UNIQUE(UserId, Category, Key)
);

CREATE TABLE SettingsHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingId INTEGER,
    OldValue TEXT,
    NewValue TEXT,
    ChangedAt DATETIME,
    ChangedBy TEXT,
    FOREIGN KEY(SettingId) REFERENCES Settings(Id)
);

CREATE TABLE SettingsSchema (
    Version INTEGER PRIMARY KEY,
    AppliedAt DATETIME,
    Description TEXT
);
```

**Unit Tests Required:**
- CRUD operations for all setting types
- Schema migration scenarios
- Conflict resolution logic
- Encryption/decryption of sensitive settings
- Export/import functionality
- Cloud sync scenarios (online/offline)

**Acceptance Criteria:**
- ✅ All settings persist across app restarts
- ✅ Settings survive app crashes (atomic writes)
- ✅ Settings can be exported/imported as JSON
- ✅ Cloud sync works when enabled
- ✅ Sensitive settings are encrypted at rest
- ✅ Schema migrations work without data loss
- ✅ 80%+ test coverage

---

### WP-0.3: Email Sync Service Completion
**Priority:** CRITICAL
**Files to Complete:**
- MIC.Infrastructure.Data/Services/RealEmailSyncService.Historical.cs

**Requirements:**
1. Complete historical email sync implementation
   - Incremental sync with checkpoint mechanism
   - Attachment download and storage
   - Metadata extraction (sender, recipients, date, subject, body preview)
   - Thread/conversation detection
   - Duplicate detection using Message-ID header
   
2. Attachment management
   - Store attachments in blob storage (local first, cloud optional)
   - Generate SHA-256 hash for deduplication
   - Virus scanning integration point (abstract interface)
   - Size limits and quota management
   - Lazy loading for large attachments

3. Sync state management
   - Track last sync timestamp per folder
   - Resume interrupted syncs
   - Detect and sync deleted emails
   - Handle folder structure changes
   - Rate limiting to respect provider API limits

4. Error handling and retry logic
   - Exponential backoff for transient errors
   - Skip corrupted emails and log errors
   - Quota exceeded handling
   - Network interruption recovery
   - Provider-specific error handling (Outlook vs Gmail)

5. Performance optimizations
   - Batch operations (sync 50-100 emails at once)
   - Parallel folder sync (max 3 concurrent)
   - Database bulk insert for efficiency
   - Background sync with cancellation support
   - Progress reporting to UI

**Database Schema for Sync State:**
```sql
CREATE TABLE EmailSyncState (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId TEXT NOT NULL,
    FolderId TEXT NOT NULL,
    LastSyncToken TEXT,
    LastSyncDate DATETIME,
    TotalEmails INTEGER,
    SyncedEmails INTEGER,
    FailedEmails INTEGER,
    Status TEXT,
    UNIQUE(AccountId, FolderId)
);

CREATE TABLE SyncErrors (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId TEXT,
    EmailId TEXT,
    ErrorMessage TEXT,
    ErrorDate DATETIME,
    Retries INTEGER DEFAULT 0
);
```

**Unit Tests Required:**
- Incremental sync logic
- Checkpoint/resume functionality
- Attachment storage and retrieval
- Duplicate detection
- Error handling and retry
- Rate limiting compliance
- Parallel sync coordination

**Acceptance Criteria:**
- ✅ All historical emails sync including attachments
- ✅ Sync can be paused and resumed
- ✅ No duplicate emails stored
- ✅ Attachments stored securely with deduplication
- ✅ Sync progress visible to user
- ✅ Handles network interruptions gracefully
- ✅ 80%+ test coverage

---

### WP-0.4: Testing Infrastructure Setup
**Priority:** CRITICAL
**Goal:** Establish foundation for comprehensive testing

**Requirements:**
1. Unit testing framework setup
   - xUnit as primary framework
   - FluentAssertions for readable assertions
   - NSubstitute for mocking
   - AutoFixture for test data generation (NO MOCK DATA - real patterns)
   
2. Test project organization
```
   MIC.Tests.Unit/
   ├── Application/
   │   ├── Commands/
   │   ├── Queries/
   │   └── Handlers/
   ├── Domain/
   │   ├── Entities/
   │   └── ValueObjects/
   ├── Infrastructure/
   │   ├── Services/
   │   └── Repositories/
   └── Presentation/
       └── ViewModels/
   
   MIC.Tests.Integration/
   ├── Email/
   ├── Database/
   ├── OAuth/
   └── KnowledgeBase/
   
   MIC.Tests.E2E/
   └── Scenarios/
```

3. Test data builders (NO MOCK DATA)
   - Use real email formats (RFC 5322 compliant)
   - Use real OAuth token structures
   - Use real database schemas
   - Generate realistic test scenarios
   
4. Integration test infrastructure
   - In-memory SQLite for database tests
   - Test email server (MailHog or similar)
   - Mock OAuth server for testing flows
   - Test containers for dependencies

5. CI/CD test pipeline setup
   - Run all tests on every commit
   - Generate code coverage reports (target 80%)
   - Fail build if coverage drops
   - Parallel test execution
   - Test result reporting

**Test Categories to Implement:**
- Unit tests for all services (80% coverage minimum)
- Integration tests for OAuth flows
- Integration tests for email sync
- Integration tests for database operations
- Integration tests for settings persistence

**Acceptance Criteria:**
- ✅ All test projects compile and run
- ✅ Test coverage reaches 80% for Phase 0 changes
- ✅ CI pipeline runs tests automatically
- ✅ Coverage reports generated
- ✅ No flaky tests (all deterministic)
- ✅ Tests use real data patterns, no mocks

---

### WP-0.5: Notification Center Completion
**Priority:** HIGH
**Files to Complete:**
- NotificationViewModel.cs (partial implementation)

**Requirements:**
1. Complete notification data model
```csharp
   public class Notification
   {
       public string Id { get; set; }
       public NotificationType Type { get; set; } // Email, Alert, System, AI
       public string Title { get; set; }
       public string Message { get; set; }
       public DateTime Timestamp { get; set; }
       public NotificationPriority Priority { get; set; }
       public bool IsRead { get; set; }
       public Dictionary<string, object> Metadata { get; set; }
       public List<NotificationAction> Actions { get; set; }
   }
```

2. Notification service implementation
   - In-app notification delivery
   - Windows toast notifications
   - Notification persistence (SQLite)
   - Notification grouping by type/source
   - Mark as read/unread functionality
   - Bulk actions (mark all read, dismiss all)
   - Notification filtering and search

3. Notification rules engine
   - User-defined priority rules
   - Do-not-disturb schedules
   - Quiet hours configuration
   - VIP sender notifications
   - Keyword-based alerts
   - Custom notification sounds per type

4. UI implementation in NotificationViewModel
   - Reactive collection of notifications
   - Real-time updates using observables
   - Notification actions (reply, archive, snooze)
   - Notification history view
   - Settings integration for preferences
   - Badge count for unread notifications

5. Integration with existing features
   - Email arrival notifications
   - Predictive alerts (high-priority emails)
   - System notifications (sync errors, updates)
   - AI assistant notifications (insights ready)
   - Knowledge base notifications (processing complete)

**Database Schema:**
```sql
CREATE TABLE Notifications (
    Id TEXT PRIMARY KEY,
    Type TEXT NOT NULL,
    Title TEXT NOT NULL,
    Message TEXT,
    Timestamp DATETIME NOT NULL,
    Priority INTEGER,
    IsRead INTEGER DEFAULT 0,
    Metadata TEXT,
    ExpiresAt DATETIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE NotificationRules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    RuleName TEXT,
    Conditions TEXT, -- JSON
    Actions TEXT, -- JSON
    IsEnabled INTEGER DEFAULT 1,
    Priority INTEGER
);
```

**Unit Tests Required:**
- Notification creation and delivery
- Notification persistence
- Rules engine evaluation
- DND schedule logic
- Grouping and filtering
- Toast notification triggering

**Acceptance Criteria:**
- ✅ Notifications display in real-time
- ✅ Toast notifications appear for high-priority items
- ✅ Notifications persist across restarts
- ✅ DND schedules work correctly
- ✅ Rules engine filters notifications
- ✅ All actions work (mark read, dismiss, snooze)
- ✅ 80%+ test coverage

---

### WP-0.6: Error Handling & Logging Standardization
**Priority:** HIGH
**Goal:** Consistent error handling across all services

**Requirements:**
1. Centralized logging service
   - Serilog implementation with structured logging
   - Multiple sinks: File, Console, Application Insights (optional)
   - Log levels: Verbose, Debug, Information, Warning, Error, Fatal
   - Contextual logging with correlation IDs
   - Performance logging for slow operations
   - Security event logging

2. Exception handling strategy
   - Custom exception hierarchy
```csharp
     MICException (base)
     ├── EmailException
     │   ├── EmailSendException
     │   ├── EmailSyncException
     │   └── EmailAuthException
     ├── KnowledgeBaseException
     ├── SettingsException
     └── InfrastructureException
```
   - Global exception handler in UI layer
   - User-friendly error messages
   - Detailed logging for diagnostics
   - Automatic error reporting option

3. Retry and resilience policies (Polly library)
   - Exponential backoff for transient errors
   - Circuit breaker for failing services
   - Timeout policies for long operations
   - Bulkhead isolation for parallel operations
   - Fallback strategies

4. Error recovery mechanisms
   - Automatic retry with user notification
   - Graceful degradation (offline mode)
   - Transaction rollback on errors
   - Corrupt data detection and repair
   - Error state persistence for diagnostics

5. Telemetry and diagnostics
   - Performance counters
   - Operation timing metrics
   - Error rate tracking
   - Memory usage monitoring
   - Network request logging

**Implementation Points:**
- Add logging to all service methods
- Wrap external calls with retry policies
- Add try-catch with proper error types
- Log all exceptions with context
- Add telemetry to critical paths

**Unit Tests Required:**
- Exception handling scenarios
- Retry policy verification
- Circuit breaker behavior
- Logging output validation
- Error recovery flows

**Acceptance Criteria:**
- ✅ All exceptions logged with context
- ✅ Retry policies prevent transient failure impact
- ✅ Users see friendly error messages
- ✅ Diagnostic logs available for troubleshooting
- ✅ No unhandled exceptions
- ✅ Performance metrics collected

---

## PHASE 0 EXECUTION PLAN

### Week 1: Critical Service Completion
**Days 1-2:** WP-0.1 OAuth Implementation (OutlookOAuthService, GmailOAuthService)
**Days 3-4:** WP-0.2 Settings Persistence (SettingsService, database schema, migrations)
**Day 5:** WP-0.4 Testing Infrastructure Setup (test projects, CI pipeline)

### Week 2: Data & Quality
**Days 1-2:** WP-0.3 Email Sync Completion (attachment storage, sync state)
**Days 3-4:** WP-0.5 Notification Center (notifications service, UI)
**Day 5:** WP-0.6 Error Handling & Logging (standardization, retry policies)

### End of Week 2: Validation Checkpoint
- ✅ All WP-0.x acceptance criteria met
- ✅ Test coverage ≥ 80% for Phase 0 changes
- ✅ Build passes with no errors/warnings
- ✅ Manual testing of critical flows
- ✅ Code review completed
- ✅ Documentation updated

---

## VALIDATION & QUALITY GATES

### Before Moving to Phase 1:
1. ✅ All NotImplementedException removed
2. ✅ OAuth flow works end-to-end for Outlook + Gmail
3. ✅ Settings persist and sync correctly
4. ✅ Email sync completes with attachments
5. ✅ Notification center functional
6. ✅ Error handling consistent across codebase
7. ✅ Test coverage ≥ 80% for Phase 0 scope
8. ✅ No critical bugs in issue tracker
9. ✅ Performance acceptable (startup < 3s, sync 1000 emails < 30s)
10. ✅ Memory usage < 300MB for typical workload

---

## COPILOT WORKING INSTRUCTIONS

### For Each Work Package:
1. **Read existing code first** - understand current implementation
2. **Check dependencies** - ensure all NuGet packages available
3. **Follow architecture** - maintain DI, CQRS, clean architecture
4. **Write tests FIRST** - TDD approach for new code
5. **Implement incrementally** - small commits, frequent builds
6. **Document as you go** - XML comments, README updates
7. **Run all tests** - ensure nothing breaks
8. **Request review** - before marking WP complete

### Code Quality Standards:
- ✅ Follow C# coding conventions
- ✅ Use async/await properly (no .Result or .Wait())
- ✅ Dispose resources properly (using statements)
- ✅ Null reference checks (nullable reference types enabled)
- ✅ XML documentation comments on public APIs
- ✅ No hardcoded strings (use constants/resources)
- ✅ No magic numbers (use named constants)
- ✅ Proper logging at appropriate levels
- ✅ Exception messages user-friendly

### Testing Standards:
- ✅ Test class per production class
- ✅ Arrange-Act-Assert pattern
- ✅ Descriptive test names (Method_Scenario_ExpectedResult)
- ✅ One assertion per test (when possible)
- ✅ Test edge cases and error scenarios
- ✅ No test interdependencies
- ✅ Fast tests (< 1s per test)
- ✅ Deterministic (no random data, use fixed seeds)

---

## RISK MITIGATION

### If You Encounter:
**"This is taking longer than expected"**
→ Break work package into smaller tasks, deliver incrementally

**"Tests are failing"**
→ Fix tests before proceeding, do not disable or skip

**"Not sure how to implement X"**
→ ASK for clarification, do not guess or use placeholders

**"Dependencies conflict"**
→ Document and escalate, do not force compatibility

**"Performance is slow"**
→ Profile first, optimize based on data, not assumptions

---

## SUCCESS METRICS FOR PHASE 0

### Completion Criteria:
- [ ] All 6 work packages delivered
- [ ] 80%+ test coverage achieved
- [ ] Zero NotImplementedException in codebase
- [ ] All critical user flows work end-to-end
- [ ] Build completes with zero errors, < 5 warnings
- [ ] No placeholder/mock data in production code paths
- [ ] Documentation updated
- [ ] Changelog complete

### Quality Metrics:
- [ ] Code review completed with no major issues
- [ ] Manual testing passed for all scenarios
- [ ] Performance benchmarks met
- [ ] Memory profiling shows no leaks
- [ ] Security review passed (no credentials in code)

---

## NEXT STEPS AFTER PHASE 0

Once Phase 0 validation passes, we proceed to:
- **Phase 1:** Critical feature completion (email send, KB RAG, predictions)
- **Phase 2:** Release infrastructure (signing, MSIX, updates)
- **Phase 3:** Quality enhancement (tests to 85%, performance tuning)
- **Phase 4+:** Enterprise features (systematic addition)

---

END OF PHASE 0 MASTER INSTRUCTIONS