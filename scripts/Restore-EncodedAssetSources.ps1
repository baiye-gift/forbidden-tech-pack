[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$assetRoot = Join-Path $ProjectRoot 'assets\scml'
if (-not (Test-Path -LiteralPath $assetRoot -PathType Container)) {
    throw "Asset source root was not found: '$assetRoot'."
}

$encodedSources = @(Get-ChildItem -LiteralPath $assetRoot -Filter '*.png.b64' -Recurse -File -ErrorAction Stop)
foreach ($encodedSource in $encodedSources) {
    $targetPath = $encodedSource.FullName.Substring(0, $encodedSource.FullName.Length - 4)
    $payload = (Get-Content -LiteralPath $encodedSource.FullName -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($payload)) {
        throw "Encoded asset source is empty: '$($encodedSource.FullName)'."
    }

    try {
        $bytes = [Convert]::FromBase64String($payload)
    }
    catch {
        throw "Encoded asset source is not valid Base64: '$($encodedSource.FullName)'."
    }

    if ($bytes.Length -lt 8 -or
        $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47 -or
        $bytes[4] -ne 0x0D -or $bytes[5] -ne 0x0A -or $bytes[6] -ne 0x1A -or $bytes[7] -ne 0x0A) {
        throw "Encoded asset source is not a PNG payload: '$($encodedSource.FullName)'."
    }

    [IO.File]::WriteAllBytes($targetPath, $bytes)
}

if ($encodedSources.Count -gt 0) {
    Write-Host "Restored $($encodedSources.Count) encoded PNG source asset(s)."
}
