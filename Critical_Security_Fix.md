# CRITICAL SECURITY FIX - Execute Immediately

## 🚨 Security Vulnerability: Hardcoded Admin Credentials

**Risk Level:** CRITICAL  
**Impact:** Production breach potential  
**Time to Fix:** 1-2 hours  

---

## Issue Location

**File:** `MIC.Infrastructure.Data/DbInitializer.cs` (or similar)

**Current Code (UNSAFE):**
```csharp
// ⚠️ SECURITY RISK - Do not use in production!
var adminUser = new User
{
    Email = "admin@mbarie.com",
    Username = "admin",
    PasswordHash = passwordHasher.HashPassword("Admin123!"), // Hardcoded password
    Role = UserRole.Administrator,
    IsActive = true
};
```

---

## Solution 1: Environment Variables (RECOMMENDED)

### Step 1: Update DbInitializer.cs

```csharp
public class DbInitializer
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger<DbInitializer> logger)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(MICDbContext context)
    {
        // Check if admin already exists
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Administrator))
        {
            _logger.LogInformation("Admin user already exists, skipping initialization");
            return;
        }

        // Get credentials from environment or configuration
        var adminEmail = _configuration["AdminUser:Email"] 
            ?? Environment.GetEnvironmentVariable("MIC_ADMIN_EMAIL");
        var adminPassword = _configuration["AdminUser:Password"] 
            ?? Environment.GetEnvironmentVariable("MIC_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogWarning("Admin credentials not configured. Skipping admin user creation.");
            _logger.LogWarning("Set MIC_ADMIN_EMAIL and MIC_ADMIN_PASSWORD environment variables.");
            return;
        }

        // Validate password strength
        if (!IsPasswordStrong(adminPassword))
        {
            throw new InvalidOperationException(
                "Admin password does not meet security requirements. " +
                "Password must be at least 12 characters with uppercase, lowercase, numbers, and symbols.");
        }

        // Create admin user
        var adminUser = new User
        {
            Email = adminEmail,
            Username = adminEmail.Split('@')[0],
            PasswordHash = _passwordHasher.HashPassword(adminPassword),
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        _logger.LogInformation("Admin user created successfully: {Email}", adminEmail);
        
        // Clear password from memory
        adminPassword = null;
    }

    private bool IsPasswordStrong(string password)
    {
        if (password.Length < 12) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
        return true;
    }
}
```

### Step 2: Update appsettings.json (Development Only)

**File: `appsettings.Development.json`**
```json
{
  "AdminUser": {
    "Email": "admin@mbarie.local",
    "Password": "Dev_Admin_Pass_2026!"
    // ⚠️ DO NOT USE IN PRODUCTION
  }
}
```

**IMPORTANT:** Add to .gitignore:
```
appsettings.Development.json
appsettings.Production.json
*.secret.json
```

### Step 3: Production Setup (Environment Variables)

**For Production Deployment:**
```powershell
# Windows (PowerShell)
$env:MIC_ADMIN_EMAIL = "admin@yourdomain.com"
$env:MIC_ADMIN_PASSWORD = "SecureRandomPassword123!@#"

# Or set system-wide
[System.Environment]::SetEnvironmentVariable("MIC_ADMIN_EMAIL", "admin@yourdomain.com", "Machine")
[System.Environment]::SetEnvironmentVariable("MIC_ADMIN_PASSWORD", "SecurePass!", "Machine")
```

**Linux/macOS:**
```bash
export MIC_ADMIN_EMAIL="admin@yourdomain.com"
export MIC_ADMIN_PASSWORD="SecureRandomPassword123!@#"
```

**Docker:**
```yaml
version: '3.8'
services:
  mic-app:
    image: mbarie/intelligence-console
    environment:
      - MIC_ADMIN_EMAIL=${ADMIN_EMAIL}
      - MIC_ADMIN_PASSWORD=${ADMIN_PASSWORD}
    env_file:
      - .env.production  # Never commit this file!
```

---

## Solution 2: Azure Key Vault (Enterprise Production)

**For enterprise deployments:**

```csharp
public class DbInitializer
{
    private readonly IKeyVaultService _keyVault;
    
    public async Task InitializeAsync(MICDbContext context)
    {
        // Retrieve from Azure Key Vault
        var adminEmail = await _keyVault.GetSecretAsync("AdminEmail");
        var adminPassword = await _keyVault.GetSecretAsync("AdminPassword");
        
        // Rest of initialization...
    }
}
```

