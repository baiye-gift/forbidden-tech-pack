[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$registryPath = Join-Path $ProjectRoot 'src\Game\Recipes\RecipeRegistry.cs'
$adapterPath = Join-Path $ProjectRoot 'src\Game\Recipes\ReconstructionRecipeAdapter.cs'
foreach ($path in @($registryPath, $adapterPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Reconstruction recipe source is missing: '$path'."
    }
}

$registry = Get-Content -LiteralPath $registryPath -Raw
$adapter = Get-Content -LiteralPath $adapterPath -Raw

if ($registry -notmatch 'ReconstructorRecipes') {
    throw 'RecipeRegistry must expose ReconstructorRecipes.'
}
if ($registry -notmatch 'MatterReconstructionPolicy\.CreatePlan') {
    throw 'Reconstructor recipes must come from MatterReconstructionPolicy.CreatePlan.'
}
if ($registry -notmatch 'ReconstructionSubstrateTags\.ForTier') {
    throw 'Reconstructor recipes must consume a tier-matched reality-substrate tag.'
}
if ($registry -notmatch 'ProtoMatterRegistration\.Tag') {
    throw 'Reconstructor recipes must consume Proto-Matter as a separate ingredient.'
}
if ($registry -notmatch 'ModIdentity\.MatterReconstructorId') {
    throw 'Reconstructor recipes must bind only to BaiyeMatterReconstructor.'
}
if ($registry -notmatch 'plan\.SubstrateKg' -or
        $registry -notmatch 'plan\.ProtoMatterKg' -or
        $registry -notmatch 'plan\.ProductKg') {
    throw 'Reconstructor recipe ingredients/results must use the tested reconstruction plan masses.'
}

if ($adapter -notmatch 'SelectUnlockedIds\s*\(' -or
        $adapter -notmatch 'CompilerRecipeFilter\.SelectIds') {
    throw 'ReconstructionRecipeAdapter must filter targets through persisted analyzed unlock state.'
}
if ($adapter -notmatch 'RecipeRegistry\.ReconstructorRecipes\.Keys') {
    throw 'Unlocked reconstruction targets must be intersected with active reconstructor recipes.'
}
if ($adapter -match 'Unlock\s*\(') {
    throw 'The recipe adapter must not mutate analysis unlock state.'
}

Write-Host 'Reconstruction recipe source contract tests passed.'
