[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$buildingPath = Join-Path $ProjectRoot 'src\Game\Registration\BuildingRegistration.cs'
$researchPath = Join-Path $ProjectRoot 'src\Game\Registration\ForbiddenResearchRegistration.cs'
$safetyPath = Join-Path $ProjectRoot 'src\Game\Safety\SafeRemovalController.cs'
$stringsPath = Join-Path $ProjectRoot 'src\Game\Localization\STRINGS.cs'
$enPath = Join-Path $ProjectRoot 'packaging\translations\en.po'
$zhPath = Join-Path $ProjectRoot 'packaging\translations\zh.po'
foreach ($path in @($buildingPath, $researchPath, $safetyPath, $stringsPath, $enPath, $zhPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Annihilation Reactor integration source is missing: '$path'."
    }
}

$building = Get-Content -LiteralPath $buildingPath -Raw
$research = Get-Content -LiteralPath $researchPath -Raw
$safety = Get-Content -LiteralPath $safetyPath -Raw
$strings = Get-Content -LiteralPath $stringsPath -Raw
$en = Get-Content -LiteralPath $enPath -Raw
$zh = Get-Content -LiteralPath $zhPath -Raw

foreach ($id in @('MatterReconstructorId', 'EntropyFluxDiverterId', 'MatterAnnihilationReactorId')) {
    if ($building -notmatch ('ModIdentity\.' + $id)) {
        throw "Implemented building registration is missing $id."
    }
    if ($research -notmatch ('ModIdentity\.' + $id)) {
        throw "Phase-2 research unlock registration is missing $id."
    }
}
if ($building -notmatch 'MatterAnnihilationReactorId[\s\S]*return\s+"Power"') {
    throw 'Matter Annihilation Reactor must register under the Power category.'
}
if ($building -match 'plan\.BuildingIds\s*\)') {
    throw 'Build-menu registration must not iterate planned-but-unimplemented IDs directly.'
}

foreach ($needle in @(
    'Buildings.AnnihilationReactor',
    'ModIdentity.MatterAnnihilationReactorId',
    'FindAllObjects<MatterAnnihilationReactorController>',
    'ProcessAnnihilationReactors',
    'controller.PrepareForSafeRemoval()',
    'controller.coolantStorage',
    'controller.protoMatterStorage')) {
    if ($safety -notmatch [regex]::Escape($needle)) {
        throw "Safe removal is missing reactor integration token '$needle'."
    }
}

if ($strings -notmatch 'class\s+BAIYEMATTERANNIHILATIONREACTOR') {
    throw 'Global STRINGS is missing Matter Annihilation Reactor localization.'
}
foreach ($po in @($en, $zh)) {
    foreach ($key in @(
        'STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.NAME',
        'STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.DESC',
        'STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.EFFECT',
        'STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.LOGIC_PORT.NAME')) {
        if ($po -notmatch [regex]::Escape($key)) {
            throw "Translation catalog is missing '$key'."
        }
    }
}
if ($zh -notmatch 'msgstr\s+"物质湮灭堆"') {
    throw 'Chinese translation must name the reactor 物质湮灭堆.'
}

Write-Host 'Matter Annihilation Reactor integration source contract tests passed.'
