#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter()] [switch] $RequireReservedIdentity,
    [Parameter()] [string] $EvidencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifestPath = Join-Path $repoRoot 'src\IconReplacer.App\Package.appxmanifest'
$appProjectPath = Join-Path $repoRoot 'src\IconReplacer.App\IconReplacer.App.csproj'
$directoryPropsPath = Join-Path $repoRoot 'Directory.Build.props'
$identityPath = Join-Path $repoRoot 'docs\store\store-identity.json'
$privacyPath = Join-Path $repoRoot 'PRIVACY.md'
$runbookPath = Join-Path $repoRoot 'docs\store\README.md'
$listingPath = Join-Path $repoRoot 'docs\store\LISTING.md'
$certificationPath = Join-Path $repoRoot 'docs\store\CERTIFICATION-NOTES.md'
$evidenceTemplatePath = Join-Path $repoRoot 'docs\store\RELEASE-EVIDENCE-TEMPLATE.md'
$licenseDecisionPath = Join-Path $repoRoot 'docs\store\LICENSE-DECISION.md'
$distributionPath = Join-Path $repoRoot 'src\IconReplacer.AppModel\DistributionChannelPolicy.cs'
$updateServicePath = Join-Path $repoRoot 'src\IconReplacer.AppModel\GitHubReleaseUpdateService.cs'
$distributionTestsPath = Join-Path $repoRoot 'tests\IconReplacer.Core.Tests\DistributionChannelPolicyTests.cs'
$setIdentityPath = Join-Path $repoRoot 'scripts\Set-StoreIdentity.ps1'
$buildStorePath = Join-Path $repoRoot 'scripts\Build-StoreMsix.ps1'
$buildMsixPath = Join-Path $repoRoot 'scripts\Build-MsixPackage.ps1'
$assetsRoot = Join-Path $repoRoot 'src\IconReplacer.App\Assets'

$checks = [System.Collections.Generic.List[object]]::new()
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

$changeClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D'
$collectionsClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E'
$classicClsid = 'B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F'

function Get-RepoRelativePath {
    param([Parameter(Mandatory)] [string] $Path)

    $root = $repoRoot.TrimEnd('\') + '\'
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length)
    }
    return $fullPath
}

function Add-Check {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [bool] $Passed,
        [Parameter(Mandatory)] [string] $Details
    )

    $checks.Add([ordered]@{ name = $Name; passed = $Passed; details = $Details })
    if (-not $Passed) {
        $errors.Add("$Name`: $Details")
    }
}

function Add-Warning {
    param([Parameter(Mandatory)] [string] $Message)
    $warnings.Add($Message)
}

function Test-ExactText {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [AllowNull()] [string] $Actual,
        [AllowNull()] [string] $Expected
    )

    Add-Check `
        -Name $Name `
        -Passed ([string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) `
        -Details "expected '$Expected', actual '$Actual'"

    if ($null -ne $Actual) {
        Add-Check `
            -Name "$Name has no surrounding whitespace" `
            -Passed ([string]::Equals($Actual, $Actual.Trim(), [StringComparison]::Ordinal) -and $Actual.IndexOf([char] 0x00A0) -lt 0) `
            -Details 'value must not contain leading, trailing, or non-breaking whitespace'
    }
}

function Test-MsixVersion {
    param([AllowNull()] [string] $Value)

    if ($Value -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        return $false
    }

    foreach ($part in $Value.Split('.')) {
        $number = 0
        if (-not [int]::TryParse($part, [ref] $number) -or $number -lt 0 -or $number -gt 65535) {
            return $false
        }
    }
    return $true
}

function Find-ElementByAttribute {
    param(
        [Parameter(Mandatory)] [System.Xml.XmlNodeList] $Nodes,
        [Parameter(Mandatory)] [string] $Attribute,
        [Parameter(Mandatory)] [string] $Value
    )

    foreach ($node in $Nodes) {
        if ([string]::Equals($node.GetAttribute($Attribute), $Value, [StringComparison]::Ordinal)) {
            return $node
        }
    }
    return $null
}

