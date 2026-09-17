[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$manifestPath = Join-Path $ProjectRoot 'assets\animation-manifest.json'
$sourceDir = Join-Path $ProjectRoot 'assets\scml\matter_reconstructor'
$scmlPath = Join-Path $sourceDir 'baiye_matter_reconstructor.scml'
$bodyPath = Join-Path $sourceDir 'baiye_matter_reconstructor_0.png'
$uiPath = Join-Path $sourceDir 'ui_0.png'
foreach ($path in @($manifestPath, $scmlPath, $bodyPath, $uiPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Matter Reconstructor asset source is missing: '$path'."
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$requiredAnimations = @('off', 'idle', 'working_pre', 'working_loop', 'working_pst', 'working_pst_complete', 'blocked', 'ui')
$actualAnimations = @($manifest.baiye_matter_reconstructor)
if (($actualAnimations -join '|') -ne ($requiredAnimations -join '|')) {
    throw "Matter Reconstructor manifest animations differ. Expected '$($requiredAnimations -join ', ')'."
}

[xml]$scml = Get-Content -LiteralPath $scmlPath -Raw
if ($scml.spriter_data.entity.name -ne 'baiye_matter_reconstructor') {
    throw 'Matter Reconstructor SCML entity must be baiye_matter_reconstructor.'
}
$animations = @($scml.spriter_data.entity.animation)
foreach ($animation in $requiredAnimations) {
    if (@($animations | Where-Object { $_.name -eq $animation }).Count -ne 1) {
        throw "Matter Reconstructor SCML is missing animation '$animation'."
    }
}
$files = @($scml.spriter_data.folder.file)
$bodyFile = @($files | Where-Object { $_.name -eq 'baiye_matter_reconstructor_0.png' })
$uiFile = @($files | Where-Object { $_.name -eq 'ui_0.png' })
if ($bodyFile.Count -ne 1 -or [int]$bodyFile[0].width -ne 400 -or
        [int]$bodyFile[0].height -ne 400 -or [double]$bodyFile[0].pivot_y -ne 0.0) {
    throw 'Matter Reconstructor body sprite must be a 400x400 bottom-anchored source.'
}
if ($uiFile.Count -ne 1 -or [int]$uiFile[0].width -ne 128 -or [int]$uiFile[0].height -ne 128) {
    throw 'Matter Reconstructor UI sprite must be a dedicated 128x128 source.'
}
$uiAnimation = @($animations | Where-Object { $_.name -eq 'ui' })[0]
if (@($uiAnimation.timeline | Where-Object { $_.name -eq 'ui_0' }).Count -ne 1) {
    throw 'Matter Reconstructor UI animation must expose the ui_0 timeline.'
}

$body = [System.Drawing.Bitmap]::new($bodyPath)
try {
    if ($body.Width -ne 400 -or $body.Height -ne 400) {
        throw 'Matter Reconstructor body PNG must be exactly 400x400.'
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
        throw 'Matter Reconstructor body art must touch the floor within its bottom three rows.'
    }
}
finally {
    $body.Dispose()
}

$configPath = Join-Path $ProjectRoot 'src\Game\Buildings\Reconstructor\MatterReconstructorConfig.cs'
$config = Get-Content -LiteralPath $configPath -Raw
if ($config -notmatch 'baiye_matter_reconstructor_kanim') {
    throw 'Matter Reconstructor config must reference baiye_matter_reconstructor_kanim.'
}

Write-Host 'Matter Reconstructor asset source contract tests passed.'
