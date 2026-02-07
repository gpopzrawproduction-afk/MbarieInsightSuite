Get-ChildItem -Path 'C:\Program Files (x86)\Windows Kits' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue | ForEach-Object { Write-Output $_.FullName }
