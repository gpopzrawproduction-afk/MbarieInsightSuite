# Mbarie Insight Suite

Operational intelligence desktop console delivering real-time monitoring, resilient email intelligence, AI-powered insights, and rigorous integration testing for data, notifications, and analytics.

## Quick Start

### Prerequisites
- .NET 9 SDK
- PostgreSQL 15+ (or SQLite for local dev)
- OpenAI API key (optional for AI features)

### Local Development

1. Clone the repository:
   ```bash
   git clone https://github.com/gpopzrawproduction-afk/MbarieInsightSuite.git
   cd MbarieInsightSuite/src/MIC
   ```

2. Configure environment variables (PowerShell example):
   ```powershell
   $env:MIC_ADMIN_USERNAME = "admin"
   $env:MIC_ADMIN_PASSWORD = "YourSecurePassword123!"
   $env:MIC_ADMIN_EMAIL = "admin@mbarieservicesltd.com"
   $env:MIC_ADMIN_FULLNAME = "System Administrator"
   $env:MIC_AI__OpenAI__ApiKey = "your-openai-api-key"
   ```

3. (Optional) Initialize the development database and apply migrations:
   ```powershell
   ./Setup-DbAndBuild.ps1
   ```

4. Run the application:
   ```bash
   dotnet run --project .\MIC.Desktop.Avalonia
   ```

5. Login with the admin credentials you set via environment variables.

### Build & Deploy

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

### Testing

```bash
# Run unit tests
dotnet test MIC.Tests.Unit

# Run integration tests
dotnet test MIC.Tests.Integration --logger:"console;verbosity=normal"

# Run with code coverage (requires Testcontainers/Docker)
dotnet test --collect:"XPlat Code Coverage"
```

**Coverage target:** Phase 0 requires ≥ 80% line coverage across unit, integration, and E2E suites. Current combined baseline sits near 13% line coverage; ensure new pull requests add meaningful tests.

## Architecture

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    MIC.Desktop.Avalonia                     │
│  (Avalonia UI Layer - Views, ViewModels, Services)         │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                 MIC.Core.Application                        │
│  (Application Logic - Commands, Queries, Handlers)         │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    MIC.Core.Domain                          │
│  (Domain Models - Entities, Value Objects, Domain Events)  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│              MIC.Infrastructure.*                           │
│  (Data, Identity, AI, Monitoring Implementation)           │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Patterns
- **Clean Architecture**: Separation of concerns with dependency inversion
- **CQRS**: Separate command and query responsibilities using MediatR
- **Repository Pattern**: Abstract data access layer
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection with project-wide registration via `AddApplication` and desktop bootstrap wiring
- **Resilient Error Handling**: Domain-specific exception hierarchy, centralized `IErrorHandlingService`, and Polly retry policies for external dependencies

## Features

### Core Features
- **Real-time Monitoring**: Dashboard with operational metrics, alerting, and live updates
- **AI-Powered Insights**: Email analysis, chat assistant, predictions, and knowledge base search
- **Email Integration**: OAuth-secured Gmail/Outlook sync with attachment ingestion and retry policies
- **Alert Management**: Create, view, acknowledge, and export intelligence alerts
- **User Authentication**: JWT-based authentication with role-based access and secure password hashing
- **Global Error Handling**: Desktop-wide error reporting pipeline with user notifications and telemetry hooks

### AI Capabilities
- **Email Analysis**: Priority detection, sentiment analysis, action item extraction
- **Chat Assistant**: Context-aware conversations with business data
- **Predictive Analytics**: AI-powered trend analysis, anomaly detection, and future forecasting
- **Knowledge Base**: Document indexing and semantic search across uploaded files
- **Automated Summarization**: Brief summaries of emails and reports
- **Real-time Predictions**: Live forecasting of email trends, alert patterns, and system metrics

### Predictions & Forecasting
The MIC application now includes advanced AI-powered predictive analytics:

- **Email Trend Forecasting**: Predict future email volumes and patterns using historical data
- **Alert Pattern Analysis**: Identify emerging alert trends and potential issues before they occur
- **Metric Anomaly Detection**: Automatically detect unusual patterns in system metrics
- **Time Horizon Control**: Configure prediction timeframes (hours, days, weeks)
- **Confidence Scoring**: Each prediction includes confidence levels and reasoning
- **Real-time Updates**: Predictions refresh automatically as new data becomes available

## Configuration

