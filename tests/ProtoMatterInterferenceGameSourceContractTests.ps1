[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$devicePath = Join-Path $ProjectRoot 'src\Game\Buildings\Common\ForbiddenTechDevice.cs'
$managerPath = Join-Path $ProjectRoot 'src\Game\Buildings\Common\ProtoMatterInterferenceManager.cs'
$configPaths = @(
    (Join-Path $ProjectRoot 'src\Game\Buildings\Analyzer\MatterAnalyzerConfig.cs'),
    (Join-Path $ProjectRoot 'src\Game\Buildings\Crusher\MassCrusherConfig.cs'),
    (Join-Path $ProjectRoot 'src\Game\Buildings\Compiler\MatterCompilerConfig.cs')
)

foreach ($path in @($devicePath, $managerPath) + $configPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Phase-2 interference integration source is missing: '$path'."
    }
}

$device = Get-Content -LiteralPath $devicePath -Raw
$manager = Get-Content -LiteralPath $managerPath -Raw

if ($device -notmatch 'Operational\.Flag\s+ProtoMatterStable' -or
        $device -notmatch 'Operational\.Flag\.Type\.Requirement') {
    throw 'ForbiddenTechDevice must expose a requirement Operational flag for proto-matter stability.'
}
if ($device -notmatch 'HashSet<string>\s+activeSources' -or
        $device -notmatch 'SetInterferenceSource\s*\(') {
    throw 'ForbiddenTechDevice must track interference sources independently.'
}
if ($device -notmatch 'ProtoMatterInterferenceManager\.Register\(this\)' -or
        $device -notmatch 'ProtoMatterInterferenceManager\.Unregister\(this\)') {
    throw 'ForbiddenTechDevice must register and unregister with the interference manager.'
}
if ($device -notmatch 'operational\.SetFlag\(ProtoMatterStable,\s*stable\)') {
    throw 'ForbiddenTechDevice must pause through Operational instead of patching fabricator execution.'
}

if ($manager -notmatch 'HashSet<ForbiddenTechDevice>\s+devices' -or
        $manager -notmatch 'ApplySource\s*\(' -or
        $manager -notmatch 'RemoveSource\s*\(') {
    throw 'ProtoMatterInterferenceManager must keep a registered device set and source lifecycle API.'
}
if ($manager -match 'FindObjectsOfType' -or $manager -match 'Update\s*\(' -or $manager -match 'Sim1000ms') {
    throw 'Interference must be event-driven and must not scan all buildings every frame/tick.'
}
if ($manager -notmatch 'Grid\.WorldIdx' -or $manager -notmatch 'ProtoMatterInterferencePolicy\.IsInRange') {
    throw 'Interference application must stay world-local and reuse the tested range policy.'
}

foreach ($configPath in $configPaths) {
    $config = Get-Content -LiteralPath $configPath -Raw
    if ($config -notmatch 'AddOrGet<ForbiddenTechDevice>\s*\(\s*\)') {
        throw "Forbidden building config is not connected to ForbiddenTechDevice: '$configPath'."
    }
}

Write-Host 'Proto-matter interference game source contract tests passed.'
