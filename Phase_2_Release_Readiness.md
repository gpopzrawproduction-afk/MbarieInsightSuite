# PHASE 2: RELEASE READINESS & PRODUCTION DEPLOYMENT

## 🎯 Phase 2 Mission

**Transform your battle-tested application into a production-ready, distributable product with enterprise-grade release infrastructure.**

**Current State:** ✅ Solid foundation (163 passing tests, 12.3% coverage)  
**Phase 2 Goal:** 🚀 Production-ready with signing, updates, monitoring, and 80%+ coverage

---

## 📊 Phase 1 Completion Summary

### Achievements 🏆
- ✅ **163 tests passing** (149 unit + 14+ integration)
- ✅ **Real data validation** via Testcontainers PostgreSQL
- ✅ **Zero mocks in integration tests**
- ✅ **E2E framework established**
- ✅ **Phase 0-1 foundation stable**

### Current Metrics
| Metric | Current | Phase 2 Target |
|--------|---------|----------------|
| **Line Coverage** | 12.3% | 80%+ |
| **Branch Coverage** | 10.6% | 70%+ |
| **Tests Passing** | 163/163 ✅ | 250+ |
| **Test Execution** | TBD | < 3 minutes |
| **Assemblies** | 8 | 8 |
| **Classes** | 301 | 301 |
| **Coverable Lines** | 27,930 | 27,930 |

### Coverage Analysis
**Current Coverage:** 3,441 / 27,930 lines (12.3%)  
**Gap to Target:** Need 22,359 more covered lines to reach 80%  
**Strategy:** Aggressive test expansion + critical path prioritization

---

## 🎯 Phase 2 Objectives

### 1. **Coverage Expansion** (Weeks 1-2)
- Increase line coverage from 12.3% to 80%+
- Focus on critical paths first (auth, email, data persistence)
- Add 90+ new unit tests
- Add 30+ new integration tests
- Implement 15+ E2E scenarios

### 2. **Release Infrastructure** (Week 3)
- Code signing setup (EV certificate)
- MSIX packaging automation
- Auto-update mechanism
- Versioning and changelog automation
- Distribution channels (Store, direct download)

### 3. **Production Monitoring** (Week 4)
- Application Insights integration
- Error tracking (Sentry)
- Performance monitoring
- User analytics (privacy-compliant)
- Health checks and diagnostics

### 4. **Documentation & Polish** (Week 5)
- User documentation
- API documentation
- Deployment guides
- Troubleshooting guides
- Marketing materials

---

## 📋 PHASE 2 WORK PACKAGES

## WP-2.1: Coverage Acceleration (Days 1-10)

### Goal: 12.3% → 80% Coverage

**Strategy:** Focus on high-impact, low-coverage areas

#### Step 1: Identify Coverage Gaps (Day 1)

**Analyze your coverage report to find:**
```powershell
# You already have the HTML report
# Open it and identify:
# 1. Files with 0% coverage
# 2. Critical files with < 20% coverage
# 3. High-value files (auth, email, data) with low coverage

# Create priority list
@"
# Coverage Priority List

## Critical (Must reach 90%+)
- [ ] UserSessionService (authentication)
- [ ] EmailService (core functionality)
- [ ] DatabaseContext (data layer)
- [ ] SecurityService (encryption, permissions)

## High Priority (Target 80%+)
- [ ] NotificationService
- [ ] SettingsService
- [ ] KnowledgeBaseService
- [ ] EmailSyncService

## Medium Priority (Target 60%+)
- [ ] ViewModels (UI logic)
- [ ] Command handlers
- [ ] Query handlers

## Low Priority (Target 40%+)
- [ ] Utility classes
- [ ] Extension methods
- [ ] Constants/configs
"@ | Out-File "COVERAGE_PRIORITIES.md"
```

#### Step 2: Expand Unit Test Coverage (Days 2-5)

**Add 90+ unit tests focusing on uncovered code paths**

