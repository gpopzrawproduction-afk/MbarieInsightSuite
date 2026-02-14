# 🔒 PERFECT SOFTWARE PROMPT PACK
## Security Fix + 3 Remaining UI Screens + Zero Warnings
## Mbarie Insight Suite — Avalonia Cross-Platform

**Run these prompts in order. Do not skip steps.**
**Framework:** Avalonia (.axaml files, not .xaml)
**Pattern:** ErrorOr<T>, ICommand<T>, Clean Architecture, CQRS

---

## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## PROMPT 1 OF 5 — SECURITY FIX
## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Paste this as a standalone Copilot session first. Confirm build passes before moving to Prompt 2.

```
TASK: Fix a critical security vulnerability in Mbarie Insight Suite before packaging.

PROBLEM:
The file MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj (or any .csproj in the solution)
may contain a hardcoded certificate password like this:
  <PackageCertificatePassword>YourPassword123!</PackageCertificatePassword>
  <PackageCertificateKeyFile>MIC.Desktop.Avalonia_TemporaryKey.pfx</PackageCertificateKeyFile>

This is a critical security issue — passwords must NEVER be committed to source control.

STEP 1 — SCAN ALL FILES:
Search the entire solution for any hardcoded secrets. Check ALL of these:
  - Every .csproj file
  - Every appsettings*.json file
  - Every .env file
  - Every .axaml and .axaml.cs file
  - Every .cs file in Infrastructure layer
  
Look for patterns like:
  - "Password" =
  - "Secret" =
  - "ApiKey" =
  - "ConnectionString" containing actual credentials
  - Any JWT secret value that is not reading from environment variable
  - Any hardcoded IP, username, or token

Report EVERY instance found, even minor ones.

STEP 2 — FIX THE CERTIFICATE PASSWORD:
Replace any <PackageCertificatePassword> value with:
  <PackageCertificatePassword>$(CERT_PASSWORD)</PackageCertificatePassword>

This reads from an environment variable at build time instead of hardcoding it.

STEP 3 — CREATE A .env.example FILE:
In the project root, create a file called .env.example (NOT .env) with:

```
# Mbarie Insight Suite — Environment Variables
# Copy this file to .env and fill in your values
# NEVER commit .env to Git

# Certificate (for MSIX packaging only)
CERT_PASSWORD=your_certificate_password_here

# Database
DATABASE_CONNECTION_STRING=your_connection_string_here

# JWT
JWT_SECRET=your_jwt_secret_minimum_32_chars_here
JWT_ISSUER=MbarieInsightSuite
JWT_AUDIENCE=MIC_Users
JWT_EXPIRY_MINUTES=60

# OpenAI
OPENAI_API_KEY=sk-your_openai_api_key_here

# Email OAuth (Gmail)
GMAIL_CLIENT_ID=your_gmail_client_id
GMAIL_CLIENT_SECRET=your_gmail_client_secret

# Email OAuth (Outlook)
OUTLOOK_CLIENT_ID=your_outlook_client_id
OUTLOOK_CLIENT_SECRET=your_outlook_client_secret
```

STEP 4 — UPDATE .gitignore:
Open .gitignore and ensure ALL of these are present:

```
# Secrets — NEVER commit these
.env
*.env
*.pfx
*.p12
*.key
*_TemporaryKey.pfx
appsettings.Production.json
secrets.json
launchSettings.json

# Build outputs
bin/
obj/
*.user
AppPackages/
BundleArtifacts/

# OS files
.DS_Store
Thumbs.db
```

STEP 5 — ADD GitHub Actions secret reference:
Create or update the file .github/workflows/build.yml to read the cert password 
from GitHub Secrets, never from the repo:

```yaml
- name: Build MSIX Package
  env:
    CERT_PASSWORD: ${{ secrets.CERT_PASSWORD }}
    JWT_SECRET: ${{ secrets.JWT_SECRET }}
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  run: dotnet publish -c Release
```

STEP 6 — VERIFY:
After all fixes, confirm:
1. Run: git diff --cached -- "*.csproj" to confirm no password in tracked files
2. Run: grep -r "Password123" . to confirm hardcoded password is gone
3. Run: dotnet build — confirm 0 errors, 0 warnings related to security

STEP 7 — COMMIT:
Commit with message: "security: remove hardcoded credentials, add env var pattern"
If the .pfx file was previously committed, also run:
  git rm --cached *.pfx
  git rm --cached *_TemporaryKey.pfx

Report back with: list of everything found and fixed.
```

---

## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## PROMPT 2 OF 5 — ZERO WARNINGS CLEANUP
## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Run this AFTER Prompt 1. Get a clean build before building UI screens.

