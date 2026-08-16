[CmdletBinding()]
param(
    [string]$PackagePath,
    [string]$CertificatePath,
    [switch]$SnapshotOnly,
    [switch]$KeepInstalled,
    [switch]$UpgradeInstalledPackage,
    [switch]$InteractiveProof,
    [switch]$RecoverInterruptedProof,
    [string]$RecoverySessionPath,
    [switch]$ApproveExplorerRegistration,
    [switch]$ApproveExplorerRestart
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($SnapshotOnly -and ($KeepInstalled -or $InteractiveProof -or $RecoverInterruptedProof)) {
    throw "SnapshotOnly cannot be combined with KeepInstalled, InteractiveProof, or RecoverInterruptedProof."
}
if ($SnapshotOnly -and $UpgradeInstalledPackage) {
    throw "SnapshotOnly cannot be combined with UpgradeInstalledPackage."
}
if ($KeepInstalled -and $InteractiveProof) {
    throw "InteractiveProof owns cleanup and cannot be combined with KeepInstalled."
}
if ($RecoverInterruptedProof -and ($KeepInstalled -or $InteractiveProof)) {
    throw "RecoverInterruptedProof cannot be combined with KeepInstalled or InteractiveProof."
}
if ($UpgradeInstalledPackage -and !$KeepInstalled) {
    throw "UpgradeInstalledPackage requires KeepInstalled so the guarded update is not converted into an uninstall proof."
}
if ($UpgradeInstalledPackage -and ($InteractiveProof -or $RecoverInterruptedProof)) {
    throw "UpgradeInstalledPackage cannot be combined with InteractiveProof or RecoverInterruptedProof."
}
if ($UpgradeInstalledPackage -and !$ApproveExplorerRestart) {
    throw "UpgradeInstalledPackage requires ApproveExplorerRestart because Explorer hosts the installed shell extension."
}
if ($ApproveExplorerRestart -and !$UpgradeInstalledPackage) {
    throw "ApproveExplorerRestart is only valid with UpgradeInstalledPackage."
}
if ($RecoverySessionPath -and !$RecoverInterruptedProof) {
    throw "RecoverySessionPath requires RecoverInterruptedProof."
}
if (!$SnapshotOnly -and !$ApproveExplorerRegistration) {
    throw "Refusing to modify Explorer registration without -ApproveExplorerRegistration."
}

if ($null -eq ("IconReplacer.Lifecycle.ShellWindowNative" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace IconReplacer.Lifecycle
{
    public static class ShellWindowNative
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
'@
}

function Get-FileSnapshot {
    param([Parameter(Mandatory)][string]$Path)

    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            exists = $false
            length = 0
            sha256 = $null
        }
    }

    $file = Get-Item -LiteralPath $Path
    return [ordered]@{
        exists = $true
        length = $file.Length
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    }
}

function Get-MsixPackageSnapshot {
    param([Parameter(Mandatory)][string]$Path)

    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            exists = $false
            path = $Path
            length = 0
            sha256 = $null
            signatureStatus = $null
            signerSubject = $null
            name = $null
            version = $null
            publisher = $null
            processorArchitecture = $null
        }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $manifestEntry = $archive.GetEntry("AppxManifest.xml")
        if ($null -eq $manifestEntry) {
            throw "The candidate MSIX does not contain AppxManifest.xml."
        }

        $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
        try {
            $manifest = [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    $identity = $manifest.Package.Identity
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    return [ordered]@{
        exists = $true
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        signatureStatus = $signature.Status.ToString()
        signerSubject = if ($signature.SignerCertificate) {
            $signature.SignerCertificate.Subject
        }
        else {
            $null
        }
        name = [string]$identity.Name
        version = [string]$identity.Version
        publisher = [string]$identity.Publisher
        processorArchitecture = [string]$identity.ProcessorArchitecture
    }
}

function Get-PackageDeploymentReadiness {
    param(
        [Parameter(Mandatory)]$Candidate,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$InstalledPackages
    )

    if (!$Candidate.exists) {
        return [ordered]@{
            canDeploy = $false
            mode = "blocked"
            detail = "The manifest-selected candidate package has not been built."
        }
    }
    if ($Candidate.signatureStatus -ne "Valid") {
        return [ordered]@{
            canDeploy = $false
            mode = "blocked"
            detail = "The candidate package signature is not valid."
        }
    }
    if ($InstalledPackages.Count -eq 0) {
        return [ordered]@{
            canDeploy = $true
            mode = "fresh-install"
            detail = "No Icon Replacer package is installed; the candidate is ready for a guarded fresh-install proof."
        }
    }
    if ($InstalledPackages.Count -ne 1) {
        return [ordered]@{
            canDeploy = $false
            mode = "blocked"
            detail = "$($InstalledPackages.Count) Icon Replacer packages are installed; resolve the duplicate package state before deployment."
        }
    }

    $installed = $InstalledPackages[0]
    if (![string]::Equals($Candidate.name, $installed.Name, [StringComparison]::OrdinalIgnoreCase) -or
        ![string]::Equals($Candidate.publisher, $installed.Publisher, [StringComparison]::OrdinalIgnoreCase) -or
        ![string]::Equals($Candidate.processorArchitecture, $installed.Architecture.ToString(), [StringComparison]::OrdinalIgnoreCase)) {
        return [ordered]@{
            canDeploy = $false
            mode = "blocked"
            detail = "The candidate identity does not match the installed package identity."
        }
    }

    $candidateVersion = [Version]$Candidate.version
    $installedVersion = [Version]$installed.Version.ToString()
    if ($candidateVersion -le $installedVersion) {
        return [ordered]@{
            canDeploy = $false
            mode = "blocked"
            detail = "Candidate $candidateVersion is not newer than installed $installedVersion."
        }
    }

    return [ordered]@{
        canDeploy = $true
        mode = "upgrade"
        detail = "Candidate $candidateVersion is a signed update over installed $installedVersion."
    }
}

function Restore-FileContent {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][bool]$Existed,
        [Parameter()][AllowNull()][byte[]]$Content
    )

    if (!$Existed) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            Remove-Item -LiteralPath $Path -Force
        }
        return
    }

    if ($null -eq $Content) {
        throw "Cannot restore $Path because its baseline content was not captured."
    }

    $parent = Split-Path -Parent $Path
    if (!(Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [System.IO.File]::WriteAllBytes($Path, $Content)
}

function Protect-RecoveryContent {
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$Content)

    $entropy = [Text.Encoding]::UTF8.GetBytes("IconReplacer.ExplorerProof.v1")
    return [Security.Cryptography.ProtectedData]::Protect(
        $Content,
        $entropy,
        [Security.Cryptography.DataProtectionScope]::CurrentUser)
}

