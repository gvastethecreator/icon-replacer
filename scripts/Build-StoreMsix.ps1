#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('x64')]
    [string] $Platform = 'x64',

    [Parameter()]
    [ValidateSet('Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $SkipTests,

    [Parameter()]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$appProject = Join-Path $repoRoot 'src\IconReplacer.App\IconReplacer.App.csproj'
$testProject = Join-Path $repoRoot 'tests\IconReplacer.Core.Tests\IconReplacer.Core.Tests.csproj'
$solutionPath = Join-Path $repoRoot 'IconReplacer.slnx'
$manifestPath = Join-Path $repoRoot 'src\IconReplacer.App\Package.appxmanifest'
$identityPath = Join-Path $repoRoot 'docs\store\store-identity.json'
$readinessScript = Join-Path $PSScriptRoot 'Test-StoreReadiness.ps1'
$packageScript = Join-Path $PSScriptRoot 'Build-MsixPackage.ps1'
$packageRoot = Join-Path $repoRoot 'artifacts\package'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\store\x64'
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$previousChannel = $env:ICON_REPLACER_DISTRIBUTION_CHANNEL
$previousPath = $env:PATH

function Assert-PathInside {
    param(
        [Parameter(Mandatory)] [string] $Candidate,
        [Parameter(Mandatory)] [string] $Parent
    )

    $candidatePath = [System.IO.Path]::GetFullPath($Candidate)
    $parentPath = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $candidatePath.StartsWith($parentPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside $Parent`: $Candidate"
    }
}

function Reset-Directory {
    param([Parameter(Mandatory)] [string] $Path)

    Assert-PathInside -Candidate $Path -Parent $artifactsRoot
    if (Test-Path -LiteralPath $Path) {
        [System.IO.Directory]::Delete($Path, $true)
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Invoke-External {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    Write-Host "==> $Name" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Resolve-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        throw 'Visual Studio Installer vswhere.exe is required.'
    }

    $installation = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installation)) {
        throw 'A Visual Studio installation with MSBuild is required.'
    }

    $candidate = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "MSBuild was not found at $candidate."
    }
    return $candidate
}

function Resolve-WinApp {
    $command = Get-Command winapp -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $packageRoot = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp'
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
        return $null
    }

    $candidate = Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter 'winapp.exe' |
        Where-Object { $_.FullName -like '*\tools\win-x64\winapp.exe' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($candidate) {
        return $candidate.FullName
    }
    return $null
}

function New-RandomPassword {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return [Convert]::ToBase64String($bytes)
}

function Get-ZipEntrySha256 {
    param(
        [Parameter(Mandatory)] [System.IO.Compression.ZipArchive] $Archive,
        [Parameter(Mandatory)] [string] $EntryName
    )

    $entry = $Archive.Entries | Where-Object {
        [string]::Equals($_.FullName.Replace('/', '\'), $EntryName, [StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1
    if (-not $entry) {
        throw "Package entry was not found: $EntryName"
    }

    $stream = $entry.Open()
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $algorithm.ComputeHash($stream)
        return ([BitConverter]::ToString($hash)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

& $readinessScript -RequireReservedIdentity
if ($LASTEXITCODE -ne 0) {
    throw 'Reserved Partner Center identity and strict Store readiness are required.'
}

$storeIdentity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
[xml] $sourceManifest = Get-Content -LiteralPath $manifestPath -Raw
$sourceIdentity = $sourceManifest.Package.Identity
$packageVersion = [string] $sourceIdentity.Version
$packageName = [string] $sourceIdentity.Name
$publisher = [string] $sourceIdentity.Publisher
$publisherDisplayName = [string] $storeIdentity.packageIdentity.publisherDisplayName
$storeId = [string] $storeIdentity.store.productId
$certificatePassword = New-RandomPassword

try {
    Reset-Directory -Path $OutputDirectory
    Reset-Directory -Path $packageRoot

    # Existing updater tests validate the direct/default channel. Run them
    # before setting the Store compile property.
    Remove-Item Env:ICON_REPLACER_DISTRIBUTION_CHANNEL -ErrorAction SilentlyContinue

    if (-not $SkipTests) {
        Invoke-External -Name 'Restore managed tests' -FilePath 'dotnet' -Arguments @(
            'restore',
            $testProject,
            '--locked-mode'
        )
        Invoke-External -Name 'Run direct-channel managed tests' -FilePath 'dotnet' -Arguments @(
            'test',
            $testProject,
            '--configuration', $Configuration,
            '--no-restore'
        )
    }

    # Clean release outputs so a direct-channel binary cannot be reused in the
    # Store package by an incremental build.
    Invoke-External -Name 'Clean release outputs' -FilePath 'dotnet' -Arguments @(
        'clean',
        $solutionPath,
        '--configuration', $Configuration,
        '-p:Platform=x64'
    )

    $env:ICON_REPLACER_DISTRIBUTION_CHANNEL = 'store'

    if (-not $SkipTests) {
        Invoke-External -Name 'Prove Store compile policy' -FilePath 'dotnet' -Arguments @(
            'test',
            $testProject,
            '--configuration', $Configuration,
            '-p:Platform=x64',
            '-p:ICON_REPLACER_DISTRIBUTION_CHANNEL=store',
            '--filter', 'FullyQualifiedName~DistributionChannelPolicyTests'
        )
    }

    $msbuild = Resolve-MSBuild
    Invoke-External -Name 'Build x64 Store-channel app, command host, and native extension' -FilePath $msbuild -Arguments @(
        $appProject,
        '/restore',
        "/p:Configuration=$Configuration",
        '/p:Platform=x64',
        '/p:RuntimeIdentifier=win-x64',
        '/p:ICON_REPLACER_DISTRIBUTION_CHANNEL=store',
        '/nologo',
        '/verbosity:minimal'
    )

    $winappPath = Resolve-WinApp
    if (-not $winappPath) {
        throw 'winapp.exe was not found after NuGet restore. Ensure Microsoft.Windows.SDK.BuildTools.WinApp is restored.'
    }
    $env:PATH = "$(Split-Path -Parent $winappPath);$previousPath"

    & $packageScript `
        -Configuration $Configuration `
        -Platform x64 `
        -CertificatePassword $certificatePassword `
        -SkipBuild
    if ($LASTEXITCODE -ne 0) {
        throw "Build-MsixPackage.ps1 failed with exit code $LASTEXITCODE."
    }

    $sourcePackage = Get-ChildItem -LiteralPath $packageRoot -File -Filter '*.msix' |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if (-not $sourcePackage) {
        throw "The package builder did not produce an MSIX under $packageRoot."
    }

    $sourceEvidencePath = Join-Path $packageRoot 'build-evidence.json'
    if (-not (Test-Path -LiteralPath $sourceEvidencePath -PathType Leaf)) {
        throw 'The package builder did not produce build-evidence.json.'
    }
    $sourceEvidence = Get-Content -LiteralPath $sourceEvidencePath -Raw | ConvertFrom-Json

    $signature = Get-AuthenticodeSignature -LiteralPath $sourcePackage.FullName
    if (-not $signature.SignerCertificate) {
        throw 'The Store MSIX does not contain a signer certificate.'
    }
    if ($signature.Status -in @(
        [System.Management.Automation.SignatureStatus]::NotSigned,
        [System.Management.Automation.SignatureStatus]::HashMismatch
    )) {
        throw "The Store MSIX signature is unusable: $($signature.Status) $($signature.StatusMessage)"
    }
    if (-not [string]::Equals($signature.SignerCertificate.Subject, $publisher, [StringComparison]::Ordinal)) {
        throw "MSIX signer '$($signature.SignerCertificate.Subject)' does not match manifest publisher '$publisher'."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($sourcePackage.FullName)
    try {
        $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('/', '\') })
        $requiredEntries = @(
            'AppxManifest.xml',
            'AppxBlockMap.xml',
            'AppxSignature.p7x',
            'IconReplacer.App.exe',
            'IconReplacer.CommandHost.exe',
            'IconReplacer.CommandHost.dll',
            'IconReplacer.CommandHost.deps.json',
            'IconReplacer.CommandHost.runtimeconfig.json',
            'IconReplacer.ShellExtension.dll',
            'Assets\StoreLogo.png',
            'Assets\Square44x44Logo.scale-200.png',
            'Assets\Square150x150Logo.scale-200.png',
            'Assets\Wide310x150Logo.scale-200.png',
            'Assets\SplashScreen.scale-200.png'
        )
        $missingEntries = @($requiredEntries | Where-Object { $entryNames -notcontains $_ })
        if ($missingEntries.Count -gt 0) {
            throw "Store MSIX is missing required entries: $($missingEntries -join ', ')"
        }

        $manifestEntry = $archive.Entries | Where-Object { $_.FullName -eq 'AppxManifest.xml' } | Select-Object -First 1
        $manifestStream = $manifestEntry.Open()
        $reader = [System.IO.StreamReader]::new($manifestStream)
        try {
            [xml] $packagedManifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
            $manifestStream.Dispose()
        }

        $packagedIdentity = $packagedManifest.Package.Identity
        if (-not [string]::Equals([string] $packagedIdentity.Name, $packageName, [StringComparison]::Ordinal) -or
            -not [string]::Equals([string] $packagedIdentity.Publisher, $publisher, [StringComparison]::Ordinal) -or
            -not [string]::Equals([string] $packagedIdentity.Version, $packageVersion, [StringComparison]::Ordinal) -or
            -not [string]::Equals([string] $packagedIdentity.ProcessorArchitecture, 'x64', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Packaged identity does not match the reviewed x64 Store identity/version.'
        }

        $shellHash = Get-ZipEntrySha256 -Archive $archive -EntryName 'IconReplacer.ShellExtension.dll'
        $commandHostHash = Get-ZipEntrySha256 -Archive $archive -EntryName 'IconReplacer.CommandHost.exe'
        $appHash = Get-ZipEntrySha256 -Archive $archive -EntryName 'IconReplacer.App.exe'
    }
    finally {
        $archive.Dispose()
    }

    $destinationName = "IconReplacer-$packageVersion-windows-x64-store.msix"
    $destinationPath = Join-Path $OutputDirectory $destinationName
    Copy-Item -LiteralPath $sourcePackage.FullName -Destination $destinationPath -Force

    $sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to resolve the source commit.'
    }

    $packageItem = Get-Item -LiteralPath $destinationPath
    $evidence = [ordered]@{
        schema = 'icon-replacer.store-build.v1'
        generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
        sourceCommit = $sourceCommit
        packageName = $packageName
        packageVersion = $packageVersion
        publisher = $publisher
        publisherDisplayName = $publisherDisplayName
        storeId = $storeId
        architecture = 'x64'
        distributionChannel = 'store'
        targetDeviceFamily = 'Windows.Desktop'
        minimumWindowsVersion = '10.0.17763.0'
        artifact = [ordered]@{
            name = $packageItem.Name
            path = $packageItem.FullName
            bytes = $packageItem.Length
            sha256 = (Get-FileHash -LiteralPath $packageItem.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        packagedExecutables = [ordered]@{
            appSha256 = $appHash
            commandHostSha256 = $commandHostHash
            nativeShellExtensionSha256 = $shellHash
        }
        comContract = [ordered]@{
            appId = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D'
            changeCommandClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D'
            collectionsCommandClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E'
            classicHandlerClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F'
            modernTargets = @('Directory', '.lnk')
            classicTargets = @('Directory', '.lnk')
        }
        buildCertificate = [ordered]@{
            subject = $signature.SignerCertificate.Subject
            thumbprint = $signature.SignerCertificate.Thumbprint
            status = $signature.Status.ToString()
            temporary = $true
            note = 'Build/test signature only. Microsoft Store replaces MSIX/AppX signatures after certification.'
        }
        packageBuilderEvidence = $sourceEvidence
        requiredEntries = $requiredEntries
        unresolvedGates = @(
            'Clean Windows 10 and Windows 11 install/lifecycle evidence',
            'Explorer modern/classic integration evidence',
            'Update and uninstall with Explorer running/restarted',
            'Store update network proof',
            'Privacy/listing/age-rating/submission review',
            'Source-code license decision'
        )
    }

    $evidencePath = Join-Path $OutputDirectory 'store-build-manifest.json'
    $evidence | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $evidencePath -Encoding utf8

    $checksumsPath = Join-Path $OutputDirectory 'SHA256SUMS.txt'
    @($destinationPath, $evidencePath) | ForEach-Object {
        $item = Get-Item -LiteralPath $_
        $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($item.Name)"
    } | Set-Content -LiteralPath $checksumsPath -Encoding ascii

    $forbiddenFiles = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -File | Where-Object {
        $_.Extension -in @('.pfx', '.cer', '.pem', '.key', '.pvk')
    })
    if ($forbiddenFiles.Count -gt 0) {
        throw "Certificate/private material leaked into the Store output: $($forbiddenFiles.FullName -join ', ')"
    }

    Write-Host ''
    Write-Host 'ICON REPLACER STORE MSIX READY FOR REVIEW' -ForegroundColor Green
    Write-Host "Package: $destinationPath"
    Write-Host "SHA-256: $($evidence.artifact.sha256)"
    Write-Host "Evidence: $evidencePath"
    Write-Host 'Do not upload until Windows 10/11 Explorer, update, uninstall, privacy, listing, and Partner Center gates are complete.' -ForegroundColor Yellow
}
finally {
    $env:PATH = $previousPath
    if ($null -eq $previousChannel) {
        Remove-Item Env:ICON_REPLACER_DISTRIBUTION_CHANNEL -ErrorAction SilentlyContinue
    }
    else {
        $env:ICON_REPLACER_DISTRIBUTION_CHANNEL = $previousChannel
    }

    if (Test-Path -LiteralPath $packageRoot) {
        Assert-PathInside -Candidate $packageRoot -Parent $artifactsRoot
        [System.IO.Directory]::Delete($packageRoot, $true)
    }
}
