# MSIX Packaging Guide - Mbarie Intelligence Console

## ? Security Pre-Flight Check (COMPLETED)

All critical security issues have been fixed:
- ? Debug password logs removed
- ? Production config hardcoded password removed
- ? SeedDataOnStartup disabled by default
- ? Build successful (Release configuration)

---

## ?? MSIX Packaging Recommendations

### **Option 1: Standard MSBuild Approach (Your Command)**

This is the simplest approach using your provided command.

```powershell
# From the project root
msbuild MIC.Desktop.Avalonia.csproj `
  /t:Publish `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true
```

**Prerequisites:**
- Windows SDK 10.0.26100+ installed
- Visual Studio Build Tools with C++ support
- `.appxmanifest` file properly configured in `MIC.Desktop.Avalonia`

**Pros:**
- Simple one-command build
- Native Windows packaging
- Creates `.appx`/`.msix` files directly

**Cons:**
- Requires Windows SDK
- Limited customization in manifest

---

### **Option 2: Recommended - dotnet CLI with Custom Manifest**

More portable and gives better control:

```powershell
# Step 1: Publish the application
dotnet publish MIC.Desktop.Avalonia.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained=false `
  --output ./publish/win-x64

# Step 2: Create MSIX package using MakeAppx tool
$WindowsSDK = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64"
& "$WindowsSDK\makeappx.exe" pack `
  /d ./publish/win-x64 `
  /p MIC.Console.msix `
  /v
```

**Pros:**
- Better tooling control
- Easier to customize manifest
- Clearer output structure

**Cons:**
- Multi-step process
- Requires manual manifest configuration

---

### **Option 3: Best Practice - Use MSIX Packaging Tool**

Download from Microsoft Store and use GUI (recommended for first-time packaging):

1. Open MSIX Packaging Tool
2. Create Package ? select `./publish/win-x64` folder
3. Configure:
   - **App Name:** Mbarie Intelligence Console
   - **Publisher:** Your Company Name
   - **Display Name:** Mbarie Intelligence Console
   - **Entry Point:** MIC.Desktop.Avalonia.exe
   - **Logo:** Add company logo (300x300px PNG)

---

## ?? Critical: Environment Variable Configuration for MSIX

**The application REQUIRES these environment variables at runtime:**

### **For Production Deployment:**

```powershell
# Set these BEFORE launching the packaged app:

# 1. Database Connection (CRITICAL)
[Environment]::SetEnvironmentVariable(
    "MIC_ConnectionStrings__MicDatabase",
    "Host=YOUR_PROD_DB_HOST;Port=5432;Database=micdb;Username=mic;Password=YOUR_SECURE_PASSWORD;SSL Mode=Require;Trust Server Certificate=false",
    [EnvironmentVariableTarget]::User
)

# 2. AI/OpenAI API Key (if using OpenAI)
[Environment]::SetEnvironmentVariable(
    "MIC_AI__OpenAI__ApiKey",
    "sk-proj-YOUR_OPENAI_KEY_HERE",
    [EnvironmentVariableTarget]::User
)

# 3. Set Environment to Production
[Environment]::SetEnvironmentVariable(
    "ASPNETCORE_ENVIRONMENT",
    "Production",
    [EnvironmentVariableTarget]::User
)

# 4. OAuth Credentials (if using Gmail/Outlook)
[Environment]::SetEnvironmentVariable(
    "MIC_OAuth2__Gmail__ClientId",
    "YOUR_GMAIL_CLIENT_ID.apps.googleusercontent.com",
    [EnvironmentVariableTarget]::User
)

[Environment]::SetEnvironmentVariable(
    "MIC_OAuth2__Gmail__ClientSecret",
    "YOUR_GMAIL_CLIENT_SECRET",
    [EnvironmentVariableTarget]::User
)

[Environment]::SetEnvironmentVariable(
    "MIC_OAuth2__Outlook__ClientId",
    "YOUR_OUTLOOK_CLIENT_ID",
    [EnvironmentVariableTarget]::User
)

[Environment]::SetEnvironmentVariable(
    "MIC_OAuth2__Outlook__ClientSecret",
    "YOUR_OUTLOOK_CLIENT_SECRET",
    [EnvironmentVariableTarget]::User
)

# 5. JWT Secret Key (CRITICAL - Use strong random key)
# Generate with: [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes((New-Guid).ToString() + (New-Guid).ToString()))
[Environment]::SetEnvironmentVariable(
    "MIC_JwtSettings__SecretKey",
    "YOUR_64_CHAR_MINIMUM_RANDOM_KEY_HERE",
    [EnvironmentVariableTarget]::User
)
```

