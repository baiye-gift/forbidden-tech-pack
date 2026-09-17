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

function Invoke-TestRunner {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = @(& pwsh -NoLogo -NoProfile -File $testRunnerPath @Arguments 2>&1)
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = (($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine)
    }
}

function Get-NormalizedNames {
    param([object[]]$Names)

    return @(($Names | ForEach-Object { [string]$_ }) | Sort-Object -Unique)
}

function Require-SameNames {
    param(
        [object[]]$Actual,
        [object[]]$Expected,
        [string]$Message
    )

    $actualNames = @(Get-NormalizedNames -Names $Actual)
    $expectedNames = @(Get-NormalizedNames -Names $Expected)
    if (($actualNames -join '|') -ne ($expectedNames -join '|')) {
        throw "$Message Expected '$($expectedNames -join ', ')', got '$($actualNames -join ', ')'."
    }
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
$testRunnerAst = Get-PowerShellAst -Path $testRunnerPath
$testRunnerParameters = @($testRunnerAst.ParamBlock.Parameters | ForEach-Object {
    $_.Name.VariablePath.UserPath
})
if ($testRunnerParameters -notcontains 'GamePath') {
    $portabilityFailures.Add('test.ps1 must accept an explicit GamePath for game integration suites.')
}

if ($portabilityFailures.Count -gt 0) {
    throw ($portabilityFailures -join [Environment]::NewLine)
}

$testScripts = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'tests') -Filter '*.ps1' -File)
$scriptContracts = @($testScripts | ForEach-Object {
    $ast = Get-PowerShellAst -Path $_.FullName
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $parameters = @($ast.ParamBlock.Parameters | ForEach-Object {
        $_.Name.VariablePath.UserPath
    })
    [pscustomobject]@{
        Name = $_.BaseName
        Path = $_.FullName
        AcceptsGamePath = $parameters -contains 'GamePath'
        RequiresGame = $_.BaseName -ne 'ReleaseWorkflowContractTests' -and
            $content -match '(?i)(OxygenNotIncluded_Data[\\/]Managed|Assembly-CSharp(?:-firstpass)?\.dll)'
    }
})

$catalogResult = Invoke-TestRunner -Arguments @('-DescribeSuites')
if ($catalogResult.ExitCode -ne 0) {
    throw "test.ps1 must expose its declarative suite catalog. Output: $($catalogResult.Output)"
}
$catalog = @($catalogResult.Output | ConvertFrom-Json)
Require-SameNames -Actual $catalog.Name -Expected $scriptContracts.Name `
    -Message 'The suite catalog must contain every PowerShell test script exactly once.'
foreach ($scriptContract in $scriptContracts) {
    $entry = @($catalog | Where-Object { $_.Name -eq $scriptContract.Name })
    if ($entry.Count -ne 1) {
        throw "Suite '$($scriptContract.Name)' must have exactly one catalog entry."
    }
    if ($scriptContract.RequiresGame -ne $scriptContract.AcceptsGamePath) {
        throw "Suite '$($scriptContract.Name)' must accept GamePath exactly when it uses game assemblies."
    }
    if ([bool]$entry[0].RequiresGame -ne $scriptContract.RequiresGame) {
        throw "Suite '$($scriptContract.Name)' catalog classification must match its assembly dependency."
    }
    if (-not [string]::Equals(
            [System.IO.Path]::GetFullPath([string]$entry[0].Path),
            [System.IO.Path]::GetFullPath($scriptContract.Path),
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Suite '$($scriptContract.Name)' must dispatch its matching test script."
    }
}

$portableResult = Invoke-TestRunner -Arguments @('-Suite', 'Portable', '-DescribePlan')
if ($portableResult.ExitCode -ne 0) {
    throw "Portable plan must be available without GamePath. Output: $($portableResult.Output)"
}
$portablePlan = $portableResult.Output | ConvertFrom-Json
$portableExpected = @($catalog | Where-Object { -not $_.RequiresGame } | ForEach-Object { $_.Name })
Require-SameNames -Actual $portablePlan.PowerShellSuites.Name -Expected $portableExpected `
    -Message 'Portable must dispatch every and only game-independent PowerShell suite.'
