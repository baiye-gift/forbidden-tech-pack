[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

function Require-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourcePath = Join-Path $ProjectRoot 'src\Game\Buildings\Crusher\MassCrusherConfig.cs'
$source = Get-Content -Raw -LiteralPath $sourcePath

Require-Condition ($source -match 'fabricator\.outStorage\.allowItemRemoval\s*=\s*true') `
    'Mass Crusher output must permit manual item removal when no rail is attached.'
Require-Condition ($source -match 'fabricator\.outStorage\.allowUIItemRemoval\s*=\s*true') `
    'Mass Crusher output must expose manual item removal in the storage UI.'
Require-Condition ($source -match '2400f\s*\*\s*ForbiddenTechOptions\.Current\.PowerMultiplier') `
    'Mass Crusher active power must use the resolved power multiplier.'
Require-Condition ($source -match '40f\s*\*\s*ForbiddenTechOptions\.Current\.HeatMultiplier') `
    'Mass Crusher self heat must use the resolved heat multiplier.'

Write-Host 'Mass Crusher config contract validation passed.'
