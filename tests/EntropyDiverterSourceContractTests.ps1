[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\EntropyDiverter\EntropyFluxDiverterConfig.cs'
$controllerPath = Join-Path $ProjectRoot 'src\Game\Buildings\EntropyDiverter\EntropyFluxDiverterController.cs'

foreach ($path in @($configPath, $controllerPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Entropy Flux Diverter source is missing: '$path'."
    }
}

$config = Get-Content -LiteralPath $configPath -Raw
$controller = Get-Content -LiteralPath $controllerPath -Raw

$requiredConfig = @(
    'ModIdentity.EntropyFluxDiverterId',
    '4, 4, "baiye_entropy_flux_diverter_kanim"',
    'EnergyConsumptionWhenActive',
    '3600f * ForbiddenTechOptions.Current.PowerMultiplier',
    '12f * ForbiddenTechOptions.Current.HeatMultiplier',
    'ConduitType.Liquid',
    'ConduitSecondaryInput',
    'ConduitSecondaryOutput',
    'useSecondaryInput = true',
    'useSecondaryOutput = true',
    'ProtoMatterRegistration.Tag',
    'ForbiddenTechDevice',
    'LogicOperationalController'
)
foreach ($needle in $requiredConfig) {
    if (-not $config.Contains($needle)) {
        throw "Entropy Flux Diverter config contract is missing '$needle'."
    }
}

$requiredController = @(
    '[SerializationConfig(MemberSerialization.OptIn)]',
    '[Serialize]',
    'processedPair',
    'EntropyFluxPolicy.Evaluate',
    'EntropyFluxPolicy.ProtoMatterCostKg',
    'ConsumeIgnoringDisease',
    'hotStorage',
    'coldStorage',
    'protoMatterStorage',
    'ForbiddenTechDevice',
    'SetOnState'
)
foreach ($needle in $requiredController) {
    if (-not $controller.Contains($needle)) {
        throw "Entropy Flux Diverter controller contract is missing '$needle'."
    }
}

Write-Host 'Entropy Flux Diverter source contract passed.'