if (@($portablePlan.PowerShellSuites).Count -ne $portableExpected.Count) {
    throw 'Portable must dispatch each game-independent PowerShell suite exactly once.'
}
if (@($portablePlan.PowerShellSuites | Where-Object { $_.RequiresGame }).Count -ne 0) {
    throw 'Portable must not dispatch a game-backed suite.'
}
if (@($portablePlan.PowerShellSuites | Where-Object { $_.Name -eq 'ReleaseWorkflowContractTests' }).Count -ne 1) {
    throw 'ReleaseWorkflowContractTests must remain in Portable.'
}

$missingGamePathResult = Invoke-TestRunner -Arguments @('-Suite', 'All', '-DescribePlan')
if ($missingGamePathResult.ExitCode -eq 0 -or
        $missingGamePathResult.Output -notmatch [regex]::Escape('-GamePath is required')) {
    throw 'All must fail explicitly when GamePath is omitted.'
}

$fakeGamePath = Join-Path ([System.IO.Path]::GetTempPath()) `
    ('forbidden-tech-suite-plan-' + [guid]::NewGuid().ToString('N'))
try {
    $fakeManagedDirectory = Join-Path $fakeGamePath 'OxygenNotIncluded_Data\Managed'
    New-Item -ItemType Directory -Force -Path $fakeManagedDirectory | Out-Null
    New-Item -ItemType File -Force -Path (Join-Path $fakeManagedDirectory 'Assembly-CSharp.dll') | Out-Null

    $allResult = Invoke-TestRunner -Arguments @(
        '-Suite', 'All', '-GamePath', $fakeGamePath, '-DescribePlan')
    if ($allResult.ExitCode -ne 0) {
        throw "All dispatch plan must accept an explicit GamePath. Output: $($allResult.Output)"
    }
    $allPlan = $allResult.Output | ConvertFrom-Json
    Require-SameNames -Actual $allPlan.PowerShellSuites.Name -Expected $catalog.Name `
        -Message 'All must dispatch every PowerShell suite.'
    if (@($allPlan.PowerShellSuites).Count -ne $catalog.Count) {
        throw 'All must dispatch each PowerShell suite exactly once.'
    }

    foreach ($invocation in @($allPlan.PowerShellSuites)) {
        $arguments = @($invocation.Arguments | ForEach-Object { [string]$_ })
        $fileIndex = [Array]::IndexOf($arguments, '-File')
        $expectedScript = @($scriptContracts | Where-Object { $_.Name -eq $invocation.Name })[0]
        if ($fileIndex -lt 0 -or $fileIndex + 1 -ge $arguments.Count -or
                -not [string]::Equals(
                    [System.IO.Path]::GetFullPath($arguments[$fileIndex + 1]),
                    [System.IO.Path]::GetFullPath($expectedScript.Path),
                    [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "All must dispatch suite '$($invocation.Name)' to its matching test script."
        }
        $gamePathIndex = [Array]::IndexOf($arguments, '-GamePath')
        if ($invocation.RequiresGame) {
            if ($gamePathIndex -lt 0 -or $gamePathIndex + 1 -ge $arguments.Count -or
                    $arguments[$gamePathIndex + 1] -ne $fakeGamePath) {
                throw "All must dispatch game-backed suite '$($invocation.Name)' with the explicit GamePath."
            }
        } elseif ($gamePathIndex -ge 0) {
            throw "Game-independent suite '$($invocation.Name)' must not receive GamePath."
        }
    }
} finally {
    Remove-Item -LiteralPath $fakeGamePath -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Release workflow contract tests passed.'
