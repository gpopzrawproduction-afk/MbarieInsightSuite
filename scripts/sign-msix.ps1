$signToolItem = Get-ChildItem -Path 'C:\Program Files (x86)\Windows Kits' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $signToolItem) { Write-Error 'SIGNTOOL_NOT_FOUND'; exit 4 }
$signTool = $signToolItem.FullName
Write-Output "FOUND_SIGNTOOL:$signTool"

# Attempt signing using existing PFX (empty password supplied)
& $signTool sign /f ".\artifacts\micdev.pfx" /p "" /fd SHA256 ".\artifacts\MbarieIntelligenceConsole.msix"
$code = $LASTEXITCODE
Write-Output "SIGN_EXIT_CODE:$code"

# Verify signature
& $signTool verify /pa ".\artifacts\MbarieIntelligenceConsole.msix"
$v = $LASTEXITCODE
Write-Output "VERIFY_EXIT_CODE:$v"
