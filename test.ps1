[CmdletBinding()]
param(
    [string[]]$Suite = @('All')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $projectRoot 'test-artifacts'
$outputAssembly = Join-Path $outputDirectory 'ForbiddenTechnologyPack.CoreTests.exe'

if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler was not found at '$csc'."
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$sources = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src\Core') -Filter '*.cs' -Recurse -File -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests') -Filter '*.cs' -Recurse -File -ErrorAction Stop
) | ForEach-Object { $_.FullName }

if ($sources.Count -eq 0) {
    throw 'No C# test sources were found.'
}

& $csc /nologo /target:exe /warn:4 "/out:$outputAssembly" @sources
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $outputAssembly --suite ($Suite -join ',')
exit $LASTEXITCODE
