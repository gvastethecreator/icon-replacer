[CmdletBinding()]
param(
    [string]$CertificatePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent()
)
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell session."
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if (!$CertificatePath) {
    $CertificatePath = Join-Path $repoRoot "artifacts\package\IconReplacer.Dev.cer"
}
$CertificatePath = [System.IO.Path]::GetFullPath($CertificatePath)
if (!(Test-Path -LiteralPath $CertificatePath -PathType Leaf)) {
    throw "Development certificate not found: $CertificatePath"
}

$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
    $CertificatePath
)
$existing = Get-ChildItem "Cert:\LocalMachine\TrustedPeople" |
    Where-Object Thumbprint -eq $certificate.Thumbprint |
    Select-Object -First 1

if (!$existing) {
    $existing = Import-Certificate `
        -FilePath $CertificatePath `
        -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" `
        -ErrorAction Stop
}

Write-Host "Development certificate trusted for local MSIX installation."
Write-Host "Subject: $($existing.Subject)"
Write-Host "Thumbprint: $($existing.Thumbprint)"
Write-Host "Store: LocalMachine\TrustedPeople"
