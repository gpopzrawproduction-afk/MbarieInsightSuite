# Quick Start: Build MSIX & Deploy

## ? Security Status: PRODUCTION READY ?

All issues fixed. Release build successful. Ready to package.

---

## ?? Build MSIX in 3 Commands

```powershell
# 1. Navigate to project
cd "C:\MbarieIntelligenceConsole\src\MIC"

# 2. Build MSIX (This is your command - now safe!)
msbuild MIC.Desktop.Avalonia.csproj `
  /t:Publish `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true

# 3. Find your .msix file
Get-ChildItem -Filter "*.msix" -Recurse
```

**Output:** `MIC.Desktop.Avalonia_[version]_x64.msix`

---

## ?? Setup Environment Variables (REQUIRED)

**Before users run the app, set these (PowerShell as Admin):**

```powershell
# Production Environment
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [EnvironmentVariableTarget]::User)

# Database Connection - CRITICAL
[Environment]::SetEnvironmentVariable(
    "MIC_ConnectionStrings__MicDatabase",
    "Host=YOUR_DB_SERVER;Port=5432;Database=micdb;Username=mic;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=false",
    [EnvironmentVariableTarget]::User
)

# JWT Secret - CRITICAL (must be 64+ random characters)
[Environment]::SetEnvironmentVariable(
    "MIC_JwtSettings__SecretKey",
    "GenerateA64CharacterRandomKeyHere0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcd",
    [EnvironmentVariableTarget]::User
)

# OpenAI API (if using AI features)
[Environment]::SetEnvironmentVariable(
    "MIC_AI__OpenAI__ApiKey",
    "sk-proj-YOUR_OPENAI_KEY",
    [EnvironmentVariableTarget]::User
)

# Gmail OAuth (if using email sync)
[Environment]::SetEnvironmentVariable("MIC_OAuth2__Gmail__ClientId", "YOUR_CLIENT_ID.apps.googleusercontent.com", [EnvironmentVariableTarget]::User)
[Environment]::SetEnvironmentVariable("MIC_OAuth2__Gmail__ClientSecret", "YOUR_CLIENT_SECRET", [EnvironmentVariableTarget]::User)

# Outlook OAuth (if using email sync)
[Environment]::SetEnvironmentVariable("MIC_OAuth2__Outlook__ClientId", "YOUR_CLIENT_ID", [EnvironmentVariableTarget]::User)
[Environment]::SetEnvironmentVariable("MIC_OAuth2__Outlook__ClientSecret", "YOUR_CLIENT_SECRET", [EnvironmentVariableTarget]::User)
```

**?? IMPORTANT:** Restart your shell or computer after setting variables!

---

## ?? Install MSIX

```powershell
# As Administrator
Add-AppxPackage -Path "C:\Path\To\MIC.Desktop.Avalonia_1.0.0.0_x64.msix"
```

---

## ?? First Run Checklist

When you launch the app for the first time:

- [ ] First-run setup dialog appears
- [ ] Create your admin user account
- [ ] App starts successfully
- [ ] You can log in with your credentials
- [ ] Database connection is working
- [ ] No errors in logs (`%LOCALAPPDATA%\MIC\logs\`)

---

## ? Troubleshooting

### App Won't Start - "Database Connection Failed"
```powershell
# 1. Verify env variable is set
$env:MIC_ConnectionStrings__MicDatabase

# 2. Test PostgreSQL connectivity
psql -h YOUR_DB_SERVER -U mic -d micdb
```

### Error: "JWT secret key cannot be null"
```powershell
# Set the JWT secret (64+ chars)
[Environment]::SetEnvironmentVariable(
    "MIC_JwtSettings__SecretKey",
    "YOUR_64_CHAR_MINIMUM_RANDOM_KEY_0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
    [EnvironmentVariableTarget]::User
)
# Then restart the app
```

### MSIX Installation Fails
- Ensure Windows 10/11 version 1809 or later
- Run as Administrator
- Check disk space (needs ~500MB)
- Try: `Add-AppxPackage -Path "path\to\msix" -ForceUpdateFromAnyVersion`

### Check Logs
```powershell
# Logs stored here:
Get-ChildItem "$env:LOCALAPPDATA\MIC\logs\"

# View latest log
Get-ChildItem "$env:LOCALAPPDATA\MIC\logs\" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content
```

---

## ?? What Was Fixed

| Issue | What | Where | Status |
|-------|------|-------|--------|
| Debug logs | Removed password/hash logging | AuthenticationService.cs | ? FIXED |
| Config password | Removed hardcoded password | appsettings.Production.json | ? FIXED |
| Demo data | Disabled auto-seeding | appsettings.json | ? FIXED |
| **Build** | Release x64 | MIC.Desktop.Avalonia.csproj | ? SUCCESS |

---

## ?? Security Guarantees

? No hardcoded credentials  
? No default user accounts  
? No passwords in logs  
? Strong Argon2id hashing  
? Environment-based configuration  
? Production-ready database setup  

---

## ?? Next Steps

1. **Build:** Run the MSBuild command above
2. **Configure:** Set environment variables
3. **Test:** Install MSIX and verify first-run setup
4. **Deploy:** Distribute to users with env var setup instructions
5. **Monitor:** Check logs at `%LOCALAPPDATA%\MIC\logs\`

---

## ?? Full Documentation

- `SECURITY_AUDIT_REPORT.md` - Detailed security findings
- `MSIX_PACKAGING_GUIDE.md` - Complete packaging guide with alternatives
- `SETUP.md` - Development environment setup (existing)

**Ready to package! ??**

