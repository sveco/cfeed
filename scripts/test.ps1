# Builds, then runs the MSTest suite with vstest.console.
# Usage: powershell -File scripts\test.ps1 [-Filter "Name~Sanitize"]
param([string]$Filter)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$vt = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
if (-not (Test-Path $vt)) { throw "vstest.console not found at $vt" }

& "$PSScriptRoot\build.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$args = @(
    "$root\Cfeed.Test\bin\Debug\Cfeed.Test.dll",
    "/TestAdapterPath:$root\packages\MSTest.TestAdapter.1.2.1\build\_common"
)
if ($Filter) { $args += "/TestCaseFilter:$Filter" }
& $vt @args
exit $LASTEXITCODE
