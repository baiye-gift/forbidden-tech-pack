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

if ($building -notmatch 'foreach\s*\(var\s+buildingId\s+in\s+AllBuildingIds\)') {
    throw 'Build-menu registration must iterate the implemented AllBuildingIds list.'
}
if ($building -match 'foreach\s*\(var\s+buildingId\s+in\s+plan\.BuildingIds\)') {
    throw 'Build-menu registration must not register planned-but-unimplemented building IDs.'
}
if ($research -notmatch 'new\s+List<string>\(plan\.Phase1BuildingIds\)') {
    throw 'The phase-1 research tech must unlock only Phase1BuildingIds.'
}
if ($research -match 'new\s+List<string>\(plan\.BuildingIds\)') {
    throw 'The phase-1 research tech must not unlock the combined phase-1/phase-2 building list.'
}

Write-Host 'Phase-2 incremental registration source contract tests passed.'
