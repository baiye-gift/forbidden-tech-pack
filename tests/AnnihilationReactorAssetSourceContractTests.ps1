[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$manifestPath = Join-Path $ProjectRoot 'assets\animation-manifest.json'
$sourceDir = Join-Path $ProjectRoot 'assets\scml\matter_annihilation_reactor'
$scmlPath = Join-Path $sourceDir 'baiye_matter_annihilation_reactor.scml'
$bodyPath = Join-Path $sourceDir 'baiye_matter_annihilation_reactor_0.png'
$uiPath = Join-Path $sourceDir 'ui_0.png'
foreach ($path in @($manifestPath, $scmlPath, $bodyPath, $uiPath, "$bodyPath.b64", "$uiPath.b64")) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Annihilation Reactor asset source is missing: '$path'."
    }
}

$requiredAnimations = @('off', 'idle', 'charge', 'working', 'unstable', 'decohere', 'locked', 'ui')
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$actualAnimations = @($manifest.baiye_matter_annihilation_reactor)
if (($actualAnimations -join '|') -ne ($requiredAnimations -join '|')) {
    throw "Matter Annihilation Reactor manifest animations differ. Expected '$($requiredAnimations -join ', ')'."
}

[xml]$scml = Get-Content -LiteralPath $scmlPath -Raw
if ($scml.spriter_data.entity.name -ne 'baiye_matter_annihilation_reactor') {
    throw 'Matter Annihilation Reactor SCML entity name is invalid.'
}
$animations = @($scml.spriter_data.entity.animation)
foreach ($animation in $requiredAnimations) {
    if (@($animations | Where-Object { $_.name -eq $animation }).Count -ne 1) {
        throw "Matter Annihilation Reactor SCML is missing animation '$animation'."
    }
}

$files = @($scml.spriter_data.folder.file)
$bodyFile = @($files | Where-Object { $_.name -eq 'baiye_matter_annihilation_reactor_0.png' })
$uiFile = @($files | Where-Object { $_.name -eq 'ui_0.png' })
if ($bodyFile.Count -ne 1 -or [int]$bodyFile[0].width -ne 560 -or
        [int]$bodyFile[0].height -ne 480 -or [double]$bodyFile[0].pivot_y -ne 0.0) {
    throw 'Matter Annihilation Reactor body sprite must be a 560x480 bottom-anchored source.'
}
if ($uiFile.Count -ne 1 -or [int]$uiFile[0].width -ne 128 -or [int]$uiFile[0].height -ne 128) {
    throw 'Matter Annihilation Reactor UI sprite must be a dedicated 128x128 source.'
}
$uiAnimation = @($animations | Where-Object { $_.name -eq 'ui' })[0]
if (@($uiAnimation.timeline | Where-Object { $_.name -eq 'ui_0' }).Count -ne 1) {
    throw 'Matter Annihilation Reactor UI animation must expose the ui_0 timeline.'
}

$body = [System.Drawing.Bitmap]::new($bodyPath)
try {
    if ($body.Width -ne 560 -or $body.Height -ne 480) {
        throw 'Matter Annihilation Reactor body PNG must be exactly 560x480.'
    }
    $touchesFloor = $false
    for ($y = $body.Height - 1; $y -ge [Math]::Max(0, $body.Height - 3); $y--) {
        for ($x = 0; $x -lt $body.Width; $x++) {
            if ($body.GetPixel($x, $y).A -gt 8) {
                $touchesFloor = $true
                break
            }
        }
        if ($touchesFloor) { break }
    }
    if (-not $touchesFloor) {
        throw 'Matter Annihilation Reactor body art must touch the floor within its bottom three rows.'
    }
}
finally {
    $body.Dispose()
}

$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\AnnihilationReactor\MatterAnnihilationReactorConfig.cs'
$config = Get-Content -LiteralPath $configPath -Raw
if ($config -notmatch 'baiye_matter_annihilation_reactor_kanim') {
    throw 'Matter Annihilation Reactor config must reference baiye_matter_annihilation_reactor_kanim.'
}

Write-Host 'Matter Annihilation Reactor asset source contract tests passed.'
