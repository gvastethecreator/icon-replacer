param(
    [ValidateSet("x64", "x86", "arm64")]
    [string]$Arch = "x64",

    [ValidateSet("x64", "x86")]
    [string]$HostArch = "x64",

    [switch]$PassThru
)

$ErrorActionPreference = "Stop"

function Find-VsDevCmd {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path -LiteralPath $vswhere) {
        $installPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($installPath)) {
            $candidate = Join-Path $installPath.Trim() "Common7\Tools\VsDevCmd.bat"
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    $fallbacks = @(
        (Join-Path $programFilesX86 "Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"),
        (Join-Path ([Environment]::GetFolderPath("ProgramFiles")) "Microsoft Visual Studio\18\Community\Common7\Tools\VsDevCmd.bat")
    )

    foreach ($candidate in $fallbacks) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Visual Studio C++ developer tools were not found. Install Visual Studio Build Tools with the C++ workload."
}

$vsDevCmd = Find-VsDevCmd
$cmd = "`"$vsDevCmd`" -arch=$Arch -host_arch=$HostArch >nul && set"
$environment = & cmd.exe /s /c $cmd
if ($LASTEXITCODE -ne 0) {
    throw "VsDevCmd failed for arch=$Arch host_arch=$HostArch."
}

foreach ($line in $environment) {
    $separator = $line.IndexOf("=")
    if ($separator -le 0) {
        continue
    }

    $name = $line.Substring(0, $separator)
    $value = $line.Substring($separator + 1)
    [Environment]::SetEnvironmentVariable($name, $value, "Process")
}

Write-Host "Native toolchain loaded: $vsDevCmd"

if ($PassThru) {
    Get-Command cl.exe, msbuild.exe, cmake.exe -ErrorAction SilentlyContinue |
        Select-Object Name, Source
}
