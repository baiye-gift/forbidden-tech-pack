[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$buildingPath = Join-Path $ProjectRoot 'src\Game\Registration\BuildingRegistration.cs'
$researchPath = Join-Path $ProjectRoot 'src\Game\Registration\ForbiddenResearchRegistration.cs'
foreach ($path in @($buildingPath, $researchPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Registration source is missing: '$path'."
    }
}

$building = Get-Content -LiteralPath $buildingPath -Raw
$research = Get-Content -LiteralPath $researchPath -Raw

if ($building -notmatch 'AllBuildingIds') {
    throw 'Build-menu registration must be driven by the implemented AllBuildingIds list.'
}
if ($building -match '(foreach|for)[\s\S]{0,120}plan\.BuildingIds') {
    throw 'Build-menu registration must not iterate planned-but-unimplemented building IDs directly.'
}
foreach ($id in @(
    'MatterAnalyzerId',
    'MassCrusherId',
    'MatterCompilerId',
    'MatterReconstructorId',
    'EntropyFluxDiverterId',
    'MatterAnnihilationReactorId')) {
    if ($building -notmatch ('ModIdentity\.' + $id)) {
        throw "Implemented build-menu list is missing $id."
    }
}
if ($building -notmatch 'MatterAnnihilationReactorId[\s\S]*return\s+"Power"') {
    throw 'Matter Annihilation Reactor must be routed to the Power build category.'
}
if ($research -notmatch 'new\s+List<string>\(plan\.Phase1BuildingIds\)') {
    throw 'The phase-1 research tech must unlock only Phase1BuildingIds.'
}
if ($research -match 'new\s+List<string>\(plan\.BuildingIds\)') {
    throw 'The phase-1 research tech must not unlock the combined phase-1/phase-2 building list.'
}
foreach ($id in @('MatterReconstructorId', 'EntropyFluxDiverterId', 'MatterAnnihilationReactorId')) {
    if ($research -notmatch ('ModIdentity\.' + $id)) {
        throw "Phase-2 implemented research unlocks are missing $id."
    }
}

Write-Host 'Phase-2 incremental registration source contract tests passed.'
