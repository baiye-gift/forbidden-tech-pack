[CmdletBinding()]
param(
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$verifyPackage = Join-Path $projectRoot 'verify-package.ps1'
$localModsDirectory = Join-Path $env:USERPROFILE 'Documents\Klei\OxygenNotIncluded\mods\local'
$installDirectory = Join-Path $localModsDirectory 'ForbiddenTechnologyPack'

if ($GamePath) {
    $managedDirectory = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed'
    if (-not (Test-Path -LiteralPath $managedDirectory -PathType Container)) {
        throw "GamePath does not look like an Oxygen Not Included installation: '$GamePath'."
    }
}

if (-not (Test-Path -LiteralPath $packageDirectory -PathType Container)) {
    throw "Package was not found at '$packageDirectory'. Run .\\build.ps1 first."
}

& $verifyPackage -PackagePath $packageDirectory

$expectedInstall = [System.IO.Path]::GetFullPath((Join-Path $env:USERPROFILE 'Documents\Klei\OxygenNotIncluded\mods\local\ForbiddenTechnologyPack'))
$resolvedInstall = [System.IO.Path]::GetFullPath($installDirectory)
if (-not $resolvedInstall.Equals($expectedInstall, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to install outside the exact local mod directory: '$resolvedInstall'."
}

New-Item -ItemType Directory -Force -Path $localModsDirectory | Out-Null
if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}
Copy-Item -LiteralPath $packageDirectory -Destination $installDirectory -Recurse -Force
Write-Host "Installed package: $installDirectory"