---

## ?? Pre-MSIX Packaging Checklist

- [ ] **Database Ready**: PostgreSQL 12+ instance available and accessible
- [ ] **Environment Variables Set**: All above variables configured on target machine
- [ ] **User Account**: First user account will be created on first app launch
- [ ] **Network Access**: 
  - [ ] Database server reachable from target machine
  - [ ] OpenAI API reachable (if using AI features)
  - [ ] Gmail/Outlook OAuth servers reachable (if using email sync)
- [ ] **Logging**: Application logs will be stored in `%LOCALAPPDATA%\MIC\logs\`
- [ ] **First Run**: App will show setup dialog on first launch

---

## ?? Step-by-Step MSIX Build Process

### **Step 1: Prepare Environment**

```powershell
# Ensure Release configuration
$env:Configuration = "Release"
$env:RuntimeIdentifier = "win-x64"

# Verify Windows SDK is installed
Get-Command makeappx.exe
```

### **Step 2: Build MSIX (Your Recommended Approach)**

```powershell
cd "C:\MbarieIntelligenceConsole\src\MIC"

msbuild MIC.Desktop.Avalonia.csproj `
  /t:Publish `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true `
  /p:AppxPackageDir="D:\MSIX_Output" `
  /p:AppxBundle=Never
```

**Parameters Explanation:**
- `/t:Publish` - Build and publish the app
- `/p:Configuration=Release` - Use Release build
- `/p:RuntimeIdentifier=win-x64` - Target 64-bit Windows
- `/p:GenerateAppxPackageOnBuild=true` - Create MSIX during build
- `/p:AppxPackageDir` - Output directory for `.msix` files
- `/p:AppxBundle=Never` - Skip bundle, just create `.msix`

### **Step 3: Verify Output**

```powershell
# Check for .msix file
Get-ChildItem -Path "D:\MSIX_Output" -Filter "*.msix"

# Expected: MIC.Desktop.Avalonia_1.0.0.0_x64.msix
```

### **Step 4: Test Installation (Optional)**

```powershell
# Add package to local system (requires Windows 10/11)
Add-AppxPackage -Path "D:\MSIX_Output\MIC.Desktop.Avalonia_1.0.0.0_x64.msix"

# Or use PowerShell as Administrator:
powershell -Command "Add-AppxPackage -Path 'D:\MSIX_Output\MIC.Desktop.Avalonia_1.0.0.0_x64.msix'"
```

---

## ?? Required Project File Modifications (If Needed)

Check `MIC.Desktop.Avalonia.csproj` for these properties:

```xml
<PropertyGroup>
  <!-- Windows App SDK / MSIX properties -->
  <TargetFramework>net9.0-windows10.0.22621.0</TargetFramework>
  <WindowsAppSDKVersion>1.4.x</WindowsAppSDKVersion>
  
  <!-- Package properties -->
  <ApplicationIcon>Assets/Icon.ico</ApplicationIcon>
  <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
  <AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>
  <GenerateAppxPackageOnBuild>true</GenerateAppxPackageOnBuild>
</PropertyGroup>
```

---

## ?? Important: Signing the MSIX Package

For production/enterprise distribution:

```powershell
# Generate self-signed certificate (development)
$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject "CN=Mbarie Intelligence Console" `
  -KeyUsage DigitalSignature `
  -KeyLength 2048 `
  -CertStoreLocation "Cert:\CurrentUser\My"

# Export certificate
Export-PfxCertificate `
  -Cert $cert `
  -FilePath "MIC.pfx" `
  -Password (ConvertTo-SecureString -String "YourPassword" -AsPlainText -Force)

