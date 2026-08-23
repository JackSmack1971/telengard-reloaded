[CmdletBinding()]
param(
    [ValidateSet('Generate', 'Check')]
    [string]$Mode = 'Check',
    [string]$RepositoryRoot,
    [string]$LedgerPath,
    [string]$IndexPath,
    [string]$OverridesPath
)

$ErrorActionPreference = 'Stop'

function Get-DefaultPath {
    param([string]$Value, [string]$RelativePath)

    if ($Value) { return $Value }
    return Join-Path $script:RepositoryRoot $RelativePath
}

function Read-Utf8 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.File]::ReadAllText($Path, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-Utf8NoBom {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Text)

    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Normalize-Status {
    param([Parameter(Mandatory = $true)][string]$Status)

    switch -Regex ($Status.Trim().ToLowerInvariant()) {
        '^implemented(?: and verified)?$' { return 'implemented' }
        '^not started$' { return 'not_started' }
        '^in progress$' { return 'in_progress' }
        '^blocked$' { return 'blocked' }
        default { return $Status.Trim().ToLowerInvariant() -replace '[^a-z0-9]+', '_' }
    }
}

function Get-Track {
    param([Parameter(Mandatory = $true)][string]$Heading)

    $value = $Heading.ToLowerInvariant()
    if ($value -match 'foundation') { return 'foundation' }
    if ($value -match 'dungeon') { return 'dungeon' }
    if ($value -match 'expedition') { return 'expedition' }
    if ($value -match 'encounter') { return 'encounters' }
    if ($value -match 'feature') { return 'features' }
    if ($value -match 'knowledge') { return 'knowledge' }
    if ($value -match 'item') { return 'items' }
    if ($value -match 'progression') { return 'progression' }
    if ($value -match 'legacy') { return 'legacy' }
    if ($value -match 'core alpha') { return 'core-alpha' }
    if ($value -match 'vertical[\s-]*slice') { return 'content' }
    if ($value -match 'presentation') { return 'presentation' }
    if ($value -match 'engineering') { return 'engineering' }
    return 'other'
}

function Get-RiskTags {
    param([Parameter(Mandatory = $true)][string]$Track)

    switch ($Track) {
        'foundation' { return @('simulation-contract') }
        'dungeon' { return @('determinism', 'simulation') }
        'expedition' { return @('save-compatibility', 'wealth') }
        'encounters' { return @('determinism', 'simulation') }
        'features' { return @('content-schema', 'determinism') }
        'knowledge' { return @('hidden-information', 'save-compatibility') }
        'items' { return @('content-schema', 'hidden-information') }
        'progression' { return @('simulation', 'save-compatibility') }
        'legacy' { return @('save-compatibility', 'hidden-information') }
        'presentation' { return @('renderer-boundary') }
        'content' { return @('content-schema', 'determinism') }
        'engineering' { return @('verification', 'documentation') }
        default { return @() }
    }
}

function Get-DefaultReview {
    param([Parameter(Mandatory = $true)][string]$Track)

    $conditional = [object[]]@('tests', 'architecture')
    if ($Track -eq 'presentation') { $conditional += 'presentation' }
    if ($Track -eq 'engineering') { $conditional += 'documentation' }
    return [ordered]@{
        required = @('correctness')
        conditional = [object[]]$conditional
    }
}

function Get-DefaultVerification {
    param([Parameter(Mandatory = $true)][string]$Track)

    return [ordered]@{
        headless = $true
        godot_manual = ($Track -eq 'presentation')
    }
}

function Assert-StringArray {
    param([object]$Value, [string]$Name)

    if ($null -eq $Value) { throw "Override field '$Name' is required." }
    foreach ($item in @($Value)) {
        if ($item -isnot [string] -or [string]::IsNullOrWhiteSpace($item)) { throw "Override field '$Name' must contain only non-empty strings." }
    }
}

function Resolve-ContextPath {
    param([string]$Path, [string]$Name)

    $relative = ($Path -split '#', 2)[0]
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)') {
        throw "Context path '$Path' must be repository-relative and cannot traverse outside the repository."
    }
    $root = [IO.Path]::GetFullPath($script:RepositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolved = [IO.Path]::GetFullPath((Join-Path $script:RepositoryRoot $relative))
    if (-not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Context path '$Path' resolves outside the repository."
    }
    return $resolved
}

