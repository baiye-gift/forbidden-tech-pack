[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$yamlPath = Join-Path $ProjectRoot 'packaging\elements\BaiyeForbiddenProtoMatter.yaml'
$yaml = Get-Content -Raw -LiteralPath $yamlPath
$required = @(
    'elementId: BaiyeForbiddenProtoMatter',
    'state: Solid',
    'isDisabled: false',
    'dlcId: ""'
)
foreach ($token in $required) {
    if (-not $yaml.Contains($token)) {
        throw "Missing element token: $token"
    }
}

foreach ($forbiddenToken in @('AnyBuildable', 'Metal', 'RawMineral', 'Food')) {
    if ($yaml -match [regex]::Escape($forbiddenToken)) {
        throw "Forbidden element tag found: $forbiddenToken"
    }
}

$solidToLiquidTransitions = @(
    'lowTempTransitionTarget:\s*(Water|Liquid|Molten)',
    'highTempTransitionTarget:\s*(Water|Liquid)'
)
foreach ($pattern in $solidToLiquidTransitions) {
    if ($yaml -match $pattern) {
        throw "Proto-Matter must not transition directly from solid to liquid: $pattern"
    }
}

$catalogPath = Join-Path $ProjectRoot 'src\Game\Elements\ElementCatalogAdapter.cs'
$catalog = Get-Content -Raw -LiteralPath $catalogPath
$endgameBlock = [regex]::Match($catalog, '(?s)EndgameIds.*?\};').Value
$rareBlock = [regex]::Match($catalog, '(?s)RareIds.*?\};').Value
if ($endgameBlock -notmatch '"Tungsten"' -or $endgameBlock -notmatch '"TempConductorSolid"') {
    throw 'Endgame element IDs must include Tungsten and TempConductorSolid.'
}
if ($endgameBlock -match '"Niobium"' -or $endgameBlock -match '"Fullerene"') {
    throw 'Niobium and Fullerene must not be classified as Endgame materials.'
}
if ($rareBlock -notmatch '"Niobium"' -or $rareBlock -notmatch '"Fullerene"') {
    throw 'Rare element IDs must include Niobium and Fullerene.'
}

$translationPath = Join-Path $ProjectRoot 'packaging\translations\zh.po'
$translation = Get-Content -Raw -LiteralPath $translationPath
foreach ($entry in @(
    @{ Context = 'STRINGS.ELEMENTS.BAIYEFORBIDDENPROTOMATTER.NAME'; English = 'Proto-Matter'; Chinese = '原质' },
    @{ Context = 'STRINGS.ELEMENTS.BAIYEFORBIDDENPROTOMATTER.DESC'; English = 'A compact, transportable substrate produced by forbidden matter processing.'; Chinese = '由禁忌物质处理生成的紧凑、可运输基质。' }
)) {
    $expected = 'msgctxt "' + [regex]::Escape($entry.Context) + '"\s+msgid "' +
        [regex]::Escape($entry.English) + '"\s+msgstr "' + [regex]::Escape($entry.Chinese) + '"'
    if ($translation -notmatch $expected) {
        throw "Translation entry must use msgctxt/msgid/msgstr: $($entry.Context)"
    }
}

Write-Host 'Element YAML validation passed.'
