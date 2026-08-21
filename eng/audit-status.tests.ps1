[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$generator = Join-Path $repoRoot 'eng\audit-status.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("telengard-audit-status-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

function Assert-Condition {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) { throw $Message }
}

function Read-Utf8 {
    param([string]$Path)

    return [System.IO.File]::ReadAllText($Path, (New-Object System.Text.UTF8Encoding($false)))
}

function Invoke-Generator {
    param([ValidateSet('Generate', 'Check')][string]$Mode)

    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $generator `
            -Mode $Mode `
            -RepositoryRoot $repoRoot `
            -LedgerPath (Join-Path $tempRoot 'audit-status.json') `
            -PlaybookPath (Join-Path $tempRoot 'AUDIT_REMEDIATION_PLAYBOOK.md') `
            -GatePath (Join-Path $tempRoot 'AUDIT-P0.md') 2>&1
    } finally {
        $ErrorActionPreference = $previousErrorAction
    }
    $code = $LASTEXITCODE
    return [pscustomobject]@{ Code = $code; Output = ($output -join "`n") }
}

function Get-OutsideGeneratedSections {
    param([string]$Text)

    $start = '<!-- BEGIN GENERATED: audit-status -->'
    $end = '<!-- END GENERATED: audit-status -->'
    $startIndex = $Text.IndexOf($start, [StringComparison]::Ordinal)
    $endIndex = $Text.IndexOf($end, [StringComparison]::Ordinal)
    Assert-Condition ($startIndex -ge 0 -and $endIndex -gt $startIndex) 'Generated markers are missing from the fixture.'
    return $Text.Substring(0, $startIndex) + $Text.Substring($endIndex + $end.Length)
}

try {
    Copy-Item (Join-Path $repoRoot 'docs\audit-status.json') (Join-Path $tempRoot 'audit-status.json')
    Copy-Item (Join-Path $repoRoot 'docs\AUDIT_REMEDIATION_PLAYBOOK.md') (Join-Path $tempRoot 'AUDIT_REMEDIATION_PLAYBOOK.md')
    Copy-Item (Join-Path $repoRoot 'docs\gates\AUDIT-P0.md') (Join-Path $tempRoot 'AUDIT-P0.md')

    $first = Invoke-Generator -Mode Generate
    Assert-Condition ($first.Code -eq 0) "Initial generation failed: $($first.Output)"
    $playbookPath = Join-Path $tempRoot 'AUDIT_REMEDIATION_PLAYBOOK.md'
    $gatePath = Join-Path $tempRoot 'AUDIT-P0.md'
    $firstPlaybookBytes = [System.IO.File]::ReadAllBytes($playbookPath)
    $firstGateBytes = [System.IO.File]::ReadAllBytes($gatePath)

    $second = Invoke-Generator -Mode Generate
    Assert-Condition ($second.Code -eq 0) "Second generation failed: $($second.Output)"
    Assert-Condition ([System.Linq.Enumerable]::SequenceEqual($firstPlaybookBytes, [System.IO.File]::ReadAllBytes($playbookPath))) 'Repeated playbook generation is not byte-stable.'
    Assert-Condition ([System.Linq.Enumerable]::SequenceEqual($firstGateBytes, [System.IO.File]::ReadAllBytes($gatePath))) 'Repeated gate generation is not byte-stable.'

    $beforePlaybook = Read-Utf8 $playbookPath
    $beforeGate = Read-Utf8 $gatePath
    $ledgerPath = Join-Path $tempRoot 'audit-status.json'
    $ledger = Read-Utf8 $ledgerPath | ConvertFrom-Json
    $ledger.packets[0].ticket.status = 'open'
    $ledger.packets[0].ticket.verified_commit = $null
    $ledger.packets[0].unresolved = 'Test-only unresolved state.'
    [System.IO.File]::WriteAllText($ledgerPath, ($ledger | ConvertTo-Json -Depth 10), (New-Object System.Text.UTF8Encoding($false)))

    $changed = Invoke-Generator -Mode Generate
    Assert-Condition ($changed.Code -eq 0) "Changed-ledger generation failed: $($changed.Output)"
    $afterPlaybook = Read-Utf8 $playbookPath
    $afterGate = Read-Utf8 $gatePath
    Assert-Condition ((Get-OutsideGeneratedSections $beforePlaybook) -ceq (Get-OutsideGeneratedSections $afterPlaybook)) 'Human-authored playbook content changed during regeneration.'
    Assert-Condition ((Get-OutsideGeneratedSections $beforeGate) -ceq (Get-OutsideGeneratedSections $afterGate)) 'Human-authored gate content changed during regeneration.'
    Assert-Condition ($afterPlaybook -match '(?ms)- id: AUD-001\s+status: open') 'Canonical status change did not reach the playbook projection.'
    Assert-Condition ($afterGate -match '(?ms)- id: AUD-001\s+status: open') 'Canonical status change did not reach the gate projection.'

    $stalePlaybook = $afterPlaybook.Replace("    area: 'determinism'", "    area: 'stale-edit'")
    [System.IO.File]::WriteAllText($playbookPath, $stalePlaybook, (New-Object System.Text.UTF8Encoding($false)))
    $stale = Invoke-Generator -Mode Check
    Assert-Condition ($stale.Code -ne 0) 'Stale playbook output did not fail with an actionable section message.'
    $restore = Invoke-Generator -Mode Generate
    Assert-Condition ($restore.Code -eq 0) "Restoring generated output failed: $($restore.Output)"

    $missingMarkerGate = (Read-Utf8 $gatePath).Replace('<!-- END GENERATED: audit-status -->', '')
    [System.IO.File]::WriteAllText($gatePath, $missingMarkerGate, (New-Object System.Text.UTF8Encoding($false)))
    $missing = Invoke-Generator -Mode Check
    Assert-Condition ($missing.Code -ne 0) 'Missing generated marker did not fail validation.'

    Copy-Item (Join-Path $repoRoot 'docs\gates\AUDIT-P0.md') $gatePath -Force
    $restore = Invoke-Generator -Mode Generate
    Assert-Condition ($restore.Code -eq 0) "Final generated-output restoration failed: $($restore.Output)"
    foreach ($path in @($playbookPath, $gatePath)) {
        $text = Read-Utf8 $path
        Assert-Condition (([regex]::Matches($text, 'BEGIN GENERATED: audit-status')).Count -eq 1) "$path does not contain exactly one generated start marker."
        Assert-Condition (([regex]::Matches($text, 'END GENERATED: audit-status')).Count -eq 1) "$path does not contain exactly one generated end marker."
    }

    Write-Host 'Audit status tests passed: deterministic generation, preservation, precedence projection, stale detection, markers, and provenance validation.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