```
TASK: Eliminate ALL compiler warnings and static analysis warnings in Mbarie Insight Suite.
Goal: dotnet build produces 0 errors and 0 warnings. Perfect clean build.

STEP 1 — GET THE FULL WARNING LIST:
Run this command and paste the output for analysis:
  dotnet build 2>&1 | grep -E "warning|error" | sort | uniq

Do NOT skip this step. We need to see every warning before fixing anything.

STEP 2 — CATEGORIZE AND FIX each warning type:

For CS8618 (Non-nullable property not initialized):
  Fix: Add required keyword, or initialize to default, or mark nullable with ?
  Example fix:
    BEFORE: public string Name { get; set; }
    AFTER:  public string Name { get; set; } = string.Empty;

For CS8600 / CS8602 / CS8604 (Null dereference):
  Fix: Add null checks or null-forgiving operators where null is genuinely impossible
  Example fix:
    BEFORE: var name = user.Profile.Name;
    AFTER:  var name = user.Profile?.Name ?? string.Empty;

For CS0649 (Field never assigned):
  Fix: Remove unused fields, or assign default values

For CS1998 (Async method lacks await):
  Fix: Either add real async work, or remove async keyword and return Task.CompletedTask

For IDE0051 / IDE0052 (Unused private member):
  Fix: Remove the unused member, or add _ prefix if intentionally kept

For IDE0060 (Unused parameter):
  Fix: Either use the parameter or replace with _ (discard)
  Example: private void Handler(object _, EventArgs e)

For NETSDK / MSB warnings (project configuration):
  Fix: Update target framework references, remove deprecated properties

For CA warnings (Code Analysis):
  CA1031: Catch specific exceptions, not Exception
  CA1062: Validate arguments are not null
  CA2007: Consider using ConfigureAwait(false) on awaited tasks

STEP 3 — ENABLE STRICT MODE going forward:
In the root Directory.Build.props file (create if it doesn't exist):

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningLevel>5</WarningLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

STEP 4 — FIX AXAML BINDING WARNINGS:
Search all .axaml files for binding expressions that might generate warnings:
  - Bindings without DataType declarations
  - x:DataType missing on DataTemplates
  - Commands bound to non-existent properties

For every DataTemplate in .axaml files, ensure it has:
  <DataTemplate x:DataType="local:YourViewModelType">

STEP 5 — VERIFY:
Run: dotnet build
Expected output: "Build succeeded." with 0 Error(s) and 0 Warning(s)

If any warnings remain that are genuinely intentional (e.g., obsolete API used 
intentionally), suppress ONLY those specific ones with:
  #pragma warning disable CS0618 // Reason: [explain why]
  ... code ...
  #pragma warning restore CS0618

Never use a blanket suppress. Each suppression needs a comment explaining why.

STEP 6 — RUN ALL TESTS:
  dotnet test
Expected: All tests pass, 0 failures

STEP 7 — COMMIT:
Commit message: "chore: eliminate all compiler warnings, achieve clean build"

Report: Total warnings fixed, final warning count (must be 0).
```

---

## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## PROMPT 3 OF 5 — PREDICTIONS UI SCREEN
## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```
CONTEXT: Mbarie Insight Suite. Avalonia framework (.axaml files).
CQRS pattern. ErrorOr<T> for error handling. All existing tests must still pass.
Visual theme: glassmorphic cyberpunk. Colors from BrandColors.cs.
Entry animations via Avalonia Transitions and Animations.

TASK: Build the complete PredictionView — replacing the current stub with a 
fully functional, visually polished Predictions screen.

━━━ PART A: APPLICATION LAYER ━━━

FILE: Application/Features/Predictions/Queries/GetMetricHistory/GetMetricHistoryQuery.cs
```csharp
public record GetMetricHistoryQuery : IQuery<List<MetricDataPointDto>>
{
    public required string MetricName { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public AggregationPeriod Period { get; init; }
}

public enum AggregationPeriod { Hourly, Daily, Weekly, Monthly }

public record MetricDataPointDto
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public double? LowerBound { get; init; }
    public double? UpperBound { get; init; }
    public bool IsPredicted { get; init; }
}
```

FILE: Application/Features/Predictions/Queries/GetMetricHistory/GetMetricHistoryQueryHandler.cs
Implement handler that:
1. Accepts GetMetricHistoryQuery
2. Fetches real metric data from IMetricRepository for the date range
3. Aggregates by the requested period (Hourly/Daily/Weekly/Monthly)
4. Returns List<MetricDataPointDto> with IsPredicted = false for all entries
5. Returns ErrorOr<List<MetricDataPointDto>> — return Error.NotFound if metric doesn't exist
6. Logs entry and exit with ILogger<GetMetricHistoryQueryHandler>

FILE: Application/Features/Predictions/Queries/GenerateForecast/GenerateForecastQuery.cs
```csharp
public record GenerateForecastQuery : IQuery<ForecastResultDto>
{
    public required string MetricName { get; init; }
    public required List<MetricDataPointDto> HistoricalData { get; init; }
    public int PeriodsAhead { get; init; } = 7;
}

public record ForecastResultDto
{
    public List<MetricDataPointDto> ForecastPoints { get; init; } = new();
    public TrendDirection Trend { get; init; }
    public double TrendPercentage { get; init; }
    public double ConfidenceScore { get; init; }    // 0.0 to 1.0
    public double AnomalyRiskScore { get; init; }   // 0.0 to 1.0
    public AnomalyRisk AnomalyRisk { get; init; }
    public double ForecastedNextValue { get; init; }
    public double StandardDeviation { get; init; }
}

public enum TrendDirection { Upward, Downward, Stable }
public enum AnomalyRisk { Low, Medium, High }
```

FILE: Application/Features/Predictions/Queries/GenerateForecast/GenerateForecastQueryHandler.cs
Implement a REAL prediction engine (no stubs) using this algorithm:

```csharp
// LINEAR REGRESSION TREND:
// 1. Get last 30 data points
// 2. Calculate slope: slope = (n*sumXY - sumX*sumY) / (n*sumX2 - sumX*sumX)
// 3. Calculate intercept: intercept = (sumY - slope*sumX) / n
// 4. Project forward by PeriodsAhead using: predicted = slope * x + intercept

// CONFIDENCE SCORE:
// 1. Calculate R-squared of the linear fit
// 2. Confidence = R-squared value (0.0 to 1.0)
// 3. Adjust down if data has high variance

// STANDARD DEVIATION for bounds:
// 1. Calculate std dev of historical data
// 2. LowerBound = predicted - (1.5 * stdDev)
// 3. UpperBound = predicted + (1.5 * stdDev)

// TREND DIRECTION:
// Upward if slope > 0.02 * mean, Downward if slope < -0.02 * mean, else Stable

// ANOMALY RISK:
// Low if predicted is within 1 stdDev of mean
// Medium if within 2 stdDev
// High if beyond 2 stdDev
```

