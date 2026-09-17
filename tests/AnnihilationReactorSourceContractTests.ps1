[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\AnnihilationReactor\MatterAnnihilationReactorConfig.cs'
$controllerPath = Join-Path $ProjectRoot 'src\Game\Buildings\AnnihilationReactor\MatterAnnihilationReactorController.cs'
foreach ($path in @($configPath, $controllerPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Annihilation Reactor runtime source is missing: '$path'."
    }
}

$config = Get-Content -LiteralPath $configPath -Raw
$controller = Get-Content -LiteralPath $controllerPath -Raw

$configRequirements = [ordered]@{
    'stable building ID' = 'ModIdentity\.MatterAnnihilationReactorId'
    '7x6 footprint' = 'ModIdentity\.MatterAnnihilationReactorId,\s*7,\s*6'
    'reactor KAnim' = 'baiye_matter_annihilation_reactor_kanim'
    'external constraint power' = 'EnergyConsumptionWhenActive\s*=\s*12000f\s*\*\s*ForbiddenTechOptions\.Current\.PowerMultiplier'
    'power input' = 'RequiresPowerInput\s*=\s*true'
    'generator output' = 'RequiresPowerOutput\s*=\s*true'
    '40 kW generator rating' = 'GeneratorWattageRating\s*=\s*40000f'
    'liquid input' = 'InputConduitType\s*=\s*ConduitType\.Liquid'
    'liquid output' = 'OutputConduitType\s*=\s*ConduitType\.Liquid'
    'generator component' = 'AddOrGet<Generator>'
    'Proto-Matter storage' = 'ProtoMatterRegistration\.Tag'
    'liquid consumer' = 'ConduitConsumer'
    'liquid dispenser' = 'ConduitDispenser'
    'automation' = 'LogicOperationalController'
    'forbidden device' = 'AddOrGet<ForbiddenTechDevice>'
}
foreach ($requirement in $configRequirements.GetEnumerator()) {
    if ($config -notmatch $requirement.Value) {
        throw "Matter Annihilation Reactor config is missing $($requirement.Key)."
    }
}

$controllerRequirements = [ordered]@{
    'serialized reactor state' = '\[Serialize\][\s\S]*ReactorState\s+state'
    'serialized state elapsed time' = '\[Serialize\][\s\S]*float\s+stateElapsedSeconds'
    'serialized condition elapsed time' = '\[Serialize\][\s\S]*float\s+conditionElapsedSeconds'
    'serialized one-shot guard' = '\[Serialize\][\s\S]*bool\s+decoherenceConsequencesApplied'
    'serialized interference timer' = '\[Serialize\][\s\S]*float\s+interferenceSecondsRemaining'
    'reactor policy transition' = 'AnnihilationReactorPolicy\.Next'
    'Proto-Matter loss policy' = 'AnnihilationReactorPolicy\.CalculateProtoMatterLoss'
    'heat pulse policy' = 'AnnihilationReactorPolicy\.CalculateHeatPulseDtu'
    'power availability' = 'EnergyConsumer'
    'generator output call' = 'generator\.GenerateJoules'
    'external interference fault' = 'forbiddenDevice\.IsInterfered'
    'apply local interference source' = 'ProtoMatterInterferenceManager\.ApplySource'
    'remove local interference source' = 'ProtoMatterInterferenceManager\.RemoveSource'
    'stable Proto-Matter consumption' = '0\.2f\s*\*\s*ForbiddenTechOptions\.Current\.CostMultiplier'
    'stable heat' = '1200000f\s*\*\s*ForbiddenTechOptions\.Current\.HeatMultiplier'
    'coolant phase margin' = 'EntropyFluxPolicy\.PhaseMarginKelvin'
    'safe-removal hook' = 'PrepareForSafeRemoval'
    'animation controller' = 'KBatchedAnimController'
    'charging animation' = '"charge"'
    'stable animation' = '"working"'
    'unstable animation' = '"unstable"'
    'decoherence animation' = '"decohere"'
    'lockout animation' = '"locked"'
}
foreach ($requirement in $controllerRequirements.GetEnumerator()) {
    if ($controller -notmatch $requirement.Value) {
        throw "Matter Annihilation Reactor controller is missing $($requirement.Key)."
    }
}

Write-Host 'Matter Annihilation Reactor runtime source contract tests passed.'