$requiredFiles = @(
    $manifestPath,
    $appProjectPath,
    $directoryPropsPath,
    $identityPath,
    $privacyPath,
    $runbookPath,
    $listingPath,
    $certificationPath,
    $evidenceTemplatePath,
    $licenseDecisionPath,
    $distributionPath,
    $updateServicePath,
    $distributionTestsPath,
    $setIdentityPath,
    $buildMsixPath
)

foreach ($requiredFile in $requiredFiles) {
    Add-Check `
        -Name "Required file: $(Get-RepoRelativePath -Path $requiredFile)" `
        -Passed (Test-Path -LiteralPath $requiredFile -PathType Leaf) `
        -Details 'file must exist'
}

Add-Check `
    -Name 'Required file: scripts\Build-StoreMsix.ps1' `
    -Passed (Test-Path -LiteralPath $buildStorePath -PathType Leaf) `
    -Details 'Store package builder must exist'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $identityPath -PathType Leaf)) {
    throw "Core Store files are missing:`n - $($errors -join "`n - ")"
}

$storeIdentity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
[xml] $manifest = Get-Content -LiteralPath $manifestPath -Raw
$manifestText = $manifest.OuterXml
$appProjectText = Get-Content -LiteralPath $appProjectPath -Raw
$propsText = Get-Content -LiteralPath $directoryPropsPath -Raw
$distributionText = Get-Content -LiteralPath $distributionPath -Raw
$updateServiceText = Get-Content -LiteralPath $updateServicePath -Raw
$buildMsixText = Get-Content -LiteralPath $buildMsixPath -Raw

$ns = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
$ns.AddNamespace('f', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$ns.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')
$ns.AddNamespace('com', 'http://schemas.microsoft.com/appx/manifest/com/windows10')
$ns.AddNamespace('desktop4', 'http://schemas.microsoft.com/appx/manifest/desktop/windows10/4')
$ns.AddNamespace('desktop5', 'http://schemas.microsoft.com/appx/manifest/desktop/windows10/5')
$ns.AddNamespace('desktop9', 'http://schemas.microsoft.com/appx/manifest/desktop/windows10/9')
$ns.AddNamespace('rescap', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')

$identityNode = $manifest.SelectSingleNode('/f:Package/f:Identity', $ns)
$publisherDisplayNameNode = $manifest.SelectSingleNode('/f:Package/f:Properties/f:PublisherDisplayName', $ns)
$applicationNode = $manifest.SelectSingleNode('/f:Package/f:Applications/f:Application[@Id="IconReplacer.App"]', $ns)
$targetFamilies = @($manifest.SelectNodes('/f:Package/f:Dependencies/f:TargetDeviceFamily', $ns))
$fullTrustNode = $manifest.SelectSingleNode('/f:Package/f:Capabilities/rescap:Capability[@Name="runFullTrust"]', $ns)

Add-Check -Name 'Manifest Identity node' -Passed ($null -ne $identityNode) -Details 'Package/Identity must exist'
Add-Check -Name 'Stable application ID' -Passed ($null -ne $applicationNode) -Details 'Application Id must remain IconReplacer.App'
Add-Check -Name 'runFullTrust declaration' -Passed ($null -ne $fullTrustNode) -Details 'desktop file and Explorer operations require the declared restricted capability'

Add-Check -Name 'One target device family' -Passed ($targetFamilies.Count -eq 1) -Details "expected Windows.Desktop only; found $($targetFamilies.Count)"
if ($targetFamilies.Count -gt 0) {
    Test-ExactText -Name 'Desktop-only target' -Actual $targetFamilies[0].GetAttribute('Name') -Expected 'Windows.Desktop'
    Test-ExactText -Name 'Minimum Windows version' -Actual $targetFamilies[0].GetAttribute('MinVersion') -Expected '10.0.17763.0'
}
Add-Check -Name 'No Windows.Universal target' -Passed ($manifestText.IndexOf('Name="Windows.Universal"', [StringComparison]::Ordinal) -lt 0) -Details 'Icon Replacer must not advertise non-desktop support'

if ($identityNode) {
    $version = $identityNode.GetAttribute('Version')
    Add-Check -Name 'Four-part MSIX version' -Passed (Test-MsixVersion -Value $version) -Details "'$version' must contain four numeric components from 0 through 65535"
}

# Packaged COM contract.
$surrogate = $manifest.SelectSingleNode('//com:SurrogateServer', $ns)
Add-Check -Name 'Packaged COM surrogate server' -Passed ($null -ne $surrogate) -Details 'windows.comServer must define a surrogate server'
if ($surrogate) {
    Test-ExactText -Name 'COM surrogate AppId' -Actual $surrogate.GetAttribute('AppId') -Expected $changeClsid
}

$classNodes = $manifest.SelectNodes('//com:SurrogateServer/com:Class', $ns)
Add-Check -Name 'Three packaged COM classes' -Passed ($classNodes.Count -eq 3) -Details "expected 3 classes; found $($classNodes.Count)"
foreach ($expectedClass in @(
    @{ Id = $changeClsid; Purpose = 'change icon command' },
    @{ Id = $collectionsClsid; Purpose = 'collections command' },
    @{ Id = $classicClsid; Purpose = 'classic context menu' }
)) {
    $classNode = Find-ElementByAttribute -Nodes $classNodes -Attribute 'Id' -Value $expectedClass.Id
    Add-Check -Name "COM class $($expectedClass.Purpose)" -Passed ($null -ne $classNode) -Details "expected CLSID $($expectedClass.Id)"
    if ($classNode) {
        Test-ExactText -Name "COM path $($expectedClass.Purpose)" -Actual $classNode.GetAttribute('Path') -Expected 'IconReplacer.ShellExtension.dll'
        Test-ExactText -Name "COM threading $($expectedClass.Purpose)" -Actual $classNode.GetAttribute('ThreadingModel') -Expected 'STA'
    }
}

# Modern Explorer verbs.
$modernItemTypes = $manifest.SelectNodes('//desktop4:FileExplorerContextMenus/desktop5:ItemType', $ns)
foreach ($target in @(
    @{ Type = 'Directory'; ChangeVerb = 'IconReplacerChangeIconDirectory'; CollectionVerb = 'IconReplacerCollectionsDirectory' },
    @{ Type = '.lnk'; ChangeVerb = 'IconReplacerChangeIconShortcut'; CollectionVerb = 'IconReplacerCollectionsShortcut' }
)) {
    $itemNode = Find-ElementByAttribute -Nodes $modernItemTypes -Attribute 'Type' -Value $target.Type
    Add-Check -Name "Modern Explorer target $($target.Type)" -Passed ($null -ne $itemNode) -Details 'target must be registered'
    if ($itemNode) {
        $verbNodes = $itemNode.SelectNodes('desktop5:Verb', $ns)
        $changeVerb = Find-ElementByAttribute -Nodes $verbNodes -Attribute 'Id' -Value $target.ChangeVerb
        $collectionVerb = Find-ElementByAttribute -Nodes $verbNodes -Attribute 'Id' -Value $target.CollectionVerb
        Add-Check -Name "Modern change verb $($target.Type)" -Passed ($null -ne $changeVerb) -Details "expected verb $($target.ChangeVerb)"
        Add-Check -Name "Modern collections verb $($target.Type)" -Passed ($null -ne $collectionVerb) -Details "expected verb $($target.CollectionVerb)"
        if ($changeVerb) {
            Test-ExactText -Name "Modern change CLSID $($target.Type)" -Actual $changeVerb.GetAttribute('Clsid') -Expected $changeClsid
        }
        if ($collectionVerb) {
            Test-ExactText -Name "Modern collections CLSID $($target.Type)" -Actual $collectionVerb.GetAttribute('Clsid') -Expected $collectionsClsid
        }
    }
}

# Classic Explorer handlers.
$classicHandlers = $manifest.SelectNodes('//desktop9:FileExplorerClassicContextMenuHandler/desktop9:ExtensionHandler', $ns)
foreach ($targetType in @('Directory', '.lnk')) {
    $handler = Find-ElementByAttribute -Nodes $classicHandlers -Attribute 'Type' -Value $targetType
    Add-Check -Name "Classic Explorer target $targetType" -Passed ($null -ne $handler) -Details 'classic handler target must be registered'
    if ($handler) {
        Test-ExactText -Name "Classic handler CLSID $targetType" -Actual $handler.GetAttribute('Clsid') -Expected $classicClsid
    }
}

# Build layout contract.
foreach ($requiredProjectText in @(
    'IconReplacer.CommandHost.exe',
    'IconReplacer.CommandHost.dll',
    'IconReplacer.CommandHost.deps.json',
    'IconReplacer.CommandHost.runtimeconfig.json',
    'IconReplacer.ShellExtension.dll',
    'BuildNativeShellExtension',
    "'$(NativeShellExtensionPlatform)' == 'x64'"
)) {
    Add-Check -Name "App project contract: $requiredProjectText" -Passed ($appProjectText.IndexOf($requiredProjectText, [StringComparison]::Ordinal) -ge 0) -Details 'x64 package must include command host and native shell extension'
}

Test-ExactText -Name 'First Store architecture' -Actual ([string] $storeIdentity.firstStoreArchitecture) -Expected 'x64'

# Distribution/update contract.
Add-Check -Name 'Store compile symbol' -Passed ($propsText.IndexOf('ICON_REPLACER_DISTRIBUTION_CHANNEL', [StringComparison]::Ordinal) -ge 0 -and $propsText.IndexOf('ICON_REPLACER_STORE', [StringComparison]::Ordinal) -ge 0) -Details 'Directory.Build.props must map the Store channel to a compile symbol'
Add-Check -Name 'Distribution channel policy' -Passed ($distributionText.IndexOf('UpdatesManagedByStore', [StringComparison]::Ordinal) -ge 0 -and $distributionText.IndexOf('microsoft-store', [StringComparison]::OrdinalIgnoreCase) -ge 0) -Details 'compiled channel policy must recognize Microsoft Store aliases'
Add-Check -Name 'Update service Store short-circuit' -Passed ($updateServiceText.IndexOf('DistributionChannelPolicy.UpdatesManagedByStore', [StringComparison]::Ordinal) -ge 0 -and $updateServiceText.IndexOf('Updates are managed by Microsoft Store', [StringComparison]::Ordinal) -ge 0) -Details 'Store build must return before constructing/sending the GitHub request'

# Certificate handling policy.
Add-Check -Name 'No default certificate password' -Passed ($buildMsixText.IndexOf('else {', [StringComparison]::Ordinal) -lt 0 -or $buildMsixText.IndexOf('"password"', [StringComparison]::Ordinal) -lt 0) -Details 'MSIX builder must not fall back to a literal password'
Add-Check -Name 'Explicit certificate password gate' -Passed ($buildMsixText.IndexOf('A non-empty certificate password is required', [StringComparison]::Ordinal) -ge 0) -Details 'MSIX builder must reject missing passwords'

# Package assets.
foreach ($asset in @(
    'AppIcon.ico',
    'AppIcon.png',
    'StoreLogo.png',
    'Square44x44Logo.scale-200.png',
    'Square150x150Logo.scale-200.png',
    'Wide310x150Logo.scale-200.png',
    'SplashScreen.scale-200.png'
)) {
    Add-Check -Name "Package asset $asset" -Passed (Test-Path -LiteralPath (Join-Path $assetsRoot $asset) -PathType Leaf) -Details 'required Store/package asset must exist'
}

# Identity mode.
$status = [string] $storeIdentity.reservationStatus
Add-Check -Name 'Known reservation status' -Passed ($status -in @('pending', 'reserved')) -Details "expected pending or reserved; actual '$status'"
if ($status -eq 'reserved') {
    foreach ($field in @(
        @{ Name = 'Reserved identity name'; Value = [string] $storeIdentity.packageIdentity.name },
        @{ Name = 'Reserved publisher'; Value = [string] $storeIdentity.packageIdentity.publisher },
        @{ Name = 'Reserved publisher display name'; Value = [string] $storeIdentity.packageIdentity.publisherDisplayName },
        @{ Name = 'Reserved PFN'; Value = [string] $storeIdentity.packageIdentity.packageFamilyName },
        @{ Name = 'Reserved Package SID'; Value = [string] $storeIdentity.packageIdentity.packageSid },
        @{ Name = 'Reserved Store ID'; Value = [string] $storeIdentity.store.productId }
    )) {
        Add-Check -Name $field.Name -Passed (-not [string]::IsNullOrWhiteSpace($field.Value)) -Details 'reserved identity values must be populated'
        if (-not [string]::IsNullOrWhiteSpace($field.Value)) {
            Add-Check -Name "$($field.Name) whitespace" -Passed ([string]::Equals($field.Value, $field.Value.Trim(), [StringComparison]::Ordinal) -and $field.Value.IndexOf([char] 0x00A0) -lt 0) -Details 'value must be copied exactly without hidden whitespace'
        }
    }

    if ($identityNode) {
        Test-ExactText -Name 'Manifest Store name' -Actual $identityNode.GetAttribute('Name') -Expected $storeIdentity.packageIdentity.name
        Test-ExactText -Name 'Manifest Store publisher' -Actual $identityNode.GetAttribute('Publisher') -Expected $storeIdentity.packageIdentity.publisher
    }
    Test-ExactText -Name 'Manifest Store publisher display name' -Actual $(if ($publisherDisplayNameNode) { $publisherDisplayNameNode.InnerText } else { $null }) -Expected $storeIdentity.packageIdentity.publisherDisplayName

    Add-Check -Name 'PFN remains metadata-only' -Passed ($manifestText.IndexOf([string] $storeIdentity.packageIdentity.packageFamilyName, [StringComparison]::Ordinal) -lt 0) -Details 'PFN must not be written into Package.appxmanifest'
    Add-Check -Name 'Package SID remains metadata-only' -Passed ($manifestText.IndexOf([string] $storeIdentity.packageIdentity.packageSid, [StringComparison]::Ordinal) -lt 0) -Details 'Package SID must not be written into Package.appxmanifest'

    $privacyText = Get-Content -LiteralPath $privacyPath -Raw
    Add-Check -Name 'Privacy publisher finalized' -Passed ($privacyText.IndexOf('To be completed with the verified Microsoft Store publisher name', [StringComparison]::OrdinalIgnoreCase) -lt 0 -and $privacyText.IndexOf('debe completarse con el nombre verificado', [StringComparison]::OrdinalIgnoreCase) -lt 0) -Details 'run Set-StoreIdentity.ps1 to finalize the privacy publisher'
}
else {
    if ($identityNode) {
        Test-ExactText -Name 'Development identity name' -Actual $identityNode.GetAttribute('Name') -Expected $storeIdentity.developmentIdentity.name
        Test-ExactText -Name 'Development publisher' -Actual $identityNode.GetAttribute('Publisher') -Expected $storeIdentity.developmentIdentity.publisher
    }
    Test-ExactText -Name 'Development publisher display name' -Actual $(if ($publisherDisplayNameNode) { $publisherDisplayNameNode.InnerText } else { $null }) -Expected $storeIdentity.developmentIdentity.publisherDisplayName
    Add-Warning 'Partner Center identity is pending. Structural checks can pass, but the package is not a final Store submission.'
    if ($RequireReservedIdentity) {
        Add-Check -Name 'Reserved Partner Center identity required' -Passed $false -Details 'reserve Icon Replacer and run Set-StoreIdentity.ps1 before packaging'
    }
}

Add-Warning 'The source-code license decision is intentionally unresolved. Complete docs/store/LICENSE-DECISION.md before stable public publication.'

$result = [ordered]@{
    schema = 'icon-replacer.store-readiness.v1'
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
    repository = 'gvastethecreator/icon-replacer'
    reservationStatus = $status
    architecture = 'x64'
    packageVersion = $(if ($identityNode) { $identityNode.GetAttribute('Version') } else { $null })
    passed = $errors.Count -eq 0
    warnings = $warnings
    checks = $checks
}

if ($EvidencePath) {
    $resolvedPath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $repoRoot $EvidencePath }
    $directory = Split-Path -Parent $resolvedPath
    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
    $result | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $resolvedPath -Encoding utf8
    Write-Host "Evidence: $resolvedPath" -ForegroundColor DarkGray
}

foreach ($warning in $warnings) {
    Write-Host "WARNING: $warning" -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    Write-Host ''
    Write-Host 'STORE READINESS FAILED' -ForegroundColor Red
    foreach ($message in $errors) {
        Write-Host " - $message" -ForegroundColor Red
    }
    exit 1
}

Write-Host ''
Write-Host "STORE READINESS PASSED ($($checks.Count) checks)" -ForegroundColor Green
Write-Host "Reservation status: $status"
Write-Host 'First Store architecture: x64'
