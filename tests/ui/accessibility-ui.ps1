param(
    [Parameter(Mandatory)]
    [int]$AppPid,
    [string]$ArtifactDir = "artifacts/ui-tests/gallery-first/accessibility",
    [ValidateRange(100, 400)]
    [int]$MinimumScalePercent = 100
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$pass = 0
$fail = 0
$results = @()

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class AccessibilityWindow
{
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr window);
}
"@

function Invoke-WinAppChecked {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = & winapp @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return $output
}

function Read-UiTree {
    param(
        [Parameter(Mandatory)][string]$Selector,
        [int]$Depth = 0
    )

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $output = Invoke-WinAppChecked @(
                "ui", "inspect", $Selector,
                "-a", "$AppPid",
                "--depth", "$Depth",
                "--json")
            return (($output -join [Environment]::NewLine) | ConvertFrom-Json)
        }
        catch {
            if ($attempt -eq 3) {
                throw
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

function Read-UiSearch {
    param([Parameter(Mandatory)][string]$Selector)

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $output = Invoke-WinAppChecked @(
                "ui", "search", $Selector,
                "-a", "$AppPid",
                "--json")
            return (($output -join [Environment]::NewLine) | ConvertFrom-Json)
        }
        catch {
            if ($attempt -eq 3) {
                throw
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

function Wait-ForUiElement {
    param(
        [Parameter(Mandatory)][string]$AutomationId,
        [int]$TimeoutMilliseconds = 5000
    )

    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            Invoke-WinAppChecked @(
                "ui", "wait-for", $AutomationId,
                "-a", "$AppPid",
                "-t", "$TimeoutMilliseconds") | Out-Null
            return
        }
        catch {
            if ($attempt -eq 2) {
                throw
            }

            Start-Sleep -Milliseconds 350
        }
    }
}

function Select-NavigationItem {
    param(
        [Parameter(Mandatory)][string]$AutomationId,
        [Parameter(Mandatory)][string]$DestinationAutomationId
    )

    $process = Get-Process -Id $AppPid -ErrorAction Stop
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    $item = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $item) {
        throw "$AutomationId was not exposed through UI Automation."
    }

    $pattern = $item.GetCurrentPattern(
        [System.Windows.Automation.SelectionItemPattern]::Pattern)
    $pattern.Select()
    Wait-ForUiElement $DestinationAutomationId
    Start-Sleep -Milliseconds 350
}

function Expand-UiElements {
    param([Parameter(Mandatory)]$Elements)

    $expanded = @()
    foreach ($element in @($Elements)) {
        $expanded += $element
        $children = $element.PSObject.Properties["children"]
        if ($null -ne $children -and $null -ne $children.Value) {
            $expanded += Expand-UiElements $children.Value
        }
    }

    return $expanded
}

function Get-UiElements {
    param([Parameter(Mandatory)]$Tree)

    $windows = $Tree.PSObject.Properties["windows"]
    if ($null -ne $windows -and @($windows.Value).Count -gt 0) {
        return Expand-UiElements $windows.Value[0].elements
    }

    $elements = $Tree.PSObject.Properties["elements"]
    if ($null -eq $elements) {
        throw "The UI Automation response did not contain elements."
    }

    return Expand-UiElements $elements.Value
}

function Assert-AccessibleElement {
    param(
        [Parameter(Mandatory)][string]$AutomationId,
        [Parameter(Mandatory)][string]$ExpectedName
    )

    $element = Get-UiElement $AutomationId
    $nameProperty = $element.PSObject.Properties["name"]
    $accessibleName = if ($null -eq $nameProperty) { "" } else { "$($nameProperty.Value)" }
    if ([string]::IsNullOrWhiteSpace($accessibleName) -or
        $accessibleName -notmatch $ExpectedName) {
        throw "$AutomationId has unexpected accessible name '$accessibleName'."
    }
}

function Get-UiElement {
    param([Parameter(Mandatory)][string]$AutomationId)

    $search = Read-UiSearch $AutomationId
    $element = @($search.matches) |
        Where-Object {
            $id = $_.PSObject.Properties["automationId"]
            $null -ne $id -and $id.Value -eq $AutomationId
        } |
        Select-Object -First 1
    if ($null -eq $element) {
        throw "$AutomationId was not exposed through UI Automation."
    }

    return $element
}

function Test-UI {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Script
    )

    try {
        & $Script
        $script:pass++
        $script:results += @{ name = $Name; status = "PASS" }
    }
    catch {
        $script:fail++
        $script:results += @{ name = $Name; status = "FAIL"; detail = "$_" }
    }
}

