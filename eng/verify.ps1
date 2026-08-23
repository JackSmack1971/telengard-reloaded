[CmdletBinding()]
param(
    [ValidateSet('Quick','Full')]
    [string]$Mode = 'Full'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DotNet = Join-Path $PSScriptRoot 'dotnet.ps1'
. (Join-Path $PSScriptRoot 'common.ps1')

function Invoke-DotNetStep {
    param([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$true)][string[]]$Arguments)
    Write-Host ""
    Write-Host "== $Name =="
    & $DotNet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
}

Push-Location $RepoRoot
try {
    Write-Host ""
    Write-Host '== Task index check =='
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot 'eng\task-index.ps1') -Mode Check
    if ($LASTEXITCODE -ne 0) { throw 'Task index check failed.' }

    Write-Host ''
    Write-Host '== Task index validation =='
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot 'eng\task-index.tests.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Task index validation failed.' }

    if ($Mode -eq 'Full') {
        Invoke-DotNetStep -Name 'Restore' -Arguments @('restore','Telengard.sln')
        Invoke-DotNetStep -Name 'Format verification' -Arguments @('format','Telengard.sln','--verify-no-changes','--no-restore')
        Invoke-DotNetStep -Name 'Release build' -Arguments @('build','Telengard.sln','--configuration','Release','--no-restore')
        Invoke-DotNetStep -Name 'Release tests' -Arguments @('test','Telengard.sln','--configuration','Release','--no-build','--no-restore','--logger','console;verbosity=normal')
    } else {
        Invoke-DotNetStep -Name 'Quick build' -Arguments @('build','Telengard.sln','--configuration','Debug')
        Invoke-DotNetStep -Name 'Quick tests' -Arguments @('test','Telengard.sln','--configuration','Debug','--no-build','--logger','console;verbosity=minimal')
    }

    if ($Mode -eq 'Full') {
        $fingerprint = Get-TelengardVerificationFingerprint -RepoRoot $RepoRoot
        $stamp = [ordered]@{
            mode = 'Full'
            fingerprint = $fingerprint
            verifiedAtUtc = [DateTime]::UtcNow.ToString('o')
            solution = 'Telengard.sln'
        }
        $stamp | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $RepoRoot '.codex\.verify-stamp.json') -Encoding UTF8
    }

    Write-Host ""
    Write-Host "Telengard verification passed ($Mode)."
} finally {
    Pop-Location
}