function Unprotect-RecoveryContent {
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$Content)

    $entropy = [Text.Encoding]::UTF8.GetBytes("IconReplacer.ExplorerProof.v1")
    return [Security.Cryptography.ProtectedData]::Unprotect(
        $Content,
        $entropy,
        [Security.Cryptography.DataProtectionScope]::CurrentUser)
}

function Get-DirectorySnapshot {
    param([Parameter(Mandatory)][string]$Path)

    if (!(Test-Path -LiteralPath $Path -PathType Container)) {
        return [ordered]@{
            exists = $false
            fileCount = 0
            totalLength = 0
            contentSha256 = $null
        }
    }

    $rootPrefix = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    $files = @(Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName)
    $lines = foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($rootPrefix.Length)
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        "$relativePath|$($file.Length)|$hash"
    }
    $payload = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $contentHash = -join ($sha256.ComputeHash($payload) | ForEach-Object {
            $_.ToString("X2")
        })
    }
    finally {
        $sha256.Dispose()
    }

    return [ordered]@{
        exists = $true
        fileCount = $files.Count
        totalLength = ($files | Measure-Object Length -Sum).Sum
        contentSha256 = $contentHash
    }
}

function Get-TextSha256 {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return -join ($sha256.ComputeHash($bytes) | ForEach-Object {
            $_.ToString("X2")
        })
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-RegistryTreeSnapshot {
    param([Parameter(Mandatory)]$RootKey)

    $rootName = $RootKey.Name
    $keys = @($RootKey) + @(Get-ChildItem -LiteralPath $RootKey.PSPath -Recurse -ErrorAction Stop)
    return @(
        foreach ($key in $keys | Sort-Object Name) {
            $properties = Get-ItemProperty -LiteralPath $key.PSPath -ErrorAction Stop
            $values = [ordered]@{}
            foreach ($property in @($properties.PSObject.Properties |
                Where-Object Name -NotMatch '^PS' |
                Sort-Object Name)) {
                $value = $property.Value
                $values[$property.Name] = if ($value -is [byte[]]) {
                    "base64:$([Convert]::ToBase64String($value))"
                }
                elseif ($value -is [array]) {
                    @($value | ForEach-Object { "$_" })
                }
                elseif ($null -eq $value) {
                    $null
                }
                else {
                    "$value"
                }
            }

            [ordered]@{
                relativePath = $key.Name.Substring($rootName.Length).TrimStart('\')
                values = $values
            }
        }
    )
}

function Get-ContextMenuSnapshot {
    $paths = @(
        "Registry::HKEY_CLASSES_ROOT\Directory\shell",
        "Registry::HKEY_CLASSES_ROOT\Directory\shellex\ContextMenuHandlers",
        "Registry::HKEY_CLASSES_ROOT\Folder\shell",
        "Registry::HKEY_CLASSES_ROOT\Folder\shellex\ContextMenuHandlers",
        "Registry::HKEY_CLASSES_ROOT\AllFilesystemObjects\shell",
        "Registry::HKEY_CLASSES_ROOT\AllFilesystemObjects\shellex\ContextMenuHandlers",
        "Registry::HKEY_CLASSES_ROOT\lnkfile\shell",
        "Registry::HKEY_CLASSES_ROOT\lnkfile\shellex\ContextMenuHandlers"
    )

    $entries = @(
        foreach ($path in $paths) {
            foreach ($key in @(Get-ChildItem -LiteralPath $path -ErrorAction SilentlyContinue)) {
                $properties = Get-ItemProperty -LiteralPath $key.PSPath -ErrorAction SilentlyContinue
                $defaultProperty = $properties.PSObject.Properties['(default)']
                $classIdProperty = $properties.PSObject.Properties['CLSID']
                $defaultValue = if ($null -ne $defaultProperty) { $defaultProperty.Value } else { $null }
                $classId = if ($null -ne $classIdProperty) { $classIdProperty.Value } else { $null }
                $commandPath = Join-Path $key.PSPath "command"
                $command = if (Test-Path -LiteralPath $commandPath) {
                    $commandProperties = Get-ItemProperty -LiteralPath $commandPath -ErrorAction SilentlyContinue
                    $commandProperty = $commandProperties.PSObject.Properties['(default)']
                    if ($null -ne $commandProperty) { $commandProperty.Value } else { $null }
                }
                else {
                    $null
                }

                $identity = "$($key.PSChildName)|$defaultValue|$classId|$command"
                if ($identity -match "(?i)IconReplacer|B8F1A86D-4C52-4C53-BF72-30B59F7F0F7[D-F]") {
                    continue
                }

                [ordered]@{
                    path = $path
                    name = $key.PSChildName
                    defaultValue = $defaultValue
                    classId = $classId
                    command = $command
                    tree = Get-RegistryTreeSnapshot -RootKey $key
                }
            }
        }
    ) | Sort-Object path, name

    $content = $entries | ConvertTo-Json -Depth 4 -Compress
    return [ordered]@{
        entryCount = $entries.Count
        contentSha256 = Get-TextSha256 -Value $content
        entries = $entries
    }
}

function Get-ExplorerSnapshot {
    $shellWindow = [IconReplacer.Lifecycle.ShellWindowNative]::GetShellWindow()
    [uint32]$shellProcessId = 0
    if ($shellWindow -ne [IntPtr]::Zero) {
        [void][IconReplacer.Lifecycle.ShellWindowNative]::GetWindowThreadProcessId(
            $shellWindow,
            [ref]$shellProcessId)
    }

    $processes = if ($shellProcessId -gt 0) {
        @(Get-Process -Id $shellProcessId -ErrorAction SilentlyContinue)
    }
    else {
        @()
    }
    $thirdPartyModules = @(
        foreach ($process in $processes) {
            try {
                foreach ($module in @($process.Modules | Where-Object {
                    $_.ModuleName -match "(?i)Start(All|Is)Back"
                })) {
                    "$($process.Id)|$($module.ModuleName)|$($module.FileName)"
                }
            }
            catch {
                # Module enumeration can be restricted; process identity is still useful evidence.
            }
        }
    ) | Sort-Object

    return [ordered]@{
        shellWindow = $shellWindow.ToInt64()
        processIdentities = @($processes | ForEach-Object {
            "$($_.Id)|$($_.StartTime.ToUniversalTime().ToString('O'))"
        })
        thirdPartyModules = $thirdPartyModules
    }
}

function Stop-IconReplacerProcesses {
    Get-Process -Name "IconReplacer.App", "IconReplacer.CommandHost" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

function Get-IconReplacerPackageModuleHolders {
    $packageRoots = @(Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue |
        ForEach-Object { [System.IO.Path]::GetFullPath($_.InstallLocation).TrimEnd('\') + '\' })
    if ($packageRoots.Count -eq 0) {
        return @()
    }

    return @(Get-Process -ErrorAction SilentlyContinue | ForEach-Object {
        $process = $_
        try {
            foreach ($module in $process.Modules) {
                $modulePath = $module.FileName
                if ($packageRoots | Where-Object {
                    $modulePath.StartsWith($_, [StringComparison]::OrdinalIgnoreCase)
                }) {
                    [pscustomobject]@{
                        ProcessName = $process.ProcessName
                        ProcessId = $process.Id
                        ModulePath = $modulePath
                    }
                    break
                }
            }
        }
        catch {
            # Protected processes cannot host this per-user development package.
        }
    })
}

function Stop-IconReplacerPackageProcesses {
    Stop-IconReplacerProcesses
    $holders = @(Get-IconReplacerPackageModuleHolders)
    foreach ($holder in $holders | Sort-Object ProcessId -Unique) {
        Stop-Process -Id $holder.ProcessId -Force -ErrorAction Stop
    }

    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        Start-Sleep -Milliseconds 250
        $remaining = @(Get-IconReplacerPackageModuleHolders)
        if ($remaining.Count -eq 0) {
            return
        }
    }

    $description = @(Get-IconReplacerPackageModuleHolders | ForEach-Object {
        "$($_.ProcessName) ($($_.ProcessId)): $($_.ModulePath)"
    }) -join "; "
    throw "Icon Replacer package modules remain in use: $description"
}

function Remove-InstalledPackage {
    $packages = @(Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue)
    foreach ($package in $packages) {
        Remove-AppxPackage -Package $package.PackageFullName -ErrorAction Stop
    }
}

function Test-SnapshotEqual {
    param(
        [Parameter(Mandatory)]$Before,
        [Parameter(Mandatory)]$After
    )

    return (($Before | ConvertTo-Json -Compress) -eq ($After | ConvertTo-Json -Compress))
}

function Assert-ContextMenuStatePreserved {
    param(
        [Parameter(Mandatory)]$Before,
        [Parameter(Mandatory)]$After,
        [Parameter(Mandatory)][string]$Stage
    )

    if (!(Test-SnapshotEqual -Before $Before.entries -After $After.entries)) {
        throw "A non-Icon-Replacer context-menu registration changed during $Stage. Before: $($Before.contentSha256); after: $($After.contentSha256)."
    }
}

function Assert-ExplorerSessionPreserved {
    param(
        [Parameter(Mandatory)]$Before,
        [Parameter(Mandatory)]$After
    )

    if (!(Test-SnapshotEqual -Before $Before.processIdentities -After $After.processIdentities)) {
        throw "Explorer restarted or crashed during the MSIX lifecycle. The lifecycle test must not disrupt the user's shell session."
    }
}

function Get-ExplorerModuleIdentities {
    param([Parameter(Mandatory)]$Snapshot)

    return @($Snapshot.thirdPartyModules | ForEach-Object {
        $parts = $_.Split('|', 3)
        if ($parts.Count -eq 3) {
            "$($parts[1])|$($parts[2])"
        }
    }) | Sort-Object -Unique
}

function Assert-ExplorerRestartedHealthy {
    param(
        [Parameter(Mandatory)]$Before,
        [Parameter(Mandatory)]$After
    )

    if ($After.shellWindow -eq 0 -or $After.processIdentities.Count -eq 0) {
        throw "Explorer did not restore a shell window after the approved restart."
    }
    if (Test-SnapshotEqual -Before $Before.processIdentities -After $After.processIdentities) {
        throw "Explorer did not acquire a new process identity after the approved restart."
    }

    $requiredModules = Get-ExplorerModuleIdentities -Snapshot $Before
    $restoredModules = Get-ExplorerModuleIdentities -Snapshot $After
    $missingModules = @($requiredModules | Where-Object { $_ -notin $restoredModules })
    if ($missingModules.Count -gt 0) {
        throw "Explorer restarted without restoring expected shell modules: $($missingModules -join ', ')."
    }
}

function Stop-ExplorerShell {
    param([Parameter(Mandatory)]$Before)

    if ($Before.processIdentities.Count -ne 1) {
        throw "Expected exactly one Explorer shell process before restart."
    }

    $shellProcessId = [int]($Before.processIdentities[0].Split('|', 2)[0])
    Stop-Process -Id $shellProcessId -Force -ErrorAction Stop
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        Start-Sleep -Milliseconds 250
        $snapshot = Get-ExplorerSnapshot
        if ($snapshot.shellWindow -eq 0 -and
            !(Get-Process -Id $shellProcessId -ErrorAction SilentlyContinue)) {
            return
        }
    }

    throw "Explorer did not stop within the approved restart window."
}

function Start-ExplorerShell {
    param([Parameter(Mandatory)]$Before)

    Start-Process -FilePath (Join-Path $env:WINDIR "explorer.exe")
    $lastSnapshot = $null
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Milliseconds 250
        $lastSnapshot = Get-ExplorerSnapshot
        if ($lastSnapshot.shellWindow -eq 0 -or $lastSnapshot.processIdentities.Count -eq 0) {
            continue
        }

        try {
            Assert-ExplorerRestartedHealthy -Before $Before -After $lastSnapshot
            return $lastSnapshot
        }
        catch {
            # Shell modules can load shortly after the desktop window appears.
        }
    }

    if ($null -eq $lastSnapshot) {
        throw "Explorer did not produce a restart snapshot."
    }
    Assert-ExplorerRestartedHealthy -Before $Before -After $lastSnapshot
    return $lastSnapshot
}

$packageMutationStarted = $false
$libraryBefore = $null
$restoreStateBefore = $null
$contextMenuBefore = $null
$explorerBefore = $null
$interactiveProofSession = $null
$interactiveProofPath = $null
$restoreStateContentBefore = $null
$restoreStateRecoveryPath = $null
$deploymentMode = "fresh-install"
$contextMenuAfterFreshRemove = $null
$explorerStopStarted = $false
$explorerRestarted = $false
try {
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$packageRoot = Join-Path $repoRoot "artifacts\package"
if (!$PackagePath) {
    $sourceManifestPath = Join-Path $repoRoot "src\IconReplacer.App\Package.appxmanifest"
    $sourceManifestXml = [xml](Get-Content -LiteralPath $sourceManifestPath -Raw)
    $sourceIdentity = $sourceManifestXml.Package.Identity
    $PackagePath = Join-Path $packageRoot "$($sourceIdentity.Name)_$($sourceIdentity.Version)_x64.msix"
}
if (!$CertificatePath) {
    $CertificatePath = Join-Path $packageRoot "IconReplacer.Dev.cer"
}

$PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
$CertificatePath = [System.IO.Path]::GetFullPath($CertificatePath)
$evidenceFileName = if ($KeepInstalled) {
    "lifecycle-install-evidence.json"
}
else {
    "lifecycle-evidence.json"
}
$evidencePath = Join-Path $packageRoot $evidenceFileName
$libraryPath = Join-Path $env:USERPROFILE ".icons"
$restoreStatePath = Join-Path $env:APPDATA "Icon Replacer\state.json"
$proofRoot = Join-Path $repoRoot "artifacts\explorer-proof"

if ($RecoverInterruptedProof) {
    $terminalStatuses = @(
        "registration-removed-awaiting-evidence-review",
        "proof-failed-cleanup-verified",
        "interrupted-proof-recovered"
    )
    $selectedSessionPath = if ($RecoverySessionPath) {
        [System.IO.Path]::GetFullPath($RecoverySessionPath)
    }
    else {
        $candidate = Get-ChildItem -LiteralPath $proofRoot -Filter "session.json" -File -Recurse -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Where-Object {
                try {
                    $candidateSession = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                    $candidateSession.status -notin $terminalStatuses
                }
                catch {
                    $false
                }
            } |
            Select-Object -First 1
        if ($candidate) { $candidate.FullName } else { $null }
    }

    if (!$selectedSessionPath -or !(Test-Path -LiteralPath $selectedSessionPath -PathType Leaf)) {
        throw "No interrupted Explorer proof session is available for recovery."
    }

    $proofRootPrefix = [System.IO.Path]::GetFullPath($proofRoot).TrimEnd('\') + '\'
    if (!$selectedSessionPath.StartsWith($proofRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Recovery session must be stored under $proofRoot."
    }

    $sessionDocument = Get-Content -LiteralPath $selectedSessionPath -Raw | ConvertFrom-Json
    if ($sessionDocument.schemaVersion -ne 1 -or
        $null -eq $sessionDocument.contextMenuBefore -or
        $null -eq $sessionDocument.explorerBefore -or
        $null -eq $sessionDocument.iconLibraryBefore -or
        $null -eq $sessionDocument.restoreStateBefore) {
        throw "The Explorer proof session does not contain a supported recovery baseline."
    }
    if ($sessionDocument.status -in $terminalStatuses) {
        Write-Host "Explorer proof session is already in terminal state: $($sessionDocument.status)"
        return
    }

    $interactiveProofSession = [ordered]@{}
    foreach ($property in $sessionDocument.PSObject.Properties) {
        $interactiveProofSession[$property.Name] = $property.Value
    }
    $interactiveProofPath = $selectedSessionPath
    $sessionDirectory = Split-Path -Parent $interactiveProofPath
    $libraryBefore = $sessionDocument.iconLibraryBefore
    $restoreStateBefore = $sessionDocument.restoreStateBefore
    $contextMenuBefore = $sessionDocument.contextMenuBefore
    $explorerBefore = $sessionDocument.explorerBefore

    if ($restoreStateBefore.exists) {
        $restoreStateRecoveryPath = Join-Path $sessionDirectory $sessionDocument.restoreStateRecoveryFile
        $sessionDirectoryPrefix = [System.IO.Path]::GetFullPath($sessionDirectory).TrimEnd('\') + '\'
        $restoreStateRecoveryPath = [System.IO.Path]::GetFullPath($restoreStateRecoveryPath)
        if (!$restoreStateRecoveryPath.StartsWith($sessionDirectoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Restore-state recovery data must remain inside its proof session."
        }
        if (!(Test-Path -LiteralPath $restoreStateRecoveryPath -PathType Leaf)) {
            throw "Restore-state recovery data is missing: $restoreStateRecoveryPath"
        }
        $restoreStateContentBefore = Unprotect-RecoveryContent -Content (
            [System.IO.File]::ReadAllBytes($restoreStateRecoveryPath))
    }

    $InteractiveProof = $true
    $interactiveProofSession.status = "interrupted-proof-recovery-running"
    $interactiveProofSession.recoveryStartedAt = [DateTimeOffset]::UtcNow.ToString("O")
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8

    $packageMutationStarted = $true
    Stop-IconReplacerProcesses
    Remove-InstalledPackage
    if (Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue) {
        throw "Icon Replacer remained installed during interrupted-proof recovery."
    }

    Restore-FileContent `
        -Path $restoreStatePath `
        -Existed $restoreStateBefore.exists `
        -Content $restoreStateContentBefore
    $interactiveProofSession.restoreStateRestored = $true

    $contextMenuAfterRecovery = Get-ContextMenuSnapshot
    Assert-ContextMenuStatePreserved `
        -Before $contextMenuBefore `
        -After $contextMenuAfterRecovery `
        -Stage "interrupted-proof recovery"
    $libraryAfterRecovery = Get-DirectorySnapshot -Path $libraryPath
    $restoreStateAfterRecovery = Get-FileSnapshot -Path $restoreStatePath
    if (!(Test-SnapshotEqual -Before $libraryBefore -After $libraryAfterRecovery)) {
        throw "The Icon Library differs from the interrupted-proof baseline."
    }
    if (!(Test-SnapshotEqual -Before $restoreStateBefore -After $restoreStateAfterRecovery)) {
        throw "Restore state differs from the interrupted-proof baseline."
    }
    $explorerAfterRecovery = Get-ExplorerSnapshot
    Assert-ExplorerSessionPreserved -Before $explorerBefore -After $explorerAfterRecovery

    if ($restoreStateRecoveryPath -and (Test-Path -LiteralPath $restoreStateRecoveryPath -PathType Leaf)) {
        Remove-Item -LiteralPath $restoreStateRecoveryPath -Force
    }
    $interactiveProofSession.status = "interrupted-proof-recovered"
    $interactiveProofSession.cleanupCompletedAt = [DateTimeOffset]::UtcNow.ToString("O")
    $interactiveProofSession.packageRemoved = $true
    $interactiveProofSession.restoreStateRecoveryRemoved = $true
    $interactiveProofSession.contextMenuAfterCleanupSha256 = $contextMenuAfterRecovery.contentSha256
    $interactiveProofSession.explorerAfterCleanup = $explorerAfterRecovery
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8

    Write-Host "Interrupted Explorer proof recovered."
    Write-Host "Evidence: $interactiveProofPath"
    return
}

if (!$SnapshotOnly) {
    if (!(Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
        throw "Package not found: $PackagePath"
    }
    if (!(Test-Path -LiteralPath $CertificatePath -PathType Leaf)) {
        throw "Public development certificate not found: $CertificatePath"
    }
}

$libraryBefore = Get-DirectorySnapshot -Path $libraryPath
$restoreStateBefore = Get-FileSnapshot -Path $restoreStatePath
$restoreStateContentBefore = if ($restoreStateBefore.exists) {
    [System.IO.File]::ReadAllBytes($restoreStatePath)
}
else {
    $null
}
$contextMenuBefore = Get-ContextMenuSnapshot
$explorerBefore = Get-ExplorerSnapshot

if ($SnapshotOnly) {
    $snapshotEvidencePath = Join-Path $packageRoot "context-menu-current.json"
    $installedPackages = @(Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue)
    $candidatePackage = Get-MsixPackageSnapshot -Path $PackagePath
    $deploymentReadiness = Get-PackageDeploymentReadiness `
        -Candidate $candidatePackage `
        -InstalledPackages $installedPackages
    [ordered]@{
        capturedAt = [DateTimeOffset]::UtcNow.ToString("O")
        installedPackageCount = $installedPackages.Count
        installedPackages = @($installedPackages | ForEach-Object {
            [ordered]@{
                packageFullName = $_.PackageFullName
                name = $_.Name
                version = $_.Version.ToString()
                publisher = $_.Publisher
                processorArchitecture = $_.Architecture.ToString()
                installLocation = $_.InstallLocation
            }
        })
        candidatePackage = $candidatePackage
        deploymentReadiness = $deploymentReadiness
        iconLibrary = $libraryBefore
        restoreState = $restoreStateBefore
        contextMenu = $contextMenuBefore
        explorer = $explorerBefore
    } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $snapshotEvidencePath -Encoding utf8

    Write-Host "Context-menu snapshot completed without package mutation."
    Write-Host "Evidence: $snapshotEvidencePath"
    return
}

$installedPackagesBefore = @(Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue)
if ($installedPackagesBefore.Count -gt 0) {
    if (!$UpgradeInstalledPackage) {
        throw "Refusing to replace an existing Icon Replacer package. Start from an uninstalled baseline or use the explicitly guarded UpgradeInstalledPackage path."
    }

    $upgradeCandidate = Get-MsixPackageSnapshot -Path $PackagePath
    $upgradeReadiness = Get-PackageDeploymentReadiness `
        -Candidate $upgradeCandidate `
        -InstalledPackages $installedPackagesBefore
    if (!$upgradeReadiness.canDeploy -or $upgradeReadiness.mode -ne "upgrade") {
        throw "The installed package cannot be upgraded by this candidate: $($upgradeReadiness.detail)"
    }
    $deploymentMode = "upgrade"
}

$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
    $CertificatePath
)
$trustedCertificate = Get-ChildItem "Cert:\LocalMachine\TrustedPeople" |
    Where-Object Thumbprint -eq $certificate.Thumbprint |
    Select-Object -First 1
if (!$trustedCertificate) {
    throw "The development certificate is not trusted in LocalMachine\TrustedPeople. Run scripts\Install-DevelopmentCertificate.ps1 from an elevated PowerShell session."
}

if ($InteractiveProof) {
    $sessionId = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss")
    $interactiveProofDirectory = Join-Path $proofRoot $sessionId
    $interactiveProofPath = Join-Path $interactiveProofDirectory "session.json"
    New-Item -ItemType Directory -Path $interactiveProofDirectory -Force | Out-Null

    $restoreStateRecoveryFile = if ($restoreStateBefore.exists) {
        "restore-state.before.dpapi"
    }
    else {
        $null
    }
    if ($restoreStateRecoveryFile) {
        $restoreStateRecoveryPath = Join-Path $interactiveProofDirectory $restoreStateRecoveryFile
        $protectedRestoreState = Protect-RecoveryContent -Content $restoreStateContentBefore
        [System.IO.File]::WriteAllBytes($restoreStateRecoveryPath, $protectedRestoreState)
    }

    $interactiveProofSession = [ordered]@{
        schemaVersion = 1
        status = "preflight-complete-registration-pending"
        startedAt = [DateTimeOffset]::UtcNow.ToString("O")
        completedAt = $null
        packagePath = $PackagePath
        packageSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
        packageFullName = $null
        artifactDirectory = $interactiveProofDirectory
        checklistPath = Join-Path $repoRoot "docs\verification\MANUAL_EXPLORER_PROOF.md"
        capturedArtifacts = @()
        iconLibraryBefore = $libraryBefore
        restoreStateBefore = $restoreStateBefore
        contextMenuBefore = $contextMenuBefore
        explorerBefore = $explorerBefore
        restoreStateBaselineCaptured = $true
        restoreStateRestored = $false
        restoreStateRecoveryFile = $restoreStateRecoveryFile
        restoreStateRecoveryRemoved = !$restoreStateBefore.exists
        packageRemoved = $false
        recoveryStartedAt = $null
        cleanupCompletedAt = $null
        contextMenuAfterCleanupSha256 = $null
        explorerAfterCleanup = $null
        failure = $null
        cleanupIssues = @()
    }
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8
}

$packageMutationStarted = $true
if ($deploymentMode -eq "fresh-install") {
    Stop-IconReplacerProcesses
    Remove-InstalledPackage
    if (Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue) {
        throw "IconReplacer remained registered before the fresh-install proof."
    }
    $contextMenuAfterFreshRemove = Get-ContextMenuSnapshot
    Assert-ContextMenuStatePreserved -Before $contextMenuBefore -After $contextMenuAfterFreshRemove -Stage "pre-install cleanup"
}
else {
    $contextMenuAfterFreshRemove = $contextMenuBefore
    $explorerStopStarted = $true
    Stop-ExplorerShell -Before $explorerBefore
    Stop-IconReplacerPackageProcesses
}

$installStartedAt = [DateTimeOffset]::UtcNow
if ($deploymentMode -eq "upgrade") {
    Add-AppxPackage -Path $PackagePath -ForceApplicationShutdown -ErrorAction Stop
}
else {
    Add-AppxPackage -Path $PackagePath -ErrorAction Stop
}
$installedPackage = Get-AppxPackage -Name "IconReplacer" -ErrorAction Stop |
    Sort-Object Version -Descending |
    Select-Object -First 1
if (!$installedPackage) {
    throw "The MSIX install completed without registering IconReplacer."
}
$installedCandidate = Get-MsixPackageSnapshot -Path $PackagePath
if ($installedPackage.Version.ToString() -ne $installedCandidate.version) {
    throw "Windows registered Icon Replacer $($installedPackage.Version), but candidate $($installedCandidate.version) was expected."
}
if ($explorerStopStarted) {
    $null = Start-ExplorerShell -Before $explorerBefore
    $explorerRestarted = $true
}
$contextMenuAfterInstall = Get-ContextMenuSnapshot
Assert-ContextMenuStatePreserved -Before $contextMenuBefore -After $contextMenuAfterInstall -Stage "package installation"

$installedManifestPath = Join-Path $installedPackage.InstallLocation "AppxManifest.xml"
$installedShellExtensionPath = Join-Path $installedPackage.InstallLocation "IconReplacer.ShellExtension.dll"
$installedCommandHostPath = Join-Path $installedPackage.InstallLocation "IconReplacer.CommandHost.exe"
$installedAppIconPaths = @(
    (Join-Path $installedPackage.InstallLocation "Assets\AppIcon.ico")
)
foreach ($requiredPath in @($installedManifestPath, $installedShellExtensionPath, $installedCommandHostPath) + $installedAppIconPaths) {
    if (!(Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Installed package is missing $requiredPath."
    }
}

$installedManifest = Get-Content -LiteralPath $installedManifestPath -Raw
if ($installedManifest -notmatch "windows.fileExplorerContextMenus" -or
    $installedManifest -notmatch "windows.fileExplorerClassicContextMenuHandler" -or
    $installedManifest -notmatch "windows.comServer") {
    throw "Installed manifest is missing the modern/classic Explorer context-menu or COM registration."
}

$aumid = "$($installedPackage.PackageFamilyName)!IconReplacer.App"
Start-Process "explorer.exe" "shell:AppsFolder\$aumid"
$appProcess = $null
for ($attempt = 0; $attempt -lt 30 -and !$appProcess; $attempt++) {
    Start-Sleep -Milliseconds 250
    $appProcess = Get-Process -Name "IconReplacer.App" -ErrorAction SilentlyContinue |
        Select-Object -First 1
}
if (!$appProcess) {
    throw "The installed app did not launch through its AUMID."
}

$libraryAfterInstall = Get-DirectorySnapshot -Path $libraryPath
$restoreStateAfterInstall = Get-FileSnapshot -Path $restoreStatePath
$libraryPreservedAfterInstall = Test-SnapshotEqual -Before $libraryBefore -After $libraryAfterInstall
$restoreStatePreservedAfterInstall = Test-SnapshotEqual -Before $restoreStateBefore -After $restoreStateAfterInstall
if (!$libraryPreservedAfterInstall -or !$restoreStatePreservedAfterInstall) {
    throw "User data changed during package installation."
}

$installProof = [ordered]@{
    startedAt = $installStartedAt.ToString("O")
    completedAt = [DateTimeOffset]::UtcNow.ToString("O")
    packageFullName = $installedPackage.PackageFullName
    packageFamilyName = $installedPackage.PackageFamilyName
    installLocation = $installedPackage.InstallLocation
    aumid = $aumid
    appProcessId = $appProcess.Id
    shellManifestRegistered = $true
    classicShellManifestRegistered = $true
    applicationIconPresent = $true
    commandHostPresent = $true
    shellExtensionPresent = $true
    iconLibraryPreserved = $libraryPreservedAfterInstall
    restoreStatePreserved = $restoreStatePreservedAfterInstall
    iconLibraryAfter = $libraryAfterInstall
    restoreStateAfter = $restoreStateAfterInstall
}

$uninstallProof = $null
if ($InteractiveProof) {
    $interactiveProofSession.status = "installed-awaiting-manual-proof"
    $interactiveProofSession.packageFullName = $installedPackage.PackageFullName
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8

    Write-Host ""
    Write-Host "Explorer proof session is active."
    Write-Host "Checklist: $($interactiveProofSession.checklistPath)"
    Write-Host "Save screenshots under: $interactiveProofDirectory"
    Write-Host "The package will be removed and the baseline rechecked after confirmation."
    [void](Read-Host "Complete the approved Explorer matrix, then press Enter to clean up")

    $capturedArtifacts = @(
        Get-ChildItem -LiteralPath $interactiveProofDirectory -File -ErrorAction SilentlyContinue |
            Where-Object Extension -In ".png", ".jpg", ".jpeg" |
            Sort-Object Name |
            ForEach-Object FullName
    )
    $interactiveProofSession.status = "manual-proof-complete-cleanup-pending"
    $interactiveProofSession.completedAt = [DateTimeOffset]::UtcNow.ToString("O")
    $interactiveProofSession.capturedArtifacts = $capturedArtifacts
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8
}

if (!$KeepInstalled) {
    Stop-IconReplacerProcesses
    $uninstallStartedAt = [DateTimeOffset]::UtcNow
    Remove-InstalledPackage
    $remainingPackage = Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue
    if ($remainingPackage) {
        throw "IconReplacer remained registered after uninstall."
    }

    $contextMenuAfterUninstall = Get-ContextMenuSnapshot
    Assert-ContextMenuStatePreserved -Before $contextMenuBefore -After $contextMenuAfterUninstall -Stage "package uninstallation"

    if ($InteractiveProof) {
        Restore-FileContent `
            -Path $restoreStatePath `
            -Existed $restoreStateBefore.exists `
            -Content $restoreStateContentBefore
        $interactiveProofSession.restoreStateRestored = $true
    }

    $libraryAfter = Get-DirectorySnapshot -Path $libraryPath
    $restoreStateAfter = Get-FileSnapshot -Path $restoreStatePath
    $libraryPreserved = Test-SnapshotEqual -Before $libraryBefore -After $libraryAfter
    $restoreStatePreserved = Test-SnapshotEqual -Before $restoreStateBefore -After $restoreStateAfter
    if (!$libraryPreserved -or !$restoreStatePreserved) {
        throw "User data changed during the install/uninstall lifecycle."
    }

    $uninstallProof = [ordered]@{
        startedAt = $uninstallStartedAt.ToString("O")
        completedAt = [DateTimeOffset]::UtcNow.ToString("O")
        packageRemoved = $true
        shellIntegrationRemovedWithPackage = $true
        iconLibraryPreserved = $libraryPreserved
        restoreStatePreserved = $restoreStatePreserved
        iconLibraryAfter = $libraryAfter
        restoreStateAfter = $restoreStateAfter
        contextMenuAfterUninstall = $contextMenuAfterUninstall
    }
}

$explorerAfter = Get-ExplorerSnapshot
if ($explorerStopStarted) {
    Assert-ExplorerRestartedHealthy -Before $explorerBefore -After $explorerAfter
}
else {
    Assert-ExplorerSessionPreserved -Before $explorerBefore -After $explorerAfter
}

if ($null -ne $interactiveProofSession) {
    if ($restoreStateRecoveryPath -and (Test-Path -LiteralPath $restoreStateRecoveryPath -PathType Leaf)) {
        Remove-Item -LiteralPath $restoreStateRecoveryPath -Force
    }
    $interactiveProofSession.status = "registration-removed-awaiting-evidence-review"
    $interactiveProofSession.cleanupCompletedAt = [DateTimeOffset]::UtcNow.ToString("O")
    $interactiveProofSession.packageRemoved = $true
    $interactiveProofSession.restoreStateRecoveryRemoved = $true
    $interactiveProofSession.contextMenuAfterCleanupSha256 = $uninstallProof.contextMenuAfterUninstall.contentSha256
    $interactiveProofSession.explorerAfterCleanup = $explorerAfter
    $interactiveProofSession |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $interactiveProofPath -Encoding utf8
}

$evidence = [ordered]@{
    capturedAt = [DateTimeOffset]::UtcNow.ToString("O")
    deploymentMode = $deploymentMode
    explorerRestartApproved = [bool]$ApproveExplorerRestart
    explorerRestarted = $explorerRestarted
    packagePath = $PackagePath
    packageSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    trustedCertificateThumbprint = $trustedCertificate.Thumbprint
    keptInstalled = [bool]$KeepInstalled
    iconLibraryBefore = $libraryBefore
    restoreStateBefore = $restoreStateBefore
    contextMenuBefore = $contextMenuBefore
    installedPackagesBefore = @($installedPackagesBefore | ForEach-Object { $_.PackageFullName })
    contextMenuAfterFreshRemove = $contextMenuAfterFreshRemove
    contextMenuAfterInstall = $contextMenuAfterInstall
    explorerBefore = $explorerBefore
    explorerAfter = $explorerAfter
    install = $installProof
    uninstall = $uninstallProof
    interactiveProof = $interactiveProofSession
}
$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $evidencePath -Encoding utf8

Write-Host "MSIX lifecycle proof completed."
Write-Host "Installed package: $($installProof.packageFullName)"
Write-Host "Kept installed: $([bool]$KeepInstalled)"
Write-Host "Evidence: $evidencePath"
}
catch {
    $failure = $_
    if ($packageMutationStarted) {
        $cleanupIssues = [System.Collections.Generic.List[string]]::new()
        Stop-IconReplacerProcesses
        if ($explorerStopStarted) {
            try {
                $currentExplorer = Get-ExplorerSnapshot
                if ($currentExplorer.shellWindow -eq 0 -or $currentExplorer.processIdentities.Count -eq 0) {
                    $null = Start-ExplorerShell -Before $explorerBefore
                }
                $explorerRestarted = $true
            }
            catch {
                $cleanupIssues.Add("Explorer restart recovery failed: $($_.Exception.Message)")
            }
        }
        if ($deploymentMode -eq "fresh-install") {
            try {
                Remove-InstalledPackage
            }
            catch {
                $cleanupIssues.Add("Package removal failed: $($_.Exception.Message)")
            }

            if (Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue) {
                $cleanupIssues.Add("Icon Replacer remained installed after failure cleanup.")
            }
        }
        else {
            $remainingUpgradePackages = @(Get-AppxPackage -Name "IconReplacer" -ErrorAction SilentlyContinue)
            if ($remainingUpgradePackages.Count -ne 1) {
                $cleanupIssues.Add("The guarded upgrade did not leave exactly one Icon Replacer package registered.")
            }
        }

        if ($null -ne $contextMenuBefore) {
            try {
                $contextMenuAfterFailure = Get-ContextMenuSnapshot
                Assert-ContextMenuStatePreserved `
                    -Before $contextMenuBefore `
                    -After $contextMenuAfterFailure `
                    -Stage "failure cleanup"
            }
            catch {
                $cleanupIssues.Add($_.Exception.Message)
            }
        }

        if ($null -ne $explorerBefore) {
            try {
                $explorerAfterFailure = Get-ExplorerSnapshot
                if ($explorerStopStarted) {
                    Assert-ExplorerRestartedHealthy -Before $explorerBefore -After $explorerAfterFailure
                }
                else {
                    Assert-ExplorerSessionPreserved -Before $explorerBefore -After $explorerAfterFailure
                }
            }
            catch {
                $cleanupIssues.Add($_.Exception.Message)
            }
        }

        if ($null -ne $libraryBefore -and
            !(Test-SnapshotEqual -Before $libraryBefore -After (Get-DirectorySnapshot -Path $libraryPath))) {
            $cleanupIssues.Add("The Icon Library changed during a failed lifecycle run.")
        }

        if ($InteractiveProof -and $null -ne $restoreStateBefore) {
            try {
                Restore-FileContent `
                    -Path $restoreStatePath `
                    -Existed $restoreStateBefore.exists `
                    -Content $restoreStateContentBefore
                if ($null -ne $interactiveProofSession) {
                    $interactiveProofSession.restoreStateRestored = $true
                }
            }
            catch {
                $cleanupIssues.Add("Restore-state rollback failed: $($_.Exception.Message)")
            }
        }

        if ($null -ne $restoreStateBefore -and
            !(Test-SnapshotEqual -Before $restoreStateBefore -After (Get-FileSnapshot -Path $restoreStatePath))) {
            $cleanupIssues.Add("Restore state changed during a failed lifecycle run.")
        }

        if ($null -ne $interactiveProofSession -and $null -ne $interactiveProofPath) {
            if ($cleanupIssues.Count -eq 0 -and
                $restoreStateRecoveryPath -and
                (Test-Path -LiteralPath $restoreStateRecoveryPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $restoreStateRecoveryPath -Force
                    $interactiveProofSession.restoreStateRecoveryRemoved = $true
                }
                catch {
                    $cleanupIssues.Add("Recovery-data removal failed: $($_.Exception.Message)")
                }
            }
            $interactiveProofSession.status = if ($cleanupIssues.Count -eq 0) {
                "proof-failed-cleanup-verified"
            }
            else {
                "proof-failed-cleanup-incomplete"
            }
            $interactiveProofSession.failure = $failure.Exception.Message
            $interactiveProofSession.cleanupIssues = @($cleanupIssues)
            $interactiveProofSession.cleanupCompletedAt = [DateTimeOffset]::UtcNow.ToString("O")
            $interactiveProofSession |
                ConvertTo-Json -Depth 10 |
                Set-Content -LiteralPath $interactiveProofPath -Encoding utf8
        }

        if ($cleanupIssues.Count -gt 0) {
            $detail = $cleanupIssues -join " "
            throw "Lifecycle failed: $($failure.Exception.Message) Cleanup verification also failed: $detail"
        }
    }

    throw $failure
}
