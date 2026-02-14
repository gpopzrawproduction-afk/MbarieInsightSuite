# Copilot Master Prompt
## Mbarie Insight Suite (MIC) - Comprehensive Developer Guide

**Last Updated:** February 13, 2026  
**Status:** Production-Ready (76%)  
**Framework:** .NET 9 / Avalonia / PostgreSQL / OpenAI

---

## 🎯 PROJECT OVERVIEW

**Mbarie Insight Suite** is an enterprise-grade desktop application for operational intelligence, real-time monitoring, AI-powered email analysis, and business decision support.

**Core Vision:**
- Real-time operational metrics and alerting
- Intelligent email management with AI analysis
- Chat-based AI assistance for business decisions
- Predictive analytics and trend forecasting
- Knowledge base for organizational insights

**Target Users:** Executives, operations managers, business intelligence professionals

**Deployment:** MSIX package for Windows (x64), 226 MB self-contained

---

## 🏗️ ARCHITECTURE & DESIGN PATTERNS

### Clean Architecture (4 Layers)

```
┌─────────────────────────────────────────────┐
│   MIC.Desktop.Avalonia (Presentation)       │  UI Layer
│   - Views, ViewModels, Services, Controls   │
└─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────┐
│   MIC.Core.Application (Application)        │  Business Logic
│   - Commands, Queries, Handlers, DTOs       │
└─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────┐
│   MIC.Core.Domain (Domain)                  │  Business Rules
│   - Entities, Value Objects, Domain Events  │
└─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────┐
│   MIC.Infrastructure.* (Implementation)     │  External Services
│   - Data, Identity, AI, Monitoring          │
└─────────────────────────────────────────────┘
```

### Key Patterns

**CQRS (Command Query Responsibility Segregation):**
- Separate command/query folders: `Features/{Feature}/Commands/` and `Features/{Feature}/Queries/`
- Each command/query has: Command/Query class, Handler, Validator (optional)
- Example: `MIC.Core.Application/Alerts/Commands/CreateAlert/`

**Repository Pattern:**
- Generic: `IRepository<T>` with CRUD operations
- Specialized: `IAlertRepository`, `IEmailRepository`, `IUserRepository`, etc.
- Implementation: `MIC.Infrastructure.Data/Repositories/`

**Dependency Injection:**
- Entry points: `AddApplication()`, `AddDataInfrastructure()`, `AddAIServices()`, `AddIdentityInfrastructure()`
- All services registered in `Program.ConfigureServices()`
- Easy mock swapping for testing

**Unit of Work Pattern:**
- `IUnitOfWork` in `MIC.Infrastructure.Data/Persistence/UnitOfWork.cs`
- Wraps all repositories for transaction management
- SaveChangesAsync() commits all changes atomically

---

## 📋 PROJECT STRUCTURE

```
MIC.slnx (Solution file)
├── MIC.Core.Domain/                    # Domain models & business rules
│   ├── Entities/                       # 12 entity classes
│   ├── Abstractions/                   # BaseEntity, IDomainEvent
│   └── Settings/                       # Configuration value objects
├── MIC.Core.Application/               # Business logic & use cases
│   ├── Alerts/                         # CRUD commands & queries
│   ├── Emails/                         # Email operations
│   ├── Chat/                           # Chat interactions
│   ├── Metrics/                        # Metrics queries
│   ├── KnowledgeBase/                  # Document management
│   ├── Authentication/                 # Login/register
│   └── Common/                         # Shared interfaces & DTOs
├── MIC.Core.Intelligence/              # Prediction & analysis
│   ├── Predictions/                    # PredictiveAnalyticsService
│   └── EmailAnalysis/                  # Email intelligence
├── MIC.Infrastructure.Data/            # Database & persistence
│   ├── Repositories/                   # Repository implementations
│   ├── Persistence/                    # DbContext, migrations, UnitOfWork
│   ├── Configurations/                 # EF Core entity configs
│   └── Services/                       # Email sync, database initialization
├── MIC.Infrastructure.Identity/        # Authentication & security
│   ├── JwtTokenService.cs              # JWT generation (HMAC-SHA256)
│   ├── PasswordHasher.cs               # PBKDF2-SHA256 (10K iterations)
│   ├── AuthenticationService.cs        # Login/register logic
│   └── OAuth services                  # Gmail, Outlook OAuth2
├── MIC.Infrastructure.AI/              # AI integration
│   ├── Services/                       # ChatService, PredictionService
│   ├── Plugins/                        # Semantic Kernel plugins
│   ├── Prompts/                        # System prompts
│   └── Configuration/                  # AI settings (OpenAI, Azure OpenAI)
├── MIC.Desktop.Avalonia/               # UI & presentation
│   ├── Views/                          # Avalonia XAML views
│   ├── ViewModels/                     # MVVM view models
│   ├── Services/                       # Desktop-specific services
│   ├── Controls/                       # Reusable UI components
│   ├── Styles/                         # Themes, colors, typography
│   ├── Converters/                     # Value converters
│   ├── Dialogs/                        # Modal dialogs
│   ├── Resources/                      # Localization files (en, fr)
│   ├── App.xaml.cs                     # Application startup
│   ├── Program.cs                      # DI configuration
│   └── appsettings.json                # Configuration files
├── MIC.Tests.Unit/                     # Unit tests (3,164 tests, 100% passing)
├── MIC.Tests.Integration/              # Integration tests (6 files)
├── MIC.Tests.E2E/                      # End-to-end tests (scaffolding)
└── docs/                               # Documentation
    ├── ADR-001-CQRS-Pattern.md
    ├── TESTING_STRATEGY.md
    └── PROJECT_STATUS_REPORT.md
```

---

## 🔐 SECURITY STANDARDS

### Password Hashing
- **Algorithm:** PBKDF2 with HMAC-SHA256
- **Iterations:** 10,000
- **Salt:** Unique per user (generated via `RNGCryptoServiceProvider`)
- **Implementation:** `MIC.Infrastructure.Identity/PasswordHasher.cs`

### JWT Tokens
- **Algorithm:** HMAC-SHA256 (HS256)
- **Secret Key:** Minimum 64 characters, from environment variable `MIC_JwtSettings__SecretKey`
- **Expiry:** Configurable (default 8 hours via `JwtSettings.ExpirationHours`)
- **Claims:** UserId, Username, Email
- **Implementation:** `MIC.Infrastructure.Identity/JwtTokenService.cs`

