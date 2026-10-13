[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $projectRoot 'build.ps1'
$verifyScript = Join-Path $projectRoot 'verify-package.ps1'
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$releaseDirectory = Join-Path $projectRoot 'release'
$metadata = Get-Content -LiteralPath (Join-Path $projectRoot 'packaging\mod_info.yaml')
$versionLine = $metadata | Where-Object { $_ -match '^version:\s*(\S+)\s*$' } | Select-Object -First 1
if ($versionLine -notmatch '^version:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$') { throw 'Missing package version.' }
$zipPath = Join-Path $releaseDirectory ("ForbiddenTechnologyPack-{0}.zip" -f $Matches[1])
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('forbidden-tech-release-stage-' + [guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('forbidden-tech-release-verify-' + [guid]::NewGuid().ToString('N'))

try {
    & $buildScript -GamePath $GamePath
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }

    & $verifyScript -PackagePath $packageDirectory

    New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null

    $stagedPackage = Join-Path $stagingRoot 'ForbiddenTechnologyPack'
    Copy-Item -LiteralPath $packageDirectory -Destination $stagedPackage -Recurse -Force
    Compress-Archive -LiteralPath $stagedPackage -DestinationPath $zipPath -CompressionLevel Optimal -Force

    $hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
    Write-Host "Release ZIP: $zipPath"
    Write-Host "SHA256: $($hash.Hash)"

    New-Item -ItemType Directory -Force -Path $extractRoot | Out-Null
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force
    $extractedPackage = Join-Path $extractRoot 'ForbiddenTechnologyPack'
    & $verifyScript -PackagePath $extractedPackage

    Write-Host "Verified extracted release package: $extractedPackage"
}
finally {
    foreach ($cleanupPath in @($stagingRoot, $extractRoot)) {
        $resolvedCleanup = [IO.Path]::GetFullPath($cleanupPath)
        $temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
        if (!$resolvedCleanup.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or
            !(Split-Path -Leaf $resolvedCleanup).StartsWith('forbidden-tech-release-')) { throw 'Unexpected temporary cleanup path.' }
    }
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
}
