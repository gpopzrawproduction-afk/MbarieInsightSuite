# ?? CERTIFICATE GENERATION & BUILD INTEGRATION GUIDE
## Mbarie Insight Suite — Self-Signed Certificates for Windows + macOS

**Status:** Execute this NOW before development begins  
**Outcome:** Self-signed certificates ready for production MSIX + DMG signing

---

## ?? PRE-REQUISITES

### Windows 11 Machine
- [ ] Administrator access
- [ ] PowerShell 7+ installed
- [ ] Windows SDK installed (for signtool.exe)
- [ ] Visual Studio Build Tools or Visual Studio Community

### macOS Machine (Intel or M1/M2)
- [ ] Terminal access
- [ ] OpenSSL installed (usually pre-installed)
- [ ] Xcode Command Line Tools: `xcode-select --install`
- [ ] codesign utility (ships with Xcode)

---

## ?? STEP 1: GENERATE WINDOWS CERTIFICATE (PFX)

**Run on Windows 11 (PowerShell as Administrator):**

```powershell
# ============================================================
# WINDOWS CERTIFICATE GENERATION
# Run: powershell -ExecutionPolicy Bypass -File create-windows-cert.ps1
# ============================================================

# Save this as: src/MIC/scripts/create-windows-cert.ps1

param(
    [string]$CertName = "Mbarie Insight Suite",
    [string]$OutputDir = "C:\MIC_Certificates",
    [string]$CertPassword = "MIC_Dev_2026_SecurePassword!",
    [int]$ValidityYears = 3
)

Write-Host "?? Generating Windows Self-Signed Certificate..."
Write-Host "=================================================="

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "? Created directory: $OutputDir"
}

# Generate certificate
Write-Host "Generating certificate: $CertName"
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=$CertName, O=Mbarie Services Ltd, C=US" `
    -KeyUsage DigitalSignature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears($ValidityYears) `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName "Mbarie Code Signing Certificate" `
    -TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.3" `
    -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider v1.0"

Write-Host "? Certificate created"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Subject: $($cert.Subject)"
Write-Host "  Valid until: $($cert.NotAfter)"

# Export as PFX (private key + cert)
$pfxPath = Join-Path $OutputDir "MIC-CodeSign.pfx"
$password = ConvertTo-SecureString -String $CertPassword -AsPlainText -Force

Export-PfxCertificate `
    -Cert $cert `
    -FilePath $pfxPath `
    -Password $password `
    -Force

Write-Host "? Exported PFX: $pfxPath"
Write-Host "  Password: (kept secure)"

# Export as CER (public cert only)
$cerPath = Join-Path $OutputDir "MIC-CodeSign.cer"
Export-Certificate `
    -Cert $cert `
    -FilePath $cerPath `
    -Force

Write-Host "? Exported CER: $cerPath"

# Create summary file
$summary = @"
CERTIFICATE SUMMARY
====================
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Name: $CertName
Valid Until: $($cert.NotAfter)
Thumbprint: $($cert.Thumbprint)

FILES CREATED:
- PFX (Private Key): $pfxPath
- CER (Public Cert): $cerPath

USAGE:
1. Add MIC-CodeSign.pfx to your secure storage
2. For MSIX signing: Use in MSBuild properties
3. For distribution: Share MIC-CodeSign.cer with users

NEXT STEPS:
1. Copy MIC-CodeSign.pfx to: src/MIC/certs/windows/
2. Update build configuration with thumbprint
3. Share MIC-CodeSign.cer for user installation
"@

$summaryPath = Join-Path $OutputDir "CERTIFICATE_SUMMARY.txt"
$summary | Out-File -FilePath $summaryPath

Write-Host ""
Write-Host "?? Summary saved: $summaryPath"
Write-Host ""
Write-Host "? WINDOWS CERTIFICATE READY!"
Write-Host "   Files: PFX (private) + CER (public)"
```

**Execute:**
```powershell
powershell -ExecutionPolicy Bypass -File create-windows-cert.ps1
```

**Output:**
```
? Created directory: C:\MIC_Certificates
? Certificate created
  Thumbprint: A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8S9T0
  Subject: CN=Mbarie Insight Suite, O=Mbarie Services Ltd, C=US
  Valid until: 2/13/2029 2:45:32 PM
? Exported PFX: C:\MIC_Certificates\MIC-CodeSign.pfx
? Exported CER: C:\MIC_Certificates\MIC-CodeSign.cer
? Summary saved: C:\MIC_Certificates\CERTIFICATE_SUMMARY.txt

? WINDOWS CERTIFICATE READY!
```

