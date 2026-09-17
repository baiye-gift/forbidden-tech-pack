[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\Reconstructor\MatterReconstructorConfig.cs'
$runtimePath = Join-Path $ProjectRoot 'src\Game\Buildings\Reconstructor\MatterReconstructor.cs'
foreach ($path in @($configPath, $runtimePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Reconstructor source is missing: '$path'."
    }
}

$config = Get-Content -LiteralPath $configPath -Raw
$runtime = Get-Content -LiteralPath $runtimePath -Raw

if ($config -notmatch 'ModIdentity\.MatterReconstructorId' -or
        $config -notmatch '4,\s*4,\s*"baiye_matter_reconstructor_kanim"') {
    throw 'Matter Reconstructor must use the stable ID, 4x4 footprint, and reconstructor KAnim.'
}
if ($config -notmatch 'EnergyConsumptionWhenActive\s*=\s*4800f\s*\*\s*ForbiddenTechOptions\.Current\.PowerMultiplier') {
    throw 'Matter Reconstructor must draw 4800 W multiplied by PowerMultiplier.'
}
if ($config -notmatch 'SelfHeatKilowattsWhenActive\s*=\s*24f\s*\*\s*ForbiddenTechOptions\.Current\.HeatMultiplier') {
    throw 'Matter Reconstructor must emit 24 kDTU/s multiplied by HeatMultiplier.'
}
if ($config -notmatch 'InputCapacityKg\s*=\s*2200f' -or
        $config -notmatch 'OutputCapacityKg\s*=\s*1100f') {
    throw 'Matter Reconstructor storage capacities must be 2200 kg input and 1100 kg output.'
}
if ($config -notmatch 'InputConduitType\s*=\s*ConduitType\.Solid' -or
        $config -notmatch 'OutputConduitType\s*=\s*ConduitType\.Solid' -or
        $config -match 'ConduitType\.Liquid') {
    throw 'Matter Reconstructor must use one solid input/output and no liquid conduit.'
}
if ($config -notmatch 'SolidConduitConsumer' -or $config -notmatch 'SolidConduitDispenser') {
    throw 'Matter Reconstructor must support solid conveyor input and output.'
}
if ($config -notmatch 'LogicOperationalController\.PORT_ID' -or
        $config -notmatch 'PoweredActiveController\.Def') {
    throw 'Matter Reconstructor must support automation and powered active state.'
}
if ($config -notmatch 'AddOrGet<ForbiddenTechDevice>\s*\(\s*\)') {
    throw 'Matter Reconstructor must participate in Proto-Matter interference through ForbiddenTechDevice.'
}
if ($config -notmatch 'ReconstructionSubstrateTags\.Common' -or
        $config -notmatch 'ReconstructionSubstrateTags\.OreOrOrganic' -or
        $config -notmatch 'ReconstructionSubstrateTags\.Industrial' -or
        $config -notmatch 'ReconstructionSubstrateTags\.Rare' -or
        $config -notmatch 'ProtoMatterRegistration\.Tag') {
    throw 'Matter Reconstructor input storage must accept all reality-substrate tiers plus Proto-Matter.'
}

if ($runtime -notmatch 'class\s+MatterReconstructor\s*:\s*ComplexFabricator') {
    throw 'Matter Reconstructor runtime must derive from ComplexFabricator.'
}
if ($runtime -notmatch 'ReconstructionRecipeAdapter\.SelectUnlockedIds' -or
        $runtime -notmatch 'RecipeRegistry\.ReconstructorRecipes') {
    throw 'Matter Reconstructor runtime must expose only analyzed reconstruction recipes.'
}
if ($runtime -notmatch 'UnlocksChanged\s*\+=' -or $runtime -notmatch 'UnlocksChanged\s*-=' ) {
    throw 'Matter Reconstructor must refresh recipes when new material templates are analyzed.'
}
if ($runtime -notmatch 'CurrentWorkingOrder\s*!=\s*null' -or
        $runtime -notmatch 'pendingRecipeRefresh') {
    throw 'Matter Reconstructor must defer recipe refresh while a work order is active.'
}
if ($runtime -notmatch 'HasOutputSpace' -or $runtime -notmatch 'RemainingCapacity') {
    throw 'Matter Reconstructor must block new work when its product storage lacks capacity.'
}
if ($runtime -notmatch 'DropStorage\(inStorage\)' -or
        $runtime -notmatch 'DropStorage\(buildStorage\)' -or
        $runtime -notmatch 'DropStorage\(outStorage\)') {
    throw 'Matter Reconstructor deconstruction must return all fabricator storages.'
}

Write-Host 'Matter Reconstructor building source contract tests passed.'
