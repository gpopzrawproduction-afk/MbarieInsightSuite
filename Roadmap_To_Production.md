# 🗺️ Complete Roadmap: Mocks → Production-Ready Software

**Project:** MIC (Multi-Intelligence Console) Desktop Application  
**Current Status:** Phase 1 - Unit Testing (Day 4 of Week 1)  
**Target:** Production-ready software installed on end-user computers  
**Timeline:** 7 weeks (Feb 7, 2026 → Mar 28, 2026)

---

## 📊 Executive Summary

This document outlines the complete journey from unit testing with mocks to production-ready software installation on end-user computers.

### The Five Phases

```
Phase 1: Unit Testing (Week 1-2)        → 80% code coverage with mocks
Phase 2: Integration Testing (Week 3)   → Real components working together
Phase 3: E2E Testing (Week 4)           → Complete user workflows
Phase 4: UAT Testing (Week 5)           → Real users, real data
Phase 5: Production Deployment (Week 6-7) → Installed software on computers
```

### Current Status
- ✅ **Tests:** 368 unit tests (all passing)
- ✅ **Coverage:** 19.3% (target: 80%)
- ✅ **Phase:** Unit Testing (Day 4 of 14)

---

# Phase 1: Unit Testing with Mocks

## Timeline: Week 1-2 (Feb 7-21, 2026)

### Objective
Build comprehensive unit test suite with 80% code coverage using mocked dependencies.

### Why Mocks?

**Advantages:**
- ✅ **Speed:** Tests run in milliseconds (entire suite < 10 seconds)
- ✅ **Isolation:** Test one component at a time
- ✅ **Reliability:** No external dependencies (no network, database, file system)
- ✅ **Coverage:** Can test edge cases and error scenarios easily
- ✅ **Debugging:** Failures pinpoint exact issues

**What we're testing:**
```csharp
// Example: Testing EmailInboxViewModel business logic
var mockEmailService = new Mock<IEmailService>();
mockEmailService.Setup(x => x.GetEmailsAsync()).ReturnsAsync(fakeEmails);

var viewModel = new EmailInboxViewModel(mockEmailService.Object);
await viewModel.LoadEmailsAsync();

// Assert business logic works correctly
Assert.Equal(10, viewModel.Emails.Count);
```

### Daily Targets (Week 1)

| Day | Date | Tests Added | Coverage Target | Focus Area |
|-----|------|-------------|-----------------|------------|
| 1 | Feb 7 | 12 | 13% | UserSessionService edge cases |
| 2 | Feb 8 | 25 | 22% | NotificationCenter, ComposeEmail ViewModels |
| 3 | Feb 9 | 128 | 22% | Localization, OAuth, Infrastructure services |
| 4 | Feb 10 | 15 | 24% | EmailInboxViewModel operations |
| 5 | Feb 11 | 30 | 30% | DashboardViewModel, SettingsViewModel |
| 6 | Feb 12 | 35 | 40% | ChatViewModel, KnowledgeBaseViewModel |
| 7 | Feb 13 | 40 | 50% | Infrastructure repositories, services |

### Daily Targets (Week 2)

| Day | Date | Tests Added | Coverage Target | Focus Area |
|-----|------|-------------|-----------------|------------|
| 8 | Feb 14 | 40 | 60% | Email sync services, attachment handling |
| 9 | Feb 15 | 35 | 65% | AI/Intelligence services |
| 10 | Feb 16 | 30 | 70% | Monitoring services, error handling |
| 11 | Feb 17 | 25 | 75% | Remaining ViewModels, edge cases |
| 12 | Feb 18 | 20 | 78% | Data layer, repositories |
| 13 | Feb 19 | 15 | 80% | Final coverage gaps |
| 14 | Feb 20 | 10 | 80%+ | Code review, refactoring tests |

### Test Structure

```
tests/
├── MIC.Core.Application.Tests/
│   ├── Services/              (High priority - 73.7% coverage already)
│   ├── Commands/
│   └── Queries/
├── MIC.Core.Domain.Tests/
│   ├── Entities/              (55.2% coverage - expand)
│   └── ValueObjects/
├── MIC.Desktop.Avalonia.Tests/
│   ├── ViewModels/            (CRITICAL - 7.7% coverage!)
│   │   ├── Email/
│   │   ├── Chat/
│   │   ├── Dashboard/
│   │   └── Settings/
│   └── Converters/
├── MIC.Infrastructure.Data.Tests/
│   ├── Repositories/          (20.6% coverage - needs work)
│   └── Configurations/
├── MIC.Infrastructure.Identity.Tests/
│   ├── Services/              (39.5% coverage - expand)
│   └── OAuth/
├── MIC.Core.Intelligence.Tests/
│   ├── Services/              (28.4% coverage - needs work)
│   └── Processors/
└── MIC.Infrastructure.AI.Tests/
    └── Services/              (0% coverage - UNTESTED!)
```

### Commands & Tools

**Run all unit tests:**
```bash
dotnet test --filter "Category!=Integration&Category!=E2E"
```

**Generate coverage report:**
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:./coverage/**/coverage.cobertura.xml \
  -targetdir:./coverage/report \
  -reporttypes:Html

# Open report
open ./coverage/report/index.html  # macOS
start ./coverage/report/index.html # Windows
xdg-open ./coverage/report/index.html # Linux
```

**Check specific project coverage:**
```bash
# Desktop ViewModels (critical gap - 7.7%)
dotnet test tests/MIC.Desktop.Avalonia.Tests/ --collect:"XPlat Code Coverage"

# Infrastructure (needs work - 20.6%)
dotnet test tests/MIC.Infrastructure.Data.Tests/ --collect:"XPlat Code Coverage"
```

### Expected Deliverables (End of Week 2)

- ✅ **500+ unit tests** (currently 368)
- ✅ **80% line coverage** (currently 19.3%)
- ✅ **70% branch coverage** (currently 17.9%)
- ✅ **All tests passing** (0 failures)
- ✅ **Fast test suite** (< 10 seconds total runtime)
- ✅ **Comprehensive documentation** of test scenarios

### Coverage Targets by Project

| Project | Current | Target | Gap | Priority |
|---------|---------|--------|-----|----------|
| MIC.Core.Application | 73.7% | 85% | +11.3% | Medium |
| MIC.Core.Domain | 55.2% | 70% | +14.8% | Medium |
| **MIC.Desktop.Avalonia** | **7.7%** | **80%** | **+72.3%** | **CRITICAL** |
| MIC.Infrastructure.Data | 20.6% | 70% | +49.4% | High |
| MIC.Infrastructure.Identity | 39.5% | 75% | +35.5% | High |
| MIC.Core.Intelligence | 28.4% | 70% | +41.6% | High |
| MIC.Infrastructure.AI | 0% | 60% | +60% | High |
| MIC.Infrastructure.Monitoring | 0% | 50% | +50% | Medium |

---

# Phase 2: Integration Testing with Real Components

## Timeline: Week 3 (Feb 22-28, 2026)

### Objective
Test real components working together with real infrastructure (database, email servers, file system).

### What Changes: Mocks → Real Components

**Unit Test (Mock):**
```csharp
// Fake email service - no real email server
var mockEmailService = new Mock<IEmailService>();
mockEmailService.Setup(x => x.GetEmails()).Returns(fakeEmails);
```

**Integration Test (Real):**
```csharp
// Real email service - actually connects to test Gmail account!
var dbContext = new ApplicationDbContext(testConnectionString);
var gmailOAuth = new GmailOAuthService(testGmailConfig);
var emailService = new EmailService(dbContext, gmailOAuth);

var emails = await emailService.SyncEmailsAsync(testAccount);
// ^ This actually downloads emails from Gmail test account!
```

### Infrastructure Setup Required

#### 1. Test Database (PostgreSQL)

**Install PostgreSQL for testing:**
```bash
# macOS
brew install postgresql@15
brew services start postgresql@15

# Ubuntu/Debian
sudo apt-get install postgresql-15
sudo systemctl start postgresql

# Windows (download installer from postgresql.org)
```

**Create test database:**
```sql
-- Connect to PostgreSQL
psql -U postgres

-- Create test database
CREATE DATABASE mic_test;

