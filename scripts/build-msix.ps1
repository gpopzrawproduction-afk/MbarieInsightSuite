param(
    [string]$CertificatePassword = "ChangeMeStrongP@ss",
    [string]$PublishConfiguration = "Release",
    [string]$Runtime = "win-x64"
)

Write-Host "== MSIX build helper ==" -ForegroundColor Cyan

# Script location is c:\...\src\MIC\scripts
# project root (MIC) is parent of the scripts folder
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# If this repo is nested differently, allow override by env var
$repoRoot = $projectRoot

$certSubject = "CN=MIC Dev Cert"
$pfxPath = Join-Path $repoRoot "artifacts\micdev.pfx"
$packageRoot = Join-Path $repoRoot "artifacts\PackageRoot"
$publishOut = Join-Path $repoRoot "artifacts\$Runtime\publish"
$msixOut = Join-Path $repoRoot "artifacts\MIC-unsigned.msix"
$msixSigned = Join-Path $repoRoot "artifacts\MIC-signed.msix"

if (-Not (Test-Path (Join-Path $repoRoot "MIC.Desktop.Avalonia"))) {
    Write-Error "MIC.Desktop.Avalonia project not found under repo root ($repoRoot)"
    exit 1
}

Write-Host "1) Create dev self-signed certificate (if missing)" -ForegroundColor Yellow
if (-not (Test-Path $pfxPath)) {
    $createNew = $true
} else {
    # Check existing PFX for Code Signing EKU; if absent, recreate
    $securePwd = ConvertTo-SecureString -String $CertificatePassword -AsPlainText -Force
    $pfxData = Get-PfxData -FilePath $pfxPath -Password $securePwd -ErrorAction SilentlyContinue
    $createNew = $true
    if ($pfxData -and $pfxData.EndEntityCertificates.Count -gt 0) {
        $existing = $pfxData.EndEntityCertificates[0]
        $hasCodeSigning = $false
        foreach ($eku in $existing.EnhancedKeyUsageList) {
            if ($eku.Value -eq '1.3.6.1.5.5.7.3.3' -or $eku.FriendlyName -eq 'Code Signing') { $hasCodeSigning = $true; break }
        }
        if ($hasCodeSigning) { $createNew = $false }
    }
}

if ($createNew) {
    Write-Host "  -> Creating new Code Signing certificate" -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Subject $certSubject -Type CodeSigningCert -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -NotAfter (Get-Date).AddYears(5) -CertStoreLocation "Cert:\CurrentUser\My"
    $securePwd = ConvertTo-SecureString -String $CertificatePassword -AsPlainText -Force
    Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath $pfxPath -Password $securePwd
    Write-Host "  -> PFX exported to $pfxPath" -ForegroundColor Green
} else {
    Write-Host "  -> PFX already exists and contains a Code Signing certificate" -ForegroundColor Green
}

Write-Host "2) Ensure AppxManifest publisher matches cert subject" -ForegroundColor Yellow
$appxManifest = Join-Path $repoRoot "artifacts\AppxManifest.xml"
if (-not (Test-Path $appxManifest)) {
    Write-Error "AppxManifest.xml not found at artifacts\AppxManifest.xml. Please ensure manifest exists.";
    exit 1
}

[xml]$manifest = Get-Content $appxManifest
$identity = $manifest.Package.Identity
if ($identity.Publisher -ne $certSubject) {
    Write-Host "  -> Updating Publisher from '$($identity.Publisher)' to '$certSubject'" -ForegroundColor Yellow
    $identity.Publisher = $certSubject
    $manifest.Save($appxManifest)
    Write-Host "  -> AppxManifest updated" -ForegroundColor Green
} else {
    Write-Host "  -> AppxManifest publisher already matches" -ForegroundColor Green
}

Write-Host "3) Publish project" -ForegroundColor Yellow
dotnet publish MIC.Desktop.Avalonia/MIC.Desktop.Avalonia.csproj -c $PublishConfiguration -r $Runtime --self-contained false -o $publishOut
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }

Write-Host "4) Prepare package root" -ForegroundColor Yellow
if (Test-Path $packageRoot) { Remove-Item $packageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $packageRoot | Out-Null

Write-Host "  -> Copying published files to package root" -ForegroundColor Yellow
Copy-Item "$publishOut\*" -Destination $packageRoot -Recurse -Force

Write-Host "  -> Copying AppxManifest.xml into package root" -ForegroundColor Yellow
Copy-Item $appxManifest -Destination (Join-Path $packageRoot "AppxManifest.xml") -Force

Write-Host "5) Create MSIX (using makeappx)" -ForegroundColor Yellow
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"
if (-not (Test-Path $makeappx)) {
    # try to find makeappx automatically
    $found = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter makeappx.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { $makeappx = $found.FullName }
}
if (-not (Test-Path $makeappx)) { Write-Error "MakeAppx.exe not found. Install Windows 10 SDK."; exit 1 }

# Remove existing output to avoid interactive overwrite prompt
if (Test-Path $msixOut) { Remove-Item $msixOut -Force -ErrorAction SilentlyContinue }
& "$makeappx" pack /d "$packageRoot" /p "$msixOut" /l
if ($LASTEXITCODE -ne 0) { Write-Error "makeappx failed"; exit 1 }

Write-Host "6) Sign MSIX using signtool" -ForegroundColor Yellow
$signtool = $null

# Try PATH first
$cmd = Get-Command -Name signtool.exe -ErrorAction SilentlyContinue
if ($cmd) { $signtool = $cmd.Source }

# If not found, look for signtool in common Windows Kits locations and prefer x64 bin under a 10.0.* folder
if (-not $signtool) {
    $all = Get-ChildItem 'C:\Program Files (x86)\Windows Kits' -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue
    if ($all) {
        $preferred = $all | Where-Object { $_.FullName -match '\\bin\\10\.0' -and $_.FullName -match '\\x64\\signtool.exe$' } | Sort-Object FullName -Descending | Select-Object -First 1
        if (-not $preferred) { $preferred = $all | Where-Object { $_.FullName -match '\\x64\\signtool.exe$' } | Select-Object -First 1 }
        if (-not $preferred) { $preferred = $all | Select-Object -First 1 }
        if ($preferred) { $signtool = $preferred.FullName }
    }
}

if (-not $signtool) {
    Write-Error "SignTool.exe not found. Install the Windows SDK (includes SignTool) or provide the full path to signtool.exe."
    Write-Host "You can run this command manually once SignTool is available:" -ForegroundColor Yellow
    Write-Host "  & '<path-to-signtool>\signtool.exe' sign /fd SHA256 /f '$pfxPath' /p '$CertificatePassword' /tr http://timestamp.digicert.com /td SHA256 '$msixOut'" -ForegroundColor Cyan
    exit 1
}

Write-Host "  -> Using signtool at: $signtool" -ForegroundColor Green
& "$signtool" sign /v /a /fd SHA256 /f "$pfxPath" /p "$CertificatePassword" /tr http://timestamp.digicert.com /td SHA256 "$msixOut"
if ($LASTEXITCODE -ne 0) { Write-Error "signtool sign failed with exit code $LASTEXITCODE"; exit 1 }

Copy-Item $msixOut $msixSigned -Force

Write-Host "✅ MSIX created and signed: $msixSigned" -ForegroundColor Green
Write-Host "Note: PFX created at $pfxPath (not committed). Do NOT check this file into source control." -ForegroundColor Yellow
