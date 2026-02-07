# Find-HangingTest.ps1
# Identifies which unit test is hanging by running with verbose output and timeout

param(
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 30
)

Write-Host "🔍 Hunting for the hanging test..." -ForegroundColor Cyan
Write-Host "   Each test will timeout after $TimeoutSeconds seconds" -ForegroundColor Gray
Write-Host ""

$testProjectPath = ".\MIC.Tests.Unit\MIC.Tests.Unit.csproj"

if (-not (Test-Path $testProjectPath)) {
    Write-Host "❌ Test project not found: $testProjectPath" -ForegroundColor Red
    Write-Host "   Run this script from the MIC solution root" -ForegroundColor Yellow
    exit 1
}

# Build first to ensure we have latest
Write-Host "🔨 Building test project..." -ForegroundColor Cyan
dotnet build $testProjectPath -c Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build succeeded`n" -ForegroundColor Green

# Run tests with very verbose output to see which one hangs
Write-Host "🧪 Running tests with detailed logging..." -ForegroundColor Cyan
Write-Host "   (Press Ctrl+C if a test hangs for >$TimeoutSeconds seconds)`n" -ForegroundColor Gray

$testProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "test $testProjectPath --logger `"console;verbosity=detailed`" --no-build" `
    -PassThru `
    -NoNewWindow `
    -RedirectStandardOutput "test_output.log" `
    -RedirectStandardError "test_errors.log"

# Wait with timeout
$timeout = New-TimeSpan -Seconds ($TimeoutSeconds * 10) # Give overall timeout
if (-not $testProcess.WaitForExit($timeout.TotalMilliseconds)) {
    Write-Host "`n❌ Tests timed out after $($timeout.TotalSeconds) seconds!" -ForegroundColor Red
    $testProcess.Kill()
    
    # Show last lines of output to identify hanging test
    Write-Host "`n📋 Last test activity before hang:" -ForegroundColor Yellow
    Get-Content "test_output.log" -Tail 50 | Write-Host
    
    Write-Host "`n🔍 Look for the test name above marked 'Starting test execution'`n" -ForegroundColor Cyan
    exit 1
} else {
    if ($testProcess.ExitCode -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Tests failed with exit code: $($testProcess.ExitCode)" -ForegroundColor Red
        Get-Content "test_output.log" | Write-Host
    }
    
    exit $testProcess.ExitCode
}