-- Create test user
CREATE USER mic_test_user WITH PASSWORD 'test_password_123';

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE mic_test TO mic_test_user;
```

**Configure test connection string:**
```json
// appsettings.Testing.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mic_test;Username=mic_test_user;Password=test_password_123"
  }
}
```

**Database migrations for test:**
```bash
# Apply migrations to test database
dotnet ef database update --connection "Host=localhost;Database=mic_test;Username=mic_test_user;Password=test_password_123"
```

#### 2. Test Email Accounts

**Create test Gmail account:**
```
1. Go to gmail.com
2. Create new account: micdesktop.test@gmail.com
3. Enable 2-factor authentication
4. Generate App Password for OAuth testing
5. Configure OAuth 2.0 credentials in Google Cloud Console
```

**Create test Outlook account:**
```
1. Go to outlook.com
2. Create new account: micdesktop.test@outlook.com
3. Register app in Azure AD for OAuth
4. Generate client ID and secret
```

**Configure test credentials:**
```json
// appsettings.Testing.json
{
  "Email": {
    "Gmail": {
      "ClientId": "test-gmail-client-id",
      "ClientSecret": "test-gmail-client-secret",
      "TestAccount": "micdesktop.test@gmail.com"
    },
    "Outlook": {
      "ClientId": "test-outlook-client-id",
      "ClientSecret": "test-outlook-client-secret",
      "TestAccount": "micdesktop.test@outlook.com"
    }
  }
}
```

#### 3. Test Data Setup

**Seed test database with realistic data:**
```csharp
public class TestDataSeeder
{
    public static async Task SeedTestData(ApplicationDbContext context)
    {
        // Create test users
        var testUsers = new[]
        {
            new User { Email = "test1@example.com", Username = "testuser1" },
            new User { Email = "test2@example.com", Username = "testuser2" }
        };
        await context.Users.AddRangeAsync(testUsers);

        // Create test emails
        var testEmails = new[]
        {
            new Email { Subject = "Test Email 1", From = "sender@test.com" },
            new Email { Subject = "Test Email 2", From = "sender2@test.com" }
        };
        await context.Emails.AddRangeAsync(testEmails);

        await context.SaveChangesAsync();
    }
}
```

### Integration Test Structure

```
tests/
├── MIC.IntegrationTests/
│   ├── Infrastructure/
│   │   ├── EmailServiceIntegrationTests.cs
│   │   ├── DatabaseIntegrationTests.cs
│   │   ├── FileSystemIntegrationTests.cs
│   │   └── AuthenticationIntegrationTests.cs
│   ├── Workflows/
│   │   ├── EmailSyncWorkflowTests.cs
│   │   ├── UserRegistrationWorkflowTests.cs
│   │   └── ChatWorkflowTests.cs
│   └── Fixtures/
│       ├── DatabaseFixture.cs          (Setup/teardown test DB)
│       ├── EmailAccountFixture.cs      (Test email accounts)
│       └── TestDataFixture.cs          (Seed test data)
```

### Example Integration Tests

**1. Email Sync Integration Test:**
```csharp
[Collection("Integration")]
public class EmailSyncIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public EmailSyncIntegrationTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _emailService = new EmailService(_context, new GmailOAuthService());
    }

    [Fact]
    public async Task SyncEmails_WithRealGmailAccount_ShouldDownloadAndSaveEmails()
    {
        // Arrange
        var testAccount = new EmailAccount
        {
            Email = "micdesktop.test@gmail.com",
            Provider = EmailProvider.Gmail
        };
        await _context.EmailAccounts.AddAsync(testAccount);
        await _context.SaveChangesAsync();

        // Act - Actually connects to Gmail!
        var result = await _emailService.SyncEmailsAsync(testAccount.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Emails);
        
        // Verify emails were saved to database
        var savedEmails = await _context.Emails
            .Where(e => e.AccountId == testAccount.Id)
            .ToListAsync();
        
        Assert.Equal(result.Emails.Count, savedEmails.Count);
        Assert.All(savedEmails, email => 
        {
            Assert.NotNull(email.MessageId);
            Assert.NotNull(email.Subject);
        });
    }

    [Fact]
    public async Task SyncEmails_WithInvalidCredentials_ShouldHandleGracefully()
    {
        // Arrange
        var invalidAccount = new EmailAccount
        {
            Email = "invalid@gmail.com",
            Provider = EmailProvider.Gmail
        };
        await _context.EmailAccounts.AddAsync(invalidAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.SyncEmailsAsync(invalidAccount.Id);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("authentication", result.Error.ToLower());
    }
}
```

**2. Database Integration Test:**
```csharp
[Collection("Integration")]
public class UserRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task CreateUser_ShouldPersistToDatabase()
    {
        // Arrange
        var user = new User
        {
            Username = "integrationtest",
            Email = "integration@test.com",
            PasswordHash = "hashed_password"
        };

        // Act
        var created = await _repository.CreateAsync(user);

        // Assert - Verify in database
        var retrieved = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "integration@test.com");
        
        Assert.NotNull(retrieved);
        Assert.Equal("integrationtest", retrieved.Username);
        Assert.NotEqual(0, retrieved.Id); // Auto-generated ID
    }

    [Fact]
    public async Task GetUserByEmail_WithNonExistent_ShouldReturnNull()
    {
        // Act
        var user = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(user);
    }
}
```

**3. Authentication Flow Integration Test:**
```csharp
[Collection("Integration")]
public class AuthenticationIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly IAuthenticationService _authService;
    private readonly IUserRepository _userRepository;

    public AuthenticationIntegrationTests(DatabaseFixture fixture)
    {
        var context = fixture.CreateContext();
        _userRepository = new UserRepository(context);
        _authService = new AuthenticationService(
            _userRepository,
            new PasswordHasher(),
            new JwtTokenService()
        );
    }

    [Fact]
    public async Task RegisterAndLogin_CompleteFlow_ShouldSucceed()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "flowtest",
            Email = "flowtest@example.com",
            Password = "SecurePassword123!"
        };

        // Act 1: Register
        var registerResult = await _authService.RegisterAsync(registerRequest);

        // Assert 1: Registration succeeded
        Assert.True(registerResult.Success);
        Assert.NotNull(registerResult.User);

        // Act 2: Login with same credentials
        var loginResult = await _authService.LoginAsync(
            "flowtest@example.com",
            "SecurePassword123!"
        );

        // Assert 2: Login succeeded
        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Token);
        Assert.Equal("flowtest", loginResult.User.Username);

        // Act 3: Verify user exists in database
        var dbUser = await _userRepository.GetByEmailAsync("flowtest@example.com");

        // Assert 3: User persisted correctly
        Assert.NotNull(dbUser);
        Assert.Equal("flowtest", dbUser.Username);
        Assert.NotEqual("SecurePassword123!", dbUser.PasswordHash); // Should be hashed
    }
}
```

**4. File System Integration Test:**
```csharp
[Collection("Integration")]
public class AttachmentStorageIntegrationTests
{
    private readonly string _testStoragePath;
    private readonly IAttachmentStorageService _storageService;

    public AttachmentStorageIntegrationTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "mic_test_attachments");
        Directory.CreateDirectory(_testStoragePath);
        _storageService = new AttachmentStorageService(_testStoragePath);
    }

    [Fact]
    public async Task SaveAttachment_ShouldWriteToFileSystem()
    {
        // Arrange
        var testContent = "Test attachment content"u8.ToArray();
        var fileName = "test_document.txt";

        // Act
        var savedPath = await _storageService.SaveAttachmentAsync(
            fileName,
            testContent
        );

        // Assert
        Assert.True(File.Exists(savedPath));
        var readContent = await File.ReadAllBytesAsync(savedPath);
        Assert.Equal(testContent, readContent);
    }

    public void Dispose()
    {
        // Cleanup test files
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, recursive: true);
        }
    }
}
```

### Database Fixture (Setup/Teardown)

```csharp
public class DatabaseFixture : IDisposable
{
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseFixture()
    {
        _connectionString = "Host=localhost;Database=mic_test;Username=mic_test_user;Password=test_password_123";
        
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_connectionString));
        
        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created and migrated
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureDeleted(); // Clean slate
        context.Database.Migrate();       // Apply migrations
    }

    public ApplicationDbContext CreateContext()
    {
        var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Clean database before each test
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        
        return context;
    }

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureDeleted(); // Cleanup
    }
}

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
}
```

### Running Integration Tests

**Run only integration tests:**
```bash
dotnet test --filter "Category=Integration"
```

**Run with detailed output:**
```bash
dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
```

**Run specific integration test:**
```bash
dotnet test --filter "FullyQualifiedName~EmailSyncIntegrationTests"
```

### Expected Deliverables (End of Week 3)

- ✅ **40+ integration tests** covering:
  - Email sync workflows (Gmail, Outlook)
  - Database operations (CRUD, transactions)
  - Authentication flows (register, login, OAuth)
  - File system operations (attachments, uploads)
  - Service integration (multiple services working together)
- ✅ **Test database** setup and configured
- ✅ **Test email accounts** configured (Gmail, Outlook)
- ✅ **All integration tests passing**
- ✅ **CI/CD pipeline** updated to run integration tests nightly

### Integration Test Targets

| Area | Tests | Priority |
|------|-------|----------|
| Email Sync (Gmail) | 8 | Critical |
| Email Sync (Outlook) | 8 | Critical |
| Database Operations | 10 | High |
| Authentication Flow | 6 | High |
| File System | 4 | Medium |
| Service Integration | 4 | Medium |
| **Total** | **40** | |

---

# Phase 3: End-to-End Testing

## Timeline: Week 4 (Mar 1-7, 2026)

### Objective
Test complete user workflows in production-like environment with UI automation.

### What is E2E Testing?

**E2E tests simulate real user interactions:**
- ✅ Launch the actual application
- ✅ Click buttons, fill forms, navigate menus
- ✅ Verify UI updates correctly
- ✅ Test complete workflows from start to finish

**Example E2E scenario:**
```
1. User launches application
2. User creates account
3. User adds Gmail account via OAuth
4. User syncs emails
5. User reads an email
6. User replies to email
7. User verifies reply was sent
```

### Tools & Setup

**Avalonia UI Testing:**
```bash
# Install Avalonia Headless platform for testing
dotnet add package Avalonia.Headless --version 11.0.0
dotnet add package Avalonia.Headless.XUnit --version 11.0.0
```

**Playwright for browser automation (OAuth flows):**
```bash
# Install Playwright
dotnet add package Microsoft.Playwright --version 1.40.0

