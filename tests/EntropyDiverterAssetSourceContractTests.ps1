[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$manifestPath = Join-Path $ProjectRoot 'assets\animation-manifest.json'
$sourceDir = Join-Path $ProjectRoot 'assets\scml\entropy_flux_diverter'
$scmlPath = Join-Path $sourceDir 'baiye_entropy_flux_diverter.scml'
$bodyPath = Join-Path $sourceDir 'baiye_entropy_flux_diverter_0.png'
$uiPath = Join-Path $sourceDir 'ui_0.png'
$bodyEncodedPath = "$bodyPath.b64"
$uiEncodedPath = "$uiPath.b64"
foreach ($path in @($manifestPath, $scmlPath, $bodyPath, $uiPath, $bodyEncodedPath, $uiEncodedPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Entropy Flux Diverter asset source is missing: '$path'."
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$requiredAnimations = @('off', 'idle', 'working', 'blocked', 'ui')
$actualAnimations = @($manifest.baiye_entropy_flux_diverter)
if (($actualAnimations -join '|') -ne ($requiredAnimations -join '|')) {
    throw "Entropy Flux Diverter manifest animations differ. Expected '$($requiredAnimations -join ', ')'."
}

[xml]$scml = Get-Content -LiteralPath $scmlPath -Raw
if ($scml.spriter_data.entity.name -ne 'baiye_entropy_flux_diverter') {
    throw 'Entropy Flux Diverter SCML entity must be baiye_entropy_flux_diverter.'
}
$animations = @($scml.spriter_data.entity.animation)
foreach ($animation in $requiredAnimations) {
    if (@($animations | Where-Object { $_.name -eq $animation }).Count -ne 1) {
        throw "Entropy Flux Diverter SCML is missing animation '$animation'."
    }
}
$files = @($scml.spriter_data.folder.file)
$bodyFile = @($files | Where-Object { $_.name -eq 'baiye_entropy_flux_diverter_0.png' })
$uiFile = @($files | Where-Object { $_.name -eq 'ui_0.png' })
if ($bodyFile.Count -ne 1 -or [int]$bodyFile[0].width -ne 400 -or
        [int]$bodyFile[0].height -ne 400 -or [double]$bodyFile[0].pivot_y -ne 0.0) {
    throw 'Entropy Flux Diverter body sprite must be a 400x400 bottom-anchored source.'
}
if ($uiFile.Count -ne 1 -or [int]$uiFile[0].width -ne 128 -or [int]$uiFile[0].height -ne 128) {
    throw 'Entropy Flux Diverter UI sprite must be a dedicated 128x128 source.'
}
$uiAnimation = @($animations | Where-Object { $_.name -eq 'ui' })[0]
if (@($uiAnimation.timeline | Where-Object { $_.name -eq 'ui_0' }).Count -ne 1) {
    throw 'Entropy Flux Diverter UI animation must expose the ui_0 timeline.'
}

$body = [System.Drawing.Bitmap]::new($bodyPath)
try {
    if ($body.Width -ne 400 -or $body.Height -ne 400) {
        throw 'Entropy Flux Diverter body PNG must be exactly 400x400.'
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
        throw 'Entropy Flux Diverter body art must touch the floor within its bottom three rows.'
    }
}
finally {
    $body.Dispose()
}

$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\EntropyDiverter\EntropyFluxDiverterConfig.cs'
$config = Get-Content -LiteralPath $configPath -Raw
if ($config -notmatch 'baiye_entropy_flux_diverter_kanim') {
    throw 'Entropy Flux Diverter config must reference baiye_entropy_flux_diverter_kanim.'
}

Write-Host 'Entropy Flux Diverter asset source contract tests passed.'