function Assert-ContextManifest {
    param([object]$Context, [string]$Name)

    if ($null -eq $Context) { throw "Override field '$Name' is required." }
    Assert-StringArray -Value $Context.required -Name "$Name.required"
    foreach ($path in @($Context.required)) {
        $relative = ($path -split '#', 2)[0]
        if ($relative -match '^docs/tasks/TEL-\d{3}\.md$') { continue }
        if (-not (Test-Path -LiteralPath (Resolve-ContextPath -Path $path -Name $Name))) { throw "Context path '$path' does not exist." }
    }
    if ($null -eq $Context.conditional) { throw "Override field '$Name.conditional' is required." }
    foreach ($conditional in $Context.conditional.PSObject.Properties) {
        Assert-StringArray -Value $conditional.Value -Name "$Name.conditional.$($conditional.Name)"
        foreach ($path in @($conditional.Value)) {
            if (-not (Test-Path -LiteralPath (Resolve-ContextPath -Path $path -Name $Name))) { throw "Context path '$path' does not exist." }
        }
    }
}

function Assert-ReviewPolicy {
    param([object]$Review, [string]$Name, [string[]]$RequiredLanes = @())

    if ($null -eq $Review) { throw "Override field '$Name' is required." }
    $valid = @('correctness', 'architecture', 'determinism', 'save', 'tests', 'documentation', 'presentation', 'security', 'gate-specific')
    Assert-StringArray -Value $Review.required -Name "$Name.required"
    if (-not (@($Review.required) -contains 'correctness')) { throw "Override review policy must require correctness." }
    $lanes = @($Review.required)
    if ($null -ne $Review.conditional) { $lanes += @($Review.conditional) }
    foreach ($lane in $lanes) {
        if ($lane -notin $valid) { throw "Unknown review lane '$lane'." }
    }
    foreach ($lane in $RequiredLanes) {
        if ($lane -notin $lanes) { throw "Override review policy '$Name' must retain the '$lane' lane derived from the ticket track." }
    }
}

function Assert-VerificationPolicy {
    param([object]$Verification, [string]$Name)

    if ($null -eq $Verification) { throw "Override field '$Name' is required." }
    foreach ($field in @('headless', 'godot_manual')) {
        $property = $Verification.PSObject.Properties[$field]
        if ($null -eq $property -or $property.Value -isnot [bool]) { throw "Override verification field '$Name.$field' must be boolean." }
    }
    if (-not $Verification.headless) { throw "Override verification policy cannot disable required headless verification." }
}

