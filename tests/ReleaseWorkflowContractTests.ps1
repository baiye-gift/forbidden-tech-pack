[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

function Get-PowerShellAst {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $tokens = $null
    $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile(
        $Path,
        [ref]$tokens,
        [ref]$errors)
    if ($errors.Count -gt 0) {
        throw "PowerShell script '$Path' has parse errors: $($errors -join '; ')"
    }
    return $ast
}

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

$portabilityFailures = [System.Collections.Generic.List[string]]::new()
$workflowPath = Join-Path $ProjectRoot '.github\workflows\feature-verification.yml'
$workflow = Get-Content -LiteralPath $workflowPath -Raw
if ($workflow -notmatch '(?m)^\s*run:\s*\.\/test\.ps1\s+-Suite\s+Portable\s*$') {
    $portabilityFailures.Add('Hosted feature verification must run the explicit Portable suite.')
}

$repositoryScripts = @(Get-ChildItem -LiteralPath $ProjectRoot -Filter '*.ps1' -File -Recurse)
foreach ($script in $repositoryScripts) {
    $content = Get-Content -LiteralPath $script.FullName -Raw
    if ($content -match '(?i)[a-z]:\\steam\\') {
        $relativePath = [System.IO.Path]::GetRelativePath($ProjectRoot, $script.FullName)
        $portabilityFailures.Add("PowerShell script '$relativePath' embeds a machine-specific Steam path.")
    }
}

$testRunnerPath = Join-Path $ProjectRoot 'test.ps1'
$testRunner = Get-Content -LiteralPath $testRunnerPath -Raw
$testRunnerAst = Get-PowerShellAst -Path $testRunnerPath
$testRunnerParameters = @($testRunnerAst.ParamBlock.Parameters | ForEach-Object {
    $_.Name.VariablePath.UserPath
})
if ($testRunnerParameters -notcontains 'GamePath') {
    $portabilityFailures.Add('test.ps1 must accept an explicit GamePath for game integration suites.')
}
if ($testRunner -notmatch '(?m)^\s*''Portable''\s*=') {
    $portabilityFailures.Add('test.ps1 must define an explicit Portable suite boundary.')
}
if ($testRunner -notmatch '-GamePath\s+\$GamePath') {
    $portabilityFailures.Add('test.ps1 must forward GamePath to game-dependent child suites.')
}

$gameDependentSuites = @(
    'AnalyzerAdapterContractTests',
    'AnalyzerRecipeRuntimeTests',
    'CrusherConfigContractTests',
    'ElementCatalogRuntimeTests',
    'OptionsLocalizationRuntimeTests',
    'ResearchRegistrationRuntimeTests',
    'SafeRemovalRuntimeTests'
)
foreach ($suiteName in $gameDependentSuites) {
    if ($testRunner -notmatch [regex]::Escape("'$suiteName'")) {
        $portabilityFailures.Add("test.ps1 must explicitly route game-dependent suite '$suiteName'.")
    }

    $suitePath = Join-Path $ProjectRoot "tests\$suiteName.ps1"
    $suiteAst = Get-PowerShellAst -Path $suitePath
    $suiteParameters = @($suiteAst.ParamBlock.Parameters | ForEach-Object {
        $_.Name.VariablePath.UserPath
    })
    if ($suiteParameters -notcontains 'GamePath') {
        $portabilityFailures.Add("Game-dependent suite '$suiteName' must accept an explicit GamePath.")
    }
}

if ($portabilityFailures.Count -gt 0) {
    throw ($portabilityFailures -join [Environment]::NewLine)
}

Write-Host 'Release workflow contract tests passed.'