Test-UI "Library accessibility names" {
    Select-NavigationItem "LibraryNavItem" "IconPreviewGrid"
    Wait-ForUiElement "IconSearchBox"
    Wait-ForUiElement "CategoryList"
    Wait-ForUiElement "VisibleSummaryText"
    Start-Sleep -Milliseconds 750
    Assert-AccessibleElement "AppNavigation" "Primary navigation"
    Assert-AccessibleElement "LibraryNavItem" "Library"
    Assert-AccessibleElement "RecentNavItem" "Recent"
    Assert-AccessibleElement "AboutNavItem" "About"
    Assert-AccessibleElement "SettingsNavItem" "Settings"
    Assert-AccessibleElement "IconSearchBox" "Search icons"
    Assert-AccessibleElement "RefreshButton" "Refresh library"
    Assert-AccessibleElement "ImportIconsButton" "Import icons"
    Assert-AccessibleElement "OpenLibraryButton" "Open icon library"
    Assert-AccessibleElement "PreviewSizeSlider" "Icon preview size"
    Assert-AccessibleElement "CategoryList" "Icon collections"
    Assert-AccessibleElement "CollectionsSplitter" "Resize Collections panel"
    Assert-AccessibleElement "IconPreviewGrid" "Icon previews"
}

Test-UI "Window DPI meets the requested proof scale" {
    $process = Get-Process -Id $AppPid -ErrorAction Stop
    $dpi = [AccessibilityWindow]::GetDpiForWindow($process.MainWindowHandle)
    $scalePercent = [int][Math]::Round(($dpi / 96.0) * 100)
    if ($scalePercent -lt $MinimumScalePercent) {
        throw "Window scale is $scalePercent percent; expected at least $MinimumScalePercent percent."
    }

    Write-Host "Window scale: $scalePercent percent"
}

Test-UI "Toolbar controls are visible and do not overlap" {
    $toolbarElements = @(
        "IconSearchBox",
        "RefreshButton",
        "ImportIconsButton",
        "OpenLibraryButton",
        "PreviewSizeSlider") | ForEach-Object { Get-UiElement $_ }

    foreach ($element in $toolbarElements) {
        if ($element.isOffscreen -or $element.width -le 0 -or $element.height -le 0) {
            throw "$($element.automationId) is clipped or offscreen."
        }
    }

    for ($leftIndex = 0; $leftIndex -lt $toolbarElements.Count; $leftIndex++) {
        for ($rightIndex = $leftIndex + 1; $rightIndex -lt $toolbarElements.Count; $rightIndex++) {
            $left = $toolbarElements[$leftIndex]
            $right = $toolbarElements[$rightIndex]
            $overlaps =
                $left.x -lt ($right.x + $right.width) -and
                ($left.x + $left.width) -gt $right.x -and
                $left.y -lt ($right.y + $right.height) -and
                ($left.y + $left.height) -gt $right.y
            if ($overlaps) {
                throw "$($left.automationId) overlaps $($right.automationId)."
            }
        }
    }
}