### OAuth2
- **Providers:** Gmail, Outlook (keyed DI `AddKeyedSingleton<IEmailOAuth2Service>`)
- **Token Storage:** Encrypted via DPAPI (Windows `ProtectedData`)
- **Refresh:** Automatic silent refresh with fallback to interactive flow
- **Implementation:** `MIC.Infrastructure.Identity/GmailOAuthService.cs`, `OutlookOAuthService.cs`

### No Hardcoded Secrets
- ✅ All API keys: environment variables
- ✅ Database passwords: environment variables
- ✅ OAuth credentials: environment variables
- ✅ JWT secrets: environment variables
- ✅ Debug logs: Sanitized (no passwords logged)
- ✅ Configuration files: No sensitive data

### Environment Variables Required for Production
```
ASPNETCORE_ENVIRONMENT = Production
MIC_ConnectionStrings__MicDatabase = Host=...;Password=...;SSL Mode=Require
MIC_JwtSettings__SecretKey = [64+ char random key]
MIC_AI__OpenAI__ApiKey = sk-proj-...
MIC_OAuth2__Gmail__ClientId = ...
MIC_OAuth2__Gmail__ClientSecret = ...
MIC_OAuth2__Outlook__ClientId = ...
MIC_OAuth2__Outlook__ClientSecret = ...
```

---

## 💾 DATA & DATABASE

### Entities (12 Total)
1. **User** - Application users with roles
2. **EmailAccount** - Connected email accounts (Gmail, Outlook, etc.)
3. **EmailMessage** - Individual emails with AI analysis
4. **EmailAttachment** - File attachments from emails
5. **IntelligenceAlert** - Real-time operational alerts
6. **OperationalMetric** - KPIs and performance metrics
7. **ChatHistory** - Saved chat conversations
8. **AssetMonitor** - Equipment/infrastructure monitoring
9. **DecisionContext** - Business decision tracking
10. **Setting** - User preferences and configuration
11. **SettingHistory** - Audit trail for setting changes
12. **OperationalMetric** - Extended metric data

### Database Providers
- **Development:** SQLite (`mic_dev.db`)
- **Production:** PostgreSQL 12+

### Migrations
- Location: `MIC.Infrastructure.Data/Persistence/Migrations/`
- Auto-applied on startup if `DatabaseSettings.RunMigrationsOnStartup = true`
- Seeding: Roles only (no default users or demo data)

### Key Tables
```sql
-- Core tables
Users (Id, Username, Email, PasswordHash, Salt, IsActive, ...)
EmailAccounts (Id, UserId, EmailAddress, Provider, ...)
EmailMessages (Id, UserId, EmailAccountId, Subject, Body, ...)
IntelligenceAlerts (Id, Title, Severity, Source, ...)
OperationalMetrics (Id, Name, Value, Unit, ...)
ChatHistory (Id, UserId, Message, Response, ...)
Settings (Id, UserId, Key, Value, ...)
```

---

## 🧪 TESTING STANDARDS

### Test Coverage (Verified 2026-02-13)
- **Total Tests:** 3,164 unit tests
- **Pass Rate:** 100%
- **Overall Coverage:** 48.2% line coverage (75.3% effective)
- **Branch Coverage:** 62.0%

### Coverage by Layer
| Layer | Coverage | Standard |
|-------|----------|----------|
| Core.Domain | **95.2%** | Excellent |
| Core.Application | **95.0%** | Excellent |
| Core.Intelligence | **86.5%** | Strong |
| Infrastructure.Monitoring | **85.1%** | Strong |
| Infrastructure.AI | **70.2%** | Good |
| Infrastructure.Identity | **59.4%** | Moderate |
| Infrastructure.Data | **35.6%** | Low (3,103 migration lines) |
| Desktop.Avalonia | **39.8%** | Low (UI views untestable) |

### Test Files & Patterns

**Unit Tests Location:** `MIC.Tests.Unit/`

```
MIC.Tests.Unit/
├── Features/
│   ├── Alerts/
│   │   ├── CreateAlertCommandHandlerTests.cs
│   │   ├── UpdateAlertCommandHandlerTests.cs
│   │   ├── DeleteAlertCommandHandlerTests.cs
│   │   └── GetAlertByIdQueryHandlerTests.cs
│   ├── Auth/
│   │   └── LoginCommandHandlerTests.cs
│   ├── Chat/
│   │   └── SaveChatInteractionCommandHandlerTests.cs
│   └── Metrics/
│       └── GetMetricsQueryHandlerTests.cs
├── Services/
├── Builders/                          # TestBuilders.cs for fake objects
└── Infrastructure/
```

**Integration Tests Location:** `MIC.Tests.Integration/`

```
MIC.Tests.Integration/
├── LoginIntegrationTests.cs           # PostgreSQL Testcontainers
├── EmailRepositoryIntegrationTests.cs
├── SettingsServiceIntegrationTests.cs
├── DesktopSettingsServiceIntegrationTests.cs
├── NotificationServiceIntegrationTests.cs
└── NotificationEventBridgeIntegrationTests.cs
```

### Testing Tools
- **Framework:** xUnit
- **Mocking:** Moq, NSubstitute
- **Assertions:** FluentAssertions
- **Containers:** Testcontainers for PostgreSQL
- **Schedulers:** ReactiveUI CurrentThreadScheduler for deterministic tests

### Test Naming Convention
```csharp
[Fact]
public void MethodName_GivenCondition_ExpectedResult()
{
    // Arrange
    
    // Act
    
    // Assert
}
```

### Mock vs Real Data
- **Unit Tests:** All dependencies mocked
- **Integration Tests:** Real PostgreSQL via Testcontainers
- **E2E Tests:** Not yet implemented

---

## 🎨 UI/UX STANDARDS

