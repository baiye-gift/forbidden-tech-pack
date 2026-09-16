[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$verify = Join-Path $ProjectRoot 'verify-package.ps1'
$manifest = Get-Content -LiteralPath (Join-Path $ProjectRoot 'assets\animation-manifest.json') -Raw | ConvertFrom-Json
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('forbidden-tech-package-test-' + [guid]::NewGuid().ToString('N'))

function New-TestPackage([string]$Path) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    Set-Content -LiteralPath (Join-Path $Path 'ForbiddenTechnologyPack.dll') -Value 'test'
    Set-Content -LiteralPath (Join-Path $Path 'mod.yaml') -Value 'title: test'
    Set-Content -LiteralPath (Join-Path $Path 'mod_info.yaml') -Value 'supportedContent: ALL'
    $anim = Join-Path $Path 'anim'
    $animGroup = Join-Path $anim 'forbidden_technology'
    $translations = Join-Path $Path 'translations'
    New-Item -ItemType Directory -Force -Path $animGroup,$translations | Out-Null
    Set-Content -LiteralPath (Join-Path $translations 'en.po') -Value 'msgid "test"'
    Set-Content -LiteralPath (Join-Path $translations 'zh.po') -Value 'msgid "test"'
    foreach ($name in $manifest.PSObject.Properties.Name) {
        $animationPackage = Join-Path $animGroup $name
        New-Item -ItemType Directory -Force -Path $animationPackage | Out-Null
        foreach ($suffix in @('.png', '_anim.bytes', '_build.bytes')) {
            Set-Content -LiteralPath (Join-Path $animationPackage ($name + $suffix)) -Value 'test'
        }
    }
}

function Invoke-Verify([string]$Path) {
    try {
        $output = & $verify -PackagePath $Path 2>&1 | Out-String
        return [pscustomobject]@{ Success = $true; Output = $output }
    }
    catch {
        return [pscustomobject]@{ Success = $false; Output = ($_ | Out-String) }
    }
}

try {
    $valid = Join-Path $tempRoot 'valid'
    New-TestPackage $valid
    $result = Invoke-Verify $valid
    if (-not $result.Success) {
        throw "A complete package should pass verification. Output: $($result.Output)"
    }

    $missing = Join-Path $tempRoot 'missing'
    Copy-Item -LiteralPath $valid -Destination $missing -Recurse
    Remove-Item -LiteralPath (Join-Path $missing 'anim\forbidden_technology\baiye_matter_compiler\baiye_matter_compiler_anim.bytes') -Force
    $result = Invoke-Verify $missing
    if ($result.Success) {
        throw 'Package verification must fail when a manifest KAnim triplet member is missing.'
    }

    $borrowed = Join-Path $tempRoot 'borrowed'
    Copy-Item -LiteralPath $valid -Destination $borrowed -Recurse
    Set-Content -LiteralPath (Join-Path $borrowed 'anim\forbidden_technology\baiye_matter_analyzer\supermaterial_refinery_kanim_anim.bytes') -Value 'test'
    $result = Invoke-Verify $borrowed
    if ($result.Success) {
        throw 'Package verification must reject base-game animation filenames.'
    }

    $flat = Join-Path $tempRoot 'flat'
    Copy-Item -LiteralPath $valid -Destination $flat -Recurse
    foreach ($name in $manifest.PSObject.Properties.Name) {
        $animationPackage = Join-Path $flat ("anim\forbidden_technology\$name")
        foreach ($asset in Get-ChildItem -LiteralPath $animationPackage -File) {
            Move-Item -LiteralPath $asset.FullName -Destination (Join-Path $flat 'anim')
        }
    }
    $result = Invoke-Verify $flat
    if ($result.Success) {
        throw 'Package verification must reject KAnim files placed directly in the anim root.'
    }

    Write-Host 'Package asset verification tests passed.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