**Example: UserSessionService expanded tests**
```csharp
// MIC.Tests.Unit/Services/UserSessionServiceExpandedTests.cs
public class UserSessionServiceExpandedTests
{
    private readonly Mock<IDbContextFactory<MICDbContext>> _mockContextFactory;
    private readonly Mock<ILogger<UserSessionService>> _mockLogger;
    private readonly UserSessionService _sut;

    public UserSessionServiceExpandedTests()
    {
        _mockContextFactory = new Mock<IDbContextFactory<MICDbContext>>();
        _mockLogger = new Mock<ILogger<UserSessionService>>();
        _sut = new UserSessionService(_mockContextFactory.Object, _mockLogger.Object);
    }

    // Edge cases
    [Fact]
    public async Task CreateSession_WithInvalidUserId_ThrowsException()
    {
        // Arrange
        var invalidUserId = -1;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut.CreateSessionAsync(invalidUserId));
    }

    [Fact]
    public async Task CreateSession_WhenDatabaseFails_ThrowsDatabaseException()
    {
        // Test error handling
    }

    [Fact]
    public async Task GetSession_WithExpiredSession_ReturnsNull()
    {
        // Test expiry logic
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task CreateSession_WithBoundaryValues_HandlesCorrectly(int userId)
    {
        // Test boundary conditions
    }

    // Permission tests
    [Fact]
    public async Task ValidatePermission_WithValidPermission_ReturnsTrue()
    {
        // Test permission checking
    }

    [Fact]
    public async Task ValidatePermission_WithExpiredSession_ReturnsFalse()
    {
        // Test expired session permissions
    }

    [Fact]
    public async Task ValidatePermission_WithRevokedPermission_ReturnsFalse()
    {
        // Test revoked permissions
    }

    // Concurrent access tests
    [Fact]
    public async Task CreateSession_ConcurrentRequests_HandlesCorrectly()
    {
        // Test thread safety
    }

    // Cleanup tests
    [Fact]
    public async Task CleanupExpiredSessions_RemovesOldSessions()
    {
        // Test cleanup logic
    }

    [Fact]
    public async Task CleanupExpiredSessions_PreservesActiveSessions()
    {
        // Test selective cleanup
    }
}
```

**Coverage Expansion Checklist:**
- [ ] Add error handling tests for all services (30+ tests)
- [ ] Add boundary value tests (20+ tests)
- [ ] Add null/empty input tests (15+ tests)
- [ ] Add concurrent access tests (10+ tests)
- [ ] Add validation tests (15+ tests)

#### Step 3: Expand Integration Test Coverage (Days 6-8)

**Add 30+ integration tests for multi-component workflows**

**Example: Email workflow integration tests**
```csharp
// MIC.Tests.Integration/Workflows/EmailWorkflowIntegrationTests.cs
public class EmailWorkflowIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CompleteEmailWorkflow_FromFetchToArchive_WorksEndToEnd()
    {
        // Arrange - Setup test account and emails
        await SeedTestEmailAccountAsync();
        
        var emailService = GetService<IEmailService>();
        var notificationService = GetService<INotificationService>();
        
        // Act 1 - Fetch emails
        var fetchResult = await emailService.FetchNewEmailsAsync("test-account");
        
        // Assert 1 - Emails fetched
        fetchResult.Should().BeGreaterThan(0);
        
        // Verify database persistence
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var emails = await context.Emails.ToListAsync();
        emails.Should().HaveCountGreaterThan(0);
        
        // Act 2 - Mark as read
        var emailId = emails.First().Id;
        await emailService.MarkAsReadAsync(emailId);
        
        // Assert 2 - Status updated
        await context.Entry(emails.First()).ReloadAsync();
        emails.First().IsRead.Should().BeTrue();
        
        // Act 3 - Archive email
        await emailService.ArchiveEmailAsync(emailId);
        
        // Assert 3 - Moved to archive
        await context.Entry(emails.First()).ReloadAsync();
        emails.First().FolderId.Should().Be("Archive");
        
        // Assert 4 - Notification created
        var notifications = await context.Notifications
            .Where(n => n.Type == NotificationType.EmailArchived)
            .ToListAsync();
        notifications.Should().HaveCount(1);
    }

    [Fact]
    public async Task EmailSync_WithAttachments_DownloadsAndStores()
    {
        // Test attachment pipeline
    }

    [Fact]
    public async Task EmailSearch_WithFilters_ReturnsCorrectResults()
    {
        // Test search integration
    }

    [Fact]
    public async Task BulkEmailOperations_HandlesMultipleEmails()
    {
        // Test bulk operations
    }

    [Fact]
    public async Task EmailNotifications_TriggerOnNewEmail()
    {
        // Test notification integration
    }
}
```

**Integration Test Expansion Checklist:**
- [ ] Email workflows (fetch, read, archive, delete) - 10 tests
- [ ] Authentication flows (login, logout, refresh) - 8 tests
- [ ] Settings workflows (save, load, sync) - 6 tests
- [ ] Knowledge base workflows (upload, search, retrieve) - 6 tests

