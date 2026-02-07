$c = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$c.Import('.\artifacts\micdev.cer')
Write-Output "CERT_SUBJECT:$($c.Subject)"
Write-Output "CERT_THUMBPRINT:$($c.Thumbprint)"