### Branding
- **Theme:** Cyberpunk/holographic aesthetic
- **Primary Color:** Deep space blue (#0B0C10)
- **Accent Colors:**
  - Cyan (#00E5FF) - Primary interactive elements
  - Magenta (#BF40FF) - AI features
  - Green (#39FF14) - Success states
  - Gold (#FFC107) - Warnings
  - Red (#FF0055) - Errors

### Color Palette
**File:** `MIC.Desktop.Avalonia/Styles/BrandColors.cs`

```csharp
public static class BrandColors
{
    public const string Primary = "#1a237e";
    public const string PrimaryDark = "#0B0C10";
    public const string PrimaryLight = "#0D1117";
    public const string AccentCyan = "#00E5FF";
    public const string AccentGold = "#FFC107";
    public const string AccentMagenta = "#BF40FF";
    public const string AccentGreen = "#39FF14";
    // ... etc
}
```

### View Patterns

**All Views follow this structure:**

1. **Root:** `<UserControl>` or `<Window>`
2. **Grid/StackPanel:** Main layout
3. **Border:** Cards and containers
4. **DataGrid/ItemsControl:** Lists
5. **Buttons/TextBox:** Interactive elements

**Example View Structure:**
```xaml
<UserControl xmlns="https://github.com/avaloniaui" ... >
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <Grid Margin="20" RowDefinitions="Auto,*">
            <!-- Header -->
            <TextBlock Grid.Row="0" Text="View Title" FontSize="24" FontWeight="Bold" />
            
            <!-- Content -->
            <StackPanel Grid.Row="1" Spacing="16">
                <!-- Cards, lists, forms -->
            </StackPanel>
        </Grid>
    </ScrollViewer>
</UserControl>
```

### ViewModel Patterns

**All ViewModels inherit from:** `ViewModelBase`

```csharp
public class MyViewModel : ViewModelBase
{
    private MyService _service;
    
    public MyViewModel(MyService service) 
        => _service = service;
    
    // Observable properties
    private string _title = "Default";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    
    // Reactive commands
    public ReactiveCommand<Unit, Unit> LoadCommand { get; }
    
    // Initialize in constructor or Initialize method
    private void Initialize()
    {
        LoadCommand = ReactiveCommand.CreateFromTask(async _ => 
            await LoadDataAsync());
    }
}
```

### Localization

**Files:** 
- `MIC.Desktop.Avalonia/Resources/Resources.resx` (English)
- `MIC.Desktop.Avalonia/Resources/Resources.fr.resx` (French)

**Usage in XAML:**
```xaml
<TextBlock Text="{StaticResource MenuFile}" />
```

**Usage in C#:**
```csharp
string text = ResourceHelper.GetString("MenuFile");
```

---

## 🚀 COMMON TASKS & PATTERNS

### Adding a New Feature

**1. Create Domain Model**
```
MIC.Core.Domain/Entities/MyEntity.cs
```

**2. Create DTOs**
```
MIC.Core.Application/MyFeature/Common/MyEntityDto.cs
```

**3. Create Commands & Queries**
```
MIC.Core.Application/MyFeature/Commands/CreateMyEntity/
├── CreateMyEntityCommand.cs
├── CreateMyEntityCommandHandler.cs
└── CreateMyEntityCommandValidator.cs (optional)

MIC.Core.Application/MyFeature/Queries/GetMyEntities/
├── GetMyEntitiesQuery.cs
└── GetMyEntitiesQueryHandler.cs
```

**4. Create Repository Interface**
```
MIC.Core.Application/Common/Interfaces/IMyEntityRepository.cs
```

**5. Implement Repository**
```
MIC.Infrastructure.Data/Repositories/MyEntityRepository.cs
```

**6. Register in DI**
```csharp
// In MIC.Core.Application/DependencyInjection.cs
services.AddScoped<IMyEntityRepository, MyEntityRepository>();
```

**7. Create ViewModel**
```
MIC.Desktop.Avalonia/ViewModels/MyFeatureViewModel.cs
```

**8. Create View**
```
MIC.Desktop.Avalonia/Views/MyFeatureView.axaml
```

**9. Add Tests**
```
MIC.Tests.Unit/Features/MyFeature/CreateMyEntityCommandHandlerTests.cs
MIC.Tests.Integration/MyEntityRepositoryIntegrationTests.cs
```

### Handling Errors

**Error Handling Service:**
```csharp
public sealed class ErrorHandlingService : IErrorHandlingService
{
    public void HandleException(Exception ex, string? context = null, bool isCritical = false)
    {
        // Logs error, shows user-friendly message
    }
    
    public async Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> operation,
        string context,
        T? defaultValue = default)
    {
        // Wraps operation with error handling
    }
}
```

**Usage:**
```csharp
var result = await _errorHandlingService.SafeExecuteAsync(
    () => _emailService.SendEmailAsync(emailData),
    "Failed to send email",
    null);
```

### Database Queries

**Using Repository:**
```csharp
public class GetMyEntitiesQueryHandler : IRequestHandler<GetMyEntitiesQuery, IReadOnlyList<MyEntityDto>>
{
    private readonly IMyEntityRepository _repository;
    
    public async Task<IReadOnlyList<MyEntityDto>> Handle(
        GetMyEntitiesQuery request, 
        CancellationToken cancellationToken)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(e => new MyEntityDto 
        { 
            Id = e.Id, 
            Name = e.Name 
        }).ToList();
    }
}
```

### Async/Await

**Always use async patterns:**
```csharp
// ✅ Good
public async Task<Result> ProcessAsync()
{
    var data = await _service.GetDataAsync();
    return data;
}

// ❌ Bad
public Result Process()
{
    var data = _service.GetDataAsync().Result;  // Deadlock risk!
    return data;
}
```

### Logging

**Using Serilog:**
```csharp
private readonly ILogger<MyService> _logger;

public MyService(ILogger<MyService> logger)
{
    _logger = logger;
}

// Usage
_logger.LogInformation("Processing user {UserId}", userId);
_logger.LogError(ex, "Failed to process user {UserId}", userId);
_logger.LogWarning("Unusual activity detected for {UserId}", userId);
```

---

## 🔄 DEVELOPMENT WORKFLOW

### Environment Setup

1. **Clone Repository**
   ```bash
   git clone https://github.com/gpopzrawproduction-afk/MbarieInsightSuite.git
   cd src/MIC
   ```

2. **Set Environment Variables**
   ```powershell
   $env:MIC_AI__OpenAI__ApiKey = "sk-proj-..."
   $env:MIC_ConnectionStrings__MicDatabase = "Host=localhost;..."
   $env:MIC_JwtSettings__SecretKey = "your-64-char-key"
   ```

3. **Run Database Setup**
   ```powershell
   ./Setup-DbAndBuild.ps1
   ```

4. **Run Application**
   ```bash
   dotnet run --project .\MIC.Desktop.Avalonia
   ```

### Build & Test

**Build:**
```bash
dotnet build MIC.slnx --configuration Release
```

**Unit Tests:**
```bash
dotnet test MIC.Tests.Unit --logger:"console;verbosity=normal"
```

**Integration Tests:**
```bash
dotnet test MIC.Tests.Integration --logger:"console;verbosity=normal"
```

**Code Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Deployment

**Build MSIX:**
```powershell
cd src/MIC
msbuild MIC.Desktop.Avalonia.csproj `
  /t:Publish `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true
```

**Detailed guide:** See `MSIX_PACKAGING_GUIDE.md`

---

## 📊 VERSION STRATEGY

### Current Version
**v1.0.0** - Production Release

### Versioning Scheme
```
v1.0.0 - Initial release (Core features)
v1.1.0 - Email send + Knowledge Base (6-8 weeks)
v1.2.0 - Accessibility + Full Predictions (12-14 weeks)
v2.0.0 - Reports + Advanced Analytics (20+ weeks)
```

### Feature Completeness

| Feature | v1.0 | v1.1 | v1.2 |
|---------|------|------|------|
| Alerts | ✅ | ✅ | ✅ |
| Metrics Dashboard | ✅ | ✅ | ✅ |
| Chat/AI | ✅ | ✅ | ✅ |
| Email (Read/Send/Reply) | ✅ | Polish | - |
| Knowledge Base | Beta | ✅ | - |
| Predictions | Beta | ✅ | - |
| Accessibility | - | - | ✅ |
| Reports | - | - | ✅ |

---

## 📚 DOCUMENTATION

### Key Documents
- `README.md` - Project overview
- `SETUP.md` - Development setup
- `DEPLOYMENT.md` - Production deployment
- `SECURITY_AUDIT_REPORT.md` - Security details
- `MSIX_PACKAGING_GUIDE.md` - MSIX build process
- `PRODUCTION_READINESS_AUDIT.md` - Full audit (this document)
- `docs/ADR-001-CQRS-Pattern.md` - Architecture decision
- `docs/TESTING_STRATEGY.md` - Testing approach

### Code Comments
- Use XML comments on public members
- Document "why" not "what"
- Keep comments updated with code

### Examples

**Good comment:**
```csharp
/// <summary>
/// Validates email format using RFC 5322 specification.
/// Note: This regex is more permissive than full RFC 5322 compliance
/// to reduce false negatives in practice.
/// </summary>
public bool ValidateEmail(string email) { ... }
```

**Bad comment:**
```csharp
// Check if email is valid
public bool ValidateEmail(string email) { ... }
```

---

## 🎯 CODING STANDARDS

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Classes | PascalCase | `EmailInboxViewModel` |
| Methods | PascalCase | `SendEmailAsync()` |
| Properties | PascalCase | `EmailAddress` |
| Fields (private) | _camelCase | `_emailService` |
| Constants | UPPER_CASE | `DEFAULT_TIMEOUT` |
| Parameters | camelCase | `emailAddress` |
| Variables | camelCase | `userInput` |
| Interfaces | IPascalCase | `IEmailService` |
| Async methods | *Async | `GetEmailsAsync()` |

### Code Organization

**Class Structure:**
```csharp
public class MyClass
{
    // 1. Constants
    private const string DefaultValue = "...";
    
    // 2. Static fields
    private static int _count;
    
    // 3. Instance fields
    private readonly IService _service;
    
    // 4. Properties
    public string Property { get; set; }
    
    // 5. Constructors
    public MyClass(IService service) { ... }
    
    // 6. Public methods
    public void PublicMethod() { ... }
    
    // 7. Protected/internal methods
    protected void ProtectedMethod() { ... }
    
    // 8. Private methods
    private void PrivateMethod() { ... }
}
```

### Async Patterns

**Always use async for I/O:**
```csharp
// Database
var users = await _repository.GetAllAsync();

// HTTP
var response = await _httpClient.GetAsync(url);

// File I/O
var content = await File.ReadAllTextAsync(path);
```

**Never use .Result or .Wait():**
```csharp
// ❌ WRONG - causes deadlocks
var result = _service.GetDataAsync().Result;

// ✅ CORRECT
var result = await _service.GetDataAsync();
```

### Error Handling

**Use specific exceptions:**
```csharp
// ✅ Good - specific exception type
if (string.IsNullOrEmpty(email))
    throw new ArgumentException("Email cannot be empty", nameof(email));

// ❌ Bad - generic exception
if (string.IsNullOrEmpty(email))
    throw new Exception("Invalid email");
```

**Handle expected errors gracefully:**
```csharp
try
{
    return await _emailService.SendAsync(email);
}
catch (SmtpException ex)
{
    _logger.LogWarning(ex, "SMTP error sending email");
    return Result.Failure("Email service temporarily unavailable");
}
```

### LINQ Usage

**Prefer LINQ over loops:**
```csharp
// ✅ Good
var activeUsers = users
    .Where(u => u.IsActive)
    .Select(u => new UserDto { Id = u.Id, Name = u.Name })
    .ToList();

// ❌ Verbose
var activeUsers = new List<UserDto>();
foreach (var user in users)
{
    if (user.IsActive)
    {
        activeUsers.Add(new UserDto { Id = user.Id, Name = user.Name });
    }
}
```

---

## 🔍 CODE REVIEW CHECKLIST

Before committing, verify:

- [ ] Code follows naming conventions
- [ ] No hardcoded secrets or passwords
- [ ] Async patterns used for I/O
- [ ] Error handling implemented
- [ ] Unit tests added/updated
- [ ] Integration tests updated if data layer changed
- [ ] XML comments on public members
- [ ] No unused imports or variables
- [ ] Build succeeds with no warnings
- [ ] All tests pass (3,164+ unit tests)
- [ ] Architecture patterns followed
- [ ] DI patterns applied
- [ ] Logging implemented where needed
- [ ] No console.WriteLine() (use Serilog)
- [ ] Database migrations created if needed
- [ ] Feature flag considered if incomplete

---

## 🚨 CRITICAL DO's AND DON'Ts

### DO ✅

- Use `async/await` for all I/O operations
- Use dependency injection for all services
- Write tests before code (TDD when possible)
- Use entity framework for database access
- Log important operations with Serilog
- Handle exceptions gracefully
- Use configuration from environment variables
- Document non-obvious code
- Follow Clean Architecture layers
- Use CQRS for business logic

### DON'T ❌

- Hardcode secrets, passwords, or API keys
- Log sensitive data (passwords, tokens, PII)
- Use `.Result` or `.Wait()` (deadlock risk)
- Bypass DI container
- Mix async and sync code
- Use `Console.WriteLine()` (use Serilog)
- Make database calls directly from ViewModels
- Ignore test failures
- Commit without running tests
- Use catch(Exception) without specific handling
- Add breaking changes without version bump

---

## 📞 QUICK REFERENCE

### Common Commands

```powershell
# Build
dotnet build MIC.slnx

# Run tests
dotnet test MIC.Tests.Unit
dotnet test MIC.Tests.Integration

# Run app
dotnet run --project .\MIC.Desktop.Avalonia

# Build MSIX
msbuild MIC.Desktop.Avalonia.csproj /t:Publish /p:Configuration=Release /p:RuntimeIdentifier=win-x64 /p:GenerateAppxPackageOnBuild=true

# Clean
dotnet clean MIC.slnx
```

### Key Files

| File | Purpose |
|------|---------|
| `MIC.slnx` | Solution file |
| `MIC.Desktop.Avalonia/Program.cs` | Application entry point & DI setup |
| `MIC.Desktop.Avalonia/appsettings.json` | Configuration (base) |
| `MIC.Desktop.Avalonia/appsettings.Development.json` | Configuration (dev) |
| `MIC.Infrastructure.Data/Persistence/MicDbContext.cs` | EF Core DbContext |
| `.github/copilot-instructions.md` | Copilot guidelines |
| `docs/ADR-001-CQRS-Pattern.md` | Architecture decisions |

---

## 📈 QUALITY METRICS

### Current Status (2026-02-13)

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Unit Tests | 3,164 | 3,500+ | 90% |
| Pass Rate | 100% | 100% | ✅ |
| Line Coverage | 48.2% | 55%+ | 88% |
| Effective Coverage | 75.3% | 80%+ | 94% |
| Build Warnings | 3 | 0 | 🟡 |
| Security Score | 90/100 | 95+ | 🟡 |
| Feature Complete | 90% | 100% | 90% |

---

## 🎓 ADDITIONAL RESOURCES

- **.NET 9 Documentation:** https://learn.microsoft.com/en-us/dotnet/
- **Avalonia UI:** https://docs.avaloniaui.net/
- **MediatR:** https://github.com/jbogard/MediatR
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/core/
- **FluentValidation:** https://fluentvalidation.net/
- **Serilog:** https://serilog.net/
- **OpenAI API:** https://platform.openai.com/docs/

---

**Last Review:** February 13, 2026  
**Next Review:** When version changes  
**Maintainer:** Development Team  

For questions or updates to this guide, please contact the technical lead.

# 🤖 COPILOT MASTER PROMPT — Mbarie Insight Suite (MIC)
## Complete Implementation Guide for Missing & Incomplete Modules

---

## 🧠 YOUR ROLE & CONTEXT

You are an expert C# / WPF / .NET developer working on **Mbarie Insight Suite (MIC)**, a
professional enterprise desktop application. You must write production-quality code that matches the
existing codebase's architecture, patterns, naming conventions, and visual theme without deviation.

---

## 🏗️ ARCHITECTURE — STRICT RULES

This project uses **Clean Architecture**. All code you write MUST respect these layer boundaries:

```
Domain           → Entities, Value Objects, Domain Events, Interfaces (NO external dependencies)
Application      → CQRS Handlers (MediatR), DTOs, FluentValidation Validators, Service Interfaces
Infrastructure   → EF Core Repositories, external service implementations (Email, AI, OAuth)
Presentation     → WPF ViewModels (MVVM), Views (XAML), Converters, Dialogs
```

### Mandatory Patterns — Never Deviate From These

| Pattern | Implementation |
|---|---|
| CQRS | MediatR — all user actions are Commands or Queries |
| Repository | IRepository<T> + Unit of Work — NEVER call DbContext directly from ViewModels |
| DI | Microsoft.Extensions.DependencyInjection — register ALL new services |
| Validation | FluentValidation on every Command/Query with rules |
| Logging | Serilog — inject `ILogger<T>` in every service and handler |
| Error Handling | IErrorHandlingService — wrap all external calls in try/catch, use this service |
| Notifications | INotificationService — show in-app toast for every significant user action |

### Naming Conventions

```
Commands:       CreateAlertCommand, SendEmailCommand, UploadDocumentCommand
Queries:        GetEmailsQuery, GetMetricsQuery, SearchKnowledgeBaseQuery
Handlers:       CreateAlertCommandHandler, GetEmailsQueryHandler
ViewModels:     EmailComposeViewModel, UserProfileViewModel
Views:          EmailComposeView.xaml, UserProfileView.xaml
Repositories:   IEmailRepository, IDocumentRepository
DTOs:           EmailDto, DocumentDto, UserProfileDto
Validators:     SendEmailCommandValidator, UploadDocumentCommandValidator
```

---

## 🎨 VISUAL THEME — NEVER BREAK THIS

All UI must use `BrandColors.cs` values. Do not hardcode hex values in XAML.

```csharp
// BrandColors.cs reference — use these StaticResource keys in XAML
PrimaryBackground   = #0B0C10   // Deep space black
SecondaryBackground = #1F2833   // Panel background
AccentCyan          = #00E5FF   // Neon cyan — primary interactive
AccentMagenta       = #BF40FF   // AI/intelligence features
AccentGreen         = #39FF14   // Success / active states
TextPrimary         = #C5C6C7   // Standard text
TextSecondary       = #66FCF1   // Secondary / labels
DangerRed           = #FF3D3D   // Errors / destructive actions
WarningAmber        = #FFB347   // Warnings
```

In XAML, always bind like:
```xml
Background="{StaticResource PrimaryBackgroundBrush}"
Foreground="{StaticResource AccentCyanBrush}"
```

All interactive elements must have:
- Hover state (slightly brighter)
- Focus indicator (1px AccentCyan border)
- Disabled state (50% opacity)
- Loading state (spinner or pulse animation where applicable)

---

## 📋 MODULE 1: EMAIL — Complete the Missing 30%

### Current State
- ✅ Read emails, display inbox, AI analysis
- ❌ Send/Compose email
- ❌ Reply with AI assistance
- ❌ Delete email
- ❌ Move to folder
- ❌ Mark as read/unread

---

### 1A — Email Compose / Send

**Application Layer — Command:**
```csharp
// File: Application/Features/Email/Commands/SendEmail/SendEmailCommand.cs
public record SendEmailCommand : IRequest<Result<string>>
{
    public string AccountId { get; init; }
    public List<string> ToAddresses { get; init; }
    public List<string> CcAddresses { get; init; } = new();
    public List<string> BccAddresses { get; init; } = new();
    public string Subject { get; init; }
    public string Body { get; init; }
    public bool IsHtml { get; init; } = false;
    public List<AttachmentDto> Attachments { get; init; } = new();
}
```

**Validator:**
```csharp
// File: Application/Features/Email/Commands/SendEmail/SendEmailCommandValidator.cs
public class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.ToAddresses).NotEmpty().WithMessage("At least one recipient required");
        RuleForEach(x => x.ToAddresses).EmailAddress().WithMessage("Invalid email address");
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(998);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000_000);
    }
}
```

**Handler:** Implement `SendEmailCommandHandler` that:
1. Resolves the correct `IEmailProviderService` by `AccountId` (Gmail vs Outlook)
2. Calls `SendAsync(message)` on the provider
3. Saves a copy to `SentItems` folder in the database
4. Returns `Result<string>` with the message ID
5. Logs success/failure with Serilog
6. Calls `IErrorHandlingService.HandleAsync()` on exception

**Infrastructure:** Implement `SendAsync` in both `GmailEmailService` and `OutlookEmailService`.

**ViewModel — `EmailComposeViewModel.cs`:**
```csharp
// Required Properties
public ObservableCollection<string> ToAddresses { get; }
public ObservableCollection<string> CcAddresses { get; }
public string Subject { get; set; }
public string Body { get; set; }
public bool IsSending { get; private set; }
public bool CanSend => ToAddresses.Any() && !string.IsNullOrWhiteSpace(Subject) && !IsSending;

// Required Commands
public ICommand SendCommand { get; }       // calls SendEmailCommand via MediatR
public ICommand AddAiAssistCommand { get; } // opens AI draft panel
public ICommand AttachFileCommand { get; }
public ICommand DiscardCommand { get; }

// AI Assist Method
private async Task GenerateAiDraftAsync()
{
    // Send current Subject + partial Body to ChatService
    // Stream the response back and append to Body
    // Show loading spinner on the AI assist button
}
```

**View — `EmailComposeView.xaml`:** Implement a compose window with:
- `To:`, `Cc:`, `Bcc:` fields (tag-input style using WrapPanel + TextBox)
- Subject field
- Rich body TextBox (monospace, neon-on-dark)
- Toolbar: [Send] [AI Draft ✨] [Attach] [Discard]
- Collapsible AI panel on the right (shows AI-generated suggestions)
- Character/word count in the status bar

---

### 1B — Reply / Reply-All / Forward

Add to `EmailDetailViewModel`:
```csharp
public ICommand ReplyCommand { get; }
public ICommand ReplyAllCommand { get; }
public ICommand ForwardCommand { get; }
```

Each command opens `EmailComposeView` pre-populated:
- **Reply:** To = original sender, Subject = "Re: {original}", Body = quoted original
- **Reply All:** To = all recipients, same quoted pattern
- **Forward:** Subject = "Fwd: {original}", Body = forwarded with original inline

Quoted format:
```
{new message body}

--- Original Message ---
From: {sender}
Date: {date}
Subject: {subject}

{original body}
```

---

### 1C — Email Actions (Delete, Move, Mark)

**Commands to implement:**
```csharp
DeleteEmailCommand    : IRequest<Result>  // soft-delete, moves to Trash folder
MoveEmailCommand      : IRequest<Result>  // { EmailId, TargetFolderName }
MarkEmailReadCommand  : IRequest<Result>  // { EmailId, IsRead }
```

**In `EmailListViewModel` and `EmailDetailViewModel`, add:**
```csharp
public ICommand DeleteCommand { get; }
public ICommand MoveToFolderCommand { get; }
public ICommand MarkAsReadCommand { get; }
public ICommand MarkAsUnreadCommand { get; }
```

**UI:** Add a right-click context menu to email list items and an action toolbar in the detail view.

---

## 📋 MODULE 2: KNOWLEDGE BASE — Build the Missing 50%

### Current State
- ✅ Basic view shell
- ❌ Document upload
- ❌ Document indexing (chunking + embeddings)
- ❌ Semantic search (RAG)
- ❌ Document management (rename, delete, tag)

---

### 2A — Domain Entities

```csharp
// Domain/Entities/KnowledgeDocument.cs
public class KnowledgeDocument : BaseEntity
{
    public string Title { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }  // "application/pdf", "text/plain", etc.
    public long FileSizeBytes { get; private set; }
    public string StoragePath { get; private set; }  // local file path or blob URI
    public bool IsIndexed { get; private set; }
    public DateTime? IndexedAt { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public List<DocumentChunk> Chunks { get; private set; } = new();
    public string UserId { get; private set; }

    public void MarkAsIndexed() { IsIndexed = true; IndexedAt = DateTime.UtcNow; }
}

// Domain/Entities/DocumentChunk.cs
public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; }
    public float[] Embedding { get; set; }   // stored as JSON in SQLite, vector in PostgreSQL
    public int TokenCount { get; set; }
}
```

---

### 2B — Document Upload

**Command:**
```csharp
public record UploadDocumentCommand : IRequest<Result<Guid>>
{
    public string FilePath { get; init; }       // local path selected by user
    public string Title { get; init; }
    public List<string> Tags { get; init; } = new();
}
```

**Handler must:**
1. Validate file type (allow: `.pdf`, `.txt`, `.md`, `.docx`, `.csv`)
2. Copy file to app's local storage directory
3. Extract text content (use `PdfPig` for PDF, plain text for others)
4. Chunk text into 512-token segments with 50-token overlap
5. Generate embeddings for each chunk via `IAiEmbeddingService`
6. Save `KnowledgeDocument` + all `DocumentChunk` records to DB
7. Notify user of completion via `INotificationService`

**ViewModel — add to `KnowledgeBaseViewModel`:**
```csharp
public ICommand UploadDocumentCommand { get; }   // opens file picker dialog
public bool IsUploading { get; private set; }
public double UploadProgress { get; private set; } // 0.0 to 1.0
public ObservableCollection<KnowledgeDocumentDto> Documents { get; }
```

---

### 2C — Semantic Search (RAG)

**Query:**
```csharp
public record SearchKnowledgeBaseQuery : IRequest<Result<List<SearchResultDto>>>
{
    public string SearchText { get; init; }
    public int TopK { get; init; } = 5;
    public List<string> FilterTags { get; init; } = new();
}

public record SearchResultDto
{
    public string DocumentTitle { get; init; }
    public string ChunkContent { get; init; }
    public float RelevanceScore { get; init; }
    public int ChunkIndex { get; init; }
    public Guid DocumentId { get; init; }
}
```

**Handler must:**
1. Generate embedding for the search query text
2. Compute cosine similarity against all stored chunk embeddings
3. Return top-K results sorted by score
4. Filter by tags if provided

**UI:** Add a search bar at the top of `KnowledgeBaseView` that:
- Shows results in real-time as the user types (debounce 400ms)
- Highlights the matched text within each chunk result
- Shows the document name and relevance score
- "Ask about this" button on each result — pipes context into the Chat module

---

### 2D — Document Management UI

In `KnowledgeBaseView`, add:
- Document list with: icon (by type), title, tags, file size, indexed status, upload date
- Right-click context menu: [Rename] [Add Tags] [Re-index] [Delete]
- Filter sidebar: filter by tag, by file type, by index status
- Detail panel (right side): preview first 500 chars, show all chunks, show metadata

---

## 📋 MODULE 3: USER PROFILE — Complete the Missing 60%

### Current State
- ✅ Display basic user info
- ❌ Change password
- ❌ Avatar upload
- ❌ Email notification preferences
- ❌ UI preferences (theme density, etc.)

---

### 3A — Change Password

**Command:**
```csharp
public record ChangePasswordCommand : IRequest<Result>
{
    public string UserId { get; init; }
    public string CurrentPassword { get; init; }
    public string NewPassword { get; init; }
    public string ConfirmPassword { get; init; }
}
```

**Validator rules:**
- `CurrentPassword` must not be empty
- `NewPassword`: min 12 chars, must include uppercase, lowercase, digit, special char
- `ConfirmPassword` must match `NewPassword`
- `NewPassword` must not equal `CurrentPassword`

**Handler must:**
1. Verify current password against stored Argon2id hash
2. Return `Result.Failure("Incorrect current password")` if it doesn't match
3. Hash new password with Argon2id
4. Update user record
5. Invalidate all existing JWT sessions for the user

---

### 3B — Avatar Upload

**Command:**
```csharp
public record UpdateAvatarCommand : IRequest<Result<string>>
{
    public string UserId { get; init; }
    public string SourceImagePath { get; init; }  // local file path
}
```

**Handler must:**
1. Validate image type (jpg, png, webp only) and size (max 5MB)
2. Resize to 256x256 pixels (use `SixLabors.ImageSharp`)
3. Save to app's local avatar directory as `{userId}.jpg`
4. Update user's `AvatarPath` in the database
5. Raise `AvatarUpdatedDomainEvent` so the MainWindow header refreshes

**ViewModel:**
```csharp
public ICommand ChangeAvatarCommand { get; }  // opens file picker, then triggers command
public string AvatarPath { get; private set; }
// Listen for AvatarUpdatedDomainEvent and refresh AvatarPath
```

---

### 3C — Notification & App Preferences

Add a `UserPreferences` entity or extend existing user entity:

```csharp
public class UserPreferences
{
    public bool EmailAlertsEnabled { get; set; } = true;
    public bool DesktopNotificationsEnabled { get; set; } = true;
    public bool AiSuggestionsEnabled { get; set; } = true;
    public int EmailSyncIntervalMinutes { get; set; } = 15;
    public string DefaultEmailAccountId { get; set; }
    public bool CompactViewMode { get; set; } = false;
    public string DateTimeFormat { get; set; } = "dd MMM yyyy HH:mm";
    public string TimeZone { get; set; } = "UTC";
}
```

**In `UserProfileView.xaml`:** Add a "Preferences" tab with toggles and dropdowns for all settings. Save immediately on change using `UpdateUserPreferencesCommand`.

---

## 📋 MODULE 4: PREDICTIONS — Upgrade from Stub to Working

### Current State
- ✅ View shell exists
- ⚠️ Handler is a stub returning hardcoded data
- ❌ Real historical analysis
- ❌ Trend forecasting

---

### 4A — Data Collection

**Add these queries to the Application layer:**

```csharp
// Gets aggregated metric history for trend analysis
public record GetMetricHistoryQuery : IRequest<Result<List<MetricDataPointDto>>>
{
    public string MetricName { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public AggregationPeriod Period { get; init; }  // Hourly, Daily, Weekly
}

public enum AggregationPeriod { Hourly, Daily, Weekly, Monthly }
```

---

### 4B — Prediction Engine (Application Layer)

Create `IPredictionService` interface and implement it:

```csharp
public interface IPredictionService
{
    Task<List<PredictionDto>> PredictNextPeriodAsync(
        string metricName,
        List<MetricDataPointDto> historicalData,
        int periodsAhead = 7);
}
```

**Implementation approach (simple, no ML library needed initially):**
1. **Linear Regression** for trend: Calculate slope and intercept from last 30 data points
2. **Seasonality Detection**: Detect 7-day weekly patterns by averaging same-day values
3. **Anomaly Threshold**: Flag predictions as "at risk" if value exceeds μ + 2σ of historical range
4. **Confidence Interval**: Return lower/upper bounds (±1 std deviation)

Later (v1.2), this can be upgraded to use ML.NET or OpenAI's analysis endpoint.

---

### 4C — Predictions UI

Replace stub UI in `PredictionView.xaml` with:
- **Metric selector** dropdown at the top
- **Date range picker** (Last 7 days, 30 days, 90 days)
- **Forecast chart** (line chart showing: historical data in AccentCyan, predicted values in AccentMagenta dashed line, confidence band as semi-transparent fill)
- **Insight cards** below the chart:
  - Trend direction (↑ Upward, ↓ Downward, → Stable)
  - Forecasted value for next period
  - Anomaly alert if predicted value is out of range
  - Confidence score (e.g., "72% confidence")

Use `LiveChartsCore.SkiaSharpView.WPF` (already in project) for the chart. Style all chart elements using the BrandColors palette.

---

## 📋 MODULE 5: REPORTS — Build from 0%

### 5A — Report Data Model

```csharp
// Domain/Entities/Report.cs
public class Report : BaseEntity
{
    public string Name { get; private set; }
    public ReportType Type { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public string Parameters { get; private set; }  // JSON: date range, filters, etc.
    public string OutputPath { get; private set; }  // local path to PDF/XLSX file
    public string UserId { get; private set; }
}

public enum ReportType
{
    AlertSummary,
    EmailActivity,
    MetricsTrend,
    AiChatSummary,
    FullDashboard
}
```

---

### 5B — Report Generation Commands

```csharp
public record GenerateReportCommand : IRequest<Result<string>>
{
    public ReportType Type { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public ReportFormat Format { get; init; }  // PDF, XLSX, CSV
    public Dictionary<string, string> Parameters { get; init; } = new();
}

public enum ReportFormat { PDF, XLSX, CSV }
```

**Handler must:**
1. Fetch relevant data from repositories based on `ReportType` and date range
2. Generate the file using:
   - PDF: `QuestPDF` library (add as NuGet package)
   - XLSX: `ClosedXML` library (add as NuGet package)
   - CSV: `CsvHelper` library (add as NuGet package)
3. Save file to `%AppData%\MIC\Reports\{userId}\{timestamp}_{type}.{ext}`
4. Save `Report` record to database
5. Show notification with "Open File" action button
6. Return the output file path

---

### 5C — Report Templates

Implement these 3 report templates first (highest value):

**Alert Summary Report:**
- Table of all alerts in date range: [Name, Severity, Triggered Count, Last Triggered, Status]
- Bar chart: alerts per day
- Summary stats: total alerts, critical count, average resolution time

**Email Activity Report:**
- Emails received per day (line chart)
- Top senders (top 10 table)
- AI analysis summary per email account
- Unread count trend

**Metrics Trend Report:**
- One section per metric
- Historical chart + trend line
- Min/Max/Average/Current statistics
- Period-over-period comparison

---

### 5D — Reports UI (`ReportsView.xaml`)

- Report type cards (click to configure + generate)
- Date range picker
- Format selector (PDF / XLSX / CSV)
- Generate button with progress indicator
- Recent reports table: [Name, Type, Date, Format, Actions: Open, Delete, Re-generate]

---

## ✅ TESTING REQUIREMENTS

For **every new feature**, you MUST generate:

**Unit Tests (xUnit):**
```csharp
// Naming: MethodName_Scenario_ExpectedResult
public async Task SendEmailCommandHandler_ValidCommand_ReturnsSuccessResult()
public async Task SendEmailCommandHandler_EmptyToAddress_ReturnsValidationError()
public async Task SearchKnowledgeBaseQuery_WithMatchingText_ReturnsRankedResults()
```

**For each Command/Query Handler, test:**
1. Happy path — valid input returns expected result
2. Validation failure — invalid input returns `Result.Failure` with error message
3. External service failure — service throws, handler returns `Result.Failure` gracefully
4. Edge cases specific to the feature

Minimum coverage targets for new code:
- Domain entities: 90%
- Command/Query handlers: 80%
- Infrastructure services: 65%
- ViewModels: 50%

---

## ⚠️ CRITICAL RULES — READ BEFORE WRITING ANY CODE

1. **NEVER call DbContext directly from ViewModels or Application layer** — use Repositories only
2. **NEVER hardcode credentials, API keys, or file paths** — use config or `IOptions<T>`
3. **NEVER use `MessageBox.Show()`** — use `INotificationService` for all user feedback
4. **NEVER use `async void`** — except for event handlers where unavoidable
5. **ALWAYS return `Result<T>` or `Result` from Handlers** — never throw exceptions to the ViewModel
6. **ALWAYS register new services in the DI container** — add to the appropriate `ServiceCollectionExtensions` file
7. **ALWAYS log entry, exit, and errors** in every handler using `ILogger<T>`
8. **ALWAYS cancel long-running operations** with `CancellationToken` — pass from ViewModel to Handler
9. **ALL UI operations** must run on the UI thread — use `Application.Current.Dispatcher.InvokeAsync()`
10. **No magic strings** — define all string constants in a `Constants.cs` file per feature folder

---

## 🚀 IMPLEMENTATION ORDER (Recommended)

Work in this sequence to maximise testable, shippable increments:

```
Week 1: Email Send/Reply (1A, 1B) → Most impactful, users notice immediately
Week 1: Email Actions (1C)        → Completes email module to 100%
Week 2: User Profile (3A, 3B, 3C) → Low complexity, high polish impact
Week 2: Knowledge Base Upload (2A, 2B) → Foundation for RAG
Week 3: Knowledge Base Search (2C, 2D) → Completes KB module
Week 3: Predictions (4A, 4B, 4C)  → Real data replaces stub
Week 4: Reports (5A, 5B, 5C, 5D)  → Final module, builds on everything above
```

---

## 📦 NEW NUGET PACKAGES APPROVED FOR ADDITION

| Package | Purpose | Module |
|---|---|---|
| `PdfPig` | PDF text extraction | Knowledge Base |
| `SixLabors.ImageSharp` | Image resize for avatars | User Profile |
| `QuestPDF` | PDF report generation | Reports |
| `ClosedXML` | Excel report generation | Reports |
| `CsvHelper` | CSV export | Reports |
| `Polly` | Retry/circuit breaker resilience | All external services |

Do NOT add any other packages without explicit approval.

---

*End of Master Prompt — Mbarie Insight Suite Implementation Guide*
*Generated: February 13, 2026 | Audit Baseline: v1.0 — 72% Production Ready*