# Sign the MSIX
signtool sign `
  /f MIC.pfx `
  /p YourPassword `
  /fd SHA256 `
  /tr http://timestamp.digicert.com `
  "D:\MSIX_Output\MIC.Desktop.Avalonia_1.0.0.0_x64.msix"
```

---

## ?? Configuration by Environment

### **Development (appsettings.Development.json)**
- Provider: SQLite
- SeedDataOnStartup: false
- Database: `%BASE_DIR%/mic_dev.db`

### **Production (appsettings.Production.json)**
- Provider: PostgreSQL
- SeedDataOnStartup: false ? (fixed)
- Database: From `MIC_ConnectionStrings__MicDatabase` env var
- All secrets: From environment variables

---

## ?? Troubleshooting

### **"Cannot find Windows SDK"**
```powershell
# Install Windows SDK 10.0.26100+
# Download from: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
```

### **"InvalidArgumentException: Value cannot be null (Parameter name: secretKey)"**
This means `MIC_JwtSettings__SecretKey` environment variable is not set.
```powershell
# Fix: Set the JWT secret
[Environment]::SetEnvironmentVariable("MIC_JwtSettings__SecretKey", "your-64-char-key", "User")
```

### **Database Connection Failed**
```powershell
# Verify PostgreSQL connectivity:
psql -h YOUR_DB_HOST -U mic -d micdb
```

### **First Run Setup Loop**
If user creation fails, check:
1. Database connectivity
2. Migrations applied (`DbInitializer` runs automatically)
3. User permissions on database

---

## ?? Distribution Options

### **Option A: Direct MSIX Installation**
Users double-click `.msix` file ? Windows Package Manager installs

### **Option B: Windows Package Manager**
Add to Windows Package Manager community repo for `winget install` support

### **Option C: Microsoft Store**
Submit to Microsoft Store for enterprise/consumer distribution

### **Option D: Web Server**
Host `.msix` on internal server with installer batch file

---

## ?? Final Recommendation

**For your immediate need, use Option 1:**

```powershell
# Simple, one-command MSIX build
msbuild MIC.Desktop.Avalonia.csproj `
  /t:Publish `
  /p:Configuration=Release `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true
```

**Then, before distributing to users:**

1. ? Document all required environment variables (provided above)
2. ? Create a setup script that sets variables in order
3. ? Test on clean Windows machine
4. ? Distribute with setup instructions

---

## ?? Quick Reference: Production Environment Variables

Save this as `setup-environment.ps1`:

```powershell
# Mbarie Intelligence Console - Production Setup
# Run as Administrator

$envVars = @{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "MIC_ConnectionStrings__MicDatabase" = "Host=YOUR_DB_HOST;Port=5432;Database=micdb;Username=mic;Password=YOUR_PASSWORD;SSL Mode=Require"
    "MIC_AI__OpenAI__ApiKey" = "sk-proj-YOUR_KEY"
    "MIC_JwtSettings__SecretKey" = "YOUR_64_CHAR_KEY"
    "MIC_OAuth2__Gmail__ClientId" = "YOUR_GMAIL_ID.apps.googleusercontent.com"
    "MIC_OAuth2__Gmail__ClientSecret" = "YOUR_GMAIL_SECRET"
}

foreach ($key in $envVars.Keys) {
    [Environment]::SetEnvironmentVariable($key, $envVars[$key], [EnvironmentVariableTarget]::User)
    Write-Host "? Set $key"
}

Write-Host "? Environment configured. Please restart your shell or reboot for changes to take effect."
```

---

## ? Status Summary

| Item | Status | Notes |
|------|--------|-------|
| Security audit | ? COMPLETE | All critical issues fixed |
| Password logging | ? FIXED | Removed from AuthenticationService |
| Config hardcoding | ? FIXED | Production config uses env vars only |
| Build | ? SUCCESS | Release build compiles without errors |
| MSIX ready | ? READY | Can build immediately |
| Production checklist | ? TODO | Configure environment variables before deployment |