---

## Solution 3: First-Run Setup Wizard (USER-FRIENDLY)

**Recommended for desktop applications:**

```csharp
public class FirstRunSetupService
{
    public async Task<bool> IsFirstRunAsync()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MBARIE",
            "setup.json");
            
        return !File.Exists(settingsPath);
    }

    public async Task CompleteFirstRunSetupAsync(string email, string password)
    {
        // Validate inputs
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format");
            
        if (!IsPasswordStrong(password))
            throw new ArgumentException("Password does not meet requirements");
        
        // Create admin user in database
        await CreateAdminUserAsync(email, password);
        
        // Mark setup as complete
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MBARIE",
            "setup.json");
            
        await File.WriteAllTextAsync(settingsPath, JsonSerializer.Serialize(new
        {
            SetupCompleted = true,
            SetupDate = DateTime.UtcNow
        }));
    }
}
```

**UI Implementation:**

```csharp
// In App.axaml.cs OnFrameworkInitializationCompleted
public override async void OnFrameworkInitializationCompleted()
{
    var setupService = _serviceProvider.GetRequiredService<IFirstRunSetupService>();
    
    if (await setupService.IsFirstRunAsync())
    {
        // Show setup wizard window
        var setupWindow = new FirstRunSetupWindow();
        await setupWindow.ShowDialog(null);
    }
    else
    {
        // Continue normal startup
        ShowMainWindow();
    }
    
    base.OnFrameworkInitializationCompleted();
}
```

---

## Immediate Action Steps

### Step 1: Assess Current State (15 min)
```powershell
# Find hardcoded credentials
cd C:\MbarieIntelligenceConsole
Get-ChildItem -Recurse -Include *.cs | 
    Select-String -Pattern "Admin123|password.*=.*\"" -CaseSensitive

# Review DbInitializer
code src/MIC.Infrastructure.Data/DbInitializer.cs
```

### Step 2: Implement Fix (1 hour)
Choose **Solution 3** (First-Run Setup) for desktop app - best UX

1. Create `FirstRunSetupService.cs`
2. Create `FirstRunSetupWindow.axaml` UI
3. Update `DbInitializer.cs` to use service
4. Update `App.axaml.cs` startup logic

### Step 3: Test (30 min)
```powershell
# Delete existing database to force first run
Remove-Item "$env:LOCALAPPDATA\MBARIE\mbarie.db" -ErrorAction SilentlyContinue

# Run application
dotnet run --project src/MIC.Desktop.Avalonia

# Verify first-run wizard appears
# Complete setup with strong password
# Restart app - should go directly to login
```

### Step 4: Update Documentation (15 min)
Create `SECURITY.md`:
```markdown
# Security Configuration

## Admin User Setup

The application uses a secure first-run setup wizard.

### First Run
1. Launch application
2. Complete setup wizard
3. Create strong admin password (12+ chars, mixed case, numbers, symbols)
4. Setup is saved securely

### Password Requirements
- Minimum 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one symbol

### Production Deployment
For automated deployments, set environment variables:
- MIC_ADMIN_EMAIL
- MIC_ADMIN_PASSWORD
```

---

## Verification Checklist

- [ ] Hardcoded credentials removed from all code files
- [ ] First-run setup wizard implemented and tested
- [ ] Password strength validation enforced
- [ ] Credentials never logged in plain text
- [ ] Setup state persisted securely
- [ ] Documentation updated
- [ ] Team notified of change
- [ ] Git history cleaned (if credentials were committed)

---

## Git History Cleanup (If Needed)

**If credentials were committed to Git:**

```powershell
# WARNING: This rewrites history!
# Coordinate with team before running

# Remove sensitive file from all history
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch src/MIC.Infrastructure.Data/DbInitializer.cs" `
  --prune-empty --tag-name-filter cat -- --all

# Force push (dangerous!)
git push origin --force --all
git push origin --force --tags
```

**Better approach:** Use BFG Repo-Cleaner:
```powershell
# Download BFG from https://rtyley.github.io/bfg-repo-cleaner/
java -jar bfg.jar --delete-files DbInitializer.cs
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

---

**⏰ DEADLINE: Complete this fix TODAY before any demo or production deployment!**