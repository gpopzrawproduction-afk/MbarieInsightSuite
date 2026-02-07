 $ErrorActionPreference = "Stop"

 $projectRoot = "C:\MbarieIntelligenceConsole\src\MIC"
 $artifacts = "$projectRoot\artifacts"
 $publishDir = "$artifacts\win-x64"
 $msixOutput = "$artifacts\MbarieIntelligenceConsole.msix"

 Write-Host "=== Creating Fresh MSIX ===" -ForegroundColor Cyan

 if (!(Test-Path $publishDir)) {
     Write-Host "❌ Publish directory not found: $publishDir" -ForegroundColor Red
     Write-Host "   Publish your app first: dotnet publish -c Release -r win-x64 --self-contained" -ForegroundColor Yellow
     exit 1
 }

 $staging = "$artifacts\msix_staging"
 if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
 New-Item -ItemType Directory -Path $staging -Force | Out-Null

 Write-Host "📦 Staging files..." -ForegroundColor Yellow
 Copy-Item "$publishDir\*" $staging -Recurse -Force

 $manifestSource = "$artifacts\AppxManifest.xml"
 $manifestDest = "$staging\AppxManifest.xml"

 if (Test-Path $manifestSource) {
     Copy-Item $manifestSource $manifestDest -Force
     Write-Host "✅ Copied AppxManifest.xml" -ForegroundColor Green
 } else {
     Write-Host "❌ AppxManifest.xml not found at: $manifestSource" -ForegroundColor Red
     exit 1
 }

 $assetsDir = "$staging\Assets"
 New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null

 $pngBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")

 $logos = @("StoreLogo.png", "Square44x44Logo.png", "Square150x150Logo.png", "Wide310x150Logo.png", "Square310x310Logo.png")
 foreach ($logo in $logos) {
     $path = Join-Path $assetsDir $logo
     [System.IO.File]::WriteAllBytes($path, $pngBytes)
 }
 Write-Host "✅ Created placeholder logos" -ForegroundColor Green

 $makeAppx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe'
 if (!(Test-Path $makeAppx)) {
     $makeAppx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe'
 }
 if (!(Test-Path $makeAppx)) {
     Write-Host "❌ makeappx.exe not found in expected locations" -ForegroundColor Red
     exit 2
 }

 Write-Host "\n🔨 Building MSIX with MakeAppx..." -ForegroundColor Yellow
 & $makeAppx pack /d $staging /p $msixOutput /o

 if ($LASTEXITCODE -ne 0) {
     Write-Host "❌ MakeAppx failed!" -ForegroundColor Red
     exit 1
 }

 if (Test-Path $msixOutput) {
     $size = [math]::Round((Get-Item $msixOutput).Length / 1MB, 2)
     Write-Host "✅ MSIX created: $msixOutput ($size MB)" -ForegroundColor Green
     try {
         Add-Type -Assembly System.IO.Compression.FileSystem
         $zip = [System.IO.Compression.ZipFile]::OpenRead($msixOutput)
         $hasManifest = $zip.Entries | Where-Object { $_.Name -eq "AppxManifest.xml" }
         $zip.Dispose()
         if ($hasManifest) {
             Write-Host "✅ MSIX structure validated" -ForegroundColor Green
         } else {
             Write-Host "⚠️ Warning: AppxManifest.xml not found in package!" -ForegroundColor Yellow
         }
     } catch {
         Write-Host "❌ MSIX is corrupted: $_" -ForegroundColor Red
     }
 }

 Remove-Item $staging -Recurse -Force
 Write-Host "🧹 Cleaned up staging directory" -ForegroundColor Gray

 Write-Host "\n🚀 MSIX is ready for signing!" -ForegroundColor Green
$ErrorActionPreference = "Stop"

$projectRoot = "C:\MbarieIntelligenceConsole\src\MIC"
$artifacts = "$projectRoot\artifacts"
$publishDir = "$artifacts\win-x64"
$msixOutput = "$artifacts\MbarieIntelligenceConsole.msix"

Write-Host "=== Creating Fresh MSIX ===" -ForegroundColor Cyan

if (!(Test-Path $publishDir)) {
    Write-Host "❌ Publish directory not found: $publishDir" -ForegroundColor Red
    Write-Host "   Publish your app first: dotnet publish -c Release -r win-x64 --self-contained" -ForegroundColor Yellow
    exit 1
}

$staging = "$artifacts\msix_staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

Write-Host "📦 Staging files..." -ForegroundColor Yellow
Copy-Item "$publishDir\*" $staging -Recurse -Force

$manifestSource = "$artifacts\AppxManifest.xml"
$manifestDest = "$staging\AppxManifest.xml"

if (Test-Path $manifestSource) {
    Copy-Item $manifestSource $manifestDest -Force
    Write-Host "✅ Copied AppxManifest.xml" -ForegroundColor Green
} else {
    Write-Host "❌ AppxManifest.xml not found at: $manifestSource" -ForegroundColor Red
    exit 1
}

$assetsDir = "$staging\Assets"
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null

$pngBytes = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")

$logos = @("StoreLogo.png", "Square44x44Logo.png", "Square150x150Logo.png", "Wide310x150Logo.png", "Square310x310Logo.png")
foreach ($logo in $logos) {
    [System.IO.File]::WriteAllBytes("$assetsDir\$logo", $pngBytes)
}
Write-Host "✅ Created placeholder logos" -ForegroundColor Green

$makeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
if (!(Test-Path $makeAppx)) {
    $makeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"
}
if (!(Test-Path $makeAppx)) {
    Write-Host "❌ makeappx.exe not found in expected locations" -ForegroundColor Red
    exit 2
}

Write-Host "\n🔨 Building MSIX with MakeAppx..." -ForegroundColor Yellow
& $makeAppx pack /d $staging /p $msixOutput /o

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ MakeAppx failed!" -ForegroundColor Red
    exit 1
}

if (Test-Path $msixOutput) {
    $size = [math]::Round((Get-Item $msixOutput).Length / 1MB, 2)
    Write-Host "✅ MSIX created: $msixOutput ($size MB)" -ForegroundColor Green
    try {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($msixOutput)
        $hasManifest = $zip.Entries | Where-Object { $_.Name -eq "AppxManifest.xml" }
        $zip.Dispose()
        if ($hasManifest) {
            Write-Host "✅ MSIX structure validated" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Warning: AppxManifest.xml not found in package!" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ MSIX is corrupted: $_" -ForegroundColor Red
    }
}

Remove-Item $staging -Recurse -Force
Write-Host "🧹 Cleaned up staging directory" -ForegroundColor Gray

Write-Host "\n🚀 MSIX is ready for signing!" -ForegroundColor Green