function Get-TaskIndex {
    $ledger = Read-Utf8 -Path $script:LedgerPath
    $lines = $ledger -split '\r?\n'
    $track = 'other'
    $tickets = New-Object System.Collections.Generic.List[object]
    $seen = @{}

    foreach ($line in $lines) {
        if ($line -match '^#{2,3}\s+(.+?)\s*$') {
            $track = Get-Track -Heading $Matches[1]
            continue
        }

        $id = $null
        $title = $null
        $status = $null
        if ($line -match '^\-\s+\[(TEL-\d{3})\.md\]\(([^)]+)\)\s+\u2014\s+(.+?)\s+\u2014\s+(.+?)\s*$') {
            $id = $Matches[1]
            $title = $Matches[3]
            $status = $Matches[4]
        } elseif ($line -match '^\-\s+(TEL-\d{3})\s+\u2014\s+(.+?)\s+\u2014\s+(.+?)\s*$') {
            $id = $Matches[1]
            $title = $Matches[2]
            $status = $Matches[3]
        }

        if (-not $id) { continue }
        if ($seen.ContainsKey($id)) { throw "Duplicate task '$id' in $script:LedgerPath." }
        $seen[$id] = $true

        $normalizedStatus = Normalize-Status -Status $status
        $decisionState = if ($normalizedStatus -eq 'implemented') { 'complete' } else { 'unknown' }
        $ticketPath = "docs/tasks/$id.md"
        $tickets.Add([ordered]@{
            id = $id
            title = $title.Trim()
            status = $normalizedStatus
            track = $track
            dependencies = @()
            risk_tags = @(Get-RiskTags -Track $track)
            decision_state = $decisionState
            blocker = $null
            context = [ordered]@{
                required = @($ticketPath)
                conditional = [ordered]@{}
            }
            review = Get-DefaultReview -Track $track
            verification = Get-DefaultVerification -Track $track
        })
    }

    if ($tickets.Count -eq 0) { throw "No TEL tasks were found in $script:LedgerPath." }

    $overrides = $null
    if (Test-Path -LiteralPath $script:OverridesPath) {
        $overrides = Read-Utf8 -Path $script:OverridesPath | ConvertFrom-Json
        if ($overrides.schema_version -ne 1) { throw "Unsupported task-index override schema version." }
    }

    $byId = @{}
    foreach ($ticket in $tickets) { $byId[$ticket.id] = $ticket }
    if ($null -ne $overrides -and $null -ne $overrides.tickets) {
        foreach ($property in $overrides.tickets.PSObject.Properties) {
            if (-not $byId.ContainsKey($property.Name)) { throw "Override references unknown task '$($property.Name)'." }
            $override = $property.Value
            $ticket = $byId[$property.Name]
            $allowed = @('track', 'dependencies', 'risk_tags', 'decision_state', 'blocker', 'blocker_kind', 'context', 'review', 'verification')
            foreach ($overrideProperty in $override.PSObject.Properties) {
                if ($overrideProperty.Name -notin $allowed) { throw "Override field '$($overrideProperty.Name)' is not supported; ledger title/status are authoritative." }
            }
            if ($null -ne $override.PSObject.Properties['track']) {
                if ($override.track -isnot [string] -or [string]::IsNullOrWhiteSpace($override.track)) { throw "Override track for '$($property.Name)' must be a non-empty string." }
                $ticket['track'] = $override.track
            }
            if ($null -ne $override.PSObject.Properties['dependencies']) {
                Assert-StringArray -Value $override.dependencies -Name "$($property.Name).dependencies"
                $dependencies = @($override.dependencies)
                if (@($dependencies | Select-Object -Unique).Count -ne $dependencies.Count) { throw "Dependencies for '$($property.Name)' must be unique." }
                foreach ($dependency in $dependencies) {
                    if ($dependency -notmatch '^TEL-\d{3}$' -or -not $byId.ContainsKey($dependency) -or $dependency -eq $property.Name) { throw "Invalid dependency '$dependency' for '$($property.Name)'." }
                }
                $ticket['dependencies'] = $dependencies
            }
            if ($null -ne $override.PSObject.Properties['risk_tags']) {
                Assert-StringArray -Value $override.risk_tags -Name "$($property.Name).risk_tags"
                if (@($override.risk_tags).Count -eq 0) { throw "Override risk_tags for '$($property.Name)' must not be empty." }
                $ticket['risk_tags'] = @($override.risk_tags)
            }
            if ($null -ne $override.PSObject.Properties['context']) {
                Assert-ContextManifest -Context $override.context -Name "$($property.Name).context"
                $ticket['context'] = $override.context
            }
            if ($null -ne $override.PSObject.Properties['review']) {
                $defaultReview = Get-DefaultReview -Track $ticket.track
                $requiredLanes = @($defaultReview.required) + @($defaultReview.conditional)
                Assert-ReviewPolicy -Review $override.review -Name "$($property.Name).review" -RequiredLanes $requiredLanes
                $ticket['review'] = $override.review
            }
            if ($null -ne $override.PSObject.Properties['verification']) {
                Assert-VerificationPolicy -Verification $override.verification -Name "$($property.Name).verification"
                $ticket['verification'] = $override.verification
            }
            if ($null -ne $override.PSObject.Properties['decision_state']) {
                if ($override.decision_state -notin @('unknown', 'blocked')) { throw "Override decision_state for '$($property.Name)' must be unknown or blocked." }
                $ticket['decision_state'] = $override.decision_state
            }
            if ($null -ne $override.PSObject.Properties['blocker']) {
                if ($override.blocker -isnot [string] -or [string]::IsNullOrWhiteSpace($override.blocker)) { throw "Override blocker for '$($property.Name)' must be a non-empty string." }
                $ticket['blocker'] = $override.blocker
                if ($ticket.decision_state -ne 'blocked' -or $override.blocker_kind -ne 'decision') { throw "Override blocker for '$($property.Name)' requires decision_state 'blocked' and blocker_kind 'decision'." }
            }
            if ($null -ne $override.PSObject.Properties['blocker_kind']) {
                if ($override.blocker_kind -notin @('decision')) { throw "Override blocker_kind for '$($property.Name)' must be 'decision'." }
                $ticket['blocker_kind'] = $override.blocker_kind
            }
            if ($null -ne $override.PSObject.Properties['track']) {
                if ($null -eq $override.PSObject.Properties['risk_tags']) { $ticket['risk_tags'] = @(Get-RiskTags -Track $ticket.track) }
                if ($null -eq $override.PSObject.Properties['review']) { $ticket['review'] = Get-DefaultReview -Track $ticket.track }
                if ($null -eq $override.PSObject.Properties['verification']) { $ticket['verification'] = Get-DefaultVerification -Track $ticket.track }
            }
        }
    }

    $statusById = @{}
    foreach ($ticket in $tickets) { $statusById[$ticket.id] = $ticket.status }
    foreach ($ticket in $tickets) {
        if ($ticket.status -eq 'implemented') {
            $ticket.decision_state = 'complete'
            $ticket.blocker = $null
            continue
        }
        if (@($ticket.risk_tags).Count -eq 0) { throw "Active task '$($ticket.id)' must declare at least one risk tag." }
        if ($ticket.track -eq 'presentation' -and -not $ticket.verification.godot_manual) { throw "Presentation task '$($ticket.id)' must require manual Godot verification." }
        $missing = @($ticket.dependencies | Where-Object { -not $statusById.ContainsKey($_) -or $statusById[$_] -ne 'implemented' })
        if ($missing.Count -gt 0) {
            $ticket.decision_state = 'blocked'
            $ticket.blocker_kind = 'dependency'
            $ticket.blocker = 'Dependency not implemented: ' + ($missing -join ', ')
        } elseif ($ticket.decision_state -eq 'blocked') {
            if ([string]::IsNullOrWhiteSpace($ticket.blocker) -or $ticket.blocker_kind -ne 'decision') { throw "Decision-blocked task '$($ticket.id)' must have a blocker and blocker_kind 'decision'." }
        } elseif ($ticket.dependencies.Count -gt 0) {
            $ticket.decision_state = 'ready'
        } else {
            $ticket.decision_state = 'unknown'
        }
    }

    $milestone = 'core-alpha'
    if ($null -ne $overrides -and $null -ne $overrides.milestone) { $milestone = [string]$overrides.milestone }
    $compactTickets = New-Object System.Collections.Generic.List[object]
    $completedIds = New-Object System.Collections.Generic.List[string]
    foreach ($ticket in $tickets) {
        if ($ticket.status -eq 'implemented') {
            $completedIds.Add($ticket.id)
            continue
        }
        $entry = [ordered]@{
            id = $ticket.id
            title = $ticket.title
            status = $ticket.status
            track = $ticket.track
            decision_state = $ticket.decision_state
        }
        if ($ticket.dependencies.Count -gt 0) { $entry['depends_on'] = [object[]]$ticket.dependencies }
        $entry['risk_tags'] = [object[]]$ticket.risk_tags
        if ($null -ne $ticket.blocker) { $entry['blocker'] = $ticket.blocker }
        if ($null -ne $ticket.blocker_kind) { $entry['blocker_kind'] = $ticket.blocker_kind }
        $entry['review'] = $ticket.review
        $entry['verification'] = $ticket.verification
        $entry['context'] = $ticket.context
        $compactTickets.Add($entry)
    }

    $ticketArray = [object[]]$compactTickets
    $completedArray = [object[]]$completedIds
    $riskByTrack = [ordered]@{}
    foreach ($knownTrack in @('foundation', 'dungeon', 'expedition', 'encounters', 'features', 'knowledge', 'items', 'progression', 'legacy', 'presentation', 'content', 'engineering', 'other')) {
        $riskByTrack[$knownTrack] = @(Get-RiskTags -Track $knownTrack)
    }
    $reviewDefaults = [ordered]@{ required = @('correctness'); conditional = @('tests', 'architecture') }
    $verificationDefaults = [ordered]@{ headless = $true; godot_manual = $false }
    $conditionalContextByRisk = [ordered]@{
        'save-compatibility' = 'save'
        'persistence' = 'save'
        'determinism' = 'determinism'
        'renderer-boundary' = 'architecture'
        'simulation' = 'architecture'
        'simulation-contract' = 'architecture'
        'content-schema' = 'content'
        'hidden-information' = 'knowledge'
        'wealth' = 'save'
        'verification' = 'verification'
        'documentation' = 'documentation'
    }
    return [ordered]@{
        schema_version = 1
        milestone = $milestone
        source = 'docs/tasks/README.md + docs/tasks/index-overrides.json'
        context_template = 'docs/tasks/{id}.md'
        conditional_context_by_risk = $conditionalContextByRisk
        risk_tags_by_track = $riskByTrack
        review_defaults = $reviewDefaults
        verification_defaults = $verificationDefaults
        completed = $completedArray
        tickets = $ticketArray
    }
}

