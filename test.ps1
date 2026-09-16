[CmdletBinding()]
param(
    [string[]]$Suite = @('All')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $projectRoot 'scripts\Get-CSharpCompiler.ps1')
$csc = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $projectRoot
$outputDirectory = Join-Path $projectRoot 'test-artifacts'
$outputAssembly = Join-Path $outputDirectory 'ForbiddenTechnologyPack.CoreTests.exe'

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$sources = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src\Core') -Filter '*.cs' -Recurse -File -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests') -Filter '*.cs' -Recurse -File -ErrorAction Stop
) | ForEach-Object { $_.FullName }

if ($sources.Count -eq 0) {
    throw 'No C# test sources were found.'
}

& $csc /nologo /target:exe /langversion:7.3 /warn:4 "/out:$outputAssembly" @sources
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $outputAssembly --suite ($Suite -join ',')
exit $LASTEXITCODE
