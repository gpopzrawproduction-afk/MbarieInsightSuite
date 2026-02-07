$possible = @(
    'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe',
    'C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe',
    'C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe'
)
$tool = $possible | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $tool) { Write-Error 'NO_SIGNTOOL'; exit 5 }
Write-Output "USING_SIGNTOOL:$tool"
& $tool sign /f ".\artifacts\micdev.pfx" /p "" /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 ".\artifacts\MbarieIntelligenceConsole.msix"
$code = $LASTEXITCODE
Write-Output "SIGN_EXIT_CODE:$code"
if ($code -ne 0) {
    Write-Output "PFX_SIGN_FAILED, trying store certificate by thumbprint fallback"
    $thumb = '95214AF3EA69E4B8FEB89B6A7CF7B12B00D87A03'
    Write-Output ("TRY_SIGN_SHA1:" + $thumb)
    & $tool sign /sha1 $thumb /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 ".\artifacts\MbarieIntelligenceConsole.msix"
    $code = $LASTEXITCODE
    Write-Output ("SIGN_EXIT_CODE_FALLBACK:" + $code)
}

# Verify signature
& $tool verify /pa ".\artifacts\MbarieIntelligenceConsole.msix"
$v = $LASTEXITCODE
Write-Output "VERIFY_EXIT_CODE:$v"
