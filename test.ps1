[CmdletBinding()]
param(
    [string[]]$Suite = @('All'),
    [string]$GamePath,
    [switch]$DescribeSuites,
    [switch]$DescribePlan
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$suiteCatalog = [ordered]@{
    'ElementYamlTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ElementYamlTests.ps1')
        RequiresGame = $false
    }
    'AssetSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\AssetSourceContractTests.ps1')
        RequiresGame = $false
    }
    'PackageAssetVerificationTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\PackageAssetVerificationTests.ps1')
        RequiresGame = $false
    }
    'ReleaseWorkflowContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ReleaseWorkflowContractTests.ps1')
        RequiresGame = $false
    }
    'Phase2OptionsSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\Phase2OptionsSourceContractTests.ps1')
        RequiresGame = $false
    }
    'Phase2RegistrationSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\Phase2RegistrationSourceContractTests.ps1')
        RequiresGame = $false
    }
    'ProtoMatterInterferenceGameSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ProtoMatterInterferenceGameSourceContractTests.ps1')
        RequiresGame = $false
    }
    'ReconstructionSubstrateTagContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ReconstructionSubstrateTagContractTests.ps1')
        RequiresGame = $false
    }
    'ReconstructionRecipeSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ReconstructionRecipeSourceContractTests.ps1')
        RequiresGame = $false
    }
    'ReconstructorConfigSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ReconstructorConfigSourceContractTests.ps1')
        RequiresGame = $false
    }
    'ReconstructorIntegrationSourceContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ReconstructorIntegrationSourceContractTests.ps1')
        RequiresGame = $false
    }
    'ElementCatalogRuntimeTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ElementCatalogRuntimeTests.ps1')
        RequiresGame = $true
    }
    'AnalyzerAdapterContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\AnalyzerAdapterContractTests.ps1')
        RequiresGame = $true
    }
    'AnalyzerRecipeRuntimeTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\AnalyzerRecipeRuntimeTests.ps1')
        RequiresGame = $true
    }
    'ResearchRegistrationRuntimeTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\ResearchRegistrationRuntimeTests.ps1')
        RequiresGame = $true
    }
    'OptionsLocalizationRuntimeTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\OptionsLocalizationRuntimeTests.ps1')
        RequiresGame = $true
    }
    'SafeRemovalRuntimeTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\SafeRemovalRuntimeTests.ps1')
        RequiresGame = $true
    }
    'CrusherConfigContractTests' = [pscustomobject]@{
        Path = (Join-Path $projectRoot 'tests\CrusherConfigContractTests.ps1')
        RequiresGame = $true
    }
}

if ($DescribeSuites) {
    @($suiteCatalog.GetEnumerator() | ForEach-Object {
        [pscustomobject]@{
            Name = $_.Key
            Path = $_.Value.Path
            RequiresGame = $_.Value.RequiresGame
        }
    }) | ConvertTo-Json -Depth 4 -Compress
    exit 0
}
$suiteGroups = @{
    'Portable' = @($suiteCatalog.GetEnumerator() | Where-Object {
        -not $_.Value.RequiresGame
    } | ForEach-Object { $_.Key })
    'All' = @($suiteCatalog.Keys)
}

$requestedPowerShellSuites = [System.Collections.Generic.List[string]]::new()
foreach ($requestedSuite in $Suite) {
    if ($suiteGroups.ContainsKey($requestedSuite)) {
        foreach ($suiteName in $suiteGroups[$requestedSuite]) {
            if (-not $requestedPowerShellSuites.Contains($suiteName)) {
                $requestedPowerShellSuites.Add($suiteName)
            }
        }
    } elseif ($suiteCatalog.Contains($requestedSuite) -and
            -not $requestedPowerShellSuites.Contains($requestedSuite)) {
        $requestedPowerShellSuites.Add($requestedSuite)
    }
}

$requestedGameSuites = @($requestedPowerShellSuites | Where-Object {
    $suiteCatalog[$_].RequiresGame
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

$powerShellInvocations = @($requestedPowerShellSuites | ForEach-Object {
    $suiteName = $_
    $definition = $suiteCatalog[$suiteName]
    $arguments = @('-NoLogo', '-NoProfile', '-File', $definition.Path, '-ProjectRoot', $projectRoot)
    if ($definition.RequiresGame) {
        $arguments += @('-GamePath', $GamePath)
    }
    [pscustomobject]@{
        Name = $suiteName
        RequiresGame = $definition.RequiresGame
        Arguments = $arguments
    }
})

$runCoreAll = @($Suite | Where-Object { $_ -ieq 'All' -or $_ -ieq 'Portable' }).Count -gt 0
$csharpSuites = if ($runCoreAll) {
    @('All')
} else {
    @($Suite | Where-Object {
        -not $suiteCatalog.Contains($_) -and -not $suiteGroups.ContainsKey($_)
    })
}

if ($DescribePlan) {
    [pscustomobject]@{
        PowerShellSuites = $powerShellInvocations
        CSharpSuites = $csharpSuites
    } | ConvertTo-Json -Depth 6 -Compress
    exit 0
}

foreach ($invocation in $powerShellInvocations) {
    $invocationArguments = @($invocation.Arguments)
    & pwsh @invocationArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
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