[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifestPath = Join-Path $projectRoot 'assets\animation-manifest.json'
$tool = Join-Path $projectRoot 'tools\kanimal-cli.exe'
$output = Join-Path $projectRoot 'packaging\anim'

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
}

if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $output | Out-Null

foreach ($name in $manifest.PSObject.Properties.Name) {
    if (-not $sourceFolders.ContainsKey($name)) {
        throw "No SCML source folder is mapped for '$name'."
    }

    $scml = Join-Path $projectRoot ("assets\scml\{0}\{1}.scml" -f $sourceFolders[$name], $name)
    if (-not (Test-Path -LiteralPath $scml -PathType Leaf)) {
        throw "SCML source is missing for '$name': '$scml'."
    }

    & $tool kanim $scml -o $output
    if ($LASTEXITCODE -ne 0) {
        throw "kanimal failed for '$name' with exit code $LASTEXITCODE."
    }

    foreach ($suffix in @('.png', '_anim.bytes', '_build.bytes')) {
        $built = Join-Path $output ($name + $suffix)
        if (-not (Test-Path -LiteralPath $built -PathType Leaf)) {
            throw "kanimal did not produce '$built'."
        }
    }
}

Write-Host "Built original KAnim assets: $output"