FILE: Application/Features/Predictions/Queries/GetAvailableMetrics/GetAvailableMetricsQuery.cs
```csharp
public record GetAvailableMetricsQuery : IQuery<List<string>>;
// Returns the list of all metric names available in the system
```
Implement handler that fetches distinct metric names from IMetricRepository.

VALIDATORS:
Write FluentValidation validators for GetMetricHistoryQuery and GenerateForecastQuery:
- MetricName: NotEmpty, MaxLength(200)
- From: Must be before To
- To: Must not be in the future beyond 1 day
- PeriodsAhead: Between 1 and 365
- HistoricalData: Must have at least 7 data points for a valid forecast

UNIT TESTS (xUnit):
Write tests for GenerateForecastQueryHandler covering:
1. Happy path: 30 data points → returns valid ForecastResultDto
2. Upward trend detected: data increasing → TrendDirection.Upward
3. Downward trend: data decreasing → TrendDirection.Downward
4. Too few data points: fewer than 7 → returns validation error
5. Empty historical data → returns Error.Validation
6. Confidence score: perfect linear data → ConfidenceScore near 1.0

━━━ PART B: VIEWMODEL ━━━

FILE: Presentation/ViewModels/PredictionViewModel.cs

Properties:
```csharp
// Controls
public ObservableCollection<string> AvailableMetrics { get; }
public string? SelectedMetric { get; set; }
public HistoryRange SelectedHistoryRange { get; set; } = HistoryRange.Days30;
public ForecastPeriod SelectedForecastPeriod { get; set; } = ForecastPeriod.Days7;
public bool IsLoading { get; private set; }
public bool HasData { get; private set; }
public bool HasError { get; private set; }
public string ErrorMessage { get; private set; } = string.Empty;

// Chart data — use ObservableCollection for Avalonia binding
public ObservableCollection<MetricDataPointDto> HistoricalPoints { get; }
public ObservableCollection<MetricDataPointDto> ForecastPoints { get; }

// Insight card values
public TrendDirection Trend { get; private set; }
public string TrendLabel { get; private set; } = string.Empty;    // "UPWARD" / "DOWNWARD" / "STABLE"
public string TrendIcon { get; private set; } = "→";              // ↑ / ↓ / →
public string TrendColor { get; private set; }                    // AccentGreen / DangerRed / AccentCyan

public double ForecastedNextValue { get; private set; }
public string ForecastedNextValueFormatted { get; private set; } = string.Empty;
public double ConfidenceScore { get; private set; }
public int ConfidencePercent { get; private set; }                // 0-100 for UI

public AnomalyRisk AnomalyRisk { get; private set; }
public string AnomalyRiskLabel { get; private set; } = string.Empty;
public string AnomalyRiskColor { get; private set; } = string.Empty;

// Forecast table
public ObservableCollection<ForecastTableRowDto> ForecastTableRows { get; }

public enum HistoryRange { Hours24, Days7, Days30, Days90 }
public enum ForecastPeriod { Days7, Days14, Days30 }
```

Commands:
```csharp
public IRelayCommand GenerateForecastCommand { get; }   // calls both queries, populates all data
public IRelayCommand<HistoryRange> SetHistoryRangeCommand { get; }
public IRelayCommand<ForecastPeriod> SetForecastPeriodCommand { get; }
public IRelayCommand RefreshCommand { get; }
public IRelayCommand ExportCommand { get; }  // exports forecast table to CSV
```

GenerateForecastCommand implementation:
1. Set IsLoading = true, HasError = false
2. Send GetMetricHistoryQuery via MediatR
3. If error → set HasError = true, ErrorMessage, IsLoading = false, return
4. Send GenerateForecastQuery with history as input
5. If error → set HasError = true, ErrorMessage, IsLoading = false, return
6. Populate all properties from ForecastResultDto
7. Build ForecastTableRows from ForecastPoints
8. Set HasData = true, IsLoading = false
9. Use CancellationToken from a stored CancellationTokenSource
10. On cancellation: just set IsLoading = false

━━━ PART C: VIEW (.axaml) ━━━

FILE: Presentation/Views/PredictionView.axaml

