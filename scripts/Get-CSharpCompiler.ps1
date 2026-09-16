function Get-ForbiddenTechnologyCSharpCompiler {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    $compiler = Join-Path $ProjectRoot 'tools\roslyn\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw "C# 7.3 compiler was not found at '$compiler'. Run .\\restore-deps.ps1 before building or testing."
    }

    $languageVersions = (& $compiler /nologo /langversion:? 2>&1) -join "`n"
    if ($languageVersions -notmatch '(^|\D)7\.3(\D|$)') {
        throw "Pinned compiler '$compiler' does not advertise C# 7.3 support. Restore dependencies again."
    }

    return $compiler
}
