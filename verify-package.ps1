[CmdletBinding()]
param(
    [string]$PackagePath = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'dist\ForbiddenTechnologyPack')
)

$ErrorActionPreference = 'Stop'
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

$textFiles = Get-ChildItem -LiteralPath $PackagePath -File -Recurse | Where-Object { $_.Extension -in '.yaml', '.json', '.po', '.txt' }
foreach ($textFile in $textFiles) {
    $content = Get-Content -LiteralPath $textFile.FullName -Raw
    if ($content -match '(?i)([a-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+|//[^/]+/[^/]+|/users/|/home/)') {
        throw "Package text file contains an absolute local path: '$($textFile.FullName)'."
    }
}

Write-Host "Package verification passed: $PackagePath"
