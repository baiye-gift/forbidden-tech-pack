[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $ProjectRoot 'assets\animation-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Animation manifest is missing: '$manifestPath'."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$expected = [ordered]@{
    'baiye_proto_matter' = @('idle')
    'baiye_matter_analyzer' = @('off', 'idle', 'working_loop', 'working_pst', 'overheat')
    'baiye_mass_crusher' = @('off', 'idle', 'working_loop', 'working_pst', 'blocked')
    'baiye_matter_compiler' = @('off', 'idle', 'working_loop', 'working_pst', 'no_coolant', 'blocked')
}
$sourceFolders = @{
    'baiye_proto_matter' = 'proto_matter'
    'baiye_matter_analyzer' = 'matter_analyzer'
    'baiye_mass_crusher' = 'mass_crusher'
    'baiye_matter_compiler' = 'matter_compiler'
}

$actualNames = @($manifest.PSObject.Properties.Name | Sort-Object)
$expectedNames = @($expected.Keys | Sort-Object)
if (($actualNames -join '|') -ne ($expectedNames -join '|')) {
    throw "Animation manifest keys differ. Expected '$($expectedNames -join ', ')', got '$($actualNames -join ', ')'."
}

foreach ($name in $expected.Keys) {
    $actualAnimations = @($manifest.$name)
    $expectedAnimations = @($expected[$name])
    if (($actualAnimations -join '|') -ne ($expectedAnimations -join '|')) {
        throw "Animation list for '$name' differs. Expected '$($expectedAnimations -join ', ')', got '$($actualAnimations -join ', ')'."
    }

    $scmlPath = Join-Path $ProjectRoot ("assets\scml\{0}\{1}.scml" -f $sourceFolders[$name], $name)
    if (-not (Test-Path -LiteralPath $scmlPath -PathType Leaf)) {
        throw "SCML source is missing for '$name': '$scmlPath'."
    }
    $scml = Get-Content -LiteralPath $scmlPath -Raw
    foreach ($animation in $expectedAnimations) {
        if ($scml -notmatch ('name="' + [regex]::Escape($animation) + '"')) {
            throw "SCML '$scmlPath' is missing animation '$animation'."
        }
    }
}

$buildAssets = Join-Path $ProjectRoot 'build-assets.ps1'
if (-not (Test-Path -LiteralPath $buildAssets -PathType Leaf)) {
    throw "Asset build script is missing: '$buildAssets'."
}

$stableReferences = @{
    'src\Game\Elements\ProtoMatterRegistration.cs' = 'baiye_proto_matter_kanim'
    'src\Game\Buildings\Analyzer\MatterAnalyzerConfig.cs' = 'baiye_matter_analyzer_kanim'
    'src\Game\Buildings\Crusher\MassCrusherConfig.cs' = 'baiye_mass_crusher_kanim'
    'src\Game\Buildings\Compiler\MatterCompilerConfig.cs' = 'baiye_matter_compiler_kanim'
}
foreach ($relativePath in $stableReferences.Keys) {
    $path = Join-Path $ProjectRoot $relativePath
    $content = Get-Content -LiteralPath $path -Raw
    if ($content -notmatch [regex]::Escape($stableReferences[$relativePath])) {
        throw "'$relativePath' does not reference final animation '$($stableReferences[$relativePath])'."
    }
}

foreach ($language in @('en', 'zh')) {
    $poPath = Join-Path $ProjectRoot ("packaging\translations\$language.po")
    if (-not (Test-Path -LiteralPath $poPath -PathType Leaf)) {
        throw "Translation file is missing: '$poPath'."
    }
}

Write-Host 'Asset source contract tests passed.'
