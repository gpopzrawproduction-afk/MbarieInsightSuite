[xml]$xml = Get-Content 'TestResults/UnitCoverage2/014bdc83-b6c0-4581-888b-16a08557e2d6/coverage.cobertura.xml'

Write-Host "Overall Coverage:" -ForegroundColor Cyan
$lineRate = [double]$xml.coverage.'line-rate' * 100
$branchRate = [double]$xml.coverage.'branch-rate' * 100
Write-Host ("  Line:   {0:F2}%" -f $lineRate)
Write-Host ("  Branch: {0:F2}%" -f $branchRate)
Write-Host ""

Write-Host "Coverage by Package:" -ForegroundColor Cyan
$xml.coverage.packages.package | ForEach-Object {
    $name = $_.name
    $pkgLineRate = [double]$_.'line-rate' * 100
    $pkgBranchRate = [double]$_.'branch-rate' * 100
    Write-Host ("{0,-40} {1,6:F1}% line  {2,6:F1}% branch" -f $name, $pkgLineRate, $pkgBranchRate)
} | Sort-Object