FULL LAYOUT (Avalonia AXAML, NOT WPF XAML):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:MIC.Desktop.Avalonia.ViewModels"
             x:DataType="vm:PredictionViewModel">

  <!-- Root: dark background with grid texture layered -->
  <Grid RowDefinitions="Auto,Auto,*,Auto">

    <!-- ROW 0: HEADER BAR -->
    <!-- Title "PREDICTIVE INTELLIGENCE" in #BF40FF (AccentMagenta) -->
    <!-- Subtitle "AI-POWERED METRIC FORECASTING" in TextDim -->
    <!-- Right side: Refresh button + Export button -->

    <!-- ROW 1: CONTROL BAR (glass panel) -->
    <!-- 4 sections horizontally: -->
    <!-- 1. Metric dropdown: "SELECT METRIC" label + ComboBox bound to AvailableMetrics -->
    <!-- 2. History range: "DATA RANGE" label + 4 pill ToggleButtons: 24H / 7D / 30D / 90D -->
    <!-- 3. Forecast period: "FORECAST" label + 3 pill ToggleButtons: 7 Days / 14 Days / 30 Days -->
    <!-- 4. Generate button: Large "GENERATE FORECAST" button with AccentMagenta glow -->

    <!-- ROW 2: MAIN CONTENT -->
    <Grid ColumnDefinitions="*,320" Row="2">

      <!-- LEFT: CHART PANEL (glass, AccentMagenta border) -->
      <!-- When IsLoading=true: show skeleton placeholder -->
      <!--   Skeleton: 3 horizontal gradient bars animating opacity -->
      <!--   Text: "RUNNING PREDICTIVE MODELS..." -->
      <!-- When HasData=true: show chart -->
      <!--   Use OxyPlot.Avalonia or LiveChartsCore.SkiaSharpView.Avalonia -->
      <!--   Historical series: AccentCyan solid line + area fill at 10% opacity -->
      <!--   Forecast series: AccentMagenta dashed line -->
      <!--   Confidence band: AccentMagenta at 8% opacity area between upper/lower bounds -->
      <!--   Vertical "TODAY" marker line: white dashed -->
      <!-- When HasError=true: error state panel -->
      <!--   Icon + ErrorMessage text + Retry button -->

      <!-- RIGHT: INSIGHTS PANEL (4 stacked glass cards) -->
      <!-- CARD 1: TREND DIRECTION -->
      <!--   Large trend icon (↑↓→) in TrendColor -->
      <!--   "TREND" label (TextDim, uppercase) -->
      <!--   TrendLabel text in TrendColor (UPWARD/DOWNWARD/STABLE) -->
      <!--   "X.X% per period" subtitle -->

      <!-- CARD 2: FORECAST VALUE -->
      <!--   Target/bullseye icon (AccentMagenta) -->
      <!--   "NEXT PERIOD" label -->
      <!--   ForecastedNextValueFormatted in Rajdhani 36px AccentMagenta -->
      <!--   ± confidence interval subtitle -->

      <!-- CARD 3: CONFIDENCE SCORE -->
      <!--   "CONFIDENCE" label -->
      <!--   Circular progress indicator (draw as Arc or use ProgressRing) -->
      <!--   ConfidencePercent% in center -->
      <!--   "Based on X data points" subtitle -->

      <!-- CARD 4: ANOMALY RISK -->
      <!--   Warning icon in AnomalyRiskColor -->
      <!--   "ANOMALY RISK" label -->
      <!--   AnomalyRiskLabel (LOW/MEDIUM/HIGH) in AnomalyRiskColor -->
      <!--   Risk description text -->
    </Grid>

    <!-- ROW 3: FORECAST TABLE (collapsible) -->
    <!-- Expander: "FORECAST DATA TABLE ▾" header -->
    <!-- DataGrid bound to ForecastTableRows -->
    <!-- Columns: Date | Predicted Value | Lower Bound | Upper Bound | Change % -->
    <!-- Alternating row colors, AccentMagenta header -->
    <!-- Empty state if no data -->
  </Grid>
</UserControl>
```

ANIMATIONS (Avalonia):
1. Header: Opacity 0→1, TranslateY -10→0, Duration 350ms on load
2. Control bar: Opacity 0→1, TranslateY 20→0, Duration 350ms, Delay 100ms
3. Chart panel: Opacity 0→1, Duration 400ms, Delay 200ms
4. Insight cards: each staggered, Delay 300ms, 350ms, 400ms, 450ms
5. Loading skeleton: Opacity 0.3→0.7→0.3, infinite loop, Duration 1200ms
6. On data load: chart fades in over 500ms

Wire PredictionView.axaml.cs to PredictionViewModel.
Register PredictionViewModel in DI container.
Add PredictionView to the navigation system (MainWindowViewModel or router).

━━━ PART D: NAVIGATION WIRING ━━━
Ensure Predictions is accessible from the left navigation sidebar.
The menu item should NOT be hidden or disabled.
If it was previously hidden with IsEnabled=false or Visibility=Collapsed, fix it now.

COMMIT: "feat: complete PredictionView with real forecast engine and full UI"
Run: dotnet build → 0 errors, 0 warnings
Run: dotnet test → all pass
```

---

## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## PROMPT 4 OF 5 — REPORTS UI SCREEN
## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```
CONTEXT: Mbarie Insight Suite. Avalonia framework (.axaml).
ErrorOr<T> pattern, CQRS, Clean Architecture.
All existing tests must still pass.

TASK: Build the complete ReportsView — replacing any stub with a fully 
functional, visually polished Reports screen.

━━━ PART A: APPLICATION LAYER ━━━

If not already implemented, add these to the Application layer:

FILE: Application/Features/Reports/Commands/GenerateReport/GenerateReportCommand.cs
```csharp
public record GenerateReportCommand : ICommand<ReportGeneratedDto>
{
    public required ReportType Type { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public ReportFormat Format { get; init; } = ReportFormat.PDF;
    public bool IncludeCharts { get; init; } = true;
    public bool IncludeRawData { get; init; } = false;
    public bool EmailToSelf { get; init; } = false;
}

public enum ReportType
{
    AlertSummary,
    EmailActivity,
    MetricsTrend,
    AiChatSummary,
    FullDashboard
}

public enum ReportFormat { PDF, XLSX, CSV }

public record ReportGeneratedDto
{
    public Guid ReportId { get; init; }
    public string OutputFilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime GeneratedAt { get; init; }
    public ReportFormat Format { get; init; }
    public ReportType Type { get; init; }
}
```

FILE: Application/Features/Reports/Commands/GenerateReport/GenerateReportCommandHandler.cs
The handler must:
1. Validate date range (From must be before To, range max 1 year)
2. Fetch relevant data from repositories based on ReportType:
   - AlertSummary: fetch alerts in date range from IAlertRepository
   - EmailActivity: fetch emails in date range from IEmailRepository
   - MetricsTrend: fetch metrics from IMetricRepository
   - AiChatSummary: fetch chat sessions
   - FullDashboard: fetch all of the above
3. Build report content as structured data (do NOT use external PDF library yet)
   Use plain text or JSON output for now — the actual file generation can be added in v1.1.
   For now: write a .txt report file with sections and data tables.
4. Save file to: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                               "MbarieInsightSuite", "Reports", userId, filename)
5. Create directory if it doesn't exist
6. Save Report entity to database via IReportRepository
7. Notify via INotificationService: "Report ready: {filename} - Click to open"
8. Return ReportGeneratedDto

