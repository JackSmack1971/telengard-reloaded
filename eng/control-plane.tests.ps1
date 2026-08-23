[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$checker = Join-Path $repoRoot 'eng\control-plane.ps1'

function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Invoke-Check([string]$Root) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $checker -RepositoryRoot $Root 2>&1 } finally { $ErrorActionPreference = $previous }
    [pscustomobject]@{ Code = $LASTEXITCODE; Output = ($output -join "`n") }
}

$baseline = Invoke-Check $repoRoot
Assert-Condition ($baseline.Code -eq 0) "Baseline control-plane check failed: $($baseline.Output)"

$profile = Join-Path $repoRoot '.codex\agents\correctness.toml'
$original = [System.IO.File]::ReadAllText($profile)
try {
    [System.IO.File]::WriteAllText($profile, $original.Replace('model_reasoning_effort = "medium"', 'model_reasoning_effort = "invalid"'))
    $invalid = Invoke-Check $repoRoot
    Assert-Condition ($invalid.Code -ne 0) 'Invalid agent reasoning was not rejected.'
} finally { [System.IO.File]::WriteAllText($profile, $original) }

Write-Host 'Control-plane tests passed: baseline and invalid-profile rejection.'
