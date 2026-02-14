# Copilot Instructions

## Architecture & Ownership
- Maintain the clean architecture split: UI in `MIC.Desktop.Avalonia`, application logic in `MIC.Core.Application`, domain models under `MIC.Core.Domain`, and concrete implementations inside `MIC.Infrastructure.*`.
- New business capabilities flow as MediatR requests + handlers + FluentValidation validators under `MIC.Core.Application`; wire supporting services in the matching infrastructure project.
- Keep view models thin and drive navigation/state via services registered in `Program.cs`; avoid bypassing the service provider or leaking infrastructure types into the UI layer.

## Dependency Injection & Configuration
- Extend the existing DI entry points (`AddApplication`, `AddDataInfrastructure`, `AddAIServices`, `AddIdentityInfrastructure`) rather than creating ad-hoc service wiring.
- When the desktop shell needs a new singleton, register it in `Program.ConfigureServices` and expose it through `Program.ServiceProvider` for tests that temporarily swap providers.
- Configuration comes from `appsettings*.json` + environment variables; prefer binding to option objects (e.g., `JwtSettings`, `DatabaseSettings`) instead of manual lookups.

## Identity & Authentication
- Implement full identity concerns by adding services to `MIC.Infrastructure.Identity` so they surface through interfaces (`IPasswordHasher`, `IJwtTokenService`, `IAuthenticationService`).
- Gmail/Outlook OAuth is keyed DI (`AddKeyedSingleton<IEmailOAuth2Service>`); reuse that pattern for additional providers.
- Never embed secrets in code—respect the runtime secret retrieval flow (`IFirstRunSetupService` → JWT key fallback) and keep hashing via the shared `PasswordHasher` implementation.

## Data & Migrations
- Database initialization runs through `DbInitializer` governed by `DatabaseSettings`; honor those flags when altering startup behavior or migrations.
- Paths for SQLite/PostgreSQL are resolved in infrastructure—keep a single source of truth and log the resolved location during startup.

## Desktop Patterns
- Use `NotificationService.Instance`, `UserSessionService.Instance`, and `NavigationService` rather than spinning up extra singletons.
- When adding view models, register them as transient services and surface interactions (dialogs, navigation) through injected abstractions instead of `Window` references.
- View-model tests rely on deterministic schedulers: set `RxApp.MainThreadScheduler/TaskpoolScheduler` to `CurrentThreadScheduler` and use helpers like `SessionStorageScope` to isolate `UserSessionService` state.

## Testing & Coverage
- Unit tests live in `MIC.Tests.Unit` (FluentAssertions + Moq); integration tests use Testcontainers in `MIC.Tests.Integration` and require Docker.
- Follow existing patterns: seed fake repositories/services, reflect into `Program.ServiceProvider` when a view model expects configured DI, and assert notifications via the provided `TestNotificationService`.
- Coverage expectations are ≥80% line coverage across unit + integration + E2E runs; collect with `dotnet test --collect:"XPlat Code Coverage"` and store Cobertura outputs under `MIC.Tests.* /TestResults`.

## Build & Tooling
- Primary workflow: `dotnet build MIC.slnx`, `dotnet test MIC.Tests.Unit`, `dotnet test MIC.Tests.Integration --logger:"console;verbosity=normal"`, optional `MIC.Tests.E2E` for smoke checks.
- Use `Setup-DbAndBuild.ps1` for first-run database prep and migrations; keep scripts idempotent and respect `DatabaseSettings` flags.
- Logging is centralized through Serilog (configured in `Program.ConfigureSerilog`); add contextual logs instead of direct `Console.WriteLine`.