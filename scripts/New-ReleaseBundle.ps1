[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^v\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$ReleaseTag,

    [ValidateSet('Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64')]
    [string]$Platform = 'x64',

    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-PathInside {
    param(
        [Parameter(Mandatory)] [string]$Candidate,
        [Parameter(Mandatory)] [string]$Parent
    )

    $candidatePath = [System.IO.Path]::GetFullPath($Candidate)
    $parentPath = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (!$candidatePath.StartsWith($parentPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside $Parent`: $Candidate"
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifestPath = Join-Path $repoRoot 'src\IconReplacer.App\Package.appxmanifest'
$manifest = [xml](Get-Content -LiteralPath $manifestPath -Raw)
$identity = $manifest.Package.Identity
$packageName = [string]$identity.Name
$packageVersion = [string]$identity.Version
$parsedVersion = $null
if ([string]::IsNullOrWhiteSpace($packageName) -or
    ![Version]::TryParse($packageVersion, [ref]$parsedVersion)) {
    throw 'Package.appxmanifest has an invalid identity.'
}

$notesPath = Join-Path $repoRoot ".github\release-notes\$ReleaseTag.md"
if (!(Test-Path -LiteralPath $notesPath -PathType Leaf)) {
    throw "Release notes not found: $notesPath"
}

$buildArguments = @{
    Configuration = $Configuration
    Platform = $Platform
}
if ($SkipBuild) {
    $buildArguments.SkipBuild = $true
}
& (Join-Path $PSScriptRoot 'Build-MsixPackage.ps1') @buildArguments

$packageRoot = Join-Path $repoRoot 'artifacts\package'
$releaseRoot = Join-Path $repoRoot "artifacts\release\$ReleaseTag"
$packagePath = Join-Path $packageRoot "${packageName}_${packageVersion}_$Platform.msix"
$certificatePath = Join-Path $packageRoot 'IconReplacer.Dev.cer'
$evidencePath = Join-Path $packageRoot 'build-evidence.json'

foreach ($requiredPath in @($packagePath, $certificatePath, $evidencePath)) {
    if (!(Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required release input not found: $requiredPath"
    }
}

Assert-PathInside -Candidate $releaseRoot -Parent (Join-Path $repoRoot 'artifacts\release')
if (Test-Path -LiteralPath $releaseRoot) {
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

$publicFiles = @($packagePath, $certificatePath)
foreach ($sourcePath in $publicFiles) {
    Copy-Item -LiteralPath $sourcePath -Destination $releaseRoot -Force
}

$releasePackagePath = Join-Path $releaseRoot (Split-Path $packagePath -Leaf)
$releaseCertificatePath = Join-Path $releaseRoot (Split-Path $certificatePath -Leaf)
$signature = Get-AuthenticodeSignature -LiteralPath $releasePackagePath
if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
    !$signature.SignerCertificate) {
    throw "Release MSIX signature is not valid: $($signature.Status)"
}

$publicCertificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
    $releaseCertificatePath
)
if ($signature.SignerCertificate.Thumbprint -ne $publicCertificate.Thumbprint) {
    throw 'The public certificate does not match the MSIX signer.'
}

$hashLines = foreach ($file in Get-ChildItem -LiteralPath $releaseRoot -File | Sort-Object Name) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    "$hash  $($file.Name)"
}
$checksumsPath = Join-Path $releaseRoot 'SHA256SUMS.txt'
$hashLines | Set-Content -LiteralPath $checksumsPath -Encoding ascii

$gitRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the source Git revision.'
}

$metadata = [ordered]@{
    releaseTag = $ReleaseTag
    packageName = $packageName
    packageVersion = $packageVersion
    architecture = $Platform
    packageFile = Split-Path $releasePackagePath -Leaf
    packageLength = (Get-Item -LiteralPath $releasePackagePath).Length
    packageSha256 = (Get-FileHash -LiteralPath $releasePackagePath -Algorithm SHA256).Hash
    signatureStatus = $signature.Status.ToString()
    signerSubject = $signature.SignerCertificate.Subject
    certificateFile = Split-Path $releaseCertificatePath -Leaf
    certificateThumbprint = $publicCertificate.Thumbprint
    certificateNotAfter = $publicCertificate.NotAfter.ToUniversalTime().ToString('O')
    sourceRevision = $gitRevision
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
}
$metadataPath = Join-Path $releaseRoot 'release-metadata.json'
$metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8

# Rebuild checksums after metadata is present, but never checksum the checksum file itself.
$hashLines = foreach ($file in Get-ChildItem -LiteralPath $releaseRoot -File |
    Where-Object Name -ne 'SHA256SUMS.txt' |
    Sort-Object Name) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    "$hash  $($file.Name)"
}
$hashLines | Set-Content -LiteralPath $checksumsPath -Encoding ascii

Write-Host 'Release bundle created.'
Write-Host "Tag: $ReleaseTag"
Write-Host "Directory: $releaseRoot"
Write-Host "Package: $releasePackagePath"
Write-Host "Checksums: $checksumsPath"
Write-Host "Certificate thumbprint: $($publicCertificate.Thumbprint)"
