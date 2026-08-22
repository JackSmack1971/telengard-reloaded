[CmdletBinding()]
param(
    [ValidateSet('Generate', 'Check')]
    [string]$Mode = 'Check',
    [string]$RepositoryRoot = '',
    [string]$LedgerPath = '',
    [string]$PlaybookPath = '',
    [string]$GatePath = ''
)

$ErrorActionPreference = 'Stop'

function Get-DefaultPath {
    param([string]$Value, [string]$RelativePath)

    if ($Value) {
        return (Resolve-Path -LiteralPath $Value).Path
    }

    return (Join-Path $RepositoryRoot $RelativePath)
}

function ConvertTo-YamlString {
    param([AllowNull()][string]$Value)

    if ([string]::IsNullOrEmpty($Value)) {
        return 'null'
    }

    return "'$($Value.Replace("'", "''"))'"
}

function Resolve-SourcePath {
    param([Parameter(Mandatory=$true)][string]$Source)

    $relative = $Source.Split('#')[0]
    $path = Join-Path $RepositoryRoot ($relative -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Canonical audit source '$Source' does not exist."
    }

    return $Source
}

function Get-ResolvedPackets {
    param([Parameter(Mandatory=$true)]$Ledger)

    if ($Ledger.schema_version -ne 1) {
        throw "Unsupported audit ledger schema_version '$($Ledger.schema_version)'."
    }

    $packets = @($Ledger.packets)
    if ($packets.Count -eq 0) {
        throw 'The canonical audit ledger contains no packets.'
    }

    $seen = @{}
    $resolved = foreach ($packet in $packets | Sort-Object id) {
        if ($packet.id -notmatch '^AUD-[0-9]{3}$') {
            throw "Invalid audit packet id '$($packet.id)'."
        }
        if ($seen.ContainsKey($packet.id)) {
            throw "Duplicate audit packet id '$($packet.id)'."
        }
        $seen[$packet.id] = $true

        if ($null -eq $packet.ticket) {
            throw "$($packet.id) must contain ticket metadata."
        }

        $ticket = $packet.ticket
        $plan = $packet.exec_plan
        $status = if ($null -ne $ticket.status) { [string]$ticket.status } elseif ($null -ne $plan.status) { [string]$plan.status } else { $null }
        $severity = if ($null -ne $ticket.severity) { [string]$ticket.severity } elseif ($null -ne $plan.severity) { [string]$plan.severity } else { $null }
        $priority = if ($null -ne $ticket.priority) { [string]$ticket.priority } elseif ($null -ne $plan.priority) { [string]$plan.priority } else { $null }
        $area = if ($null -ne $ticket.area) { [string]$ticket.area } elseif ($null -ne $plan.area) { [string]$plan.area } else { $null }
        $compatibilitySensitive = if ($null -ne $ticket.compatibility_sensitive) { [bool]$ticket.compatibility_sensitive } elseif ($null -ne $plan.compatibility_sensitive) { [bool]$plan.compatibility_sensitive } else { $null }
        $verifiedCommit = if ($null -ne $ticket.verified_commit) { [string]$ticket.verified_commit } elseif ($null -ne $plan.verified_commit) { [string]$plan.verified_commit } else { $null }

        if ($status -notin @('open', 'closed', 'blocked', 'deferred')) {
            throw "$($packet.id) has invalid status '$status'."
        }
        if ($severity -notin @('low', 'medium', 'high')) {
            throw "$($packet.id) has invalid severity '$severity'."
        }
        if ($priority -notin @('P0', 'P1', 'P2')) {
            throw "$($packet.id) has invalid priority '$priority'."
        }
        if ([string]::IsNullOrWhiteSpace($area)) {
            throw "$($packet.id) must define an area."
        }
        if ($null -eq $compatibilitySensitive) {
            throw "$($packet.id) must define compatibility_sensitive."
        }

        $ticketSource = Resolve-SourcePath -Source ([string]$ticket.source)
        if ($verifiedCommit -and $verifiedCommit -notmatch '^[0-9a-fA-F]{7,40}$') {
            throw "$($packet.id) has invalid verified_commit '$verifiedCommit'."
        }

        $planPath = $null
        $planStatus = $null
        $planState = 'none'
        if ($null -ne $plan) {
            if ([string]::IsNullOrWhiteSpace([string]$plan.path)) {
                throw "$($packet.id) exec_plan must define a path."
            }
            $planPath = Resolve-SourcePath -Source ([string]$plan.path)
            $planStatus = if ($null -ne $plan.status) { [string]$plan.status } else { $null }
            if ($planPath -match '(^|/)active/') { $planState = 'active' }
            elseif ($planPath -match '(^|/)completed/') { $planState = 'completed' }
            else { throw "$($packet.id) exec_plan must be under active or completed."
            }
            if ($planStatus -and $planStatus -ne $planState) {
                throw "$($packet.id) exec_plan status '$planStatus' does not match its '$planState' location."
            }
            if ($plan.verified_commit -and ([string]$plan.verified_commit) -notmatch '^[0-9a-fA-F]{7,40}$') {
                throw "$($packet.id) has invalid exec_plan verified_commit '$($plan.verified_commit)'."
            }
        }

        $unresolved = if ($null -ne $packet.unresolved) { [string]$packet.unresolved } else { $null }
        if ($status -eq 'closed' -and [string]::IsNullOrWhiteSpace($verifiedCommit)) {
            throw "$($packet.id) is closed but has no verified_commit."
        }
        if ($status -ne 'closed' -and [string]::IsNullOrWhiteSpace($unresolved)) {
            throw "$($packet.id) is not closed and must declare unresolved state."
        }

        [pscustomobject]@{
            id = [string]$packet.id
            status = $status
            severity = $severity
            priority = $priority
            area = $area
            compatibility_sensitive = $compatibilitySensitive
            verified_commit = $verifiedCommit
            verification_state = if ($verifiedCommit) { 'verified' } else { 'unresolved' }
            ticket_source = $ticketSource
            exec_plan = $planPath
            exec_plan_status = if ($plan) { $planState } else { 'none' }
            unresolved = $unresolved
        }
    }

    return @($resolved)
}