# Install browsers
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### E2E Test Structure

```
tests/
├── MIC.E2E.Tests/
│   ├── Workflows/
│   │   ├── UserOnboardingE2ETests.cs      (First-time setup)
│   │   ├── EmailManagementE2ETests.cs     (Read, reply, archive)
│   │   ├── ChatE2ETests.cs                (Chat features)
│   │   └── SettingsE2ETests.cs            (Configure app)
│   ├── Authentication/
│   │   ├── GmailOAuthE2ETests.cs          (Gmail OAuth flow)
│   │   └── OutlookOAuthE2ETests.cs        (Outlook OAuth flow)
│   ├── Helpers/
│   │   ├── AppLauncher.cs                 (Launch application)
│   │   ├── UIAutomation.cs                (UI interaction helpers)
│   │   └── ScreenshotHelper.cs            (Capture on failure)
│   └── Fixtures/
│       ├── E2ETestFixture.cs              (Setup/teardown)
│       └── TestDataFixture.cs             (Test accounts)
```

### Example E2E Tests

**1. User Onboarding E2E Test:**
```csharp
[Collection("E2E")]
public class UserOnboardingE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public UserOnboardingE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NewUser_CompleteOnboarding_ShouldCreateAccountAndAddEmail()
    {
        // Arrange
        var app = await _fixture.LaunchApplicationAsync();

        // Act & Assert - First-time setup wizard
        
        // Step 1: Welcome screen
        var welcomeScreen = await app.WaitForWindowAsync("Welcome to MIC");
        Assert.NotNull(welcomeScreen);
        await welcomeScreen.ClickButtonAsync("Get Started");

        // Step 2: Create account
        var createAccountScreen = await app.WaitForWindowAsync("Create Account");
        await createAccountScreen.FillTextBoxAsync("Username", "e2etest");
        await createAccountScreen.FillTextBoxAsync("Email", "e2etest@example.com");
        await createAccountScreen.FillTextBoxAsync("Password", "SecurePass123!");
        await createAccountScreen.ClickButtonAsync("Create Account");

        // Wait for account creation
        await Task.Delay(2000);

        // Step 3: Add email account
        var addEmailScreen = await app.WaitForWindowAsync("Add Email Account");
        Assert.NotNull(addEmailScreen);
        await addEmailScreen.ClickButtonAsync("Add Gmail");

        // Step 4: OAuth flow (Playwright handles browser)
        var oauthSuccess = await _fixture.CompleteGmailOAuthAsync("e2etest@gmail.com");
        Assert.True(oauthSuccess);

        // Step 5: Verify main window appears
        var mainWindow = await app.WaitForWindowAsync("MIC Desktop");
        Assert.NotNull(mainWindow);

        // Step 6: Verify email account appears in UI
        var emailAccounts = await mainWindow.GetListItemsAsync("EmailAccountsList");
        Assert.Contains(emailAccounts, account => account.Contains("e2etest@gmail.com"));

        // Cleanup
        await app.CloseAsync();
    }
}
```

**2. Email Management E2E Test:**
```csharp
[Collection("E2E")]
public class EmailManagementE2ETests : IClassFixture<E2ETestFixture>
{
    [Fact]
    public async Task User_ReadAndReplyToEmail_ShouldSendReply()
    {
        // Arrange
        var app = await _fixture.LaunchApplicationAsync();
        await app.LoginAsync("e2etest@example.com", "SecurePass123!");

        // Act - Navigate to inbox
        var mainWindow = await app.GetMainWindowAsync();
        await mainWindow.ClickMenuItemAsync("Email", "Inbox");

        // Wait for emails to load
        await Task.Delay(3000);

        // Select first email
        var emailList = await mainWindow.FindControlAsync("EmailList");
        await emailList.ClickItemAsync(0);

        // Verify email preview appears
        var emailPreview = await mainWindow.FindControlAsync("EmailPreview");
        Assert.NotNull(emailPreview);
        
        var subject = await emailPreview.GetTextAsync("SubjectText");
        Assert.NotEmpty(subject);

        // Click Reply button
        await mainWindow.ClickButtonAsync("Reply");

        // Fill reply
        var composeWindow = await app.WaitForWindowAsync("Reply");
        await composeWindow.FillTextBoxAsync("Body", "This is an E2E test reply.");
        await composeWindow.ClickButtonAsync("Send");

        // Verify sent confirmation
        await Task.Delay(2000);
        var notification = await mainWindow.WaitForNotificationAsync("Email sent successfully");
        Assert.NotNull(notification);

        // Verify reply appears in sent folder
        await mainWindow.ClickMenuItemAsync("Email", "Sent");
        await Task.Delay(2000);
        
        var sentEmails = await mainWindow.GetListItemsAsync("EmailList");
        Assert.Contains(sentEmails, email => email.Contains("This is an E2E test reply"));

        // Cleanup
        await app.CloseAsync();
    }
}
```

**3. Gmail OAuth E2E Test (with Playwright):**
```csharp
[Collection("E2E")]
public class GmailOAuthE2ETests : IClassFixture<E2ETestFixture>
{
    [Fact]
    public async Task GmailOAuth_CompleteFlow_ShouldAuthorizeAndSync()
    {
        // Arrange
        var app = await _fixture.LaunchApplicationAsync();
        await app.LoginAsync("e2etest@example.com", "SecurePass123!");

        // Act - Start OAuth flow
        var settingsWindow = await app.OpenSettingsAsync();
        await settingsWindow.ClickButtonAsync("Add Email Account");
        await settingsWindow.ClickButtonAsync("Gmail");

        // OAuth opens browser - use Playwright
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false // Show browser for debugging
        });
        var page = await browser.NewPageAsync();

        // Wait for OAuth redirect
        await page.WaitForURLAsync("**/accounts.google.com/**");

        // Fill Google login
        await page.FillAsync("input[type='email']", "micdesktop.test@gmail.com");
        await page.ClickAsync("button:has-text('Next')");
        await Task.Delay(2000);

        await page.FillAsync("input[type='password']", _fixture.GetTestGmailPassword());
        await page.ClickAsync("button:has-text('Next')");
        await Task.Delay(3000);

        // Grant permissions
        await page.ClickAsync("button:has-text('Allow')");

        // Wait for redirect back to app
        await page.WaitForURLAsync("**/oauth/callback**");
        await Task.Delay(2000);

        // Assert - Verify account added in app
        var accountsList = await app.GetListItemsAsync("EmailAccountsList");
        Assert.Contains(accountsList, acc => acc.Contains("micdesktop.test@gmail.com"));

        // Verify sync started
        var syncStatus = await app.GetTextAsync("SyncStatusText");
        Assert.Contains("Syncing", syncStatus);

        // Wait for sync to complete
        await Task.Delay(10000);

        // Verify emails downloaded
        await app.ClickMenuItemAsync("Email", "Inbox");
        var emails = await app.GetListItemsAsync("EmailList");
        Assert.NotEmpty(emails);

        // Cleanup
        await app.CloseAsync();
    }
}
```

### E2E Test Helpers

**AppLauncher.cs:**
```csharp
public class AppLauncher
{
    private Process? _appProcess;

    public async Task<Application> LaunchAsync()
    {
        // Build the application
        var buildResult = await ProcessHelper.RunAsync(
            "dotnet",
            "build -c Release src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj"
        );
        
        if (buildResult.ExitCode != 0)
            throw new Exception("Failed to build application");

        // Start the application
        _appProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj --no-build",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        // Wait for application to start
        await Task.Delay(5000);

        // Connect to running app
        return new Application(_appProcess);
    }

    public async Task ShutdownAsync()
    {
        if (_appProcess != null && !_appProcess.HasExited)
        {
            _appProcess.Kill();
            await _appProcess.WaitForExitAsync();
        }
    }
}
```

