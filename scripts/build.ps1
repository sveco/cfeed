# Restores packages and builds the app and test projects with VS 2022 Build Tools.
# Usage: powershell -File scripts\build.ps1 [-OutputPath bin\verify\]
#   -OutputPath builds into another folder under each project (for example bin\verify\), so the build
#   works while cfeed.exe from bin\Debug is running and locks its files.
param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$ms = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $ms)) { throw "MSBuild not found at $ms" }

$extra = @()
if ($OutputPath) { $extra += "-p:OutputPath=$OutputPath" }

Push-Location $root
try {
    & $ms cfeed.sln -t:restore -p:RestorePackagesConfig=true -v:quiet -nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    foreach ($proj in 'CRR\cFeed.csproj', 'Cfeed.Test\Cfeed.Test.csproj') {
        & $ms $proj -v:minimal -nologo @extra
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}
finally { Pop-Location }
