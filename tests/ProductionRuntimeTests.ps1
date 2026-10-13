[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $ProjectRoot
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\production-runtime'
New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$assembly = Join-Path $probeDirectory 'ProductionRuntimeTests.exe'
$sources = @(Get-ChildItem (Join-Path $ProjectRoot 'src\Core') -Filter '*.cs' | ForEach-Object FullName)
$sources += @('tests\runtime\ProductionHost.cs',
    'src\Game\Buildings\Common\SimulationTime.cs',
    'src\Game\Buildings\Compiler\MatterCompiler.cs',
    'src\Game\Buildings\Compiler\CoolantController.cs',
    'src\Game\Buildings\MassEnergy\ProtoAnnihilationReactor.cs',
    'src\Game\Buildings\MassEnergy\EnergyMatterCondenser.cs') | ForEach-Object { Join-Path $ProjectRoot $_ }
& $compiler /nologo /target:exe /langversion:7.3 "/out:$assembly" @sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $assembly
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$paymentAssembly = Join-Path $probeDirectory 'PaymentRuntimeTests.exe'
$paymentSources = $sources + @('src\Game\Buildings\Common\FabricatorPowerBudget.cs', 'tests\runtime\PaymentBehavior.cs' | ForEach-Object { Join-Path $ProjectRoot $_ })
& $compiler /nologo /target:exe /langversion:7.3 /define:PAYMENT_HOST "/out:$paymentAssembly" @paymentSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $paymentAssembly
exit $LASTEXITCODE