#### Step 4: Add E2E Scenario Tests (Days 9-10)

**Add 15+ E2E tests for critical user journeys**

**Example: Complete user journey**
```csharp
// MIC.Tests.E2E/UserJourneys/NewUserJourneyTests.cs
[AvaloniaFact]
public async Task NewUserJourney_CompleteWorkflow_Succeeds()
{
    // Arrange
    using var host = new ApplicationTestHost();
    await host.StartApplicationAsync();
    
    // Step 1: Registration
    var loginPage = new LoginPage(host.MainWindow);
    await loginPage.NavigateToRegisterAsync();
    await loginPage.RegisterAsync("newuser@example.com", "SecurePass123!");
    
    // Assert: On dashboard
    var dashboard = new DashboardPage(host.MainWindow);
    dashboard.Should().BeVisible();
    
    // Step 2: Add email account
    var settingsPage = await dashboard.NavigateToSettingsAsync();
    await settingsPage.AddEmailAccountAsync("Gmail");
    
    // Mock OAuth completion
    await MockOAuthSuccessAsync();
    
    // Step 3: Wait for email sync
    await Task.Delay(3000);
    
    // Assert: Emails synced
    var inboxPage = await dashboard.NavigateToInboxAsync();
    inboxPage.EmailCount.Should().BeGreaterThan(0);
    
    // Step 4: Interact with email
    await inboxPage.SelectFirstEmailAsync();
    var emailDetailPage = inboxPage.GetSelectedEmailDetail();
    emailDetailPage.Subject.Should().NotBeEmpty();
    
    // Step 5: Perform actions
    await emailDetailPage.MarkAsReadAsync();
    await emailDetailPage.ToggleFlagAsync();
    
    // Step 6: Verify notifications
    var notificationCenter = await dashboard.OpenNotificationCenterAsync();
    notificationCenter.NotificationCount.Should().BeGreaterThan(0);
    
    // Step 7: Settings persistence
    await settingsPage.ChangeThemeAsync("Dark");
    await host.RestartApplicationAsync();
    
    // Assert: Settings persisted
    var newSettings = await dashboard.NavigateToSettingsAsync();
    newSettings.CurrentTheme.Should().Be("Dark");
}

[AvaloniaFact]
public async Task SearchAndFilterJourney_FindsRelevantEmails()
{
    // Test search workflow
}

[AvaloniaFact]
public async Task NotificationInteractionJourney_RespondsToAlerts()
{
    // Test notification workflow
}

[AvaloniaFact]
public async Task MultiAccountJourney_SwitchesBetweenAccounts()
{
    // Test multi-account support
}
```

**E2E Test Checklist:**
- [ ] New user registration → setup → usage - 3 tests
- [ ] Existing user login → operations - 3 tests
- [ ] Email workflows - 4 tests
- [ ] Search and filter - 2 tests
- [ ] Notifications - 2 tests
- [ ] Settings management - 1 test

**WP-2.1 Deliverables:**
- [ ] 90+ new unit tests added
- [ ] 30+ new integration tests added
- [ ] 15+ E2E scenarios implemented
- [ ] Coverage increased from 12.3% to 80%+
- [ ] All tests passing (250+ total)

---

## WP-2.2: Release Infrastructure (Days 11-15)

### Goal: Production-ready packaging and distribution

#### Step 1: Code Signing Setup (Day 11)

**Acquire EV Code Signing Certificate**

1. **Choose a CA (Certificate Authority):**
   - DigiCert (recommended, $474/year)
   - Sectigo (formerly Comodo, $375/year)
   - SSL.com ($299/year)

2. **EV Certificate Requirements:**
   - Business registration documents
   - D-U-N-S number (business verification)
   - Physical USB token for storage (FIPS 140-2 Level 2)

3. **Install Certificate:**
```powershell
# After receiving your certificate on USB token
# Verify installation
certutil -store My

# Should show your EV certificate
```

4. **Sign Test Build:**
```powershell
# Sign executable
signtool sign /f "cert.pfx" /p "password" /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 "MyApp.exe"

# Verify signature
signtool verify /pa /v "MyApp.exe"
```

**Alternative for Development (Self-Signed):**
```powershell
# Create self-signed cert for testing
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=MBARIE Development" -CertStoreLocation Cert:\CurrentUser\My

# Export for signing
$pwd = ConvertTo-SecureString -String "DevPassword123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "DevCert.pfx" -Password $pwd

# Sign with self-signed (for testing only)
signtool sign /f "DevCert.pfx" /p "DevPassword123" /fd SHA256 "MyApp.exe"
```

