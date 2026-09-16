[CmdletBinding()]
param(
    [string]$GamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $projectRoot 'build.ps1'
$verifyScript = Join-Path $projectRoot 'verify-package.ps1'
$packageDirectory = Join-Path $projectRoot 'dist\ForbiddenTechnologyPack'
$releaseDirectory = Join-Path $projectRoot 'release'
$zipPath = Join-Path $releaseDirectory 'ForbiddenTechnologyPack-0.1.0.zip'
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('forbidden-tech-release-stage-' + [guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('forbidden-tech-release-verify-' + [guid]::NewGuid().ToString('N'))

try {
    & $buildScript -GamePath $GamePath
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }

    & $verifyScript -PackagePath $packageDirectory

    if (Test-Path -LiteralPath $releaseDirectory) {
        Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null

    $stagedPackage = Join-Path $stagingRoot 'ForbiddenTechnologyPack'
    Copy-Item -LiteralPath $packageDirectory -Destination $stagedPackage -Recurse -Force
    Compress-Archive -LiteralPath $stagedPackage -DestinationPath $zipPath -CompressionLevel Optimal

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
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
}
