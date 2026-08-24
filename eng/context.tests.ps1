[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resolver = Join-Path $repoRoot 'eng\context.ps1'

function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Invoke-Context([string[]]$Arguments) {
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $resolver @Arguments 2>&1 } finally { $ErrorActionPreference = $previousErrorAction }
    [pscustomobject]@{ Code = $LASTEXITCODE; Text = ($output -join "`n") }
}

$full = Invoke-Context @('-Ticket', 'TEL-123')
Assert-Condition ($full.Code -eq 0) "Full context resolution failed: $($full.Text)"
Assert-Condition ($full.Text -notmatch '(?m)^(True|False)$') 'Resolver leaked PowerShell method return values into the pack.'
Assert-Condition ($full.Text -match '(?m)^REQUIRED$' -and $full.Text -match 'docs/tasks/TEL-123\.md') 'Full pack omitted required ticket context.'
Assert-Condition ($full.Text -match 'docs/presentation/GODOT_CLIENT_BLUEPRINT\.md#presentation-contract') 'Presentation blueprint anchor was not normalized.'
Assert-Condition ($full.Text -match 'src/Telengard.Core/presentation/' -and $full.Text -match 'src/Telengard.Godot/') 'Presentation source roots were not included.'
Assert-Condition ($full.Text -match '(?m)^CONDITIONAL$' -and $full.Text -match 'docs/ARCHITECTURE\.md#architectural-boundary') 'Risk-derived conditional context was not included.'
Assert-Condition ($full.Text -match '(?m)^REVIEW$' -and $full.Text -match '(?m)^presentation$') 'Presentation review lane was not included.'
Assert-Condition ($full.Text -match '(?m)^VERIFY$' -and $full.Text -match '(?m)^headless$') 'Headless verification was not included.'
Assert-Condition ($full.Text -notmatch 'Modern Telengard architecture|# Architectural boundary') 'Resolver concatenated file contents instead of printing references.'

$lane = Invoke-Context @('-Ticket', 'TEL-123', '-Lane', 'presentation')
Assert-Condition ($lane.Code -eq 0) "Presentation context resolution failed: $($lane.Text)"
Assert-Condition ($lane.Text -match '(?m)^REVIEW$' -and $lane.Text -match '(?m)^presentation$') 'Presentation lane pack omitted its review lane.'
Assert-Condition ($lane.Text -notmatch '(?m)^VERIFY$' -and $lane.Text -notmatch '(?m)^correctness$') 'Lane pack included unrelated sections or lanes.'

$missing = Invoke-Context @('-Ticket', 'TEL-999')
Assert-Condition ($missing.Code -ne 0) 'Unknown ticket was not rejected.'
Write-Host 'Context tests passed: full packs, lane filtering, reference-only output, and unknown-ticket rejection.'