FILE: Application/Features/Reports/Queries/GetReports/GetReportsQuery.cs
```csharp
public record GetReportsQuery : IQuery<List<ReportSummaryDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record ReportSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public ReportType Type { get; init; }
    public ReportFormat Format { get; init; }
    public DateTime GeneratedAt { get; init; }
    public long FileSizeBytes { get; init; }
    public string FileSizeFormatted { get; init; } = string.Empty; // "2.3 MB"
    public string OutputFilePath { get; init; } = string.Empty;
    public bool FileExists { get; init; }  // true if file still exists on disk
}
```

FILE: Application/Features/Reports/Commands/DeleteReport/DeleteReportCommand.cs
```csharp
public record DeleteReportCommand : ICommand
{
    public required Guid ReportId { get; init; }
}
```
Handler: deletes the Report entity from DB AND deletes the file from disk if it exists.

VALIDATORS:
- GenerateReportCommand: FromDate < ToDate, date range max 365 days, Type is valid enum
- DeleteReportCommand: ReportId not empty

UNIT TESTS:
1. GenerateReportCommandHandler_ValidAlertSummary_CreatesFileAndReturnsDto
2. GenerateReportCommandHandler_InvalidDateRange_ReturnsValidationError
3. GenerateReportCommandHandler_FromDateAfterToDate_ReturnsError
4. DeleteReportCommandHandler_ExistingReport_DeletesFromDbAndDisk
5. DeleteReportCommandHandler_NonExistentReport_ReturnsNotFoundError
6. GetReportsQueryHandler_ReturnsPagedResults

━━━ PART B: VIEWMODEL ━━━

FILE: Presentation/ViewModels/ReportsViewModel.cs

```csharp
public partial class ReportsViewModel : ViewModelBase
{
    // Report type selection
    public ReportType SelectedReportType { get; set; } = ReportType.AlertSummary;
    
    // Date range
    public DateTimeOffset FromDate { get; set; } = DateTimeOffset.Now.AddDays(-30);
    public DateTimeOffset ToDate { get; set; } = DateTimeOffset.Now;
    
    // Format selection
    public ReportFormat SelectedFormat { get; set; } = ReportFormat.PDF;
    
    // Options
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeRawData { get; set; }
    public bool EmailToSelf { get; set; }
    
    // State
    public bool IsGenerating { get; private set; }
    public double GenerationProgress { get; private set; }   // 0.0 to 1.0
    public string GenerationStatusText { get; private set; } = string.Empty;
    
    // Report list
    public ObservableCollection<ReportSummaryDto> Reports { get; } = new();
    public bool HasReports { get; private set; }
    public bool IsLoadingReports { get; private set; }
    
    // Selected report for actions
    public ReportSummaryDto? SelectedReport { get; set; }
    
    // Report type cards — which one is active
    public bool IsAlertSummarySelected { get; private set; } = true;
    public bool IsEmailActivitySelected { get; private set; }
    public bool IsMetricsTrendSelected { get; private set; }
    public bool IsAiChatSelected { get; private set; }
    public bool IsFullDashboardSelected { get; private set; }
    
    // Commands
    public IRelayCommand GenerateReportCommand { get; }
    public IRelayCommand<ReportType> SelectReportTypeCommand { get; }
    public IRelayCommand<ReportSummaryDto> OpenReportCommand { get; }    // opens file with default app
    public IRelayCommand<ReportSummaryDto> DeleteReportCommand { get; }
    public IRelayCommand<ReportSummaryDto> RegenerateReportCommand { get; }
    public IRelayCommand LoadReportsCommand { get; }
    public IRelayCommand<ReportFormat> SelectFormatCommand { get; }
}
```

GenerateReportCommand implementation:
1. Validate SelectedMetric is chosen
2. Set IsGenerating = true
3. Simulate progress updates: "Fetching data..." at 20%, "Building report..." at 60%, 
   "Saving file..." at 90%, "Complete!" at 100%
   Use Task.Delay(500) between steps for user feedback
4. Send GenerateReportCommand via MediatR
5. On success: add result to Reports collection at position 0, show success notification
6. On error: show error in a dialog via IDialogService
7. Set IsGenerating = false

OpenReportCommand:
  Use Process.Start(new ProcessStartInfo(report.OutputFilePath) { UseShellExecute = true })
  to open the file with the user's default application (PDF viewer, Excel, etc.)

━━━ PART C: VIEW (.axaml) ━━━

FILE: Presentation/Views/ReportsView.axaml

FULL LAYOUT:

