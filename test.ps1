[CmdletBinding()]
param(
    [string[]]$Suite = @('All')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$powerShellSuites = @{
    'ElementYamlTests' = (Join-Path $projectRoot 'tests\ElementYamlTests.ps1')
    'ElementCatalogRuntimeTests' = (Join-Path $projectRoot 'tests\ElementCatalogRuntimeTests.ps1')
    'AnalyzerAdapterContractTests' = (Join-Path $projectRoot 'tests\AnalyzerAdapterContractTests.ps1')
    'AnalyzerRecipeRuntimeTests' = (Join-Path $projectRoot 'tests\AnalyzerRecipeRuntimeTests.ps1')
    'ResearchRegistrationRuntimeTests' = (Join-Path $projectRoot 'tests\ResearchRegistrationRuntimeTests.ps1')
    'OptionsLocalizationRuntimeTests' = (Join-Path $projectRoot 'tests\OptionsLocalizationRuntimeTests.ps1')
    'SafeRemovalRuntimeTests' = (Join-Path $projectRoot 'tests\SafeRemovalRuntimeTests.ps1')
    'CrusherConfigContractTests' = (Join-Path $projectRoot 'tests\CrusherConfigContractTests.ps1')
    'AssetSourceContractTests' = (Join-Path $projectRoot 'tests\AssetSourceContractTests.ps1')
    'PackageAssetVerificationTests' = (Join-Path $projectRoot 'tests\PackageAssetVerificationTests.ps1')
    'ReleaseWorkflowContractTests' = (Join-Path $projectRoot 'tests\ReleaseWorkflowContractTests.ps1')
}
$runAll = @($Suite | Where-Object { $_ -ieq 'All' }).Count -gt 0
$requestedPowerShellSuites = if ($runAll) {
    @($powerShellSuites.Keys)
} else {
    @($Suite | Where-Object { $powerShellSuites.ContainsKey($_) })
}
if ($requestedPowerShellSuites.Count -gt 0) {
    foreach ($suiteName in $requestedPowerShellSuites) {
        & pwsh -NoLogo -NoProfile -File $powerShellSuites[$suiteName] -ProjectRoot $projectRoot
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

$csharpSuites = if ($runAll) {
    @('All')
} else {
    @($Suite | Where-Object { -not $powerShellSuites.ContainsKey($_) })
}
if ($csharpSuites.Count -eq 0) {
    exit 0
}

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

& $outputAssembly --suite ($csharpSuites -join ',')
exit $LASTEXITCODE