Test-UI "Library commands accept keyboard focus" {
    foreach ($automationId in @(
        "IconSearchBox",
        "RefreshButton",
        "ImportIconsButton",
        "OpenLibraryButton",
        "PreviewSizeSlider",
        "CategoryList",
        "CollectionsSplitter")) {
        Invoke-WinAppChecked @("ui", "focus", $automationId, "-a", "$AppPid") | Out-Null
    }
}

Test-UI "Icon previews expose icon and collection names" {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    $namedIcons = @()
    do {
        $elements = Get-UiElements (Read-UiTree "IconPreviewGrid" 2)
        $namedIcons = @($elements | Where-Object {
            $_.type -eq "Image" -and
            -not $_.isOffscreen -and
            $_.name -match ", .+ collection$"
        })
        if ($namedIcons.Count -eq 0) {
            Start-Sleep -Milliseconds 500
        }
    } while ($namedIcons.Count -eq 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($namedIcons.Count -eq 0) {
        throw "No visible icon preview exposed both its icon and collection name."
    }
}

Test-UI "Recent restore actions identify targets" {
    Select-NavigationItem "RecentNavItem" "RecentChangesList"
    $elements = Get-UiElements (Read-UiTree "RecentChangesList" 3)
    $restoreActions = @($elements | Where-Object {
        $id = $_.PSObject.Properties["automationId"]
        $null -ne $id -and $id.Value -match "^Restore-"
    })
    if ($restoreActions.Count -eq 0) {
        throw "No Restore actions were exposed in Recent changes."
    }
    $genericNames = @($restoreActions | Where-Object {
        $name = $_.PSObject.Properties["name"]
        $null -eq $name -or $name.Value -notmatch "^Restore original icon for .+"
    })
    if ($genericNames.Count -gt 0) {
        throw "One or more Restore actions do not identify their target."
    }
}

Test-UI "Settings controls and full path are named" {
    Select-NavigationItem "SettingsNavItem" "ThemeSelector"
    Assert-AccessibleElement "ThemeSelector" "App theme"
    Assert-AccessibleElement "SettingsOpenLibraryButton" "Open Icon Library"
    Assert-AccessibleElement "SettingsLibraryPath" ".+"
}

Test-UI "About credits, links, version, and updates are named" {
    Select-NavigationItem "AboutNavItem" "AboutVersionText"
    Assert-AccessibleElement "AboutVersionText" "Version [0-9]+\.[0-9]+\.[0-9]+"
    Assert-AccessibleElement "ProjectGitHubLink" "GitHub"
    Assert-AccessibleElement "ProjectReleasesLink" "releases"
    Assert-AccessibleElement "CheckForUpdatesButton" "Check for updates"

    $deadline = [DateTime]::UtcNow.AddSeconds(12)
    do {
        $status = Get-UiElement "UpdateStatusTitle"
        $nameProperty = $status.PSObject.Properties["name"]
        $statusName = if ($null -eq $nameProperty) { "" } else { "$($nameProperty.Value)" }
        if ($statusName -notmatch "^Checking for updates$") {
            break
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    if ([string]::IsNullOrWhiteSpace($statusName) -or $statusName -eq "Checking for updates") {
        throw "The update check did not reach a readable final state."
    }
}

Test-UI "Return to Library" {
    Select-NavigationItem "LibraryNavItem" "IconPreviewGrid"
    Wait-ForUiElement "IconSearchBox"
}

Invoke-WinAppChecked @(
    "ui", "screenshot",
    "-a", "$AppPid",
    "-o", (Join-Path $ArtifactDir "library-accessibility.png")) | Out-Null

Write-Host "Passed: $pass | Failed: $fail"
$results | Where-Object { $_.status -eq "FAIL" } | ForEach-Object {
    Write-Host "  FAIL: $($_.name) - $($_.detail)" -ForegroundColor Red
}
$results | ConvertTo-Json -Depth 4 | Out-File (Join-Path $ArtifactDir "results.json")

if ($fail -gt 0) {
    exit 1
}
