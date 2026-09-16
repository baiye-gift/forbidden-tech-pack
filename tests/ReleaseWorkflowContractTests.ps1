[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

foreach ($relative in @('README.md', 'CHANGELOG.md', 'docs\test-matrix.md', 'pack-release.ps1')) {
    $path = Join-Path $ProjectRoot $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Task 12 release file is missing: '$relative'."
    }
}

$install = Get-Content -LiteralPath (Join-Path $ProjectRoot 'install.ps1') -Raw
if ($install -notmatch [regex]::Escape("Klei\OxygenNotIncluded\mods\local")) {
    throw 'install.ps1 must install to the local mod directory.'
}
if ($install -match [regex]::Escape("Klei\OxygenNotIncluded\mods\Dev")) {
    throw 'install.ps1 must not default to the Dev mod directory.'
}

$release = Get-Content -LiteralPath (Join-Path $ProjectRoot 'pack-release.ps1') -Raw
foreach ($required in @('build.ps1', 'verify-package.ps1', 'ForbiddenTechnologyPack-0.1.0.zip', 'Get-FileHash', 'Expand-Archive')) {
    if ($release -notmatch [regex]::Escape($required)) {
        throw "pack-release.ps1 is missing required release behavior '$required'."
    }
}

$readme = Get-Content -LiteralPath (Join-Path $ProjectRoot 'README.md') -Raw
foreach ($topic in @('Matter Analyzer', 'Mass Crusher', 'Matter Compiler', 'Safe Removal')) {
    if ($readme -notmatch [regex]::Escape($topic)) {
        throw "README.md is missing public topic '$topic'."
    }
}

$matrix = Get-Content -LiteralPath (Join-Path $ProjectRoot 'docs\test-matrix.md') -Raw
foreach ($section in @('Content mode', 'Building behavior', 'Configuration and persistence', 'Safe removal', 'Performance soak')) {
    if ($matrix -notmatch [regex]::Escape($section)) {
        throw "docs/test-matrix.md is missing verification section '$section'."
    }
}
if ($matrix -notmatch 'PENDING') {
    throw 'Manual in-game checks must remain explicitly PENDING until they are executed.'
}

Write-Host 'Release workflow contract tests passed.'
