[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$buildingPath = Join-Path $ProjectRoot 'src\Game\Registration\BuildingRegistration.cs'
$researchPath = Join-Path $ProjectRoot 'src\Game\Registration\ForbiddenResearchRegistration.cs'
$safePath = Join-Path $ProjectRoot 'src\Game\Safety\SafeRemovalController.cs'
$stringsPath = Join-Path $ProjectRoot 'src\Game\Localization\STRINGS.cs'
$optionsPath = Join-Path $ProjectRoot 'src\Game\Options\ForbiddenTechOptions.cs'
$zhPath = Join-Path $ProjectRoot 'packaging\translations\zh.po'
$enPath = Join-Path $ProjectRoot 'packaging\translations\en.po'
foreach ($path in @($buildingPath, $researchPath, $safePath, $stringsPath, $optionsPath, $zhPath, $enPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Reconstructor integration source is missing: '$path'."
    }
}

$building = Get-Content -LiteralPath $buildingPath -Raw
$research = Get-Content -LiteralPath $researchPath -Raw
$safe = Get-Content -LiteralPath $safePath -Raw
$strings = Get-Content -LiteralPath $stringsPath -Raw
$options = Get-Content -LiteralPath $optionsPath -Raw
$zh = Get-Content -LiteralPath $zhPath -Raw
$en = Get-Content -LiteralPath $enPath -Raw

if ($building -notmatch 'ModIdentity\.MatterReconstructorId') {
    throw 'The implemented Matter Reconstructor must be in BuildingRegistration.AllBuildingIds.'
}
if ($building -match 'ModIdentity\.EntropyFluxDiverterId' -or
        $building -match 'ModIdentity\.MatterAnnihilationReactorId') {
    throw 'Unimplemented Phase-2 buildings must not be registered in the build menu.'
}

if ($research -notmatch 'ModIdentity\.ProtoFieldResearchId' -or
        $research -notmatch 'ModIdentity\.ResearchId') {
    throw 'Phase-2 research must use the stable ProtoFieldResearchId and depend on Phase-1 research.'
}
if ($research -notmatch '\{\s*"basic",\s*160f\s*\}' -or
        $research -notmatch '\{\s*"advanced",\s*120f\s*\}' -or
        $research -notmatch '\{\s*"nuclear",\s*40f\s*\}') {
    throw 'Proto-Matter Field Engineering must cost 160 basic, 120 advanced, and 40 nuclear research points.'
}
if ($research -notmatch 'MatterReconstructorId' -or
        $research -notmatch 'ReconstructorEnabled') {
    throw 'Phase-2 research must unlock the implemented Matter Reconstructor only when its option is enabled.'
}
if ($research -match 'EntropyFluxDiverterId' -or $research -match 'MatterAnnihilationReactorId') {
    throw 'Phase-2 research must not unlock unfinished buildings.'
}

if ($safe -notmatch 'using\s+ForbiddenTechnologyPack\.Game\.Buildings\.Reconstructor' -or
        $safe -notmatch 'ProcessBuildings\(FindAllObjects<MatterReconstructor>\(\),\s*report\)' -or
        $safe -notmatch 'fabricator\s+is\s+MatterReconstructor' -or
        $safe -notmatch 'CountLiveBuildings\(FindAllObjects<MatterReconstructor>\(\)\)') {
    throw 'Safe removal must process, recognize, and count Matter Reconstructor instances.'
}
if ($safe -notmatch 'ModIdentity\.ProtoFieldResearchId') {
    throw 'Completed safe removal must also hide Phase-2 research.'
}

if ($strings -notmatch 'class\s+BAIYEMATTERRECONSTRUCTOR' -or
        $strings -notmatch 'class\s+BAIYEFORBIDDENPROTOFIELDENGINEERING') {
    throw 'STRINGS must define Matter Reconstructor and Proto-Matter Field Engineering localization roots.'
}
foreach ($token in @('NAME', 'DESC', 'EFFECT', 'LOGIC_PORT')) {
    if ($strings -notmatch $token) {
        throw "Matter Reconstructor localization is missing '$token'."
    }
}
if ($options -notmatch '\[Option\("物质工程成本倍率"') {
    throw 'CostMultiplier must be displayed as 物质工程成本倍率 without renaming the serialized property.'
}
if ($options -notmatch 'public\s+float\s+CostMultiplier') {
    throw 'The serialized CostMultiplier property name must remain stable.'
}

foreach ($catalog in @($zh, $en)) {
    if ($catalog -notmatch 'BAIYEMATTERRECONSTRUCTOR' -or
            $catalog -notmatch 'BAIYEFORBIDDENPROTOFIELDENGINEERING') {
        throw 'Both translation catalogs must contain the reconstructor building and Phase-2 research keys.'
    }
}

Write-Host 'Matter Reconstructor gameplay integration source contract tests passed.'
