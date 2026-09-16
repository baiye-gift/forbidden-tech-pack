[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$archiveDirectory = Join-Path $projectRoot 'artifacts\dependencies'
$workingDirectory = Join-Path $projectRoot 'obj\dependencies'
$libDirectory = Join-Path $projectRoot 'lib'
$toolDirectory = Join-Path $projectRoot 'tools'

$packages = @(
    @{ Name='PLib'; Uri='https://api.nuget.org/v3-flatcontainer/plib/4.25.0/plib.4.25.0.nupkg'; Sha256='5E0042E65BA9401682E9FFDA4E637E5083D4433814A26E52804FBE9BC1758324' },
    @{ Name='ILRepack'; Uri='https://api.nuget.org/v3-flatcontainer/ilrepack/2.0.48/ilrepack.2.0.48.nupkg'; Sha256='799017B829A6ED69FAC0D4FC0A874A4A6A9951F46D73A3CCE20BA016796B949F' },
    @{ Name='kanimal'; Uri='https://github.com/skairunner/kanimal-SE/releases/download/1.3.31/Windows.NET.dependent.zip'; Sha256='363C62CD38B7FDD5E14FAD7AF7D187CB94BCD61D5FA0EC7FAEBB4A9FB5413AAE' }
)

function Get-VerifiedArchive([hashtable]$package) {
    $archivePath = Join-Path $archiveDirectory ($package.Name + [IO.Path]::GetExtension($package.Uri))
    if (-not (Test-Path -LiteralPath $archivePath)) {
        Invoke-WebRequest -Uri $package.Uri -OutFile $archivePath
    }

    $actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    if (-not [string]::Equals($actualHash, $package.Sha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw "SHA256 mismatch for $($package.Name): expected $($package.Sha256), got $actualHash."
    }

    return $archivePath
}

function Expand-VerifiedArchive([string]$archivePath, [string]$destination) {
    if (Test-Path -LiteralPath $destination) {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }

    [IO.Directory]::CreateDirectory($destination) | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $destination)
}

function Get-RequiredFile([string]$root, [string]$fileName) {
    $matches = @(Get-ChildItem -LiteralPath $root -Filter $fileName -Recurse -File)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one '$fileName' under '$root', found $($matches.Count)."
    }

    return $matches[0]
}

New-Item -ItemType Directory -Force -Path $archiveDirectory, $workingDirectory, $libDirectory, $toolDirectory | Out-Null
$archives = @{}
foreach ($package in $packages) {
    $archives[$package.Name] = Get-VerifiedArchive $package
    Expand-VerifiedArchive $archives[$package.Name] (Join-Path $workingDirectory $package.Name)
}

$plib = Join-Path (Join-Path $workingDirectory 'PLib') 'lib\net48\PLib.dll'
if (-not (Test-Path -LiteralPath $plib)) {
    throw "PLib package does not contain '$plib'."
}
Copy-Item -LiteralPath $plib -Destination (Join-Path $libDirectory 'PLib.dll') -Force

$ilRepack = Get-RequiredFile (Join-Path $workingDirectory 'ILRepack') 'ILRepack.exe'
Copy-Item -Path (Join-Path $ilRepack.Directory.FullName '*') -Destination $toolDirectory -Recurse -Force

$kanimal = Get-RequiredFile (Join-Path $workingDirectory 'kanimal') 'kanimal-cli.exe'
Copy-Item -Path (Join-Path $kanimal.Directory.FullName '*') -Destination $toolDirectory -Recurse -Force

Write-Host 'Dependencies restored and verified.'
