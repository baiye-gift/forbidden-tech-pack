[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifestPath = Join-Path $projectRoot 'assets\animation-manifest.json'
$tool = Join-Path $projectRoot 'tools\kanimal-cli.exe'
$output = Join-Path $projectRoot 'packaging\anim'
$outputGroup = Join-Path $output 'forbidden_technology'

foreach ($required in @($manifestPath, $tool)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required asset input is missing: '$required'."
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

$sourceFolders = @{
    'baiye_proto_matter' = 'proto_matter'
    'baiye_matter_analyzer' = 'matter_analyzer'
    'baiye_mass_crusher' = 'mass_crusher'
    'baiye_matter_compiler' = 'matter_compiler'
    'baiye_proto_annihilation_reactor' = 'proto_annihilation_reactor'
    'baiye_energy_matter_condenser' = 'energy_matter_condenser'
}

if (Test-Path -LiteralPath $output) {
    $resolvedOutput = [IO.Path]::GetFullPath($output)
    $expectedOutput = [IO.Path]::GetFullPath((Join-Path $projectRoot 'packaging\anim'))
    if ($resolvedOutput -ne $expectedOutput) { throw 'Unexpected animation output path.' }
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outputGroup | Out-Null

foreach ($name in $manifest.PSObject.Properties.Name) {

    $scml = Join-Path $projectRoot ("assets\scml\{0}\{1}.scml" -f $sourceFolders[$name], $name)
    if (-not (Test-Path -LiteralPath $scml -PathType Leaf)) {
        throw "SCML source missing: $scml"
    }

    $animationPackage = Join-Path $outputGroup $name

    New-Item -ItemType Directory -Force -Path $animationPackage | Out-Null

    # Pinned kanimal targets .NET 3.1. Use an installed newer major runtime too.
    $previousRollForward = $env:DOTNET_ROLL_FORWARD
    try {
        $env:DOTNET_ROLL_FORWARD = 'Major'
        & $tool kanim $scml -o $animationPackage
    } finally {
        $env:DOTNET_ROLL_FORWARD = $previousRollForward
    }

    if ($LASTEXITCODE -ne 0) {
        throw "kanimal failed for '$name'"
    }

    foreach ($suffix in @('.png','_anim.bytes','_build.bytes')) {
        $built = Join-Path $animationPackage ($name + $suffix)
        if (-not (Test-Path -LiteralPath $built -PathType Leaf)) {
            throw "kanimal did not produce '$built'."
        }
    }
}

Write-Host "Built original KAnim assets: $output"