**UIAutomation.cs:**
```csharp
public class UIAutomation
{
    private readonly Application _app;

    public UIAutomation(Application app)
    {
        _app = app;
    }

    public async Task<Window> WaitForWindowAsync(string title, int timeoutMs = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            var window = _app.Windows.FirstOrDefault(w => w.Title == title);
            if (window != null)
                return window;

            await Task.Delay(500);
        }

        throw new TimeoutException($"Window '{title}' not found within {timeoutMs}ms");
    }

    public async Task ClickButtonAsync(Window window, string buttonName)
    {
        var button = window.FindControl<Button>(buttonName);
        if (button == null)
            throw new Exception($"Button '{buttonName}' not found");

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(500); // Wait for UI to update
    }

    public async Task FillTextBoxAsync(Window window, string textBoxName, string value)
    {
        var textBox = window.FindControl<TextBox>(textBoxName);
        if (textBox == null)
            throw new Exception($"TextBox '{textBoxName}' not found");

        textBox.Text = value;
        await Task.Delay(200);
    }

    public async Task<List<string>> GetListItemsAsync(Window window, string listName)
    {
        var listBox = window.FindControl<ListBox>(listName);
        if (listBox == null)
            throw new Exception($"ListBox '{listName}' not found");

        return listBox.Items.Cast<object>()
            .Select(item => item.ToString() ?? "")
            .ToList();
    }
}
```

**ScreenshotHelper.cs:**
```csharp
public class ScreenshotHelper
{
    public static async Task CaptureOnFailureAsync(string testName)
    {
        var screenshotPath = Path.Combine(
            "E2ETestResults",
            "Screenshots",
            $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        // Capture screenshot using platform-specific tools
        if (OperatingSystem.IsWindows())
        {
            // Use Windows.Graphics.Capture
            await CaptureWindowsScreenshotAsync(screenshotPath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Use screencapture
            await Process.Start("screencapture", screenshotPath).WaitForExitAsync();
        }
        else if (OperatingSystem.IsLinux())
        {
            // Use scrot
            await Process.Start("scrot", screenshotPath).WaitForExitAsync();
        }
    }
}
```

### E2E Test Configuration

**appsettings.E2ETesting.json:**
```json
{
  "E2E": {
    "TestTimeout": 60000,
    "ScreenshotsOnFailure": true,
    "VideoCaptureEnabled": false,
    "SlowMotion": 500,
    "TestAccounts": {
      "Gmail": {
        "Email": "micdesktop.test@gmail.com",
        "Password": "test_password_secure_123"
      },
      "Outlook": {
        "Email": "micdesktop.test@outlook.com",
        "Password": "test_password_secure_456"
      }
    }
  },
  "Database": {
    "ConnectionString": "Host=localhost;Database=mic_e2e_test;Username=mic_test_user;Password=test_password_123"
  }
}
```

### Running E2E Tests

**Run all E2E tests:**
```bash
dotnet test --filter "Category=E2E"
```

**Run with screenshots on failure:**
```bash
dotnet test --filter "Category=E2E" -- E2E.ScreenshotsOnFailure=true
```

**Run specific E2E workflow:**
```bash
dotnet test --filter "FullyQualifiedName~UserOnboardingE2ETests"
```

**Run in headed mode (show UI):**
```bash
dotnet test --filter "Category=E2E" -- E2E.Headless=false
```

### Expected Deliverables (End of Week 4)

- ✅ **15+ E2E tests** covering:
  - User onboarding (registration, first setup)
  - Email management (read, reply, archive, delete)
  - OAuth flows (Gmail, Outlook)
  - Settings configuration
  - Chat workflows
  - Search and filtering
- ✅ **UI automation framework** set up (Avalonia Headless + Playwright)
- ✅ **Screenshot capture** on test failures
- ✅ **All E2E tests passing** in staging environment
- ✅ **Documentation** of test scenarios

### E2E Test Targets

| Workflow | Tests | Priority |
|----------|-------|----------|
| User Onboarding | 3 | Critical |
| Email Management | 5 | Critical |
| OAuth Flows | 3 | Critical |
| Settings | 2 | Medium |
| Chat | 2 | Medium |
| **Total** | **15** | |

---

# Phase 4: User Acceptance Testing (UAT)

## Timeline: Week 5 (Mar 8-14, 2026)

### Objective
Real users test the application with their real email accounts in a production-like environment.

### What is UAT?

**User Acceptance Testing involves:**
- ✅ Real users (not developers)
- ✅ Real email accounts (their Gmail, Outlook, etc.)
- ✅ Real workflows (daily email management tasks)
- ✅ Real feedback (bugs, UX issues, feature requests)

**Goal:** Validate that the application meets user needs and works in real-world scenarios.

### UAT Environment Setup

#### 1. Staging Server Deployment

**Server Requirements:**
```
OS: Ubuntu 22.04 LTS
CPU: 4 cores
RAM: 8 GB
Disk: 50 GB SSD
Network: Public IP with HTTPS
```

**Install prerequisites:**
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Install PostgreSQL
sudo apt install postgresql-15 postgresql-contrib

# Install Nginx (reverse proxy)
sudo apt install nginx

# Install Certbot (SSL certificates)
sudo apt install certbot python3-certbot-nginx
```

**Create staging database:**
```bash
sudo -u postgres psql

CREATE DATABASE mic_staging;
CREATE USER mic_staging_user WITH PASSWORD 'staging_secure_password';
GRANT ALL PRIVILEGES ON DATABASE mic_staging TO mic_staging_user;
\q
```

**Deploy application:**
```bash
# Clone repository
git clone https://github.com/yourorg/mic-desktop.git
cd mic-desktop

# Build for production
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o /var/www/mic-staging

