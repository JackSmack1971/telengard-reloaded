[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$LocalDotNet = Join-Path $RepoRoot '.dotnet\dotnet.exe'
$GlobalJson = Join-Path $RepoRoot 'global.json'
$Solution = Join-Path $RepoRoot 'Telengard.sln'

Write-Host "Telengard repository: $RepoRoot"
Write-Host "Local dotnet:        $LocalDotNet"
Write-Host "Local SDK present:   $(Test-Path -LiteralPath $LocalDotNet -PathType Leaf)"
Write-Host "global.json present: $(Test-Path -LiteralPath $GlobalJson -PathType Leaf)"
Write-Host "Solution present:    $(Test-Path -LiteralPath $Solution -PathType Leaf)"

if (-not (Test-Path -LiteralPath $LocalDotNet -PathType Leaf)) {
    throw 'Repository-local .dotnet\dotnet.exe is missing. Restore the .dotnet directory before development.'
}
if (-not (Test-Path -LiteralPath $GlobalJson -PathType Leaf)) { throw 'global.json is missing.' }
if (-not (Test-Path -LiteralPath $Solution -PathType Leaf)) { throw 'Telengard.sln is missing.' }

$configured = (Get-Content -LiteralPath $GlobalJson -Raw | ConvertFrom-Json).sdk.version
$actual = & (Join-Path $PSScriptRoot 'dotnet.ps1') --version
if ($LASTEXITCODE -ne 0) { throw 'dotnet --version failed through the repository wrapper.' }

Write-Host "Configured SDK:      $configured"
Write-Host "Resolved SDK:        $actual"
if ($actual -ne $configured) {
    Write-Warning "Resolved SDK '$actual' differs from global.json '$configured'. global.json rollForward rules may explain this; verify intentionally before changing SDK versions."
}

Write-Host ''
Write-Host 'Canonical commands:'
Write-Host '  ./eng/dotnet.ps1 --info'
Write-Host '  ./eng/verify.ps1 -Mode Quick'
Write-Host '  ./eng/verify.ps1 -Mode Full'
Write-Host ''
& (Join-Path $PSScriptRoot 'dotnet.ps1') --info
exit $LASTEXITCODE
