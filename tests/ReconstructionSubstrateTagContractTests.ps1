[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$tagPath = Join-Path $ProjectRoot 'src\Game\Elements\ReconstructionSubstrateTags.cs'
$adapterPath = Join-Path $ProjectRoot 'src\Game\Elements\ElementCatalogAdapter.cs'

foreach ($path in @($tagPath, $adapterPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Reconstruction substrate source is missing: '$path'."
    }
}

$tags = Get-Content -LiteralPath $tagPath -Raw
$adapter = Get-Content -LiteralPath $adapterPath -Raw

$requiredIds = @(
    'BaiyeRealitySubstrateCommon',
    'BaiyeRealitySubstrateOreOrOrganic',
    'BaiyeRealitySubstrateIndustrial',
    'BaiyeRealitySubstrateRare'
)
foreach ($id in $requiredIds) {
    if ($tags -notmatch [regex]::Escape($id)) {
        throw "Missing stable reconstruction substrate tag '$id'."
    }
}

if ($tags -notmatch 'ForTier\s*\(\s*MaterialTier\s+targetTier\s*\)' -or
        $tags -notmatch 'MatterReconstructionPolicy\.RequiredSubstrateTier') {
    throw 'Substrate tag selection must reuse MatterReconstructionPolicy.RequiredSubstrateTier.'
}
if ($tags -notmatch 'AttachToElement\s*\(' -or
        $tags -notmatch 'element\.oreTags') {
    throw 'Eligible element substrate tags must be attached through Element.oreTags for native recipe matching.'
}
if ($tags -notmatch 'ProtoMatterRegistration\.Hash') {
    throw 'Proto-Matter must be explicitly excluded from reality-substrate tagging.'
}
if ($adapter -notmatch 'ReconstructionSubstrateTags\.AttachToElement') {
    throw 'ElementCatalogAdapter must attach reconstruction substrate tags while building the active catalog.'
}
if ($adapter -match 'BaiyeRealitySubstrateCommon' -or
        $adapter -match 'BaiyeRealitySubstrateOreOrOrganic' -or
        $adapter -match 'BaiyeRealitySubstrateIndustrial' -or
        $adapter -match 'BaiyeRealitySubstrateRare') {
    throw 'ElementCatalogAdapter must not duplicate reconstruction tag IDs; mapping belongs in ReconstructionSubstrateTags.'
}

Write-Host 'Reconstruction substrate tag source contract tests passed.'