```xml
<UserControl xmlns="https://github.com/avaloniaui" ...>
  <DockPanel>

    <!-- HEADER: "INTELLIGENCE REPORTS" + report count -->
    <Border DockPanel.Dock="Top" ... >
      <!-- Title in Rajdhani AccentCyan -->
      <!-- "X reports generated" subtitle in TextDim -->
    </Border>

    <!-- REPORT TYPE CARDS (horizontal scrollable row) -->
    <ScrollViewer DockPanel.Dock="Top" HorizontalScrollBarVisibility="Auto">
      <ItemsControl Items="{Binding ReportTypes}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="12"/>
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <!-- Each card: 200×160px glass panel -->
        <!-- Large icon (48px), report name, description, sample badge -->
        <!-- Selected card: AccentCyan border + scale 1.03 -->
        <!-- Hover: transition opacity, border glow -->
      </ItemsControl>
    </ScrollViewer>

    <!-- CONFIGURATION PANEL (glass, full width) -->
    <Border DockPanel.Dock="Top" ... >
      <Grid ColumnDefinitions="*,*,*">
        <!-- COL 1: DATE RANGE -->
        <!-- "DATE RANGE" label -->
        <!-- CalendarDatePicker for FromDate -->
        <!-- CalendarDatePicker for ToDate -->
        <!-- Quick select buttons: This Week / This Month / Last 30 Days -->

        <!-- COL 2: FORMAT -->
        <!-- "EXPORT FORMAT" label -->
        <!-- 3 format cards: PDF / XLSX / CSV with icons -->
        <!-- Selected format: AccentCyan filled background -->

        <!-- COL 3: OPTIONS -->
        <!-- "OPTIONS" label -->
        <!-- ToggleSwitch: Include Charts -->
        <!-- ToggleSwitch: Include Raw Data -->
        <!-- ToggleSwitch: Email report to me -->
      </Grid>

      <!-- GENERATE BUTTON (full width at bottom of config panel) -->
      <!-- Text: "GENERATE REPORT" when idle -->
      <!-- Text: GenerationStatusText when IsGenerating=true -->
      <!-- ProgressBar below button, visible only when IsGenerating=true -->
    </Border>

    <!-- RECENT REPORTS TABLE -->
    <Border DockPanel.Dock="Bottom" Padding="0,16,0,0">
      <!-- "RECENT REPORTS" header + "Clear All" link -->

      <!-- When HasReports=false: empty state -->
      <!-- Large icon + "NO REPORTS GENERATED" -->
      <!-- "Select a report type above and click Generate" -->

      <!-- When HasReports=true: DataGrid -->
      <DataGrid Items="{Binding Reports}"
                SelectedItem="{Binding SelectedReport}"
                IsReadOnly="True">
        <DataGrid.Columns>
          <DataGridTextColumn Header="REPORT TYPE" Binding="{Binding Type}"/>
          <DataGridTextColumn Header="GENERATED" Binding="{Binding GeneratedAt, StringFormat='{}{0:dd MMM yyyy HH:mm}'}"/>
          <DataGridTextColumn Header="FORMAT" Binding="{Binding Format}"/>
          <DataGridTextColumn Header="SIZE" Binding="{Binding FileSizeFormatted}"/>
          <!-- Actions column: Open / Delete / Regenerate buttons -->
        </DataGrid.Columns>
      </DataGrid>
    </Border>
  </DockPanel>
</UserControl>
```

ANIMATIONS:
1. Report type cards: staggered FadeInUp, 80ms delay per card on load
2. Type card selection: smooth border color transition (0.2s)
3. Generate button click: button scales 0.97 briefly, then progress bar animates in
4. New report in table: row slides down from top with height animation
5. Delete: row fades out + collapses height before being removed

Wire ReportsView.axaml.cs to ReportsViewModel.
Register ReportsViewModel in DI.
Ensure Reports is accessible from the sidebar — NOT hidden.

COMMIT: "feat: complete ReportsView with generation engine and full UI"
Run: dotnet build → 0 errors, 0 warnings
Run: dotnet test → all pass
```

---

## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## PROMPT 5 OF 5 — NOTIFICATIONS PANEL
## ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```
CONTEXT: Mbarie Insight Suite. Avalonia framework (.axaml).
ErrorOr<T>, CQRS, Clean Architecture.

TASK: Build the complete NotificationsPanelView — a slide-in overlay drawer 
that appears when the bell icon in the header is clicked. This must be a real,
working, persistent notification system — NOT a stub.

━━━ PART A: DOMAIN & APPLICATION LAYER ━━━

Verify (or create if missing) the Notification entity in Domain layer:

FILE: Domain/Entities/Notification.cs
```csharp
public class Notification : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationSeverity Severity { get; private set; }
    public bool IsRead { get; private set; }
    public bool IsDismissed { get; private set; }
    public string? ActionRoute { get; private set; }  // e.g., "alerts/123" for deep link
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static Notification Create(
        string title, string message,
        NotificationType type, NotificationSeverity severity,
        string userId, string? actionRoute = null)
    { ... }

    public void MarkAsRead() { IsRead = true; }
    public void Dismiss() { IsDismissed = true; }
}

public enum NotificationType { Alert, Email, System, AiEvent, Report }
public enum NotificationSeverity { Info, Warning, Critical }
```

FILE: Application/Features/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs
```csharp
public record GetNotificationsQuery : IQuery<NotificationsResultDto>
{
    public NotificationType? FilterType { get; init; }  // null = all
    public bool IncludeRead { get; init; } = false
    public int PageSize { get; init; } = 50;
}

public record NotificationsResultDto
{
    public List<NotificationDto> Notifications { get; init; } = new();
    public int UnreadCount { get; init; }
    public int TotalCount { get; init; }
}

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public NotificationSeverity Severity { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TimeAgo { get; init; } = string.Empty;  // "2 min ago", "1 hr ago"
    public string? ActionRoute { get; init; }
    public string IconColor { get; init; } = string.Empty;  // based on Type + Severity
}
```

FILE: Application/Features/Notifications/Commands/MarkNotificationRead/MarkNotificationReadCommand.cs
```csharp
public record MarkNotificationReadCommand : ICommand
{
    public required Guid NotificationId { get; init; }
}
```

FILE: Application/Features/Notifications/Commands/MarkAllRead/MarkAllNotificationsReadCommand.cs
```csharp
public record MarkAllNotificationsReadCommand : ICommand
{
    public required string UserId { get; init; }
}
```

FILE: Application/Features/Notifications/Commands/DismissNotification/DismissNotificationCommand.cs
```csharp
public record DismissNotificationCommand : ICommand
{
    public required Guid NotificationId { get; init; }
}
```

Implement all 4 handlers. Each handler:
- Uses INotificationRepository
- Logs with ILogger<T>
- Returns ErrorOr<Success> or ErrorOr<NotificationsResultDto>