# Run migrations
cd /var/www/mic-staging
./MIC.Desktop database update
```

**Configure Nginx:**
```nginx
# /etc/nginx/sites-available/mic-staging
server {
    listen 80;
    server_name staging.mic-desktop.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**Enable HTTPS:**
```bash
sudo certbot --nginx -d staging.mic-desktop.com
```

**Start application as service:**
```ini
# /etc/systemd/system/mic-staging.service
[Unit]
Description=MIC Desktop Staging
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/mic-staging
ExecStart=/var/www/mic-staging/MIC.Desktop
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable mic-staging
sudo systemctl start mic-staging
```

#### 2. Beta Tester Recruitment

**Recruit 5-10 beta testers:**
```
Target profiles:
1. Power email users (100+ emails/day)
2. Multi-account users (2+ email accounts)
3. Different email providers (Gmail, Outlook, custom domains)
4. Different platforms (Windows, macOS, Linux)
5. Different use cases (work, personal, mixed)
```

**Beta tester invitation email:**
```
Subject: Invitation to Beta Test MIC Desktop Email Client

Hi [Name],

You're invited to beta test MIC Desktop, a new unified email client!

What you'll do:
- Install the application on your computer
- Connect your real email accounts (Gmail, Outlook, etc.)
- Use the app for your daily email management (1 week)
- Provide feedback on your experience

What we need from you:
- 30-60 minutes for initial setup and orientation
- Daily use for 1 week (Mar 8-14)
- Quick survey after each day (~5 minutes)
- Final feedback session (30 minutes)

Compensation:
- Early access to the product
- Free lifetime Pro license ($99 value)
- Your name in the credits (optional)

Interested? Reply to confirm and we'll send setup instructions.

Thanks,
MIC Desktop Team
```

#### 3. Beta Testing Materials

**Setup Guide for Beta Testers:**
```markdown
# MIC Desktop Beta Testing Guide

## Installation

1. Download installer from: https://staging.mic-desktop.com/download
2. Install the application:
   - Windows: Run MIC-Desktop-Setup.exe
   - macOS: Open MIC-Desktop.dmg and drag to Applications
   - Linux: Run chmod +x MIC-Desktop.AppImage && ./MIC-Desktop.AppImage

3. Launch the application

## Initial Setup

1. Create your account:
   - Click "Get Started"
   - Enter username, email, and password
   - Click "Create Account"

2. Add your email account(s):
   - Click "Add Email Account"
   - Select your provider (Gmail, Outlook, etc.)
   - Complete OAuth authorization
   - Wait for initial sync (may take a few minutes)

3. Explore the interface:
   - Email inbox
   - Chat (if enabled)
   - Settings

## Daily Tasks (Next 7 Days)

Please use MIC Desktop for your normal email activities:
- ✅ Reading emails
- ✅ Replying to emails
- ✅ Composing new emails
- ✅ Organizing emails (archive, delete, folders)
- ✅ Searching emails
- ✅ Managing multiple accounts (if applicable)

## Feedback

### Daily Survey (5 minutes)
Complete each evening: https://forms.gle/abc123

Questions:
1. What tasks did you complete today?
2. Any bugs or issues encountered?
3. What worked well?
4. What was frustrating?
5. Overall satisfaction (1-10)

### Bug Reporting
If you encounter a bug:
1. Note what you were doing when it happened
2. Take a screenshot if possible
3. Report via: https://staging.mic-desktop.com/bugs
   Or email: beta@mic-desktop.com

### Final Feedback (Mar 14)
30-minute video call to discuss your overall experience.

## Support

- Email: beta@mic-desktop.com
- Slack channel: #mic-beta-testers
- Response time: < 4 hours during business hours

Thank you for helping us build a better email client! 🙏
```

**Daily Survey Template:**
```
MIC Desktop Beta - Daily Check-in

Date: [Auto-filled]
Tester ID: [Auto-filled]

1. How many hours did you use MIC Desktop today?
   ( ) < 1 hour
   ( ) 1-2 hours
   ( ) 2-4 hours
   ( ) 4+ hours
   ( ) Did not use today

2. What tasks did you complete? (Select all that apply)
   [ ] Read emails
   [ ] Replied to emails
   [ ] Composed new emails
   [ ] Archived/deleted emails
   [ ] Searched for emails
   [ ] Managed folders
   [ ] Switched between accounts
   [ ] Updated settings
   [ ] Other: __________

3. Did you encounter any bugs or issues?
   ( ) No
   ( ) Yes - minor (didn't block me)
   ( ) Yes - major (blocked me from completing task)
   
   If yes, please describe: _______________

4. What worked really well today?
   [Free text]

5. What was frustrating or confusing?
   [Free text]

6. Overall satisfaction today (1-10):
   [Slider: 1 (Very Unsatisfied) to 10 (Very Satisfied)]

7. Any additional comments?
   [Free text]
```

### UAT Monitoring & Support

**Setup monitoring dashboard:**
```bash
# Install Grafana for monitoring
docker run -d \
  --name=grafana \
  -p 3000:3000 \
  grafana/grafana

# Install Sentry for error tracking
# Add to appsettings.Staging.json:
{
  "Sentry": {
    "Dsn": "https://your-sentry-dsn@sentry.io/project-id",
    "Environment": "staging",
    "TracesSampleRate": 1.0
  }
}
```

**Monitor key metrics:**
- Active users (daily, hourly)
- Email sync success rate
- Error rate (by type)
- Performance (response times)
- Crashes and exceptions

**Daily standup for UAT week:**
```
Time: 9:00 AM daily (Mar 8-14)
Duration: 15 minutes

Agenda:
1. Review yesterday's metrics
2. Review bug reports from beta testers
3. Prioritize critical fixes
4. Deploy fixes to staging
5. Communicate updates to testers
```

### UAT Success Criteria

**Minimum requirements to pass UAT:**
- ✅ **No critical bugs** (app crashes, data loss)
- ✅ **Email sync works reliably** (>95% success rate)
- ✅ **OAuth flows work** for Gmail and Outlook
- ✅ **Average satisfaction score** ≥ 7/10
- ✅ **At least 80% of testers** complete the full week
- ✅ **Major workflows tested** by real users

**Metrics to track:**
```
User Engagement:
- Daily active users: Target 80%+ (8+ of 10 testers)
- Average session length: Target 30+ minutes
- Tasks completed: Target 10+ tasks/user/day

Quality Metrics:
- Critical bugs: Max 0
- Major bugs: Max 2
- Minor bugs: Max 10
- Average satisfaction: Min 7/10
- Email sync success rate: Min 95%

Feature Usage:
- Email reading: 100% of testers
- Email composing: 80% of testers
- Email search: 60% of testers
- Multi-account: 40% of testers (if applicable)
```

### Expected Deliverables (End of Week 5)

- ✅ **Staging environment** fully operational
- ✅ **5-10 beta testers** completed full week
- ✅ **Daily feedback** collected from all testers
- ✅ **Bug reports** triaged and prioritized
- ✅ **Critical fixes** deployed
- ✅ **Final feedback sessions** completed
- ✅ **UAT report** documenting:
  - User satisfaction scores
  - Bugs found and fixed
  - Feature usage statistics
  - Recommendations for production release

### UAT Report Template

```markdown
# UAT Report - MIC Desktop (Week 5)

## Executive Summary
[2-3 paragraphs summarizing results]

## Participant Demographics
- Total testers: 10
- Completed full week: 9 (90%)
- Email providers:
  - Gmail: 8 testers
  - Outlook: 4 testers
  - Other: 2 testers
- Platforms:
  - Windows: 6 testers
  - macOS: 3 testers
  - Linux: 1 tester

## Usage Statistics
- Total sessions: 315
- Average session length: 42 minutes
- Total emails processed: 2,847
  - Read: 2,103
  - Sent: 412
  - Archived: 198
  - Deleted: 134

## User Satisfaction
- Overall average: 8.2/10
- Day 1: 6.8/10 (learning curve)
- Day 7: 8.9/10 (improvement over time)

Daily breakdown:
| Day | Avg Score | Testers |
|-----|-----------|---------|
| 1   | 6.8       | 10      |
| 2   | 7.4       | 10      |
| 3   | 7.9       | 9       |
| 4   | 8.1       | 9       |
| 5   | 8.3       | 9       |
| 6   | 8.6       | 9       |
| 7   | 8.9       | 9       |

## Bugs Found
### Critical (0)
None

### Major (2)
1. **Email sync fails for accounts with 10,000+ emails**
   - Status: Fixed on Day 3
   - Resolution: Implemented pagination

2. **Attachment download fails for files >25MB**
   - Status: Fixed on Day 5
   - Resolution: Increased buffer size

### Minor (8)
[List of minor bugs with status]

## Feature Feedback

### What Worked Well
1. "Email search is incredibly fast!" (7 mentions)
2. "OAuth setup was seamless" (6 mentions)
3. "Love the unified inbox" (5 mentions)
4. "Notification system is perfect" (4 mentions)

### What Needs Improvement
1. "Compose window could be larger" (4 mentions)
2. "Need keyboard shortcuts" (3 mentions)
3. "Dark mode is too dark" (2 mentions)

## Recommendations for Production

### Must Fix Before Launch
1. ✅ Pagination for large inboxes (FIXED)
2. ✅ Large attachment handling (FIXED)
3. ⏳ Add keyboard shortcuts (prioritized for v1.1)

### Nice to Have
1. Compose window resize
2. Dark mode adjustments
3. Email templates

## Conclusion
[2-3 paragraphs with go/no-go recommendation]

**Recommendation: PROCEED TO PRODUCTION** ✅
```

---

# Phase 5: Production Deployment

## Timeline: Week 6-7 (Mar 15-28, 2026)

### Objective
Build, package, sign, and distribute production-ready installers for Windows, macOS, and Linux.

## Week 6: Build & Package (Mar 15-21)

### Day 1-2: Production Build

**Clean the codebase:**
```bash
# Remove all test code references
git clean -fdx

# Ensure no debug code
grep -r "TODO\|FIXME\|DEBUG" src/
# Fix any remaining debug code

# Update version numbers
# Update src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj:
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

**Build for Windows (x64):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o ./publish/windows-x64

# Result: publish/windows-x64/MIC.Desktop.exe (~120MB)
```

**Build for Windows (ARM64):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r win-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish/windows-arm64
```

**Build for macOS (x64):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:CreatePackage=true \
  -o ./publish/macos-x64

# Result: publish/macos-x64/MIC.Desktop.app
```

**Build for macOS (ARM64 - Apple Silicon):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:CreatePackage=true \
  -o ./publish/macos-arm64
```

**Build for Linux (x64):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish/linux-x64

# Result: publish/linux-x64/MIC.Desktop
```

**Build for Linux (ARM64):**
```bash
dotnet publish src/MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained true \
  -o ./publish/linux-arm64
```

**Verify builds:**
```bash
# Check file sizes
du -sh ./publish/*/MIC.Desktop*

# Test each build
./publish/windows-x64/MIC.Desktop.exe --version
./publish/macos-x64/MIC.Desktop.app/Contents/MacOS/MIC.Desktop --version
./publish/linux-x64/MIC.Desktop --version
```

### Day 3-4: Create Installers

#### Windows Installer (Inno Setup)

**Install Inno Setup:**
```
Download from: https://jrsoftware.org/isdl.php
Install Inno Setup 6.2.2 or later
```

**Create installer script:**
```inno
; installer.iss
#define MyAppName "MIC Desktop"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Company Name"
#define MyAppURL "https://mic-desktop.com"
#define MyAppExeName "MIC.Desktop.exe"

[Setup]
AppId={{YOUR-GUID-HERE}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/support
AppUpdatesURL={#MyAppURL}/updates
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
OutputDir=installers
OutputBaseFilename=MIC-Desktop-Setup-{#MyAppVersion}-win-x64
SetupIconFile=assets\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\windows-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\windows-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Custom code for installation
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Check if .NET 8 is installed
  if not IsDotNetInstalled(net80, 0) then
  begin
    MsgBox('.NET 8.0 Runtime is required. Please install it first.', mbError, MB_OK);
    Result := False;
  end;
end;
```

**Compile installer:**
```bash
# Windows
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss

# Result: installers/MIC-Desktop-Setup-1.0.0-win-x64.exe
```

#### macOS Installer (DMG)

**Install create-dmg:**
```bash
brew install create-dmg
```

**Create DMG:**
```bash
# Create temporary folder for DMG contents
mkdir -p dmg-temp
cp -r publish/macos-x64/MIC.Desktop.app dmg-temp/

# Create Applications symlink
ln -s /Applications dmg-temp/Applications

# Create DMG
create-dmg \
  --volname "MIC Desktop" \
  --volicon "assets/icon.icns" \
  --window-pos 200 120 \
  --window-size 800 400 \
  --icon-size 100 \
  --icon "MIC.Desktop.app" 200 190 \
  --hide-extension "MIC.Desktop.app" \
  --app-drop-link 600 185 \
  --background "assets/dmg-background.png" \
  "installers/MIC-Desktop-1.0.0-macos-x64.dmg" \
  "dmg-temp/"

# Cleanup
rm -rf dmg-temp
```

**Create universal macOS installer (x64 + ARM64):**
```bash
# Merge x64 and ARM64 builds into universal binary
lipo -create \
  publish/macos-x64/MIC.Desktop.app/Contents/MacOS/MIC.Desktop \
  publish/macos-arm64/MIC.Desktop.app/Contents/MacOS/MIC.Desktop \
  -output MIC.Desktop.universal

# Copy universal binary back
cp MIC.Desktop.universal publish/macos-universal/MIC.Desktop.app/Contents/MacOS/MIC.Desktop

# Create universal DMG
create-dmg \
  --volname "MIC Desktop" \
  "installers/MIC-Desktop-1.0.0-macos-universal.dmg" \
  "publish/macos-universal/"
```

#### Linux Installer (AppImage)

**Install AppImage tools:**
```bash
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage
```

**Create AppImage structure:**
```bash
mkdir -p MIC.AppDir/usr/bin
mkdir -p MIC.AppDir/usr/share/applications
mkdir -p MIC.AppDir/usr/share/icons/hicolor/256x256/apps

# Copy executable
cp publish/linux-x64/MIC.Desktop MIC.AppDir/usr/bin/

# Copy dependencies
cp -r publish/linux-x64/* MIC.AppDir/usr/bin/

# Create desktop file
cat > MIC.AppDir/usr/share/applications/mic-desktop.desktop << EOF
[Desktop Entry]
Type=Application
Name=MIC Desktop
Comment=Unified Email Client
Exec=MIC.Desktop
Icon=mic-desktop
Categories=Network;Email;
Terminal=false
EOF

# Copy icon
cp assets/icon-256.png MIC.AppDir/usr/share/icons/hicolor/256x256/apps/mic-desktop.png

# Create AppRun script
cat > MIC.AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin:${PATH}"
exec "${HERE}/usr/bin/MIC.Desktop" "$@"
EOF
chmod +x MIC.AppDir/AppRun

# Symlinks for AppImage
ln -s usr/share/applications/mic-desktop.desktop MIC.AppDir/
ln -s usr/share/icons/hicolor/256x256/apps/mic-desktop.png MIC.AppDir/
```

**Build AppImage:**
```bash
./appimagetool-x86_64.AppImage MIC.AppDir installers/MIC-Desktop-1.0.0-linux-x64.AppImage

# Result: installers/MIC-Desktop-1.0.0-linux-x64.AppImage
```

**Create .deb package (Debian/Ubuntu):**
```bash
mkdir -p deb-package/DEBIAN
mkdir -p deb-package/usr/bin
mkdir -p deb-package/usr/share/applications
mkdir -p deb-package/usr/share/icons/hicolor/256x256/apps

# Copy files
cp -r publish/linux-x64/* deb-package/usr/bin/
cp assets/mic-desktop.desktop deb-package/usr/share/applications/
cp assets/icon-256.png deb-package/usr/share/icons/hicolor/256x256/apps/mic-desktop.png

# Create control file
cat > deb-package/DEBIAN/control << EOF
Package: mic-desktop
Version: 1.0.0
Section: net
Priority: optional
Architecture: amd64
Depends: libc6, libgcc-s1, libstdc++6
Maintainer: Your Name <your.email@example.com>
Description: MIC Desktop - Unified Email Client
 MIC Desktop is a modern, unified email client that supports
 multiple email accounts including Gmail and Outlook.
EOF

# Build .deb
dpkg-deb --build deb-package installers/mic-desktop_1.0.0_amd64.deb
```

### Day 5: Code Signing

#### Windows Code Signing

**Obtain code signing certificate:**
```
1. Purchase certificate from DigiCert, Sectigo, or similar CA
2. Receive .pfx file and password
3. Store securely (DO NOT commit to Git!)
```

**Sign the installer:**
```bash
# Install Windows SDK for signtool.exe
# Or use signtool from Visual Studio

# Sign the installer
signtool sign \
  /f "certificates/codesign.pfx" \
  /p "CERTIFICATE_PASSWORD" \
  /t http://timestamp.digicert.com \
  /fd SHA256 \
  /d "MIC Desktop" \
  /du "https://mic-desktop.com" \
  installers/MIC-Desktop-Setup-1.0.0-win-x64.exe

# Verify signature
signtool verify /pa installers/MIC-Desktop-Setup-1.0.0-win-x64.exe
```

#### macOS Code Signing & Notarization

**Obtain Apple Developer Certificate:**
```
1. Enroll in Apple Developer Program ($99/year)
2. Create Developer ID Application certificate
3. Download and install in Keychain
```

**Sign the app:**
```bash
# Sign the app bundle
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: Your Name (TEAM_ID)" \
  --options runtime \
  --entitlements entitlements.plist \
  publish/macos-universal/MIC.Desktop.app

# Verify signature
codesign --verify --deep --strict --verbose=2 \
  publish/macos-universal/MIC.Desktop.app
```

**Create entitlements.plist:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
    <key>com.apple.security.network.client</key>
    <true/>
    <key>com.apple.security.network.server</key>
    <true/>
</dict>
</plist>
```

**Notarize with Apple:**
```bash
# Create DMG first
create-dmg ... (as above)

# Submit for notarization
xcrun notarytool submit \
  installers/MIC-Desktop-1.0.0-macos-universal.dmg \
  --apple-id "your-apple-id@example.com" \
  --team-id "YOUR_TEAM_ID" \
  --password "app-specific-password" \
  --wait

# Staple notarization ticket to DMG
xcrun stapler staple installers/MIC-Desktop-1.0.0-macos-universal.dmg

# Verify notarization
spctl -a -vv -t install installers/MIC-Desktop-1.0.0-macos-universal.dmg
```

**Generate app-specific password:**
```
1. Go to appleid.apple.com
2. Sign in with Apple ID
3. Security → App-Specific Passwords
4. Generate password
5. Save securely
```

### Day 6-7: Verification & Testing

**Test installers on clean machines:**

**Windows:**
```
1. Spin up clean Windows 11 VM
2. Download installer
3. Run MIC-Desktop-Setup-1.0.0-win-x64.exe
4. Verify installation completes
5. Launch application
6. Test basic functionality:
   - Create account
   - Add Gmail account
   - Sync emails
   - Send email
7. Verify no errors
```

**macOS:**
```
1. Spin up clean macOS VM (or use separate Mac)
2. Download DMG
3. Open MIC-Desktop-1.0.0-macos-universal.dmg
4. Drag to Applications
5. Launch application
6. Verify Gatekeeper allows execution
7. Test basic functionality (same as Windows)
```

**Linux:**
```
1. Spin up clean Ubuntu 22.04 VM
2. Download AppImage
3. chmod +x MIC-Desktop-1.0.0-linux-x64.AppImage
4. Run ./MIC-Desktop-1.0.0-linux-x64.AppImage
5. Test basic functionality
```

**Create checksums:**
```bash
# SHA256 checksums for verification
cd installers

sha256sum MIC-Desktop-Setup-1.0.0-win-x64.exe > checksums.txt
sha256sum MIC-Desktop-1.0.0-macos-universal.dmg >> checksums.txt
sha256sum MIC-Desktop-1.0.0-linux-x64.AppImage >> checksums.txt
sha256sum mic-desktop_1.0.0_amd64.deb >> checksums.txt

# Sign checksums file
gpg --clearsign checksums.txt
```

## Week 7: Distribution & Launch (Mar 22-28)

### Day 1-2: Upload to Distribution Channels

#### 1. Direct Download (Website)

**Upload to cloud storage (AWS S3):**
```bash
# Install AWS CLI
pip install awscli

# Configure AWS credentials
aws configure

# Create S3 bucket
aws s3 mb s3://mic-desktop-downloads

# Set bucket policy for public read
aws s3api put-bucket-policy \
  --bucket mic-desktop-downloads \
  --policy file://bucket-policy.json

# Upload installers
aws s3 sync installers/ s3://mic-desktop-downloads/v1.0.0/ \
  --acl public-read

# Set up CloudFront CDN (optional, for faster downloads)
aws cloudfront create-distribution \
  --origin-domain-name mic-desktop-downloads.s3.amazonaws.com
```

**Update website download page:**
```html
<!-- downloads.html -->
<section class="downloads">
  <h1>Download MIC Desktop</h1>
  
  <div class="download-cards">
    <div class="card">
      <h2>Windows</h2>
      <p>Windows 10/11 (x64)</p>
      <a href="https://downloads.mic-desktop.com/v1.0.0/MIC-Desktop-Setup-1.0.0-win-x64.exe" 
         class="download-btn">
        Download for Windows
      </a>
      <p class="size">122 MB</p>
      <p class="sha">SHA256: abc123...</p>
    </div>
    
    <div class="card">
      <h2>macOS</h2>
      <p>macOS 11+ (Universal)</p>
      <a href="https://downloads.mic-desktop.com/v1.0.0/MIC-Desktop-1.0.0-macos-universal.dmg" 
         class="download-btn">
        Download for Mac
      </a>
      <p class="size">95 MB</p>
      <p class="sha">SHA256: def456...</p>
    </div>
    
    <div class="card">
      <h2>Linux</h2>
      <p>Ubuntu/Debian (AppImage)</p>
      <a href="https://downloads.mic-desktop.com/v1.0.0/MIC-Desktop-1.0.0-linux-x64.AppImage" 
         class="download-btn">
        Download AppImage
      </a>
      <p class="size">105 MB</p>
      <p class="sha">SHA256: ghi789...</p>
    </div>
  </div>
  
  <div class="install-instructions">
    <h3>Installation Instructions</h3>
    <!-- Add platform-specific instructions -->
  </div>
</section>
```

#### 2. Microsoft Store (Windows)

**Package as MSIX:**
```bash
# Install MSIX Packaging Tool
# Download from Microsoft Store

# Or use command line:
makeappx pack /d publish/windows-x64 /p MIC-Desktop-1.0.0.msix

# Sign MSIX
signtool sign \
  /f "certificates/codesign.pfx" \
  /p "CERTIFICATE_PASSWORD" \
  /fd SHA256 \
  MIC-Desktop-1.0.0.msix
```

**Submit to Microsoft Partner Center:**
```
1. Go to partner.microsoft.com
2. Create new app submission
3. Upload MSIX package
4. Fill in app metadata:
   - App name: MIC Desktop
   - Category: Productivity
   - Description: [Your description]
   - Screenshots: [Add 4+ screenshots]
   - Privacy policy URL
   - Support URL
5. Set pricing: Free or Paid
6. Submit for certification
7. Wait 3-7 days for approval
```

#### 3. Mac App Store (macOS)

**Prepare for App Store:**
```bash
# Install Xcode from App Store

# Create App Store build
xcodebuild -project MIC.Desktop.xcodeproj \
  -scheme MIC.Desktop \
  -configuration Release \
  -archivePath MIC.Desktop.xcarchive \
  archive

# Export for App Store
xcodebuild -exportArchive \
  -archivePathMIC.Desktop.xcarchive \
  -exportPath ./export \
  -exportOptionsPlist ExportOptions.plist
```

**Submit to App Store Connect:**
```
1. Go to appstoreconnect.apple.com
2. Create new app
3. Upload build using Transporter app
4. Fill in app metadata:
   - App name: MIC Desktop
   - Category: Productivity
   - Description
   - Screenshots (required for macOS)
   - Privacy policy
5. Submit for review
6. Wait 2-7 days for approval
```

#### 4. Linux Package Repositories

**Publish to Snap Store:**
```bash
# Install snapcraft
sudo snap install snapcraft --classic

# Create snapcraft.yaml
# Build snap
snapcraft

# Login to Snap Store
snapcraft login

# Upload snap
snapcraft upload mic-desktop_1.0.0_amd64.snap --release=stable
```

**Publish to Flathub:**
```bash
# Create Flatpak manifest
# Submit PR to flathub/flathub repository
# Wait for approval and merge
```

### Day 3-4: Marketing & Launch

**Launch announcement:**

**Email to beta testers:**
```
Subject: 🎉 MIC Desktop is Live!

Hi [Name],

Thank you for being a beta tester! MIC Desktop v1.0 is now available to everyone.

Download now:
https://mic-desktop.com/download

Your feedback helped us:
✅ Fix 12 bugs before launch
✅ Improve email sync performance by 40%
✅ Add keyboard shortcuts
✅ Polish the UI

As promised, here's your free Pro license:
[LICENSE_KEY]

Keep the feedback coming! We have exciting features planned for v1.1.

Thanks again for your help! 🙏

The MIC Desktop Team
```

**Social media announcements:**
```
Twitter/X:
🎉 Introducing MIC Desktop v1.0!

A unified email client for Gmail, Outlook & more
✅ Fast, native desktop app
✅ Multi-account support
✅ Privacy-focused
✅ Free & open-source

Download: https://mic-desktop.com

#email #productivity #opensource

---

LinkedIn:
We're excited to announce the launch of MIC Desktop v1.0!

After 2 months of development and testing, we're releasing a unified email client that brings all your email accounts into one beautiful, privacy-focused application.

Key features:
• Native desktop app for Windows, macOS & Linux
• Gmail & Outlook support via OAuth 2.0
• Fast local search
• Multilingual support (5 languages)
• Free & open-source

Try it today: https://mic-desktop.com

Thanks to our amazing beta testers for their feedback! 🙏

---

Reddit (r/productivity, r/opensource):
Title: [Release] MIC Desktop v1.0 - Unified Email Client

I'm excited to share MIC Desktop, a new email client I've been working on!

**What it does:**
- Unified inbox for Gmail, Outlook, and more
- Native desktop app (Windows, macOS, Linux)
- Privacy-focused (your data stays local)
- Fast search and filtering
- Free & open-source

**Download:** https://mic-desktop.com
**GitHub:** https://github.com/yourorg/mic-desktop

I'd love your feedback! 🙏
```

**Product Hunt launch:**
```
1. Prepare Product Hunt submission:
   - Product name: MIC Desktop
   - Tagline: "Unified email client for Gmail, Outlook & more"
   - Description: [Detailed description]
   - Gallery: [Screenshots, demo video]
   - First comment: Founder's story
2. Schedule launch for weekday morning (Tuesday-Thursday)
3. Engage with comments throughout the day
4. Share launch with network for upvotes
```

### Day 5-7: First Installations & Support

**Setup support infrastructure:**

**1. Support email:**
```
Create: support@mic-desktop.com
Setup auto-responder:
---
Thank you for contacting MIC Desktop support!

We've received your message and will respond within 24 hours.

In the meantime:
• Check our documentation: https://docs.mic-desktop.com
• Search known issues: https://github.com/yourorg/mic-desktop/issues
• Join our community: https://discord.gg/mic-desktop

Best regards,
MIC Desktop Team
---
```

**2. Documentation site:**
```
Create comprehensive docs:
• Installation guides (Windows, macOS, Linux)
• Getting started tutorial
• Adding email accounts (Gmail, Outlook)
• Troubleshooting common issues
• FAQ
• Keyboard shortcuts
• Privacy policy
• Terms of service
```

**3. Community channels:**
```
Setup:
• Discord server for community support
• GitHub Discussions for feature requests
• GitHub Issues for bug reports
```

**Monitor first installations:**

**Day 5-7 Daily Checklist:**
```
Morning (9 AM):
☐ Check download statistics
☐ Review new support tickets
☐ Monitor error logs (Sentry)
☐ Check social media mentions
☐ Respond to community questions

Afternoon (2 PM):
☐ Review crash reports
☐ Triage new bug reports
☐ Deploy hot fixes if critical issues found
☐ Update documentation based on common questions

Evening (6 PM):
☐ Daily metrics summary
☐ Plan tomorrow's priorities
☐ Respond to remaining support tickets
```

**Key metrics to track:**

```
Downloads:
• Total downloads by platform
• Download-to-install conversion rate
• Active installations (phone home if implemented)

Usage:
• Daily active users
• Email accounts connected
• Emails synced
• Feature usage (compose, search, etc.)

Quality:
• Crash rate (< 0.1% target)
• Error rate (< 1% target)
• Support ticket volume
• Average satisfaction rating

Engagement:
• Social media mentions
• GitHub stars
• Discord members
• Product Hunt ranking
```

**Success criteria (End of Week 7):**

```
Downloads:
☐ 1,000+ total downloads
☐ 500+ active installations
☐ < 5% uninstall rate

Quality:
☐ < 0.1% crash rate
☐ No critical bugs
☐ < 10 support tickets/day
☐ Average rating ≥ 4/5

Community:
☐ 100+ GitHub stars
☐ 50+ Discord members
☐ #1-10 on Product Hunt
☐ Positive sentiment on social media
```

---

# Phase 6: Ongoing Maintenance & Updates

## Post-Launch (Week 8+)

### Objective
Maintain production software, fix bugs, and plan future releases.

### Weekly Routine

**Monday:**
```
☐ Review weekend metrics
☐ Triage new bug reports
☐ Plan week's priorities
☐ Update roadmap based on feedback
```

**Tuesday-Thursday:**
```
☐ Fix bugs
☐ Implement small features
☐ Improve documentation
☐ Respond to support tickets
```

**Friday:**
```
☐ Code review
☐ Write tests for new features
☐ Deploy fixes to staging
☐ Prepare release notes
```

### Monthly Routine

**Week 1:**
```
☐ Plan month's features (v1.1, v1.2, etc.)
☐ Review metrics from previous month
☐ Community feedback review
```

**Week 2-3:**
```
☐ Implement planned features
☐ Write tests
☐ Update documentation
```

**Week 4:**
```
☐ Internal testing
☐ Beta testing (if major features)
☐ Prepare release
☐ Deploy monthly update
```

### Version Update Process

**For bug fixes (v1.0.1, v1.0.2):**
```
1. Fix bug in development branch
2. Write test to prevent regression
3. Test locally
4. Deploy to staging
5. Smoke test in staging
6. Build new installers
7. Upload to distribution channels
8. Announce via in-app update notification
```

**For minor updates (v1.1, v1.2):**
```
1. Develop features for 2-3 weeks
2. Internal testing (1 week)
3. Beta testing with opt-in users (1 week)
4. Fix critical issues
5. Build release candidates
6. Final testing
7. Build production installers
8. Code sign
9. Upload to distribution channels
10. Marketing announcement
11. Monitor for issues
```

**For major updates (v2.0):**
```
1. Plan features (1 month)
2. Develop (2-3 months)
3. Alpha testing internal (2 weeks)
4. Beta testing public (4 weeks)
5. Release candidate (2 weeks)
6. Production release
7. Major marketing campaign
```

### Auto-Update System

**Implement auto-updater:**
```csharp
// Add to your application
public class UpdateService
{
    private const string UpdateUrl = "https://api.mic-desktop.com/updates";
    
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var response = await _httpClient.GetFromJsonAsync<UpdateInfo>(
            $"{UpdateUrl}/latest?platform={RuntimeInformation.OSDescription}"
        );
        
        if (response?.Version > currentVersion)
            return response;
        
        return null;
    }
    
    public async Task DownloadAndInstallUpdateAsync(UpdateInfo update)
    {
        // Download installer
        var installer = await DownloadAsync(update.DownloadUrl);
        
        // Verify signature
        if (!VerifySignature(installer, update.Signature))
            throw new SecurityException("Invalid update signature");
        
        // Install update
        Process.Start(installer);
        Application.Current.Shutdown();
    }
}
```

**Update API endpoint:**
```json
GET https://api.mic-desktop.com/updates/latest?platform=windows

