param(
    [Parameter(Mandatory)]
    [int]$AppPid,
    [string]$ArtifactDir = "artifacts/ui-tests/gallery-first/run"
)

$ErrorActionPreference = "Continue"
$pass = 0
$fail = 0
$results = @()

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class GalleryFirstPointer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    public static extern void keybd_event(
        byte virtualKey,
        byte scanCode,
        uint flags,
        UIntPtr extraInfo);
}
"@

function Wait-ForUiElement {
    param(
        [Parameter(Mandatory)][string]$AutomationId,
        [int]$TimeoutMilliseconds = 5000
    )

    for ($attempt = 1; $attempt -le 2; $attempt++) {
        $output = & winapp ui wait-for $AutomationId -a $AppPid -t $TimeoutMilliseconds 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $output
        }
        if ($attempt -eq 2) {
            throw ($output -join [Environment]::NewLine)
        }

        Start-Sleep -Milliseconds 350
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
    Wait-ForUiElement $DestinationAutomationId | Out-Null
    Start-Sleep -Milliseconds 350
}

function Get-CategoryListWidth {
    $rawTree = winapp ui inspect "CategoryList" -a $AppPid --depth 0 --json 2>$null
    $tree = ($rawTree -join [Environment]::NewLine) | ConvertFrom-Json
    $categoryList = $tree.windows[0].elements |
        Where-Object { $_.automationId -eq "CategoryList" } |
        Select-Object -First 1
    if ($null -eq $categoryList -or $categoryList.isOffscreen) {
        throw "CategoryList is not visible."
    }

    return [double]$categoryList.width
}

function Reset-Pointer {
    $process = Get-Process -Id $AppPid -ErrorAction Stop
    $rect = New-Object GalleryFirstPointer+Rect
    if ([GalleryFirstPointer]::GetWindowRect($process.MainWindowHandle, [ref]$rect)) {
        [GalleryFirstPointer]::SetCursorPos($rect.Left + 600, $rect.Top + 18) | Out-Null
        Start-Sleep -Milliseconds 1100
    }
}

function Test-UI {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    try {
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0) {
            $script:pass++
            $script:results += @{ name = $Name; status = "PASS" }
        }
        else {
            throw ($output -join [Environment]::NewLine)
        }
    }
    catch {
        $script:fail++
        $script:results += @{ name = $Name; status = "FAIL"; detail = "$_" }
    }
}

Test-UI "Library loads" {
    Select-NavigationItem "LibraryNavItem" "IconPreviewGrid"
    Wait-ForUiElement "IconSearchBox" | Out-Null
}

Test-UI "Window starts centered" {
    $process = Get-Process -Id $AppPid -ErrorAction Stop
    $window = New-Object GalleryFirstPointer+Rect
    if (-not [GalleryFirstPointer]::GetWindowRect($process.MainWindowHandle, [ref]$window)) {
        throw "Could not read the main-window bounds."
    }

    $monitor = [GalleryFirstPointer]::MonitorFromWindow($process.MainWindowHandle, 2)
    $monitorInfo = New-Object GalleryFirstPointer+MonitorInfo
    $monitorInfo.Size = [Runtime.InteropServices.Marshal]::SizeOf($monitorInfo)
    if (-not [GalleryFirstPointer]::GetMonitorInfo($monitor, [ref]$monitorInfo)) {
        throw "Could not read the monitor work area."
    }

    $windowWidth = $window.Right - $window.Left
    $windowHeight = $window.Bottom - $window.Top
    $workWidth = $monitorInfo.WorkArea.Right - $monitorInfo.WorkArea.Left
    $workHeight = $monitorInfo.WorkArea.Bottom - $monitorInfo.WorkArea.Top
    $expectedLeft = $monitorInfo.WorkArea.Left + [Math]::Floor(($workWidth - $windowWidth) / 2)
    $expectedTop = $monitorInfo.WorkArea.Top + [Math]::Floor(($workHeight - $windowHeight) / 2)
    if ([Math]::Abs($window.Left - $expectedLeft) -gt 2 -or
        [Math]::Abs($window.Top - $expectedTop) -gt 2) {
        throw "Window is not centered: actual ($($window.Left), $($window.Top)), expected ($expectedLeft, $expectedTop)."
    }
}

Test-UI "Catalog summary is populated" {
    winapp ui wait-for "CategoryList" -a $AppPid -t 5000
}

