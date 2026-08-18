[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [string]$CertificatePassword = $env:ICON_REPLACER_CERT_PASSWORD,

    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

function Assert-PathInside {
    param(
        [Parameter(Mandatory)]
        [string]$Candidate,

        [Parameter(Mandatory)]
        [string]$Parent
    )

    $candidatePath = [System.IO.Path]::GetFullPath($Candidate)
    $parentPath = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (!$candidatePath.StartsWith($parentPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside $Parent`: $Candidate"
    }
}

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    throw "A non-empty certificate password is required. Set ICON_REPLACER_CERT_PASSWORD or pass -CertificatePassword."
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$projectPath = Join-Path $repoRoot "src\IconReplacer.App\IconReplacer.App.csproj"
$sourceManifest = Join-Path $repoRoot "src\IconReplacer.App\Package.appxmanifest"
$sourceAssetsRoot = Join-Path $repoRoot "src\IconReplacer.App\Assets"
$packageRoot = Join-Path $repoRoot "artifacts\package"
$stagingRoot = Join-Path $packageRoot "staging\$Platform"
$certificatePath = Join-Path $packageRoot "IconReplacer.Dev.pfx"
$certificatePublicPath = Join-Path $packageRoot "IconReplacer.Dev.cer"
$sourceManifestXml = [xml](Get-Content -LiteralPath $sourceManifest -Raw)
$sourceIdentity = $sourceManifestXml.Package.Identity
$packageName = [string]$sourceIdentity.Name
$packageVersion = [string]$sourceIdentity.Version
$parsedPackageVersion = $null
if ([string]::IsNullOrWhiteSpace($packageName) -or
    [string]::IsNullOrWhiteSpace($packageVersion) -or
    ![Version]::TryParse($packageVersion, [ref]$parsedPackageVersion)) {
    throw "Package.appxmanifest has an invalid identity name or version."
}
$packagePath = Join-Path $packageRoot "${packageName}_${packageVersion}_$Platform.msix"
$evidencePath = Join-Path $packageRoot "build-evidence.json"

& (Join-Path $PSScriptRoot "Build-AppIconAssets.ps1") | Out-Host

if (!(Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw "winapp is required. Run winui-setup and retry."
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

if (!$SkipBuild) {
    & (Join-Path $repoRoot "BuildAndRun.ps1") `
        -Project $projectPath `
        -SkipRun `
        -ExtraArgs @(
            "/p:Configuration=$Configuration",
            "/p:Platform=$Platform"
        )
    if ($LASTEXITCODE -ne 0) {
        throw "BuildAndRun.ps1 failed with exit code $LASTEXITCODE."
    }
}

$configurationRoot = Join-Path $repoRoot "src\IconReplacer.App\bin\$Platform\$Configuration"
$generatedManifest = Get-ChildItem -Path $configurationRoot -Filter "AppxManifest.xml" -File -Recurse |
    Where-Object { $_.DirectoryName -notmatch "\\AppX(?:\\|$)" } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (!$generatedManifest) {
    throw "No generated AppxManifest.xml was found under $configurationRoot."
}

$layoutRoot = $generatedManifest.DirectoryName
$requiredLayoutFiles = @(
    "AppxManifest.xml",
    "resources.pri",
    "IconReplacer.App.exe",
    "IconReplacer.CommandHost.exe",
    "IconReplacer.CommandHost.dll",
    "IconReplacer.CommandHost.deps.json",
    "IconReplacer.CommandHost.runtimeconfig.json",
    "IconReplacer.ShellExtension.dll"
)
$requiredAssetFiles = @(
    "Assets\AppIcon.ico",
    "Assets\AppIcon.png",
    "Assets\LockScreenLogo.scale-200.png",
    "Assets\SplashScreen.scale-200.png",
    "Assets\Square150x150Logo.scale-200.png",
    "Assets\Square44x44Logo.scale-200.png",
    "Assets\Square44x44Logo.targetsize-24_altform-unplated.png",
    "Assets\Square44x44Logo.targetsize-24_altform-lightunplated.png",
    "Assets\Square44x44Logo.targetsize-48_altform-unplated.png",
    "Assets\Square44x44Logo.targetsize-48_altform-lightunplated.png",
    "Assets\StoreLogo.png",
    "Assets\Wide310x150Logo.scale-200.png"
)

$missingLayoutFiles = @($requiredLayoutFiles | Where-Object {
    !(Test-Path -LiteralPath (Join-Path $layoutRoot $_) -PathType Leaf)
})
if ($missingLayoutFiles.Count -gt 0) {
    throw "The Release package layout is incomplete: $($missingLayoutFiles -join ', ')"
}

Assert-PathInside -Candidate $stagingRoot -Parent $packageRoot
if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

Get-ChildItem -LiteralPath $layoutRoot -Force | Copy-Item -Destination $stagingRoot -Recurse -Force
Copy-Item -LiteralPath $sourceAssetsRoot -Destination $stagingRoot -Recurse -Force
Get-ChildItem -LiteralPath $stagingRoot -File -Recurse |
    Where-Object { $_.Extension -eq ".pdb" -or $_.Name -like "*.build.appxrecipe" } |
    Remove-Item -Force

if (!(Test-Path -LiteralPath $certificatePath -PathType Leaf)) {
    Invoke-ExternalCommand -FilePath "winapp" -Arguments @(
        "cert", "generate",
        "--manifest", $sourceManifest,
        "--output", $certificatePath,
        "--password", $CertificatePassword,
        "--valid-days", "365",
        "--export-cer",
        "--if-exists", "skip"
    )
}

Assert-PathInside -Candidate $packagePath -Parent $packageRoot
if (Test-Path -LiteralPath $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Invoke-ExternalCommand -FilePath "winapp" -Arguments @(
    "package", $stagingRoot,
    "--manifest", (Join-Path $stagingRoot "AppxManifest.xml"),
    "--skip-pri",
    "--cert", $certificatePath,
    "--cert-password", $CertificatePassword,
    "--output", $packagePath
)

if (!(Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "winapp completed without producing $packagePath."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('/', '\') })
}
finally {
    $archive.Dispose()
}

$requiredPackageEntries = $requiredLayoutFiles + $requiredAssetFiles + "AppxSignature.p7x"
$missingPackageEntries = @($requiredPackageEntries | Where-Object {
    $entryNames -notcontains $_
})
if ($missingPackageEntries.Count -gt 0) {
    throw "The MSIX is missing required entries: $($missingPackageEntries -join ', ')"
}

$signature = Get-AuthenticodeSignature -LiteralPath $packagePath
if (!$signature.SignerCertificate) {
    throw "The generated MSIX does not contain a signer certificate."
}

$manifestXml = [xml](Get-Content -LiteralPath $generatedManifest.FullName -Raw)
$identity = $manifestXml.Package.Identity
$evidence = [ordered]@{
    capturedAt = [DateTimeOffset]::UtcNow.ToString("O")
    configuration = $Configuration
    platform = $Platform
    packagePath = $packagePath
    packageLength = (Get-Item -LiteralPath $packagePath).Length
    packageSha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    packageName = $identity.Name
    packageVersion = $identity.Version
    publisher = $identity.Publisher
    processorArchitecture = $identity.ProcessorArchitecture
    layoutRoot = $layoutRoot
    certificatePath = $certificatePath
    certificatePublicPath = $certificatePublicPath
    signerSubject = $signature.SignerCertificate.Subject
    signatureStatus = $signature.Status.ToString()
    requiredEntries = $requiredPackageEntries
}
$evidence | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $evidencePath -Encoding utf8

Write-Host "MSIX package built and signed."
Write-Host "Package: $packagePath"
Write-Host "Certificate: $certificatePath"
Write-Host "Public certificate: $certificatePublicPath"
Write-Host "Evidence: $evidencePath"