Response:
{
  "version": "1.0.1",
  "releaseDate": "2026-04-01",
  "downloadUrl": "https://downloads.mic-desktop.com/v1.0.1/MIC-Desktop-Setup-1.0.1-win-x64.exe",
  "signature": "SHA256_SIGNATURE_HERE",
  "releaseNotes": "Bug fixes and performance improvements",
  "required": false,
  "changelog": [
    "Fixed email sync issue for large inboxes",
    "Improved search performance",
    "Updated translations"
  ]
}
```

---

# Summary Timeline

```
┌─────────────────────────────────────────────────────────────────┐
│                        COMPLETE TIMELINE                        │
└─────────────────────────────────────────────────────────────────┘

Week 1-2 (Feb 7-21):     Unit Testing → 80% coverage with mocks
Week 3 (Feb 22-28):      Integration Testing → Real components
Week 4 (Mar 1-7):        E2E Testing → Complete user workflows  
Week 5 (Mar 8-14):       UAT → Real users, real data
Week 6 (Mar 15-21):      Build & Package → Create installers
Week 7 (Mar 22-28):      Distribution & Launch → LIVE SOFTWARE!
Week 8+:                 Maintenance & Updates → Ongoing support

┌─────────────────────────────────────────────────────────────────┐
│                   FROM MOCKS TO PRODUCTION                      │
└─────────────────────────────────────────────────────────────────┘