#### Step 2: MSIX Packaging Automation (Days 12-13)

**Create MSIX packaging project**

1. **Add MSIX Project:**
```powershell
# Install MSIX Packaging Tool
winget install Microsoft.MsixPackagingTool

# Or add to solution manually
# Right-click solution → Add → New Project → Windows Application Packaging Project
```

2. **Configure Package Manifest:**

**File: `MIC.Package/Package.appxmanifest`**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Identity Name="com.mbarie.intelligenceconsole"
            Publisher="CN=Your Company"
            Version="1.0.0.0" />
  
  <Properties>
    <DisplayName>MBARIE Intelligence Console</DisplayName>
    <PublisherDisplayName>Haroon Ahmed Amin</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  
  <Resources>
    <Resource Language="en-US" />
  </Resources>
  
  <Applications>
    <Application Id="MBARIE" Executable="MIC.Presentation.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="MBARIE Intelligence Console"
                          Description="AI-powered email intelligence platform"
                          BackgroundColor="transparent"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
      
      <Extensions>
        <uap:Extension Category="windows.protocol">
          <uap:Protocol Name="mbarie" />
        </uap:Extension>
        <uap:Extension Category="windows.fileTypeAssociation">
          <uap:FileTypeAssociation Name="mbarieemail">
            <uap:SupportedFileTypes>
              <uap:FileType>.eml</uap:FileType>
              <uap:FileType>.msg</uap:FileType>
            </uap:SupportedFileTypes>
          </uap:FileTypeAssociation>
        </uap:Extension>
      </Extensions>
    </Application>
  </Applications>
  
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <Capability Name="internetClient" />
    <Capability Name="internetClientServer" />
    <Capability Name="privateNetworkClientServer" />
  </Capabilities>
</Package>
```

3. **Create Build Script:**

**File: `build-msix.ps1`**
```powershell
# MSIX Build Script
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0.0",
    [string]$CertificateThumbprint = "" # EV cert thumbprint
)

Write-Host "Building MSIX Package..." -ForegroundColor Cyan

# Step 1: Clean previous builds
Remove-Item -Path ".\artifacts\msix" -Recurse -Force -ErrorAction SilentlyContinue
New-Item -Path ".\artifacts\msix" -ItemType Directory -Force

# Step 2: Build application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet publish src/MIC.Presentation/MIC.Presentation.csproj `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output "./artifacts/publish"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Step 3: Update version in manifest
Write-Host "Updating version to $Version..." -ForegroundColor Yellow
$manifestPath = ".\MIC.Package\Package.appxmanifest"
[xml]$manifest = Get-Content $manifestPath
$manifest.Package.Identity.Version = $Version
$manifest.Save($manifestPath)

# Step 4: Create MSIX package
Write-Host "Creating MSIX package..." -ForegroundColor Yellow
$makeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"