try {
    if (-not $RepositoryRoot) { $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    $script:RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $script:LedgerPath = Get-DefaultPath -Value $LedgerPath -RelativePath 'docs/tasks/README.md'
    $script:IndexPath = Get-DefaultPath -Value $IndexPath -RelativePath 'docs/tasks/index.json'
    $script:OverridesPath = Get-DefaultPath -Value $OverridesPath -RelativePath 'docs/tasks/index-overrides.json'

    $index = Get-TaskIndex
    $json = ($index | ConvertTo-Json -Compress -Depth 12).Replace("`r`n", "`n") + "`n"
    if ($Mode -eq 'Check') {
        if (-not (Test-Path -LiteralPath $script:IndexPath)) { throw "Missing generated task index '$script:IndexPath'. Run powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Generate." }
        $current = (Read-Utf8 -Path $script:IndexPath).Replace("`r`n", "`n").TrimStart([char]0xFEFF)
        if ($current -cne $json) { throw "Stale generated task index '$script:IndexPath'. Run powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Generate." }
        Write-Host "Task index synchronized: $script:IndexPath."
    } else {
        Write-Utf8NoBom -Path $script:IndexPath -Text $json
        Write-Host "Generated task index: $script:IndexPath."
    }
} catch {
    Write-Error $_.Exception.ToString()
    exit 1
}
