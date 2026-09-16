[CmdletBinding()]
param(
    [string]$GamePath,
    [string]$DestinationPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$verifyPackage = Join-Path $projectRoot 'verify-package.ps1'
$modsDirectory = if ($DestinationPath) {
    $DestinationPath
} else {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)) 'Klei\OxygenNotIncluded\mods\Dev'
}
$installDirectory = Join-Path $modsDirectory 'ForbiddenTechnologyPack'

if ($GamePath) {
    $managedDirectory = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed'
    if (-not (Test-Path -LiteralPath $managedDirectory -PathType Container)) {
        throw "GamePath does not look like an Oxygen Not Included installation: '$GamePath'."
    }
}

if (-not (Test-Path -LiteralPath $packageDirectory)) {
    throw "Package was not found at '$packageDirectory'. Run .\\build.ps1 first."
}

& $verifyPackage -PackagePath $packageDirectory
New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
Copy-Item -Path (Join-Path $packageDirectory '*') -Destination $installDirectory -Recurse -Force
Write-Host "Installed package: $installDirectory"