& $makeAppx pack `
    /d ".\artifacts\publish" `
    /p ".\artifacts\msix\MBARIE_$Version.msix" `
    /l

if ($LASTEXITCODE -ne 0) {
    Write-Error "MSIX creation failed!"
    exit 1
}

# Step 5: Sign MSIX package
Write-Host "Signing MSIX package..." -ForegroundColor Yellow
if ($CertificateThumbprint) {
    $signTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
    
    & $signTool sign `
        /sha1 $CertificateThumbprint `
        /fd SHA256 `
        /tr http://timestamp.digicert.com `
        /td SHA256 `
        ".\artifacts\msix\MBARIE_$Version.msix"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Signing failed!"
        exit 1
    }
} else {
    Write-Warning "No certificate provided. Package is unsigned!"
}

Write-Host "`n✅ MSIX package created: .\artifacts\msix\MBARIE_$Version.msix" -ForegroundColor Green
```

4. **Automate in CI/CD:**

**File: `.github/workflows/release.yml`**
```yaml
name: Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build-msix:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Extract version from tag
      id: version
      shell: pwsh
      run: |
        $version = "${{ github.ref }}" -replace 'refs/tags/v', ''
        echo "VERSION=$version" >> $env:GITHUB_OUTPUT
    
    - name: Build MSIX
      shell: pwsh
      run: |
        .\build-msix.ps1 -Version "${{ steps.version.outputs.VERSION }}" -Configuration Release
    
    - name: Upload MSIX artifact
      uses: actions/upload-artifact@v3
      with:
        name: MSIX-Package
        path: ./artifacts/msix/*.msix
    
    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        files: ./artifacts/msix/*.msix
        draft: false
        prerelease: false
```

#### Step 3: Auto-Update Mechanism (Days 14-15)

**Implement Squirrel.Windows for auto-updates**

1. **Install Squirrel:**
```powershell
dotnet add package Squirrel.Windows
```

2. **Create Update Manager:**

**File: `MIC.Infrastructure.Services/Updates/UpdateService.cs`**
```csharp
using Squirrel;
using Microsoft.Extensions.Logging;

namespace MIC.Infrastructure.Services.Updates;

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync();
    Task<ReleaseEntry> DownloadUpdatesAsync(UpdateInfo updateInfo, IProgress<int> progress);
    Task ApplyUpdatesAsync(ReleaseEntry release);
    Task<string> GetCurrentVersionAsync();
}

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly string _updateUrl;
    
    public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _updateUrl = configuration["Updates:Url"] ?? "https://releases.mbarie.com";
    }
    
    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates from {Url}", _updateUrl);
            
            using var updateManager = await UpdateManager.GitHubUpdateManager(_updateUrl);
            var updateInfo = await updateManager.CheckForUpdate();
            
            if (updateInfo.ReleasesToApply.Any())
            {
                _logger.LogInformation("Update available: {Version}", 
                    updateInfo.FutureReleaseEntry.Version);
            }
            else
            {
                _logger.LogInformation("Application is up to date");
            }
            
            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            throw;
        }
    }
    
    public async Task<ReleaseEntry> DownloadUpdatesAsync(
        UpdateInfo updateInfo, 
        IProgress<int> progress)
    {
        try
        {
            _logger.LogInformation("Downloading update {Version}", 
                updateInfo.FutureReleaseEntry.Version);
            
            using var updateManager = await UpdateManager.GitHubUpdateManager(_updateUrl);
            
            await updateManager.DownloadReleases(
                updateInfo.ReleasesToApply,
                p => progress?.Report(p));
            
            _logger.LogInformation("Update downloaded successfully");
            return updateInfo.FutureReleaseEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download updates");
            throw;
        }
    }
    
    public async Task ApplyUpdatesAsync(ReleaseEntry release)
    {
        try
        {
            _logger.LogInformation("Applying update {Version}", release.Version);
            
            using var updateManager = await UpdateManager.GitHubUpdateManager(_updateUrl);
            await updateManager.ApplyReleases(updateInfo);
            
            _logger.LogInformation("Update applied. Restart required.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply updates");
            throw;
        }
    }
    
    public async Task<string> GetCurrentVersionAsync()
    {
        using var updateManager = await UpdateManager.GitHubUpdateManager(_updateUrl);
        return updateManager.CurrentlyInstalledVersion()?.ToString() ?? "Unknown";
    }
}
```

3. **Integrate in Startup:**

**File: `App.axaml.cs`**
```csharp
public override async void OnFrameworkInitializationCompleted()
{
    // ... existing initialization
    
    // Check for updates on startup
    await CheckForUpdatesAsync();
    
    base.OnFrameworkInitializationCompleted();
}

private async Task CheckForUpdatesAsync()
{
    try
    {
        var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
        var updateInfo = await updateService.CheckForUpdatesAsync();
        
        if (updateInfo.ReleasesToApply.Any())
        {
            var result = await ShowUpdateDialogAsync(updateInfo.FutureReleaseEntry.Version);
            
            if (result == UpdateDialogResult.InstallNow)
            {
                var progress = new Progress<int>(p => 
                    UpdateProgressBar.Value = p);
                
                var release = await updateService.DownloadUpdatesAsync(updateInfo, progress);
                await updateService.ApplyUpdatesAsync(release);
                
                // Restart application
                RestartApplication();
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Update check failed");
        // Silently fail - don't block app startup
    }
}
```

4. **Create Release Pipeline:**

**File: `create-release.ps1`**
```powershell
param(
    [string]$Version = "1.0.0",
    [string]$ReleaseNotes = "Bug fixes and improvements"
)

# Build application
dotnet publish -c Release -r win-x64 --self-contained true

# Create Squirrel release
$squirrel = ".\tools\Squirrel.exe"

& $squirrel --releasify .\artifacts\MBARIE.$Version.nupkg `
    --releaseDir .\releases `
    --setupIcon .\assets\icon.ico `
    --icon .\assets\icon.ico `
    --shortcut-locations Desktop,StartMenu `
    --no-msi

# Upload to release server
# ... upload logic
```

**WP-2.2 Deliverables:**
- [ ] EV code signing certificate acquired and configured
- [ ] MSIX packaging automated
- [ ] Auto-update mechanism implemented
- [ ] Release pipeline functional
- [ ] Distribution channels configured

---

## WP-2.3: Production Monitoring (Days 16-18)

### Goal: Real-time visibility into production health

#### Step 1: Application Insights Integration (Day 16)

```csharp
// Install package
dotnet add package Microsoft.ApplicationInsights.AspNetCore

// Configure in Program.cs
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnablePerformanceCounterCollectionModule = true;
});

// Track custom events
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
    }
    
    public void TrackException(Exception exception, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
    }
    
    public void TrackMetric(string metricName, double value)
    {
        _telemetryClient.TrackMetric(metricName, value);
    }
}
```

#### Step 2: Error Tracking with Sentry (Day 17)

```csharp
// Install Sentry
dotnet add package Sentry

// Configure
services.AddSentry(options =>
{
    options.Dsn = "https://your-dsn@sentry.io/project-id";
    options.Environment = "Production";
    options.TracesSampleRate = 1.0;
    options.AttachStacktrace = true;
});

// Global error handler
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var exception = (Exception)args.ExceptionObject;
    SentrySdk.CaptureException(exception);
};
```

#### Step 3: Health Checks (Day 18)

```csharp
// Add health checks
services.AddHealthChecks()
    .AddDbContextCheck<MICDbContext>()
    .AddCheck<EmailServiceHealthCheck>("email_service")
    .AddCheck<NotificationServiceHealthCheck>("notification_service");

// Expose endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**WP-2.3 Deliverables:**
- [ ] Application Insights configured
- [ ] Sentry error tracking active
- [ ] Health check endpoints implemented
- [ ] Monitoring dashboards created

---

## WP-2.4: Documentation & Polish (Days 19-20)

### User Documentation

**Create:**
- [ ] Getting Started Guide
- [ ] Feature Documentation
- [ ] Troubleshooting Guide
- [ ] FAQ
- [ ] Video Tutorials (optional)

### Developer Documentation

**Update:**
- [ ] README.md with current status
- [ ] ARCHITECTURE.md
- [ ] API Documentation
- [ ] Contributing Guide
- [ ] Deployment Guide

### Marketing Materials

**Prepare:**
- [ ] Feature list
- [ ] Screenshots
- [ ] Demo video
- [ ] Pricing page (if applicable)
- [ ] Website/landing page

---

## 📊 Phase 2 Success Metrics

| Metric | Target | Validation |
|--------|--------|------------|
| **Line Coverage** | 80%+ | Coverage report |
| **Branch Coverage** | 70%+ | Coverage report |
| **Total Tests** | 250+ | Test run output |
| **Test Execution** | < 3 min | CI pipeline |
| **Signed Release** | Yes | Signature verification |
| **Auto-Update** | Working | Update test |
| **Monitoring** | Active | Dashboard checks |
| **Documentation** | Complete | Review checklist |

---

## 🎯 Quick Start - Week 3 Tasks

### Monday: Coverage Sprint
```powershell
# Create 30 new unit tests
# Focus on services with < 20% coverage
# Target: +10% coverage by end of day
```

### Tuesday: Integration Tests
```powershell
# Create 10 new integration tests
# Focus on workflows
# Target: +5% coverage
```

### Wednesday: E2E Scenarios
```powershell
# Create 5 E2E tests
# Critical user journeys
# Target: +3% coverage
```

### Thursday: Code Signing
```powershell
# Setup EV certificate
# Sign test build
# Verify signature
```

### Friday: MSIX Packaging
```powershell
# Create MSIX project
# Build first package
# Test installation
```

---

## 🚀 Final Phase 2 Checklist

### Coverage Goals
- [ ] Line coverage ≥ 80%
- [ ] Branch coverage ≥ 70%
- [ ] 250+ tests passing
- [ ] < 3 minute test execution

### Release Infrastructure
- [ ] Code signing working
- [ ] MSIX packaging automated
- [ ] Auto-update functional
- [ ] Distribution channels ready

### Production Monitoring
- [ ] Application Insights configured
- [ ] Sentry error tracking active
- [ ] Health checks implemented
- [ ] Dashboards created

### Documentation
- [ ] User guide complete
- [ ] API docs updated
- [ ] Deployment guide ready
- [ ] Marketing materials prepared

---

**🎯 Start Phase 2 with aggressive coverage expansion! Your foundation is solid—now make it bulletproof! 🚀**