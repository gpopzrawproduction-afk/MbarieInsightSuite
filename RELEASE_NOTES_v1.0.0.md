# MIC v1.0.0 — Production Release

**Release Date:** February 13, 2026

---

## Features

- **AI-Powered Intelligence Console** — Chat interface with OpenAI/Azure OpenAI integration, context-aware conversations, automated email summarization
- **Email Management** — OAuth2-secured Gmail and Outlook sync, IMAP integration, attachment ingestion, retry policies
- **Alert Monitoring & Analytics** — Create, view, acknowledge, and export intelligence alerts with real-time notifications
- **Metrics Dashboard** — Live operational metrics, KPI cards, trend charts, and export capabilities
- **Knowledge Base** — Document upload, indexing, and semantic search across uploaded files
- **Predictive Analytics** — Email trend forecasting, alert pattern analysis, metric anomaly detection with confidence scoring
- **Multilingual Support** — English, French, Spanish, Arabic (RTL), Chinese
- **Dark/Light Theme** — System-aware theme switching
- **User Authentication** — JWT-based auth with Argon2id password hashing, role-based access control

---

## Quality Metrics

| Metric | Value |
|---|---|
| **Unit Tests** | 3,164 (100% passing) |
| **Line Coverage** | 48.2% overall / 75.3% effective (excl. migrations + XAML) |
| **Branch Coverage** | 62.0% |
| **Build Warnings** | 3 (cosmetic only) |
| **Build Errors** | 0 |
| **Test Runtime** | ~22 seconds |

### Coverage by Module

| Module | Coverage | Assessment |
|---|---:|---|
| Core.Domain | 95.2% | Excellent |
| Core.Application | 95.0% | Excellent |
| Core.Intelligence | 86.5% | Strong |
| Infrastructure.Monitoring | 85.1% | Strong |
| Infrastructure.AI | 70.2% | Good |
| Infrastructure.Identity | 59.5% | Moderate |
| Desktop.Avalonia | 39.9% | UI-heavy (views untestable) |
| Infrastructure.Data | 35.6% | Migration-heavy (auto-gen) |

---

## Technical Stack

| Component | Technology |
|---|---|
| Runtime | .NET 9.0 |
| UI Framework | Avalonia UI 11.x |
| Database | PostgreSQL 14+ / SQLite |
| AI Backend | OpenAI / Azure OpenAI via Semantic Kernel |
| Auth | JWT + Argon2id, OAuth2 (Gmail, Outlook) |
| Architecture | Clean Architecture, CQRS (MediatR), Repository Pattern |
| Testing | xUnit, FluentAssertions 7, NSubstitute, Moq, Testcontainers |
| Logging | Serilog (console + rolling file) |
| CI/CD | GitHub Actions |

---

## System Requirements

- **OS:** Windows 10/11 (64-bit)
- **Runtime:** .NET 9.0 (included in self-contained package)
- **RAM:** 4 GB minimum, 8 GB recommended
- **Database:** PostgreSQL 14+ or SQLite (built-in)
- **Network:** Internet for AI features and email sync

---

## Installation

### Standalone Executable
1. Extract `publish/win-x64/` to your desired location
2. Run `MIC.Desktop.Avalonia.exe`
3. Configure database connection on first launch
4. Set admin credentials via environment variables (see README)
5. Optionally connect email accounts (Gmail/Outlook OAuth)

### From Source
```powershell
git clone https://github.com/gpopzrawproduction-afk/MbarieInsightSuite.git
cd MbarieInsightSuite/src/MIC
./Setup-DbAndBuild.ps1
dotnet run --project MIC.Desktop.Avalonia
```

---

## Known Limitations

- `MetricsDashboardViewModel` and `KnowledgeBaseViewModel` use `Dispatcher.UIThread` which limits unit test coverage for those views
- IMAP sync (`RealEmailSyncService`) requires live mail server — integration-tested only
- MSIX packaging available but not signed for Store distribution in this release

---

## Deployment Package

| Item | Details |
|---|---|
| **Path** | `publish/win-x64/` |
| **Files** | 431 |
| **Size** | ~226 MB (self-contained) |
| **Executable** | `MIC.Desktop.Avalonia.exe` |

---

## Changelog

### Added
- 175 test files with 3,164 unit tests
- Comprehensive domain entity coverage (95%+)
- Application layer CQRS handler coverage (95%+)
- Infrastructure service tests (AI, Data, Identity, Monitoring)
- ViewModel property and command tests
- Flaky test stabilization via xUnit collection serialization
- Production deployment package (win-x64, self-contained)

### Fixed
- `Setting` class constructor usage across all test files (object initializer pattern)
- `AttachmentStoreResult` property name (`ContentHash` not `Hash`)
- HTML entity decoding test (entities decoded then stripped by tag regex)
- Chinese parent culture matching (zh-TW → zh-Hant, not zh)
- `NotificationCenterViewModel` filter tests (timing-dependent with ReactiveUI)
- `UserProfileViewModel` initials test (parallel singleton interference)
- Unified xUnit test collections to prevent `UserSessionService.Instance` race conditions

### Security
- OAuth2 token flows tested (Gmail, Outlook)
- JWT token generation and validation tested
- Password hashing (Argon2id) tested
- First-run setup flow tested
- No credentials in source code