Mocks (Week 1-2)         → Fast, isolated testing
↓
Real Components (Week 3)  → Database, email servers
↓
Full Application (Week 4) → UI automation, workflows
↓
Real Users (Week 5)       → Beta testing, feedback
↓
Production Build (Week 6) → Signed installers
↓
End Users (Week 7)        → 🎉 INSTALLED SOFTWARE!
```

---

# Appendix

## A. Required Tools & Services

### Development Tools
- ✅ .NET 8 SDK
- ✅ Visual Studio Code or Visual Studio
- ✅ Git
- ✅ Docker (for test databases)

### Testing Tools
- ✅ xUnit.net
- ✅ Moq (mocking framework)
- ✅ Coverlet (code coverage)
- ✅ ReportGenerator (coverage reports)
- ✅ Playwright (browser automation)
- ✅ Avalonia.Headless (UI testing)

### Build & Packaging Tools
- ✅ Inno Setup (Windows installer)
- ✅ create-dmg (macOS DMG)
- ✅ appimagetool (Linux AppImage)
- ✅ dpkg (Linux .deb packages)

### Code Signing
- ✅ Windows: Code signing certificate + signtool
- ✅ macOS: Apple Developer account + codesign
- ✅ Linux: GPG key (optional)

### Distribution Services
- ✅ AWS S3 or Azure Blob (file hosting)
- ✅ CloudFront or Azure CDN (optional)
- ✅ Microsoft Partner Center (Windows Store)
- ✅ Apple App Store Connect (Mac App Store)

### Monitoring & Analytics
- ✅ Sentry (error tracking)
- ✅ Grafana (metrics dashboard)
- ✅ Google Analytics (usage tracking)

## B. Estimated Costs

```
Development (assuming solo developer):
• Time: 7 weeks full-time
• Opportunity cost: $0 (your time)

