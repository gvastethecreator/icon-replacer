[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$python = Get-Command python -ErrorAction SilentlyContinue
if (!$python) {
    throw "Python is required to regenerate official app-icon assets."
}

& $python.Source (Join-Path $PSScriptRoot "Build-AppIconAssets.py")
if ($LASTEXITCODE -ne 0) {
    throw "Official app-icon asset generation failed with exit code $LASTEXITCODE."
}
