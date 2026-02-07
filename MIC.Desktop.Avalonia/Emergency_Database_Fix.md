# EMERGENCY DATABASE FIX - Execute Immediately

## 🚨 Problem Summary
Entity Framework detects pending model changes but won't apply migrations, causing:
- No tables created in database
- Application fails at startup
- All queries throw "no such table" exceptions

## ✅ Solution: 3-Step Emergency Fix

---

## STEP 1: Force Database Recreation (10 minutes)

### Option A: Clean Slate (RECOMMENDED)

**Execute these commands in order:**

```powershell
# Navigate to your Infrastructure.Data project
cd C:\MbarieIntelligenceConsole\src\MIC.Infrastructure.Data

# 1. Delete existing database (clean start)
Remove-Item "$env:LOCALAPPDATA\MBARIE\mbarie.db" -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\MBARIE\mbarie.db-shm" -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\MBARIE\mbarie.db-wal" -ErrorAction SilentlyContinue

# 2. Delete ALL existing migrations (they're corrupted)
Remove-Item -Path "Migrations\*.cs" -Force

# 3. Create fresh initial migration
dotnet ef migrations add InitialCreate --startup-project ..\MIC.Presentation\MIC.Presentation.csproj

# 4. Apply migration to create database
dotnet ef database update --startup-project ..\MIC.Presentation\MIC.Presentation.csproj

# 5. Verify database created
if (Test-Path "$env:LOCALAPPDATA\MBARIE\mbarie.db") {
    Write-Host "✅ Database created successfully!" -ForegroundColor Green
    
    # Show tables
    sqlite3 "$env:LOCALAPPDATA\MBARIE\mbarie.db" ".tables"
} else {
    Write-Host "❌ Database creation failed!" -ForegroundColor Red
}
```

### Option B: Manual Database Creation (if Option A fails)

```powershell
# 1. Create database directory
$dbPath = "$env:LOCALAPPDATA\MBARIE"
New-Item -ItemType Directory -Force -Path $dbPath

# 2. Create database manually with SQLite
sqlite3 "$dbPath\mbarie.db" "VACUUM;"

# 3. Verify
if (Test-Path "$dbPath\mbarie.db") {
    Write-Host "✅ Database file created" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create database file" -ForegroundColor Red
}
```

---

## STEP 2: Fix Value Converter Warnings (15 minutes)

The runlog shows **numerous EF value-comparer warnings** for collection properties. These cause tracking issues.

### Find All Affected Entities

**Search your codebase:**
```powershell
# Find all entities with collection properties that need converters
Get-ChildItem -Path "C:\MbarieIntelligenceConsole\src" -Filter "*.cs" -Recurse | 
    Select-String -Pattern "public List<string>" -Context 2,2 |
    Select-Object -ExpandProperty Filename -Unique
```

### Fix Pattern for Each Entity

**Example: Alert entity with Tags property**

**Before (causes warning):**
```csharp
public class Alert
{
    public int Id { get; set; }
    public List<string> Tags { get; set; } // ⚠️ Causes EF warning
}
```

**After (fixed):**
```csharp
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class Alert
{
    public int Id { get; set; }
    
    // Store as JSON string in database
    private string _tagsJson { get; set; }
    
    [NotMapped]
    public List<string> Tags 
    { 
        get => string.IsNullOrEmpty(_tagsJson) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(_tagsJson);
        set => _tagsJson = JsonSerializer.Serialize(value ?? new List<string>());
    }
}
```

**OR use EF Core's JSON column (EF Core 7+):**

```csharp
// In your DbContext's OnModelCreating:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Alert>()
        .Property(a => a.Tags)
        .HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
        .Metadata.SetValueComparer(
            new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
}
```

### Apply This Fix to ALL Entities

**Common entities that need fixing:**
- Alert → Tags, Recipients, ActionHistory
- Metric → Labels, Dimensions
- User → Roles, Permissions
- Email → To, Cc, Bcc
- Notification → Tags, Actions
- Settings → (any List<string> properties)

---

## STEP 3: Update Startup Configuration (5 minutes)

Ensure database initialization happens correctly at startup.

**File: `Program.cs` or `App.axaml.cs`**

```csharp
public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ✅ CRITICAL: Initialize database BEFORE creating any windows
            InitializeDatabaseAsync().Wait();
            
            var serviceProvider = ConfigureServices();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var serviceProvider = new ServiceCollection()
                .AddDbContextFactory<MICDbContext>(options =>
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MBARIE",
                        "mbarie.db");
                    
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    options.UseSqlite($"Data Source={dbPath}");
                })
                .BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MICDbContext>>();
            
            await using var context = await contextFactory.CreateDbContextAsync();
            
            // Apply migrations
            await context.Database.MigrateAsync();
            
            Console.WriteLine("✅ Database initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
            throw;
        }
    }
}
```

---

## STEP 4: Verify Fix (5 minutes)