### Environment Variables
Key environment variables (see `.env.example` for complete list):
```
# Admin bootstrap (first run only if no users exist)
MIC_ADMIN_USERNAME=admin
MIC_ADMIN_PASSWORD=YourSecurePassword123!
MIC_ADMIN_EMAIL=admin@mbarieservicesltd.com
MIC_ADMIN_FULLNAME=System Administrator

# AI Configuration
MIC_AI__OpenAI__ApiKey=your-openai-api-key
MIC_AI__AzureOpenAI__Endpoint=your-azure-endpoint
MIC_AI__AzureOpenAI__ApiKey=your-azure-key

# Database
MIC_ConnectionStrings__MicDatabase=Host=localhost;Port=5432;Database=micdb;Username=postgres;Password=password

# OAuth2 (Email Integration)
MIC_OAuth2__Gmail__ClientId=your-gmail-client-id
MIC_OAuth2__Gmail__ClientSecret=your-gmail-client-secret
MIC_OAuth2__Outlook__ClientId=your-outlook-client-id
MIC_OAuth2__Outlook__ClientSecret=your-outlook-client-secret

# JWT Settings
MIC_JwtSettings__SecretKey=your-jwt-secret-key
```

### Configuration Files
- `appsettings.json`: Base configuration (committed)
- `appsettings.Development.json`: Development overrides (committed)
- `appsettings.Production.json`: Production overrides (committed)

## Development

### Project Structure
```
MIC/
├── MIC.Desktop.Avalonia/          # Avalonia UI application
├── MIC.Core.Application/          # Application layer (CQRS)
├── MIC.Core.Domain/              # Domain layer (entities, aggregates)
├── MIC.Core.Intelligence/        # Intelligence services
├── MIC.Infrastructure.AI/        # AI service implementations
├── MIC.Infrastructure.Data/      # Data persistence (EF Core)
├── MIC.Infrastructure.Identity/  # Authentication & authorization
├── MIC.Infrastructure.Monitoring/# Telemetry and monitoring
├── MIC.Tests.Unit/               # Unit tests
├── MIC.Tests.Integration/        # Integration tests
└── MIC.Tests.E2E/                # End-to-end tests
```

### Adding New Features
1. **Domain Layer**: Add entities in `MIC.Core.Domain/Entities/`
2. **Application Layer**: Add commands/queries in `MIC.Core.Application/Features/`
3. **Infrastructure**: Implement repositories/services in appropriate infrastructure project
4. **UI Layer**: Add views/viewmodels in `MIC.Desktop.Avalonia/`

### Testing Strategy
- **Unit Tests**: Validate handlers, services, and view models (FluentAssertions + Moq)
- **Integration Tests**: Execute against PostgreSQL Testcontainers for data persistence scenarios
- **E2E Tests**: Exercise critical desktop workflows (currently smoke coverage only)
- **Code Coverage**: Target ≥ 80% before Phase 1; track Cobertura outputs under `MIC.Tests.* /TestResults`

## Security

### Authentication & Authorization
- JWT-based authentication with configurable expiration
- Password hashing using Argon2id via `PasswordHasher`
- Environment-based secret management
- No hard-coded credentials in source code

### Security Best Practices
- **Secrets Management**: Use environment variables or user secrets
- **Input Validation**: Validate all user inputs
- **SQL Injection Protection**: Use EF Core parameterized queries
- **XSS Protection**: Avalonia provides built-in protection

## Deployment

### CI/CD Pipeline
GitHub Actions workflow automates:
- Build on every push
- Run tests with code coverage
- Publish artifacts on release tags
- Create GitHub releases

### Deployment Options
1. **Standalone Executable**: Self-contained .NET publish
2. **Installer**: Windows installer using Inno Setup
3. **Container**: Docker container (future)

### Health Checks
- Database connectivity check
- AI service availability
- Authentication service status

### Monitoring & Observability

### Logging
- Structured logging with Serilog
- Console + rolling file logs in `%LocalAppData%\MIC\logs`
- Log levels configurable per environment
- Error pipeline integrates with `IErrorHandlingService` for consistent messaging

### Health Checks
- Desktop bootstrap validates database connectivity and migrations during startup
- Background services emit telemetry through monitoring infrastructure (planned)

## Contributing

### Development Workflow
1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Run test suite
5. Submit pull request

### Code Style
- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation for public APIs
- Write unit tests for new features

### Commit Messages
Follow Conventional Commits format:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Test changes
- `refactor:` Code refactoring
- `chore:` Maintenance tasks

## License

[Add your license here]

## Support

- **Documentation**: [docs/](docs/)
- **Issue Tracker**: [GitHub Issues](https://github.com/your-org/mbarie-intelligence-console/issues)
- **Discussion**: [GitHub Discussions](https://github.com/your-org/mbarie-intelligence-console/discussions)

## Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/)
- AI powered by [OpenAI](https://openai.com/) / [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
- Icons from [Material Design Icons](https://materialdesignicons.com/)
- Database by [PostgreSQL](https://www.postgresql.org/) / [SQLite](https://www.sqlite.org/)