---

## ?? STEP 2: GENERATE macOS CERTIFICATE (P12)

**Run on macOS (Intel or M1/M2):**

```bash
#!/bin/bash
# Save as: src/MIC/scripts/create-macos-cert.sh

set -e

CERT_NAME="Mbarie Insight Suite"
CERT_DIR="$HOME/MIC_Certificates"
CERT_PASSWORD="MIC_Dev_2026_SecurePassword!"
VALID_DAYS=1095  # 3 years

echo "?? Generating macOS Self-Signed Certificate..."
echo "=========================================="

# Create directory
mkdir -p "$CERT_DIR"
echo "? Created directory: $CERT_DIR"

# Generate private key + certificate
echo "Generating certificate: $CERT_NAME"
openssl req -x509 \
  -newkey rsa:2048 \
  -keyout "$CERT_DIR/MIC-CodeSign.key" \
  -out "$CERT_DIR/MIC-CodeSign.cer" \
  -days $VALID_DAYS \
  -nodes \
  -subj "/C=US/ST=California/L=San Francisco/O=Mbarie Services Ltd/CN=$CERT_NAME"

echo "? Certificate generated"
echo "  Subject: /C=US/ST=California/L=San Francisco/O=Mbarie Services Ltd/CN=$CERT_NAME"
echo "  Valid for: $VALID_DAYS days"

# Export to PKCS12 format (P12)
P12_PATH="$CERT_DIR/MIC-CodeSign.p12"
openssl pkcs12 \
  -export \
  -out "$P12_PATH" \
  -inkey "$CERT_DIR/MIC-CodeSign.key" \
  -in "$CERT_DIR/MIC-CodeSign.cer" \
  -password pass:"$CERT_PASSWORD" \
  -name "$CERT_NAME"

echo "? Exported P12: $P12_PATH"

# Get certificate fingerprint
FINGERPRINT=$(openssl x509 -fingerprint -noout -in "$CERT_DIR/MIC-CodeSign.cer" | cut -d= -f2)
echo "? Certificate fingerprint: $FINGERPRINT"

# Create summary
SUMMARY_PATH="$CERT_DIR/CERTIFICATE_SUMMARY.txt"
cat > "$SUMMARY_PATH" << EOF
CERTIFICATE SUMMARY (macOS)
===========================
Generated: $(date)
Name: $CERT_NAME
Valid Until: $(openssl x509 -enddate -noout -in "$CERT_DIR/MIC-CodeSign.cer" | cut -d= -f2)
Fingerprint: $FINGERPRINT

FILES CREATED:
- Private Key: $CERT_DIR/MIC-CodeSign.key
- Certificate: $CERT_DIR/MIC-CodeSign.cer
- PKCS12 (P12): $P12_PATH

USAGE:
1. Add MIC-CodeSign.p12 to your secure storage
2. For app codesigning: Use "Mbarie Insight Suite" as signing identity
3. For DMG signing: Use codesign command with this identity

NEXT STEPS:
1. Copy MIC-CodeSign.p12 to: src/MIC/certs/macos/
2. Update build configuration with identity name
3. Use MIC-CodeSign.cer for distribution

SECURITY NOTE:
- Keep MIC-CodeSign.key private
- MIC-CodeSign.cer can be shared (public)
- MIC-CodeSign.p12 contains private key - protect it!
EOF

echo ""
echo "?? Summary saved: $SUMMARY_PATH"
echo ""
echo "? macOS CERTIFICATE READY!"
echo "   Files: P12 (private) + CER (public)"
```

**Execute:**
```bash
chmod +x create-macos-cert.sh
./create-macos-cert.sh
```

**Output:**
```
?? Generating macOS Self-Signed Certificate...
? Created directory: /Users/yourname/MIC_Certificates
Generating certificate: Mbarie Insight Suite
? Certificate generated
? Exported P12: /Users/yourname/MIC_Certificates/MIC-CodeSign.p12
? Certificate fingerprint: AB:CD:EF:12:34:56:78:90:AB:CD:EF:12:34:56:78:90
? Summary saved: /Users/yourname/MIC_Certificates/CERTIFICATE_SUMMARY.txt

? macOS CERTIFICATE READY!
```

---

## ?? STEP 3: ORGANIZE CERTIFICATES IN PROJECT

**Directory structure:**