function Get-GeneratedSection {
    param(
        [Parameter(Mandatory=$true)]$Packets,
        [Parameter(Mandatory=$true)][ValidateSet('Playbook', 'Gate')][string]$Target,
        [Parameter(Mandatory=$true)][string]$NewLine
    )

    $heading = if ($Target -eq 'Playbook') { '## Machine-readable remediation index (generated)' } else { '## Current audit status and provenance (generated)' }
    $lines = @(
        '<!-- BEGIN GENERATED: audit-status -->',
        $heading,
        '',
        '```yaml',
        'remediations:'
    )

    foreach ($packet in $Packets) {
        $compatibility = if ($packet.compatibility_sensitive) { 'true' } else { 'false' }
        $lines += "  - id: $($packet.id)"
        $lines += "    status: $($packet.status)"
        $lines += "    severity: $($packet.severity)"
        $lines += "    priority: $($packet.priority)"
        $lines += "    area: $(ConvertTo-YamlString $packet.area)"
        $lines += "    compatibility_sensitive: $compatibility"
        $lines += "    verified_commit: $(ConvertTo-YamlString $packet.verified_commit)"
        $lines += "    verification_state: $($packet.verification_state)"
        $lines += "    ticket_source: $(ConvertTo-YamlString $packet.ticket_source)"
        $lines += "    exec_plan: $(ConvertTo-YamlString $packet.exec_plan)"
        $lines += "    exec_plan_status: $($packet.exec_plan_status)"
        if ($packet.unresolved) {
            $lines += "    unresolved: $(ConvertTo-YamlString $packet.unresolved)"
        }
    }

    $lines += '```'
    $lines += '<!-- END GENERATED: audit-status -->'
    return ($lines -join $NewLine)
}

function Get-NewLine {
    param([Parameter(Mandatory=$true)][string]$Text)

    if ($Text.Contains("`r`n")) { return "`r`n" }
    return "`n"
}

