$files = Get-ChildItem 'MIC.Tests.Unit/Infrastructure/Data/Repositories/*RepositoryTests.cs'
foreach ($file in $files) {
    $count = (Select-String -Path $file.FullName -Pattern '\[Fact\]' -AllMatches).Count
    $name = $file.Name
    Write-Host ("{0,-45} {1,3} tests" -f $name, $count)
}