```
src/MIC/
??? certs/
?   ??? windows/
?   ?   ??? MIC-CodeSign.pfx          (PRIVATE - keep secure)
?   ?   ??? MIC-CodeSign.cer          (PUBLIC - can share)
?   ?   ??? README.md
?   ??? macos/
?   ?   ??? MIC-CodeSign.p12          (PRIVATE - keep secure)
?   ?   ??? MIC-CodeSign.cer          (PUBLIC - can share)
?   ?   ??? README.md
?   ??? CERTIFICATE_GUIDE.md          (this file)
??? scripts/
?   ??? build-windows-msix.ps1
?   ??? build-macos-dmg.sh
?   ??? create-certificates.ps1
??? ...
```

**Update .gitignore:**

```
# ? ADD THESE LINES to .gitignore

# Certificates (NEVER commit private keys!)
*.pfx
*.p12
*.key
*.pem
certs/windows/MIC-CodeSign.pfx
certs/macos/MIC-CodeSign.p12

# Build outputs
*.msix
*.dmg
dist/
publish/

# Configuration with secrets
build-config.local.json
cert-config.local.json
```

---

## ?? STEP 4: CREATE BUILD CONFIGURATION

**File: `src/MIC/build-config.json` (TEMPLATE - customize for your machine)**

```json
{
  "build": {
    "version": "1.0.0",
    "configuration": "Release",
    "solutionFile": "MIC.slnx"
  },
  "windows": {
    "enabled": true,
    "platform": "win-x64",
    "certPath": "C:\\MIC_Certificates\\MIC-CodeSign.pfx",
    "certPassword": "MIC_Dev_2026_SecurePassword!",
    "thumbprint": "A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8S9T0",
    "outputDir": "D:\\MSIX_Output",
    "publisher": "CN=Mbarie Services Ltd, O=Mbarie Services Ltd, C=US"
  },
  "macos": {
    "enabled": true,
    "platforms": ["osx-x64", "osx-arm64"],
    "certPath": "/Users/yourname/MIC_Certificates/MIC-CodeSign.p12",
    "certPassword": "MIC_Dev_2026_SecurePassword!",
    "signingIdentity": "Mbarie Insight Suite",
    "outputDir": "/Users/yourname/Desktop/macOS_Distribution",
    "createUniversal": true
  },
  "shared": {
    "publisher": "Mbarie Services Ltd",
    "appName": "Mbarie Insight Suite",
    "appId": "com.mbarieservices.mic"
  }
}
```

**Create local copy (add to .gitignore):**

```bash
cp build-config.json build-config.local.json
```

**Edit `build-config.local.json` with YOUR paths:**
- Windows: Update certPath, certPassword, thumbprint, outputDir
- macOS: Update certPath, certPassword, signingIdentity, outputDir

---

## ?? STEP 5: CREATE BUILD SCRIPTS

### Windows Build Script

**File: `src/MIC/scripts/build-windows-msix.ps1`**

```powershell
param(
    [string]$ConfigPath = "../build-config.local.json"
)

# Load configuration
$config = Get-Content $ConfigPath | ConvertFrom-Json
$windows = $config.windows
$build = $config.build

Write-Host "?? Building Windows MSIX..."
Write-Host "============================"

# Validate configuration
if (-not (Test-Path $windows.certPath)) {
    Write-Error "Certificate not found: $($windows.certPath)"
    exit 1
}

if (-not (Test-Path $windows.outputDir)) {
    New-Item -ItemType Directory -Path $windows.outputDir -Force | Out-Null
}

# Build
Write-Host "Building solution..."
msbuild $build.solutionFile `
    /t:Publish `
    /p:Configuration=$build.configuration `
    /p:RuntimeIdentifier=$windows.platform `
    /p:GenerateAppxPackageOnBuild=true `
    /p:AppxPackageSigningEnabled=true `
    /p:PackageCertificateKeyFile="$($windows.certPath)" `
    /p:PackageCertificatePassword="$($windows.certPassword)" `
    /p:AppxPackageDir="$($windows.outputDir)"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host "? Build successful"

# Verify signature
Write-Host ""
Write-Host "Verifying signatures..."
$msixFiles = Get-ChildItem "$($windows.outputDir)\*.msix" -ErrorAction SilentlyContinue

if ($msixFiles.Count -eq 0) {
    Write-Error "No MSIX files found in $($windows.outputDir)"
    exit 1
}

foreach ($file in $msixFiles) {
    Write-Host "  Verifying: $($file.Name)"
    & signtool.exe verify /pa "$($file.FullName)"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Signature valid"
    } else {
        Write-Error "  ? Signature verification failed"
        exit 1
    }
}

Write-Host ""
Write-Host "? WINDOWS MSIX READY FOR DISTRIBUTION!"
Write-Host "   Location: $($windows.outputDir)"
```

