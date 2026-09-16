[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$modsDirectory = Join-Path $GamePath 'mods\Dev'
$installDirectory = Join-Path $modsDirectory 'ForbiddenTechnologyPack'

if (-not (Test-Path -LiteralPath $packageDirectory)) {
    throw "Package was not found at '$packageDirectory'. Run .\\build.ps1 first."
}

New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
Copy-Item -Path (Join-Path $packageDirectory '*') -Destination $installDirectory -Recurse -Force
Write-Host "Installed package: $installDirectory"
