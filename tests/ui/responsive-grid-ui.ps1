param(
    [Parameter(Mandatory)]
    [int]$AppPid,
    [string]$ArtifactDir = "artifacts/ui-tests/gallery-first/responsive-grid"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class ResponsiveGridWindow
{
    [DllImport("user32.dll")]
    public static extern bool MoveWindow(
        IntPtr window,
        int x,
        int y,
        int width,
        int height,
        bool repaint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
}
"@

$process = Get-Process -Id $AppPid -ErrorAction Stop
winapp ui wait-for "IconPreviewGrid" -a $AppPid -t 5000 | Out-Null
winapp ui set-value "TextBox" " " -a $AppPid | Out-Null
winapp ui set-value "PreviewSizeSlider" "64" -a $AppPid | Out-Null
Start-Sleep -Milliseconds 500

$cases = @(
    @{ name = "compact"; width = 1000; height = 780 },
    @{ name = "reference"; width = 1200; height = 851 },
    @{ name = "wide"; width = 1450; height = 900 }
)
$results = @()

foreach ($case in $cases) {
    if (-not [ResponsiveGridWindow]::MoveWindow(
        $process.MainWindowHandle,
        30,
        25,
        $case.width,
        $case.height,
        $true)) {
        throw "Could not resize the app for $($case.name)."
    }

    [ResponsiveGridWindow]::SetCursorPos(700, 45) | Out-Null
    Start-Sleep -Milliseconds 900

    $rawTree = winapp ui inspect "IconPreviewGrid" -a $AppPid --depth 1 --json 2>$null
    $tree = ($rawTree -join [Environment]::NewLine) | ConvertFrom-Json
    $grid = $tree.windows[0].elements |
        Where-Object { $_.automationId -eq "IconPreviewGrid" } |
        Select-Object -First 1
    if ($null -eq $grid) {
        throw "IconPreviewGrid was not returned for $($case.name)."
    }

    $visible = @($grid.children | Where-Object {
        $_.type -eq "ListItem" -and -not $_.isOffscreen -and $_.width -gt 0
    })
    $firstRowY = ($visible | Measure-Object -Property y -Minimum).Minimum
    $firstRow = @($visible | Where-Object { [Math]::Abs($_.y - $firstRowY) -le 2 } | Sort-Object x)
    if ($firstRow.Count -lt 2) {
        throw "The first row did not expose enough cells for $($case.name)."
    }

    $widths = @($firstRow | ForEach-Object { [double]$_.width })
    $gaps = for ($index = 1; $index -lt $firstRow.Count; $index++) {
        [double]$firstRow[$index].x -
            ([double]$firstRow[$index - 1].x + [double]$firstRow[$index - 1].width)
    }
    $widthSpread = ($widths | Measure-Object -Maximum).Maximum -
        ($widths | Measure-Object -Minimum).Minimum
    $gapSpread = ($gaps | Measure-Object -Maximum).Maximum -
        ($gaps | Measure-Object -Minimum).Minimum
    if ($widthSpread -gt 2 -or $gapSpread -gt 2) {
        throw "Grid spacing drifted for $($case.name): width spread $widthSpread, gap spread $gapSpread."
    }

    winapp ui screenshot -a $AppPid -o "$ArtifactDir/$($case.name).png" 2>$null | Out-Null
    $results += [pscustomobject]@{
        name = $case.name
        columns = $firstRow.Count
        cellWidthMin = ($widths | Measure-Object -Minimum).Minimum
        cellWidthMax = ($widths | Measure-Object -Maximum).Maximum
        gapMin = ($gaps | Measure-Object -Minimum).Minimum
        gapMax = ($gaps | Measure-Object -Maximum).Maximum
    }
}

for ($index = 1; $index -lt $results.Count; $index++) {
    if ($results[$index].columns -le $results[$index - 1].columns) {
        throw "Column count did not increase from $($results[$index - 1].name) to $($results[$index].name)."
    }
}

$results | ConvertTo-Json -Depth 4 | Out-File "$ArtifactDir/results.json"
$results | Format-Table -AutoSize
Write-Host "Passed: responsive columns and spacing are stable across $($results.Count) widths."
