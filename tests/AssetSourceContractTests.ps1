[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Get-BottomTransparentRows([string]$Path) {
    $bitmap = [System.Drawing.Bitmap]::new($Path)
    try {
        for ($y = $bitmap.Height - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                if ($bitmap.GetPixel($x, $y).A -gt 8) {
                    return $bitmap.Height - 1 - $y
                }
            }
        }
        return $bitmap.Height
    }
    finally {
        $bitmap.Dispose()
    }
}

$manifestPath = Join-Path $ProjectRoot 'assets\animation-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Animation manifest is missing: '$manifestPath'."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$expected = [ordered]@{
    'baiye_proto_matter' = @('idle')
    'baiye_matter_analyzer' = @('off', 'idle', 'working_pre', 'working_loop', 'working_pst', 'working_pst_complete', 'overheat', 'ui')
    'baiye_mass_crusher' = @('off', 'idle', 'working_pre', 'working_loop', 'working_pst', 'working_pst_complete', 'blocked', 'ui')
    'baiye_matter_compiler' = @('off', 'idle', 'working_pre', 'working_loop', 'working_pst', 'working_pst_complete', 'no_coolant', 'blocked', 'ui')
}
$sourceFolders = @{
    'baiye_proto_matter' = 'proto_matter'
    'baiye_matter_analyzer' = 'matter_analyzer'
    'baiye_mass_crusher' = 'mass_crusher'
    'baiye_matter_compiler' = 'matter_compiler'
}
$buildingCanvasSizes = @{
    'baiye_matter_analyzer' = 300
    'baiye_mass_crusher' = 400
    'baiye_matter_compiler' = 500
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
    [xml]$scml = Get-Content -LiteralPath $scmlPath -Raw
    if ($scml.spriter_data.entity.name -ne $name) {
        throw "SCML '$scmlPath' entity must keep the source name '$name'; the game adds the _kanim runtime suffix."
    }
    $sourceAnimations = @($scml.spriter_data.entity.animation)
    foreach ($animation in $expectedAnimations) {
        $sourceAnimation = @($sourceAnimations | Where-Object { $_.name -eq $animation })
        if ($sourceAnimation.Count -ne 1) {
            throw "SCML '$scmlPath' is missing animation '$animation'."
        }
        if ($animation -in @('working_pre', 'working_pst_complete') -and $sourceAnimation[0].looping -ne 'false') {
            throw "SCML '$scmlPath' animation '$animation' must be non-looping."
        }
    }

    $animationIds = @($sourceAnimations | ForEach-Object { $_.id })
    if (@($animationIds | Select-Object -Unique).Count -ne $animationIds.Count) {
        throw "SCML '$scmlPath' contains duplicate animation IDs."
    }
    if ($buildingCanvasSizes.ContainsKey($name)) {
        $files = @($scml.spriter_data.folder.file)
        $bodyFile = @($files | Where-Object { $_.name -eq ($name + '_0.png') })
        $uiFile = @($files | Where-Object { $_.name -eq 'ui_0.png' })
        if ($bodyFile.Count -ne 1 -or $uiFile.Count -ne 1) {
            throw "SCML '$scmlPath' must define separate body and UI sprite files."
        }
        if ([int]$bodyFile[0].width -ne $buildingCanvasSizes[$name] -or
            [int]$bodyFile[0].height -ne $buildingCanvasSizes[$name]) {
            throw "SCML '$scmlPath' body canvas must match its building footprint."
        }
        if ([double]$bodyFile[0].pivot_y -ne 0.0) {
            throw "SCML '$scmlPath' body sprite must be bottom-anchored so it cannot extend below the floor."
        }

        $uiAnimation = @($sourceAnimations | Where-Object { $_.name -eq 'ui' })[0]
        $uiTimelines = @($uiAnimation.timeline | Where-Object { $_.name -eq 'ui_0' })
        if ($uiTimelines.Count -ne 1) {
            throw "SCML '$scmlPath' must expose a ui_0 timeline so ONI compiles the build symbol named ui."
        }
        $uiObjects = @($uiTimelines[0].key | ForEach-Object { $_.object })
        if ($uiObjects.Count -eq 0 -or @($uiObjects | Where-Object { $_.file -ne $uiFile[0].id }).Count -ne 0) {
            throw "SCML '$scmlPath' UI animation must render the dedicated UI sprite."
        }

        $bodyPath = Join-Path (Split-Path -Parent $scmlPath) $bodyFile[0].name
        $uiPath = Join-Path (Split-Path -Parent $scmlPath) $uiFile[0].name
        foreach ($spritePath in @($bodyPath, $uiPath)) {
            if (-not (Test-Path -LiteralPath $spritePath -PathType Leaf)) {
                throw "SCML sprite source is missing: '$spritePath'."
            }
        }
        $bottomTransparentRows = Get-BottomTransparentRows $bodyPath
        if ($bottomTransparentRows -gt 2) {
            throw "SCML body '$bodyPath' leaves $bottomTransparentRows transparent rows below its feet; at most 2 are allowed so the building sits on the tile."
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