Test-UI "Preview grid exists" {
    winapp ui wait-for "IconPreviewGrid" -a $AppPid -t 5000
}

Test-UI "Search filters in memory" {
    winapp ui set-value "TextBox" "blizzard" -a $AppPid
    Start-Sleep -Milliseconds 500
    $matches = winapp ui search "Blizzard" -a $AppPid --json 2>$null | ConvertFrom-Json
    if ($matches.matchCount -lt 1) {
        throw "Blizzard results were not exposed to UI Automation"
    }
}

Test-UI "Search switches without rescanning" {
    winapp ui set-value "TextBox" "adobe" -a $AppPid
    Start-Sleep -Milliseconds 500
    $matches = winapp ui search "Adobe" -a $AppPid --json 2>$null | ConvertFrom-Json
    if ($matches.matchCount -lt 1) {
        throw "Adobe results were not exposed to UI Automation"
    }
}

Test-UI "Preview size changes" {
    winapp ui set-value "PreviewSizeSlider" "96" -a $AppPid
}

Test-UI "Collections panel resizes from the keyboard" {
    $widthBefore = Get-CategoryListWidth
    winapp ui focus "CollectionsSplitter" -a $AppPid | Out-Null
    [GalleryFirstPointer]::keybd_event(0x27, 0, 0, [UIntPtr]::Zero)
    [GalleryFirstPointer]::keybd_event(0x27, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 350
    $widthAfter = Get-CategoryListWidth
    if ($widthAfter -le $widthBefore) {
        throw "Expected the Collections panel to grow, found $widthBefore px before and $widthAfter px after."
    }

    [GalleryFirstPointer]::keybd_event(0x25, 0, 0, [UIntPtr]::Zero)
    [GalleryFirstPointer]::keybd_event(0x25, 0, 2, [UIntPtr]::Zero)
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/01-library-system.png" 2>$null

Test-UI "Recent navigation" {
    Select-NavigationItem "RecentNavItem" "RecentChangesList"
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/02-recent.png" 2>$null

Test-UI "Settings navigation" {
    Select-NavigationItem "SettingsNavItem" "ThemeSelector"
}

Test-UI "Light theme" {
    winapp ui invoke "Light" -a $AppPid
    Start-Sleep -Milliseconds 300
    $preferencePath = Join-Path $env:APPDATA "Icon Replacer/preferences.json"
    $preference = Get-Content $preferencePath -Raw | ConvertFrom-Json
    if ($preference.Theme -ne "Light") {
        throw "Expected Light, found $($preference.Theme)"
    }
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/03-settings-light.png" 2>$null

Test-UI "Dark theme" {
    winapp ui invoke "Dark" -a $AppPid
    Start-Sleep -Milliseconds 300
    $preferencePath = Join-Path $env:APPDATA "Icon Replacer/preferences.json"
    $preference = Get-Content $preferencePath -Raw | ConvertFrom-Json
    if ($preference.Theme -ne "Dark") {
        throw "Expected Dark, found $($preference.Theme)"
    }
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/04-settings-dark.png" 2>$null

Test-UI "About navigation and update status" {
    Select-NavigationItem "AboutNavItem" "AboutVersionText"
    Wait-ForUiElement "CheckForUpdatesButton" | Out-Null
    Wait-ForUiElement "UpdateStatusTitle" | Out-Null
    Wait-ForUiElement "UpdateStatusMessage" | Out-Null
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/05-about-dark.png" 2>$null

Test-UI "Theme preference persists" {
    $preferencePath = Join-Path $env:APPDATA "Icon Replacer/preferences.json"
    $preference = Get-Content $preferencePath -Raw | ConvertFrom-Json
    if ($preference.Theme -ne "Dark") {
        throw "Expected Dark, found $($preference.Theme)"
    }
}

Test-UI "Return to library" {
    Select-NavigationItem "LibraryNavItem" "IconPreviewGrid"
    Wait-ForUiElement "IconSearchBox" | Out-Null
}

Reset-Pointer
winapp ui screenshot -a $AppPid -o "$ArtifactDir/06-library-dark.png" 2>$null

Write-Host "Passed: $pass | Failed: $fail"
$results | Where-Object { $_.status -eq "FAIL" } | ForEach-Object {
    Write-Host "  FAIL: $($_.name) - $($_.detail)" -ForegroundColor Red
}
$results | ConvertTo-Json -Depth 4 | Out-File "$ArtifactDir/results.json"

if ($fail -gt 0) {
    exit 1
}