### Test 1: Database Exists
```powershell
$dbPath = "$env:LOCALAPPDATA\MBARIE\mbarie.db"
if (Test-Path $dbPath) {
    Write-Host "✅ Database file exists" -ForegroundColor Green
    
    # Show file size
    $size = (Get-Item $dbPath).Length / 1KB
    Write-Host "   Size: $size KB"
} else {
    Write-Host "❌ Database file missing!" -ForegroundColor Red
}
```

### Test 2: Tables Created
```powershell
sqlite3 "$env:LOCALAPPDATA\MBARIE\mbarie.db" ".tables"
# Should show: Alerts, Metrics, Users, Emails, Notifications, etc.
```

### Test 3: Application Starts
```powershell
cd C:\MbarieIntelligenceConsole\src\MIC.Presentation
dotnet run --configuration Debug
# Should start without "no such table" errors
```

### Test 4: Check Logs
```powershell
# After running app, check for errors
Get-Content "C:\MbarieIntelligenceConsole\src\MIC\runlog.txt" -Tail 50
# Should NOT show "no such table" errors
```

---

## 🔍 TROUBLESHOOTING

### Issue: "dotnet ef not recognized"
**Solution:**
```powershell
dotnet tool install --global dotnet-ef
# OR update if already installed
dotnet tool update --global dotnet-ef
```

### Issue: Migration still fails with "pending changes"
**Solution:**
```powershell
# 1. Ensure DbContext is using correct connection string
# 2. Delete bin/ and obj/ folders
Remove-Item -Recurse -Force "C:\MbarieIntelligenceConsole\src\MIC.Infrastructure.Data\bin"
Remove-Item -Recurse -Force "C:\MbarieIntelligenceConsole\src\MIC.Infrastructure.Data\obj"

# 3. Rebuild
dotnet build

# 4. Try migration again
dotnet ef migrations add InitialCreate --startup-project ..\MIC.Presentation\MIC.Presentation.csproj --force
```

### Issue: "no such table" errors persist
**Solution:**
```powershell
# Verify correct database path is being used
# Check connection string in appsettings.json
Get-Content "C:\MbarieIntelligenceConsole\src\MIC.Presentation\appsettings.json"

# Ensure it matches:
# "Data Source=%LOCALAPPDATA%\MBARIE\mbarie.db"
```

### Issue: Value converter warnings remain
**Solution:**
- Apply the value converter fix to EVERY entity with List<string> properties
- Regenerate migrations after fixing
- Ensure ValueComparer is set for each converted property

---

## ✅ SUCCESS CRITERIA

After completing all steps, you should have:
- [x] Database file exists at `%LOCALAPPDATA%\MBARIE\mbarie.db`
- [x] All tables created (Alerts, Metrics, Users, Emails, etc.)
- [x] Application starts without errors
- [x] No "no such table" exceptions in runlog.txt
- [x] No EF value-comparer warnings
- [x] Dashboard and registration flows work

---

## 📋 POST-FIX VALIDATION CHECKLIST

Run these commands in order:

```powershell
# 1. Verify database
Test-Path "$env:LOCALAPPDATA\MBARIE\mbarie.db"

# 2. List tables
sqlite3 "$env:LOCALAPPDATA\MBARIE\mbarie.db" ".tables"

# 3. Check schema
sqlite3 "$env:LOCALAPPDATA\MBARIE\mbarie.db" ".schema Alerts"
sqlite3 "$env:LOCALAPPDATA\MBARIE\mbarie.db" ".schema Users"

# 4. Start application
cd C:\MbarieIntelligenceConsole\src\MIC.Presentation
dotnet run --configuration Debug

# 5. Check logs for errors
Get-Content "C:\MbarieIntelligenceConsole\src\MIC\runlog.txt" -Tail 100 | 
    Select-String -Pattern "error|exception|fail" -CaseSensitive:$false
```

Expected output:
- ✅ Database exists
- ✅ All tables listed
- ✅ Schema shows correct columns
- ✅ Application starts
- ✅ No error messages in logs

---

## 🚀 AFTER FIX: Resume Phase 0

Once database is working:

1. **Update PHASE_0_CHECKLIST.md:**
   - Mark WP-0.5 (Notification Center) as COMPLETE ✅
   - Note database issue resolved

2. **Continue with WP-0.6:**
   - Error Handling & Logging standardization
   - Apply the patterns from PHASE_0_IMPLEMENTATION_MASTER.md

3. **Then proceed to Integration & Polish (Day 10)**

---

## 📞 IF STILL STUCK

If these steps don't resolve the issue:

1. **Capture detailed error:**
```powershell
# Run with verbose logging
$env:DOTNET_ENVIRONMENT="Development"
dotnet run --configuration Debug > debug_output.txt 2>&1
Get-Content debug_output.txt
```

2. **Share these files:**
- Your DbContext class
- Migration files (in Migrations folder)
- appsettings.json (connection string)
- Full error from runlog.txt

3. **Document what you tried:**
- Which steps completed successfully
- Which steps failed
- Exact error messages

---

**Execute this fix NOW before proceeding with any Phase 0 work.**

**Estimated time: 30-45 minutes total**

**Priority: CRITICAL - Blocks all functionality**