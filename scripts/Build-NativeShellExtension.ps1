param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$sourceRoot = Join-Path $repoRoot "src\IconReplacer.ShellExtension"
$buildRoot = Join-Path $repoRoot "artifacts\native\build\$Platform\$Configuration"
$outputRoot = Join-Path $repoRoot "artifacts\native\$Platform\$Configuration"
$cmakeCache = Join-Path $buildRoot "CMakeCache.txt"

if (Test-Path -LiteralPath $cmakeCache) {
    $cachedSourceLine = Select-String -LiteralPath $cmakeCache -Pattern '^CMAKE_HOME_DIRECTORY:INTERNAL=(.+)$'
    $cachedSource = $cachedSourceLine.Matches.Groups[1].Value
    $expectedSource = [System.IO.Path]::GetFullPath($sourceRoot).Replace('\', '/')
    if ($cachedSource -and -not $cachedSource.Equals($expectedSource, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Removing a CMake cache created for another checkout: $cachedSource"
        Remove-Item -LiteralPath $buildRoot -Recurse -Force
    }
}

& (Join-Path $scriptRoot "Initialize-NativeToolchain.ps1") -Arch $Platform | Out-Host

New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

cmake -S $sourceRoot -B $buildRoot -G "Visual Studio 17 2022" -A $Platform | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "CMake configure failed for IconReplacer.ShellExtension."
}

cmake --build $buildRoot --config $Configuration --target IconReplacer.ShellExtension IconReplacer.ShellExtension.Smoke | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Native shell extension build failed."
}

$builtDll = Join-Path $buildRoot "out\$Configuration\IconReplacer.ShellExtension.dll"
if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Native shell extension DLL was not produced at $builtDll."
}

Copy-Item -LiteralPath $builtDll -Destination (Join-Path $outputRoot "IconReplacer.ShellExtension.dll") -Force

$appIconRoot = Join-Path $repoRoot "src\IconReplacer.App\Assets"
$appIconNames = @("AppIcon.ico")
$builtAssets = Join-Path $buildRoot "out\$Configuration\Assets"
$outputAssets = Join-Path $outputRoot "Assets"
New-Item -ItemType Directory -Force -Path $builtAssets, $outputAssets | Out-Null
foreach ($appIconName in $appIconNames) {
    $appIcon = Join-Path $appIconRoot $appIconName
    Copy-Item -LiteralPath $appIcon -Destination (Join-Path $builtAssets $appIconName) -Force
    Copy-Item -LiteralPath $appIcon -Destination (Join-Path $outputAssets $appIconName) -Force
}

$builtSmoke = Join-Path $buildRoot "out\$Configuration\IconReplacer.ShellExtension.Smoke.exe"
if (-not (Test-Path -LiteralPath $builtSmoke)) {
    throw "Native shell extension smoke executable was not produced at $builtSmoke."
}
Copy-Item -LiteralPath $builtSmoke -Destination (Join-Path $outputRoot "IconReplacer.ShellExtension.Smoke.exe") -Force

$builtPdb = Join-Path $buildRoot "out\$Configuration\IconReplacer.ShellExtension.pdb"
if (Test-Path -LiteralPath $builtPdb) {
    Copy-Item -LiteralPath $builtPdb -Destination (Join-Path $outputRoot "IconReplacer.ShellExtension.pdb") -Force
}

Get-Item -LiteralPath (Join-Path $outputRoot "IconReplacer.ShellExtension.dll")
