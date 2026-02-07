# Pack and Sign MSIX for MbarieIntelligenceConsole
# Usage: run from repository root (MIC folder)

$ErrorActionPreference = 'Stop'

$makeAppxPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\makeappx.exe"
)

$global:makeAppx = $null
foreach ($p in $makeAppxPaths) {
    if (Test-Path $p) {
        Write-Output "FOUND_MAKEAPPX:$p"
        $global:makeAppx = $p
        break
    }
}

if (-not $global:makeAppx) {
    Write-Output "Searching Windows Kits for makeappx.exe using where.exe fallback..."
    $whereOut = cmd /c "where /r "C:\\Program Files (x86)\\Windows Kits\\10\\bin" makeappx.exe" 2>$null
    if ($whereOut) {
        $first = ($whereOut | Select-Object -First 1).ToString().Trim()
        $global:makeAppx = $first
        Write-Output "FOUND_MAKEAPPX:$global:makeappx"
    } else {
        Write-Error "MAKEAPPX_NOT_FOUND"
        exit 2
    }
}

Write-Output "PACK_USING:$global:makeAppx"

# Pack the folder into MSIX
& $global:makeAppx pack /d ".\artifacts\win-x64" /p ".\artifacts\MbarieIntelligenceConsole.msix" /nv

if (Test-Path ".\artifacts\MbarieIntelligenceConsole.msix") {
    Write-Output "MSIX_CREATED"
    Write-Output (Get-Item ".\artifacts\MbarieIntelligenceConsole.msix").FullName
    Write-Output ("MSIX_SIZE_MB:" + [math]::Round(((Get-Item ".\artifacts\MbarieIntelligenceConsole.msix").Length)/1MB,2))
} else {
    Write-Error "MSIX_NOT_CREATED"
    exit 3
}

# Locate signtool
Write-Output "Searching for signtool.exe using where.exe fallback..."
$whereSig = cmd /c "where /r "C:\\Program Files (x86)\\Windows Kits" signtool.exe" 2>$null
if ($whereSig) {
    $signTool = ($whereSig | Select-Object -First 1).ToString().Trim()
    Write-Output "FOUND_SIGNTOOL:$signTool"
} else {
    Write-Error "SIGNTOOL_NOT_FOUND"
    exit 4
}

# Attempt signing without password (common for dev PFX with empty password)
Write-Output "TRYING_SIGN_NO_PASSWORD"
& $signTool sign /f ".\artifacts\micdev.pfx" /fd SHA256 ".\artifacts\MbarieIntelligenceConsole.msix"
$signResult = $LASTEXITCODE
Write-Output ("SIGN_EXIT_CODE:" + $signResult)
if ($signResult -eq 0) {
    Write-Output "SIGNED_OK"
} else {
    Write-Output "SIGNED_FAILED"
}

# Verify signature
Write-Output "VERIFYING"
& $signTool verify /pa ".\artifacts\MbarieIntelligenceConsole.msix"
$verifyResult = $LASTEXITCODE
Write-Output ("VERIFY_EXIT_CODE:" + $verifyResult)
if ($verifyResult -eq 0) {
    Write-Output "VERIFY_OK"
} else {
    Write-Output "VERIFY_FAILED"
}

Write-Output "DONE"
