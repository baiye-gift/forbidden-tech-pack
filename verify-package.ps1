[CmdletBinding()]
param(
    [string]$PackagePath = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'dist\ForbiddenTechnologyPack')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not (Test-Path -LiteralPath $PackagePath -PathType Container)) {
    throw "Package directory was not found: '$PackagePath'."
}

$dlls = @(Get-ChildItem -LiteralPath $PackagePath -Filter '*.dll' -File -Recurse)
if ($dlls.Count -ne 1 -or $dlls[0].Name -ne 'ForbiddenTechnologyPack.dll') {
    throw "Expected exactly one merged ForbiddenTechnologyPack.dll, found: $($dlls.Name -join ', ')."
}

foreach ($metadataFile in @('mod.yaml', 'mod_info.yaml')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PackagePath $metadataFile) -PathType Leaf)) {
        throw "Package metadata is missing: '$metadataFile'."
    }
}

if (Get-ChildItem -LiteralPath $PackagePath -Filter 'PLib.dll' -File -Recurse) {
    throw 'Package must not contain a standalone PLib.dll.'
}

$manifestPath = Join-Path $projectRoot 'assets\animation-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Animation manifest is missing: '$manifestPath'."
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$animDirectory = Join-Path $PackagePath 'anim'
if (-not (Test-Path -LiteralPath $animDirectory -PathType Container)) {
    throw "Package animation directory is missing: '$animDirectory'."
}

foreach ($name in $manifest.PSObject.Properties.Name) {
    $animationPackage = Join-Path $animDirectory ("forbidden_technology\$name")
    if (-not (Test-Path -LiteralPath $animationPackage -PathType Container)) {
        throw "Package is missing loadable KAnim directory 'anim\forbidden_technology\$name'."
    }
    foreach ($suffix in @('.png', '_anim.bytes', '_build.bytes')) {
        $assetPath = Join-Path $animationPackage ($name + $suffix)
        if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
            throw "Package is missing required KAnim asset '$($name + $suffix)'."
        }
    }
}

if (Get-ChildItem -LiteralPath $animDirectory -File) {
    throw 'Package contains KAnim files directly in anim; ONI only scans anim/<group>/<resource>/ directories.'
}

$baseGameAnimationNames = @(
    'supermaterial_refinery_kanim',
    'rockrefinery_kanim',
    'tungsten_kanim'
)
foreach ($assetFile in Get-ChildItem -LiteralPath $animDirectory -File -Recurse) {
    foreach ($baseName in $baseGameAnimationNames) {
        if ($assetFile.Name -like "*$baseName*") {
            throw "Package contains a base-game animation filename: '$($assetFile.Name)'."
        }
    }
}

foreach ($language in @('en', 'zh')) {
    $translation = Join-Path $PackagePath ("translations\$language.po")
    if (-not (Test-Path -LiteralPath $translation -PathType Leaf)) {
        throw "Package translation is missing: '$language.po'."
    }
}

$textFiles = Get-ChildItem -LiteralPath $PackagePath -File -Recurse | Where-Object { $_.Extension -in '.yaml', '.json', '.po', '.txt' }
foreach ($textFile in $textFiles) {
    $content = Get-Content -LiteralPath $textFile.FullName -Raw
    if ($content -match '(?i)([a-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+|//[^/]+/[^/]+|/users/|/home/)') {
        throw "Package text file contains an absolute local path: '$($textFile.FullName)'."
    }
}

Write-Host "Package verification passed: $PackagePath"