TimeAgo calculation for NotificationDto:
```csharp
private static string GetTimeAgo(DateTime createdAt)
{
    var diff = DateTime.UtcNow - createdAt;
    return diff.TotalMinutes < 1    ? "just now"
         : diff.TotalMinutes < 60   ? $"{(int)diff.TotalMinutes} min ago"
         : diff.TotalHours < 24     ? $"{(int)diff.TotalHours} hr ago"
         : diff.TotalDays < 7       ? $"{(int)diff.TotalDays} days ago"
         : createdAt.ToString("dd MMM");
}
```

UNIT TESTS:
1. GetNotificationsQueryHandler_ReturnsCorrectUnreadCount
2. GetNotificationsQueryHandler_WithTypeFilter_ReturnsOnlyThatType
3. MarkNotificationReadCommand_ExistingNotification_MarksAsRead
4. MarkAllNotificationsReadCommand_MultiplUnread_MarksAllAsRead
5. DismissNotificationCommand_ExistingNotification_SetsDismissed
6. TimeAgo_JustNow_ReturnsJustNow
7. TimeAgo_2HoursAgo_Returns2HrAgo

━━━ PART B: VIEWMODEL ━━━

FILE: Presentation/ViewModels/NotificationsPanelViewModel.cs

```csharp
public partial class NotificationsPanelViewModel : ViewModelBase
{
    // Panel state
    public bool IsPanelOpen { get; set; }
    public bool IsLoading { get; private set; }
    
    // Filter
    public NotificationType? ActiveFilter { get; set; }  // null = ALL
    
    // Data
    public ObservableCollection<NotificationDto> Notifications { get; } = new();
    public int UnreadCount { get; private set; }
    public bool HasNotifications { get; private set; }
    public bool HasUnread { get; private set; }
    
    // Grouped for display
    public ObservableCollection<NotificationGroupDto> GroupedNotifications { get; } = new();

    // Commands
    public IRelayCommand<NotificationType?> SetFilterCommand { get; }
    public IRelayCommand<NotificationDto> MarkReadCommand { get; }
    public IRelayCommand<NotificationDto> DismissCommand { get; }    // swipe/click dismiss
    public IRelayCommand MarkAllReadCommand { get; }
    public IRelayCommand<NotificationDto> NavigateToSourceCommand { get; }  // deep link
    public IRelayCommand OpenSettingsCommand { get; }   // opens notification settings
    public IRelayCommand ClosePanelCommand { get; }
    public IRelayCommand RefreshCommand { get; }
}

public record NotificationGroupDto
{
    public string GroupLabel { get; init; } = string.Empty;  // "TODAY", "YESTERDAY", "THIS WEEK"
    public List<NotificationDto> Items { get; init; } = new();
}
```

IsPanelOpen behavior:
- When set to true: load notifications if not already loaded
- Subscribe to a real-time notification event from INotificationEventService
- When a new notification arrives: add to top of list, increment UnreadCount

━━━ PART C: VIEW (.axaml) ━━━

FILE: Presentation/Views/NotificationsPanelView.axaml

IMPORTANT: This is a PANEL (drawer), not a separate window.
It overlays the main content area from the right side.
Implement it as a UserControl that slides in from the right edge.

```xml
<UserControl xmlns="https://github.com/avaloniaui" ...
             Width="380">
  
  <!-- The panel is always in the visual tree but translated off-screen -->
  <!-- When IsPanelOpen=true → TranslateX slides to 0 -->
  <!-- When IsPanelOpen=false → TranslateX = 380 (off right edge) -->
  
  <UserControl.Styles>
    <Style Selector="UserControl.open">
      <Setter Property="RenderTransform" Value="translate(0px, 0px)"/>
      <Setter Property="Opacity" Value="1"/>
    </Style>
    <Style Selector="UserControl.closed">
      <Setter Property="RenderTransform" Value="translate(380px, 0px)"/>
      <Setter Property="Opacity" Value="0"/>
    </Style>
  </UserControl.Styles>
  
  <Border Background="{StaticResource SecondaryBgBrush}"
          BorderBrush="{StaticResource AccentCyanBrush}"
          BorderThickness="1,0,0,0"
          BoxShadow="-8 0 40 0 #1A00E5FF">
    
    <DockPanel>
      
      <!-- HEADER -->
      <Border DockPanel.Dock="Top" Padding="16,12">
        <Grid ColumnDefinitions="*,Auto,Auto">
          <!-- Left: "NOTIFICATIONS" + unread badge -->
          <StackPanel Orientation="Horizontal" Grid.Column="0">
            <!-- TextBlock "NOTIFICATIONS" Rajdhani 18px AccentCyan -->
            <!-- Badge showing UnreadCount, AccentCyan bg, dark text, hidden if 0 -->
          </StackPanel>
          <!-- Middle: "Mark All Read" button (ghost, small, hidden if no unread) -->
          <!-- Right: X close button -->
        </Grid>
      </Border>

      <!-- FILTER TABS -->
      <Border DockPanel.Dock="Top" Padding="12,0">
        <StackPanel Orientation="Horizontal" Spacing="4">
          <!-- Pills: [ALL] [ALERTS] [EMAIL] [SYSTEM] [AI] -->
          <!-- Active pill: AccentCyan background, dark text -->
          <!-- Inactive: transparent, TextDim text -->
          <!-- Each bound to SetFilterCommand with the relevant NotificationType? -->
        </StackPanel>
      </Border>

      <!-- NOTIFICATION LIST (scrollable) -->
      <!-- When IsLoading: 3 skeleton placeholder items -->
      <!-- When HasNotifications=false: empty state -->
      <!--   Bell icon + "ALL CAUGHT UP" text + "No new notifications" subtitle -->
      
      <ScrollViewer>
        <ItemsControl Items="{Binding GroupedNotifications}">
          <!-- Each group: date header + list of NotificationDto items -->
          
          <!-- Date header: "TODAY" / "YESTERDAY" etc in TextDim uppercase 10px -->
          
          <!-- Each notification item: -->
          <!--   Left: colored circle icon (32px) based on Type+Severity -->
          <!--     Alert+Critical = DangerRed -->
          <!--     Alert+Warning  = AccentAmber -->
          <!--     Email          = AccentCyan -->
          <!--     AI             = AccentMagenta -->
          <!--     System         = TextSecondary -->
          <!--   Content: Title (bold if unread), Message (2 lines, TextDim), TimeAgo -->
          <!--   Right: unread dot (8px AccentCyan circle) if !IsRead -->
          <!--   On hover: show action row: [View →] [Dismiss ×] -->
          <!--   On click: MarkReadCommand + NavigateToSourceCommand -->
        </ItemsControl>
      </ScrollViewer>

    </DockPanel>
  </Border>
</UserControl>
```

