[CmdletBinding()]
param(
    [ValidateSet('Check')]
    [string]$Mode = 'Check',
    [string]$RepositoryRoot = ''
)

$ErrorActionPreference = 'Stop'

function Read-Utf8([string]$Path) {
    return [System.IO.File]::ReadAllText($Path, (New-Object System.Text.UTF8Encoding($false)))
}

function Resolve-RepoPath([string]$RelativePath) {
    $clean = ($RelativePath -split '#', 2)[0].Trim().Replace('/', '\')
    if ([string]::IsNullOrWhiteSpace($clean) -or $clean -match '[*{}]') { return $null }
    return Join-Path $script:RepositoryRoot $clean
}

function Assert-PathExists([string]$Reference, [string]$Source) {
    $path = Resolve-RepoPath $Reference
    if ($null -ne $path -and -not (Test-Path -LiteralPath $path)) {
        throw "$Source references missing path '$Reference'."
    }
}

function Get-Anchor([string]$Path, [string]$Anchor) {
    $text = Read-Utf8 $Path
    $slug = ($Anchor.ToLowerInvariant() -replace '[^a-z0-9 -]', '' -replace '\s+', '-').Trim('-')
    foreach ($line in ($text -split '\r?\n')) {
        if ($line -match '^#{1,6}\s+(.+?)\s*#*\s*$') {
            $heading = $Matches[1]
            $headingSlug = ($heading.ToLowerInvariant() -replace '[^a-z0-9 -]', '' -replace '\s+', '-').Trim('-')
            if ($headingSlug -eq $slug -or $headingSlug -eq $Anchor.ToLowerInvariant().Trim('-') -or $headingSlug.StartsWith(($Anchor.ToLowerInvariant().Trim('-') + '-'))) { return $true }
        }
    }
    return $false
}

function Assert-Reference([string]$Reference, [string]$Source) {
    Assert-PathExists $Reference $Source
    if ($Reference.Contains('#')) {
        $parts = $Reference -split '#', 2
        $path = Resolve-RepoPath $parts[0]
        if ($null -ne $path -and (Test-Path -LiteralPath $path -PathType Leaf) -and -not (Get-Anchor $path $parts[1])) {
            throw "$Source references missing anchor '$Reference'."
        }
    }
}

function Get-LocalReferences([string]$Path) {
    $text = Read-Utf8 $Path
    $references = New-Object System.Collections.Generic.List[string]
    foreach ($match in [regex]::Matches($text, '\[[^\]]+\]\(([^)]+)\)')) {
        $value = $match.Groups[1].Value.Trim().Trim('<', '>')
        if ($value -notmatch '^(?:https?://|mailto:|#)') { $references.Add($value.Split(' ')[0]) }
    }
    foreach ($match in [regex]::Matches($text, '(?<![\w./-])((?:\.codex|\.agents|docs|eng|src|tests|tools|content)/[A-Za-z0-9_./{}#-]+)')) {
        $value = $match.Groups[1].Value.TrimEnd('.', ',', ';', ':', ')')
        if ($value -notmatch '\{[^}]+\}') { $references.Add($value) }
    }
    return @($references | Select-Object -Unique)
}

function Test-InstructionGraph {
    $roots = @('AGENTS.md', 'docs/CODEX.md') + @((Get-ChildItem $script:RepositoryRoot -Recurse -File -Include AGENTS.md,AGENTS.override.md | ForEach-Object { $_.FullName.Substring($script:RepositoryRoot.Length + 1) }))
    $roots = @($roots | Select-Object -Unique)
    $skillNames = @(Get-ChildItem (Join-Path $script:RepositoryRoot '.codex/skills'), (Join-Path $script:RepositoryRoot '.agents/skills') -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name -Unique)
    foreach ($root in $roots) {
        $path = Join-Path $script:RepositoryRoot ($root -replace '/', '\')
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Instruction graph root '$root' does not exist." }
        $text = Read-Utf8 $path
        foreach ($reference in Get-LocalReferences $path) { Assert-Reference $reference $root }
        foreach ($skill in [regex]::Matches($text, '\$([a-z][a-z0-9-]+)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique) {
            if ($skill -notin $skillNames) { throw "$root references missing skill '$skill'." }
        }
    }
    Write-Host "Instruction graph valid ($($roots.Count) roots)."
}

function Test-Skills {
    $skillRoots = @('.codex/skills', '.agents/skills')
    $count = 0
    foreach ($root in $skillRoots) {
        $rootPath = Join-Path $script:RepositoryRoot ($root -replace '/', '\')
        if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) { continue }
        foreach ($directory in Get-ChildItem $rootPath -Directory) {
            $skillPath = Join-Path $directory.FullName 'SKILL.md'
            if (-not (Test-Path -LiteralPath $skillPath -PathType Leaf)) { throw "Canonical skill directory '$root/$($directory.Name)' is missing SKILL.md." }
            $lines = (Read-Utf8 $skillPath) -split '\r?\n'
            if ($lines.Count -lt 3 -or $lines[0].Trim() -ne '---') { throw "Skill '$skillPath' is missing frontmatter." }
            $end = [Array]::IndexOf($lines, '---', 1)
            if ($end -lt 0) { throw "Skill '$skillPath' has unterminated frontmatter." }
            $fields = @{}
            foreach ($line in $lines[1..($end - 1)]) {
                if ($line -notmatch '^([A-Za-z_][A-Za-z0-9_-]*):\s*(.+?)\s*$') { throw "Skill '$skillPath' has invalid frontmatter line '$line'." }
                $fields[$Matches[1]] = $Matches[2].Trim().Trim("'").Trim('"')
            }
            if ([string]::IsNullOrWhiteSpace($fields.name) -or [string]::IsNullOrWhiteSpace($fields.description)) { throw "Skill '$skillPath' must define name and description frontmatter." }
            $count++
        }
    }
    if ($count -eq 0) { throw 'No canonical skills were discovered.' }
    Write-Host "Skill discovery valid ($($count) skills)."
}

function Get-TomlValue([string]$Text, [string]$Key) {
    $pattern = '(?m)^\s*' + [regex]::Escape($Key) + '\s*=\s*([^#\r\n]+?)\s*$'
    $match = [regex]::Match($Text, $pattern)
    if ($match.Success) { return $match.Groups[1].Value.Trim().Trim("'").Trim('"') }
    return $null
}

function Test-Agents {
    $profiles = @(Get-ChildItem (Join-Path $script:RepositoryRoot '.codex/agents') -Filter '*.toml' -File)
    if ($profiles.Count -eq 0) { throw 'No custom agent profiles were discovered.' }
    $validReasoning = @('low', 'medium', 'high', 'xhigh')
    $validJustifications = @('architecture', 'security', 'save-compatibility', 'product-ambiguity', 'high-impact-boundary')
    $names = @{}
    foreach ($profile in $profiles) {
        $text = Read-Utf8 $profile.FullName
        foreach ($key in @('name', 'model', 'model_reasoning_effort')) { if (-not (Get-TomlValue $text $key)) { throw "Agent profile '$($profile.Name)' must define $key." } }
        $name = Get-TomlValue $text 'name'
        if ($names.ContainsKey($name)) { throw "Agent profile name '$name' is duplicated." }
        $names[$name] = $true
        $model = Get-TomlValue $text 'model'
        if ($model -notmatch '^gpt-[0-9]+(?:\.[0-9]+)?-(?:terra|luna|sol)$') { throw "Agent profile '$($profile.Name)' has invalid model '$model'." }
        $reasoning = Get-TomlValue $text 'model_reasoning_effort'
        if ($reasoning -notin $validReasoning) { throw "Agent profile '$($profile.Name)' has invalid reasoning value '$reasoning'." }
        if ($reasoning -in @('high', 'xhigh')) {
            $justification = Get-TomlValue $text 'justification'
            if ($justification -notin $validJustifications) { throw "Agent profile '$($profile.Name)' needs an allowlisted high-effort justification." }
        }
    }
    Write-Host "Custom agent profiles valid ($($profiles.Count) profiles)."
}

function Test-ContextBudgets {
    $config = Read-Utf8 (Join-Path $script:RepositoryRoot '.codex/config.toml')
    $rootLimit = [int](Get-TomlValue $config 'root_agents_max_bytes')
    $skillLimit = [int](Get-TomlValue $config 'workflow_skill_soft_max_bytes')
    if ($rootLimit -le 0 -or $skillLimit -le 0) { throw 'Control-plane byte limits must be positive in .codex/config.toml.' }
    $root = Get-Item (Join-Path $script:RepositoryRoot 'AGENTS.md')
    if ($root.Length -gt $rootLimit) { throw "AGENTS.md is $($root.Length) bytes, over configured limit $rootLimit." }
    foreach ($skill in Get-ChildItem (Join-Path $script:RepositoryRoot '.codex/skills') -Directory | ForEach-Object { Get-Item (Join-Path $_.FullName 'SKILL.md') }) {
        if ($skill.Length -gt $skillLimit) { throw "Workflow skill '$($skill.FullName)' is $($skill.Length) bytes, over soft limit $skillLimit." }
    }
    Write-Host "Context budgets valid (root <= $rootLimit; workflow skills <= $skillLimit bytes)."
}

function Test-Tickets {
    $indexPath = Join-Path $script:RepositoryRoot 'docs/tasks/index.json'
    $index = Read-Utf8 $indexPath | ConvertFrom-Json
    $tickets = @($index.tickets)
    $validStatuses = @('not_started', 'in_progress', 'blocked')
    $validRisks = @('save-compatibility', 'renderer-boundary', 'determinism', 'simulation', 'simulation-contract', 'content-schema', 'hidden-information', 'wealth', 'verification', 'documentation')
    $ids = @{}
    foreach ($ticket in $tickets) {
        if ($ticket.id -notmatch '^TEL-[0-9]{3}$' -or $ids.ContainsKey($ticket.id)) { throw "Ticket IDs must be unique valid TEL-### values; duplicate/invalid '$($ticket.id)'." }
        $ids[$ticket.id] = $true
    }
    foreach ($ticket in $tickets) {
        if ($ticket.status -notin $validStatuses) { throw "Ticket '$($ticket.id)' has invalid status '$($ticket.status)'." }
        foreach ($dependency in @($ticket.depends_on)) { if (-not $ids.ContainsKey($dependency) -and -not (@($index.completed) -contains $dependency)) { throw "Ticket '$($ticket.id)' depends on unknown '$dependency'." } }
        if ($null -ne $ticket.risk_tags) { foreach ($risk in @($ticket.risk_tags)) { if ($risk -notin $validRisks) { throw "Ticket '$($ticket.id)' has invalid risk tag '$risk'." } } }
        foreach ($reference in @($ticket.context.required) + @($ticket.context.conditional.PSObject.Properties | ForEach-Object { $_.Value })) { foreach ($item in @($reference)) { Assert-Reference $item "ticket $($ticket.id)" } }
    }
    foreach ($ticket in $tickets) { foreach ($dependency in @($ticket.depends_on)) { if (-not $ids.ContainsKey($dependency) -and -not (@($index.completed) -contains $dependency)) { throw "Ticket '$($ticket.id)' depends on unknown '$dependency'." } } }
    Write-Host "Ticket metadata valid ($($tickets.Count) active tickets)."
}

function Test-PlansAndGates {
    $active = Join-Path $script:RepositoryRoot 'docs/exec-plans/active'
    $owners = @{}
    if (Test-Path -LiteralPath $active) {
        foreach ($plan in Get-ChildItem $active -File) {
            if ($plan.Name -eq '.gitkeep') { continue }
            $matches = @([regex]::Matches((Read-Utf8 $plan.FullName), 'TEL-[0-9]{3}') | ForEach-Object { $_.Value } | Select-Object -Unique)
            if ($matches.Count -ne 1) { throw "Active plan '$($plan.Name)' must have exactly one TEL owner." }
            if ($owners.ContainsKey($matches[0])) { throw "TEL owner '$($matches[0])' has multiple active plans." }
            $owners[$matches[0]] = $plan.Name
            foreach ($reference in Get-LocalReferences $plan.FullName) { Assert-Reference $reference $plan.Name }
        }
    }
    foreach ($gate in Get-ChildItem (Join-Path $script:RepositoryRoot 'docs/gates') -Filter '*.md' -File) { foreach ($reference in Get-LocalReferences $gate.FullName) { Assert-Reference $reference $gate.Name } }
    Write-Host "Plans and gates valid ($($owners.Count) active plan owners)."
}

function Test-ControlPlaneTruth {
    foreach ($file in @('AGENTS.md', 'docs/CODEX.md') + @((Get-ChildItem $script:RepositoryRoot -Recurse -File -Include AGENTS.md,AGENTS.override.md | ForEach-Object { $_.FullName.Substring($script:RepositoryRoot.Length + 1) }))) {
        $path = Join-Path $script:RepositoryRoot ($file -replace '/', '\')
        foreach ($reference in Get-LocalReferences $path) { Assert-Reference $reference $file }
    }
    Write-Host 'Control-plane truth valid.'
}

try {
    if (-not $RepositoryRoot) { $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    $script:RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    Test-InstructionGraph
    Test-Skills
    Test-Agents
    Test-ContextBudgets
    Test-Tickets
    Test-PlansAndGates
    Test-ControlPlaneTruth
    Write-Host 'Control-plane check passed.'
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