function Replace-GeneratedSection {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$Section,
        [Parameter(Mandatory=$true)][ValidateSet('Playbook', 'Gate')][string]$Target,
        [Parameter(Mandatory=$true)][bool]$AllowBootstrap,
        [Parameter(Mandatory=$true)][string]$NewLine
    )

    $startMarker = '<!-- BEGIN GENERATED: audit-status -->'
    $endMarker = '<!-- END GENERATED: audit-status -->'
    $startCount = ([regex]::Matches($Text, [regex]::Escape($startMarker))).Count
    $endCount = ([regex]::Matches($Text, [regex]::Escape($endMarker))).Count
    if ($startCount -ne $endCount -or $startCount -gt 1) {
        throw "$Target must contain exactly one generated audit-status marker pair."
    }

    if ($startCount -eq 1) {
        $start = $Text.IndexOf($startMarker, [StringComparison]::Ordinal)
        $end = $Text.IndexOf($endMarker, $start, [StringComparison]::Ordinal)
        if ($end -lt $start) { throw "$Target generated audit-status markers are out of order." }
        $end += $endMarker.Length
        return $Text.Substring(0, $start) + $Section + $Text.Substring($end)
    }

    if (-not $AllowBootstrap) {
        throw "$Target is missing generated audit-status markers. Run ./eng/audit-status.ps1 -Mode Generate."
    }

    $anchor = if ($Target -eq 'Playbook') { '## 4. Machine-readable remediation index' } else { '## Result' }
    $anchorIndex = $Text.IndexOf($anchor, [StringComparison]::Ordinal)
    if ($anchorIndex -lt 0) { throw "$Target has no bootstrap anchor '$anchor'." }

    $prefix = $Text.Substring(0, $anchorIndex).TrimEnd("`r", "`n")
    if ($Target -eq 'Playbook') {
        $humanSection = $Text.IndexOf('# AUD-001', $anchorIndex, [StringComparison]::Ordinal)
        if ($humanSection -lt 0) { throw "$Target has no human-authored AUD-001 anchor after the generated index." }
        $suffix = $Text.Substring($humanSection)
    } else {
        $suffix = $Text.Substring($anchorIndex)
    }
    return $prefix + $NewLine + $NewLine + $Section + $NewLine + $NewLine + $suffix
}

function Write-Utf8NoBom {
    param([Parameter(Mandatory=$true)][string]$Path, [Parameter(Mandatory=$true)][string]$Text)

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Read-Utf8 {
    param([Parameter(Mandatory=$true)][string]$Path)

    return [System.IO.File]::ReadAllText($Path, (New-Object System.Text.UTF8Encoding($false)))
}

try {
    if (-not $RepositoryRoot) {
        $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $LedgerPath = Get-DefaultPath -Value $LedgerPath -RelativePath 'docs/audit-status.json'
    $PlaybookPath = Get-DefaultPath -Value $PlaybookPath -RelativePath 'docs/AUDIT_REMEDIATION_PLAYBOOK.md'
    $GatePath = Get-DefaultPath -Value $GatePath -RelativePath 'docs/gates/AUDIT-P0.md'

    $ledger = Read-Utf8 -Path $LedgerPath | ConvertFrom-Json
    $packets = Get-ResolvedPackets -Ledger $ledger

    foreach ($target in @(
        [pscustomobject]@{ Name = 'Playbook'; Path = $PlaybookPath },
        [pscustomobject]@{ Name = 'Gate'; Path = $GatePath }
    )) {
        $current = Read-Utf8 -Path $target.Path
        $newLine = Get-NewLine -Text $current
        $section = Get-GeneratedSection -Packets $packets -Target $target.Name -NewLine $newLine
        $expected = Replace-GeneratedSection -Text $current -Section $section -Target $target.Name -AllowBootstrap:($Mode -eq 'Generate') -NewLine $newLine

        if ($Mode -eq 'Check') {
            if ($current -cne $expected) {
                throw "Stale generated audit-status section in $($target.Path). Run ./eng/audit-status.ps1 -Mode Generate; affected section: $($target.Name)."
            }
            Write-Host "Audit status synchronized: $($target.Path) [$($target.Name)]."
        } elseif ($current -cne $expected) {
            Write-Utf8NoBom -Path $target.Path -Text $expected
            Write-Host "Generated audit status: $($target.Path) [$($target.Name)]."
        } else {
            Write-Host "Audit status already synchronized: $($target.Path) [$($target.Name)]."
        }
    }

    if ($Mode -eq 'Generate') {
        Write-Host 'Audit status generation passed.'
    } else {
        Write-Host 'Audit status check passed.'
    }
} catch {
    Write-Error $_
    exit 1
}