Tools & Services:
• .NET, Visual Studio Code: FREE
• GitHub (private repo): FREE or $4/month
• Apple Developer Program: $99/year (for macOS)
• Code signing certificate: $100-400/year
• Cloud storage (AWS S3): ~$5-20/month
• Domain name: ~$12/year
• Email service (support@): ~$6/month
• Sentry (error tracking): FREE tier or $26/month
• Total Year 1: ~$300-800

Marketing (optional):
• Product Hunt: FREE
• Social media: FREE
• Paid ads: $0-1000+ (optional)
```

## C. Risk Mitigation

**Technical Risks:**
```
Risk: Email sync fails for some providers
Mitigation: Comprehensive integration tests, beta testing

Risk: Code signing issues delay release
Mitigation: Setup signing early in Week 6

Risk: Performance issues at scale
Mitigation: Load testing during integration phase

Risk: Platform-specific bugs
Mitigation: Test on multiple machines per platform
```

**Business Risks:**
```
Risk: Low adoption
Mitigation: Marketing, community building, solve real problems

Risk: Support overwhelm
Mitigation: Good documentation, community support, FAQ

Risk: Competitor launches similar product
Mitigation: Move fast, unique features, great UX

Risk: App Store rejection
Mitigation: Follow guidelines carefully, test on TestFlight/Beta
```

## D. Success Metrics

**Technical Success:**
```
✅ 80% code coverage
✅ < 0.1% crash rate
✅ < 1 second app startup time
✅ < 100MB installer size
✅ All tests passing (500+ tests)
```

**Business Success:**
```
✅ 1,000+ downloads in first month
✅ 4+ star average rating
✅ 50% week-1 retention rate
✅ < 10 support tickets/day
✅ Positive community sentiment
```

**User Success:**
```
✅ Users can connect email accounts in < 2 minutes
✅ Email sync completes in < 30 seconds
✅ Search returns results in < 500ms
✅ Users complete primary workflows without support
✅ ≥ 7/10 satisfaction score
```

---

# 🎯 You Are Here

```
┌─────────────────────────────────────────────────────────────────┐
│ CURRENT STATUS: Week 1, Day 4 - Unit Testing Phase             │
│ Coverage: 19.3% → Target: 80%                                  │
│ Tests: 368 (all passing) → Target: 500+                        │
│ Focus: Desktop ViewModels (7.7% → 80%)                         │
└─────────────────────────────────────────────────────────────────┘

Next Steps:
1. Continue adding EmailInboxViewModel tests (12 more today)
2. Maintain daily targets (15-30 tests/day)
3. Track coverage daily
4. Stay focused on Desktop.Avalonia (biggest gap)

You're 10 days away from 80% coverage!
Then 3 weeks to integration/E2E testing
Then 3 weeks to production deployment
Then SOFTWARE INSTALLED ON COMPUTERS! 🎉
```

---

**END OF ROADMAP DOCUMENT**

This roadmap will guide you from where you are today (mocks, 19.3% coverage) all the way to production-ready software installed on end-user computers. Save this document and refer to it throughout the journey!