### macOS Build Script

**File: `src/MIC/scripts/build-macos-dmg.sh`**

```bash
#!/bin/bash
set -e

CONFIG_PATH="../build-config.local.json"

# Parse JSON config
read_config() {
    echo $(cat "$CONFIG_PATH" | grep "\"$1\"" | head -1 | cut -d: -f2 | xargs)
}

CERT_PATH=$(read_config "certPath" | tr -d '\"')
CERT_PASSWORD=$(read_config "certPassword" | tr -d '\"')
SIGNING_ID=$(read_config "signingIdentity" | tr -d '\"')
OUTPUT_DIR=$(read_config "outputDir" | tr -d '\"')

echo "?? Building macOS DMG..."
echo "========================"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build for both architectures
echo "Building for Intel x64..."
dotnet publish MIC.Desktop.Avalonia.csproj \
    --configuration Release \
    --runtime osx-x64 \
    --self-contained \
    --output ./publish/osx-x64

echo "Building for Apple Silicon (M1/M2/M3)..."
dotnet publish MIC.Desktop.Avalonia.csproj \
    --configuration Release \
    --runtime osx-arm64 \
    --self-contained \
    --output ./publish/osx-arm64

echo "? Builds complete"

# Create universal binary
echo ""
echo "Creating universal binary..."
mkdir -p ./publish/osx-universal
cp -r ./publish/osx-x64/MIC.Desktop.Avalonia.app ./publish/osx-universal/

EXEC_PATH="./publish/osx-universal/MIC.Desktop.Avalonia.app/Contents/MacOS/MIC.Desktop.Avalonia"

lipo -create \
    ./publish/osx-x64/MIC.Desktop.Avalonia.app/Contents/MacOS/MIC.Desktop.Avalonia \
    ./publish/osx-arm64/MIC.Desktop.Avalonia.app/Contents/MacOS/MIC.Desktop.Avalonia \
    -output "$EXEC_PATH"

echo "? Universal binary created"

# Code sign the app
echo ""
echo "Code signing application..."
codesign --deep --force --verify --verbose \
    --sign "$SIGNING_ID" \
    ./publish/osx-universal/MIC.Desktop.Avalonia.app

echo "? App signed"

# Create DMG
echo ""
echo "Creating DMG..."
hdiutil create \
    -volname "Mbarie Insight Suite" \
    -srcfolder ./publish/osx-universal \
    -ov -format UDZO \
    "$OUTPUT_DIR/MIC-Installer-Universal.dmg"

echo "? DMG created"

# Sign the DMG
echo ""
echo "Code signing DMG..."
codesign --force --sign "$SIGNING_ID" \
    "$OUTPUT_DIR/MIC-Installer-Universal.dmg"

echo "? DMG signed"

echo ""
echo "? macOS DMG READY FOR DISTRIBUTION!"
echo "   Location: $OUTPUT_DIR/MIC-Installer-Universal.dmg"
echo "   Supports: Intel x64 + Apple Silicon M1/M2/M3"
```

---

## ? VERIFICATION CHECKLIST

**Windows (after running build-windows-msix.ps1):**
- [ ] MSIX file created in output directory
- [ ] signtool.exe verification passes
- [ ] Certificate thumbprint matches config

**macOS (after running build-macos-dmg.sh):**
- [ ] Universal binary created (supports both architectures)
- [ ] codesign reports "valid on disk"
- [ ] DMG created in output directory
- [ ] DMG mounts and contains .app bundle

---

## ?? NEXT STEPS

1. **Execute certificate generation:**
   ```powershell
   # Windows
   .\create-windows-cert.ps1
   ```
   ```bash
   # macOS
   chmod +x create-macos-cert.sh
   ./create-macos-cert.sh
   ```

2. **Copy certificates into project:**
   ```bash
   mkdir -p src/MIC/certs/windows
   mkdir -p src/MIC/certs/macos
   
   # Windows
   cp C:\MIC_Certificates\MIC-CodeSign.* src/MIC/certs/windows/
   
   # macOS
   cp ~/MIC_Certificates/MIC-CodeSign.* src/MIC/certs/macos/
   ```

3. **Create local build config:**
   ```bash
   cp src/MIC/build-config.json src/MIC/build-config.local.json
   # Edit build-config.local.json with YOUR paths
   ```

4. **Begin WEEK 1 development** with both platforms ready

---

**Ready?** Once certificates are generated and in place, we can begin building the Email module with full cross-platform support! ??

