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

Write-Host 'Element YAML validation passed.'