ADDING PANEL TO MAINWINDOW:
In MainWindow.axaml, the notifications panel must be added as an overlay layer.
Add it to the root Grid as the LAST child (so it renders on top):

```xml
<!-- Main window root grid -->
<Grid>
  <!-- existing sidebar + content area -->
  <Grid ...>
    <local:SidebarView .../>
    <local:ContentArea .../>
  </Grid>
  
  <!-- Notifications overlay: always present, slides in/out -->
  <!-- Backdrop: semi-transparent black, click to close, only visible when panel open -->
  <Border Background="#990B0C10"
          IsVisible="{Binding NotificationsPanel.IsPanelOpen}"
          x:Name="NotificationsBackdrop">
    <!-- Pointer pressed on backdrop = close panel -->
  </Border>
  
  <!-- The panel itself, right-aligned -->
  <local:NotificationsPanelView
    HorizontalAlignment="Right"
    DataContext="{Binding NotificationsPanel}"
    Classes.open="{Binding NotificationsPanel.IsPanelOpen}"
    Classes.closed="{Binding !NotificationsPanel.IsPanelOpen}"/>
</Grid>
```

In MainWindowViewModel, expose:
```csharp
public NotificationsPanelViewModel NotificationsPanel { get; }
public IRelayCommand ToggleNotificationsCommand { get; }  // toggles IsPanelOpen
public int UnreadNotificationCount { get; private set; }  // for header badge
```

Wire the bell icon in the header to ToggleNotificationsCommand.
Wire the unread badge count on the bell to UnreadNotificationCount.

ANIMATIONS (Avalonia Transitions):
Add Transitions to the UserControl:
```xml
<UserControl.Transitions>
  <Transitions>
    <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" 
                                    Easing="CubicEaseOut"/>
    <DoubleTransition Property="Opacity" Duration="0:0:0.25"/>
  </Transitions>
</UserControl.Transitions>
```

Individual notification animations:
- New notification arrives: item height animates 0→auto + opacity 0→1, Duration 250ms
- Dismiss: item opacity 1→0 + height auto→0, Duration 200ms, then remove from list
- Unread count badge: scale 1.3→1.0 when count changes (pop effect)

COMMIT: "feat: complete NotificationsPanelView with real persistence and slide animation"
Run: dotnet build → 0 errors, 0 warnings
Run: dotnet test → all pass
```

---

## ✅ FINAL VERIFICATION PROMPT

Run this as the last Copilot session after all 5 prompts are done:

```
TASK: Final verification pass on Mbarie Insight Suite before packaging.

Run the following checks and fix anything that fails:

1. BUILD CHECK:
   Run: dotnet build
   Required: 0 errors, 0 warnings
   If any warnings exist: fix them now, do not suppress unless truly necessary

2. TEST CHECK:
   Run: dotnet test
   Required: 100% pass rate
   If any tests fail: fix the implementation, not the test

3. NAVIGATION CHECK:
   Verify ALL 7 nav items are accessible and not hidden:
   - Dashboard ✓
   - Alerts ✓
   - Metrics ✓
   - Chat AI ✓
   - Email ✓
   - Knowledge Base ✓
   - Predictions ✓  ← was stub
   - Reports ✓      ← was stub
   - Notifications (panel, via bell icon) ✓  ← was stub

4. SECRETS CHECK:
   Run: grep -r "Password123" .
   Run: grep -r "hardcoded" .
   Run: grep -rn "ApiKey\s*=" . --include="*.cs" | grep -v "Environment\|Configuration\|IOptions"
   Required: No results for any of the above

5. GITIGNORE CHECK:
   Confirm .gitignore includes: .env, *.pfx, *.p12, appsettings.Production.json
   Run: git status --short
   Confirm no secrets or build artifacts are tracked

6. VERSION CHECK:
   All .csproj files should show: <Version>1.0.0</Version>
   Update any that are still on 0.x or different values

7. COMMIT FINAL STATE:
   git add -A
   git commit -m "chore: final pre-release verification — zero warnings, all modules live"
   git push origin main
   git tag -a v1.0.0 -m "Mbarie Insight Suite v1.0.0 — Production Release"
   git push origin v1.0.0

Report back with results of each check.
```

---

## 📋 SUMMARY — RUN IN THIS ORDER

| # | Prompt | Expected Time | Outcome |
|---|--------|---------------|---------|
| 1 | Security Fix | 20 min | Zero secrets in repo |
| 2 | Zero Warnings | 30 min | Clean build, 0 warnings |
| 3 | Predictions UI | 45 min | Full screen + real engine |
| 4 | Reports UI | 45 min | Full screen + generation |
| 5 | Notifications Panel | 45 min | Slide-in drawer + persistence |
| Final | Verification | 15 min | v1.0.0 tagged and pushed |

**Total estimated time: ~3.5 hours**  
**After this: packaging is the only remaining step.** 🚀

---
*Generated: February 14, 2026 | MIC v1.0.0 Pre-Release Polish Pack*