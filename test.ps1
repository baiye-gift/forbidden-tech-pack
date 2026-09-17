[CmdletBinding()]
param(
    [string[]]$Suite = @('All'),
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$portablePowerShellSuites = [ordered]@{
    'ElementYamlTests' = (Join-Path $projectRoot 'tests\ElementYamlTests.ps1')
    'AssetSourceContractTests' = (Join-Path $projectRoot 'tests\AssetSourceContractTests.ps1')
    'PackageAssetVerificationTests' = (Join-Path $projectRoot 'tests\PackageAssetVerificationTests.ps1')
    'ReleaseWorkflowContractTests' = (Join-Path $projectRoot 'tests\ReleaseWorkflowContractTests.ps1')
}
$gameDependentPowerShellSuites = [ordered]@{
    'ElementCatalogRuntimeTests' = (Join-Path $projectRoot 'tests\ElementCatalogRuntimeTests.ps1')
    'AnalyzerAdapterContractTests' = (Join-Path $projectRoot 'tests\AnalyzerAdapterContractTests.ps1')
    'AnalyzerRecipeRuntimeTests' = (Join-Path $projectRoot 'tests\AnalyzerRecipeRuntimeTests.ps1')
    'ResearchRegistrationRuntimeTests' = (Join-Path $projectRoot 'tests\ResearchRegistrationRuntimeTests.ps1')
    'OptionsLocalizationRuntimeTests' = (Join-Path $projectRoot 'tests\OptionsLocalizationRuntimeTests.ps1')
    'SafeRemovalRuntimeTests' = (Join-Path $projectRoot 'tests\SafeRemovalRuntimeTests.ps1')
    'CrusherConfigContractTests' = (Join-Path $projectRoot 'tests\CrusherConfigContractTests.ps1')
}
$powerShellSuites = @{}
foreach ($entry in $portablePowerShellSuites.GetEnumerator()) {
    $powerShellSuites[$entry.Key] = $entry.Value
}
foreach ($entry in $gameDependentPowerShellSuites.GetEnumerator()) {
    $powerShellSuites[$entry.Key] = $entry.Value
}
$suiteGroups = @{
    'Portable' = @($portablePowerShellSuites.Keys)
    'All' = @($portablePowerShellSuites.Keys) + @($gameDependentPowerShellSuites.Keys)
}

$requestedPowerShellSuites = [System.Collections.Generic.List[string]]::new()
foreach ($requestedSuite in $Suite) {
    if ($suiteGroups.ContainsKey($requestedSuite)) {
        foreach ($suiteName in $suiteGroups[$requestedSuite]) {
            if (-not $requestedPowerShellSuites.Contains($suiteName)) {
                $requestedPowerShellSuites.Add($suiteName)
            }
        }
    } elseif ($powerShellSuites.ContainsKey($requestedSuite) -and
            -not $requestedPowerShellSuites.Contains($requestedSuite)) {
        $requestedPowerShellSuites.Add($requestedSuite)
    }
}

$requestedGameSuites = @($requestedPowerShellSuites | Where-Object {
    $gameDependentPowerShellSuites.Contains($_)
})
if ($requestedGameSuites.Count -gt 0) {
    if ([string]::IsNullOrWhiteSpace($GamePath)) {
        throw "-GamePath is required for game integration suites: $($requestedGameSuites -join ', ')."
    }
    if (-not (Test-Path -LiteralPath $GamePath -PathType Container)) {
        throw "GamePath was not found: '$GamePath'."
    }
    $gameAssemblyPath = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed\Assembly-CSharp.dll'
    if (-not (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf)) {
        throw "GamePath does not contain the required game assembly: '$gameAssemblyPath'."
    }
}

foreach ($suiteName in $requestedPowerShellSuites) {
    if ($gameDependentPowerShellSuites.Contains($suiteName)) {
        & pwsh -NoLogo -NoProfile -File $powerShellSuites[$suiteName] `
            -ProjectRoot $projectRoot -GamePath $GamePath
    } else {
        & pwsh -NoLogo -NoProfile -File $powerShellSuites[$suiteName] -ProjectRoot $projectRoot
    }
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$runCoreAll = @($Suite | Where-Object { $_ -ieq 'All' -or $_ -ieq 'Portable' }).Count -gt 0
$csharpSuites = if ($runCoreAll) {
    @('All')
} else {
    @($Suite | Where-Object {
        -not $powerShellSuites.ContainsKey($_) -and -not $suiteGroups.ContainsKey($_)
    })
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
