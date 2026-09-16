[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$managedDirectory = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed'
$plib = Join-Path $projectRoot 'lib\PLib.dll'
$sourceDirectory = Join-Path $projectRoot 'src'
$objDirectory = Join-Path $projectRoot 'obj'
$rawAssembly = Join-Path $objDirectory 'ForbiddenTechnologyPack.raw.dll'
$toolDirectory = Join-Path $projectRoot 'tools'
$ilRepack = Join-Path $toolDirectory 'ILRepack.exe'
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$packageAssembly = Join-Path $packageDirectory 'ForbiddenTechnologyPack.dll'
. (Join-Path $projectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $projectRoot

foreach ($requiredPath in @($managedDirectory, $plib, $ilRepack)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required build input was not found: '$requiredPath'. Run .\\restore-deps.ps1 before building."
    }
}

& (Join-Path $projectRoot 'build-assets.ps1')
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$sources = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' -Recurse -File | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) {
    throw 'No C# sources were found under src.'
}

$referenceNames = @(
    'Assembly-CSharp.dll',
    'Assembly-CSharp-firstpass.dll',
    '0Harmony.dll',
    'Newtonsoft.Json.dll',
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.UI.dll',
    'netstandard.dll'
)
$references = @($plib)
foreach ($referenceName in $referenceNames) {
    $referencePath = Join-Path $managedDirectory $referenceName
    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Required game assembly was not found: '$referencePath'."
    }
    $references += $referencePath
}

New-Item -ItemType Directory -Force -Path $objDirectory | Out-Null
if (Test-Path -LiteralPath $packageDirectory) {
    Remove-Item -LiteralPath $packageDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null

$compilerArguments = @('/nologo', '/target:library', '/langversion:7.3', '/warn:4', "/out:$rawAssembly")
$compilerArguments += $references | ForEach-Object { "/reference:$_" }
$compilerArguments += $sources
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $ilRepack "/out:$packageAssembly" "/lib:$managedDirectory" $rawAssembly $plib
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Copy-Item -LiteralPath (Join-Path $projectRoot 'packaging\mod.yaml') -Destination $packageDirectory
Copy-Item -LiteralPath (Join-Path $projectRoot 'packaging\mod_info.yaml') -Destination $packageDirectory
foreach ($assetDirectory in @('elements', 'anim', 'translations')) {
    $assetSource = Join-Path (Join-Path $projectRoot 'packaging') $assetDirectory
    if (Test-Path -LiteralPath $assetSource) {
        Copy-Item -LiteralPath $assetSource -Destination (Join-Path $packageDirectory $assetDirectory) -Recurse -Force
    }
}

Write-Host "Built package: $packageDirectory"
