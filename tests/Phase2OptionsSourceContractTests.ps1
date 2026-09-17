[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$optionsPath = Join-Path $ProjectRoot 'src\Game\Options\ForbiddenTechOptions.cs'
if (-not (Test-Path -LiteralPath $optionsPath -PathType Leaf)) {
    throw "Options source is missing: '$optionsPath'."
}

$content = Get-Content -LiteralPath $optionsPath -Raw
$contracts = [ordered]@{
    '物质重构器开关' = 'public\s+bool\s+ReconstructorEnabled\s*\{'
    '熵流偏转器开关' = 'public\s+bool\s+EntropyDiverterEnabled\s*\{'
    '物质湮灭堆开关' = 'public\s+bool\s+AnnihilationReactorEnabled\s*\{'
    '物质重构器 RawOptions 映射' = 'ReconstructorEnabled\s*=\s*ReconstructorEnabled'
    '熵流偏转器 RawOptions 映射' = 'EntropyDiverterEnabled\s*=\s*EntropyDiverterEnabled'
    '物质湮灭堆 RawOptions 映射' = 'AnnihilationReactorEnabled\s*=\s*AnnihilationReactorEnabled'
}

foreach ($entry in $contracts.GetEnumerator()) {
    if ($content -notmatch $entry.Value) {
        throw "ForbiddenTechOptions is missing phase-2 contract: $($entry.Key)."
    }
}

foreach ($label in @('启用物质重构器', '启用熵流偏转器', '启用物质湮灭堆')) {
    if ($content -notmatch [regex]::Escape($label)) {
        throw "ForbiddenTechOptions must expose Chinese option label '$label'."
    }
}

Write-Host 'Phase-2 options source contract tests passed.'
