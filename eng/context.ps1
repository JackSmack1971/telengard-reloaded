[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^TEL-[0-9]{3}$')][string]$Ticket,
    [ValidateSet('correctness', 'architecture', 'determinism', 'save', 'tests', 'documentation', 'presentation', 'security', 'gate-specific')][string]$Lane,
    [string]$RepositoryRoot = ''
)

$ErrorActionPreference = 'Stop'

function Read-Utf8([string]$Path) {
    return [System.IO.File]::ReadAllText($Path, (New-Object System.Text.UTF8Encoding($false)))
}

function Add-Unique([System.Collections.Generic.List[string]]$Items, [string]$Value) {
    if (-not [string]::IsNullOrWhiteSpace($Value) -and -not $Items.Contains($Value)) { $Items.Add($Value) }
}

function Add-References([System.Collections.Generic.List[string]]$Items, [object]$Values) {
    foreach ($value in @($Values)) { Add-Unique $Items ([string]$value) }
}

function Get-ConditionalReferences([object]$TicketMetadata) {
    $references = New-Object System.Collections.Generic.List[string]
    foreach ($property in @($TicketMetadata.context.conditional.PSObject.Properties)) {
        Add-References $references $property.Value
    }

    $riskReferences = @{
        'hidden-information' = 'docs/INVARIANTS.md#visibility-and-knowledge'
        'renderer-boundary' = 'docs/ARCHITECTURE.md#architectural-boundary'
        'simulation' = 'docs/ARCHITECTURE.md#authoritative-state'
        'simulation-contract' = 'docs/ARCHITECTURE.md#architectural-boundary'
        'determinism' = 'docs/INVARIANTS.md#determinism-and-generation'
        'save-compatibility' = 'docs/INVARIANTS.md#saves-and-versions'
        'persistence' = 'docs/INVARIANTS.md#saves-and-versions'
        'wealth' = 'docs/INVARIANTS.md#expedition-and-economy'
        'content-schema' = 'docs/modern-telengard-spec.md'
        'verification' = 'docs/DEVELOPMENT.md'
        'documentation' = 'docs/CODEX.md'
    }
    foreach ($risk in @($TicketMetadata.risk_tags)) {
        if ($riskReferences.ContainsKey([string]$risk)) { Add-Unique $references $riskReferences[[string]$risk] }
    }
    return @($references)
}

function Get-SourceReferences([object]$TicketMetadata) {
    $references = New-Object System.Collections.Generic.List[string]
    if ($TicketMetadata.track -eq 'presentation') {
        Add-Unique $references 'src/Telengard.Core/presentation/'
        Add-Unique $references 'src/Telengard.Godot/'
    }
    return @($references)
}

function Get-RequiredReferences([object]$TicketMetadata) {
    $references = New-Object System.Collections.Generic.List[string]
    foreach ($reference in @($TicketMetadata.context.required)) {
        if ($reference -eq 'docs/presentation/GODOT_CLIENT_BLUEPRINT.md') {
            Add-Unique $references 'docs/presentation/GODOT_CLIENT_BLUEPRINT.md#presentation-contract'
        } else {
            Add-Unique $references ([string]$reference)
        }
    }
    Add-References $references (Get-SourceReferences $TicketMetadata)
    return @($references)
}

function Is-PresentationReference([string]$Reference) {
    return $Reference -match '^(docs/presentation/|src/Telengard\.Core/presentation/|src/Telengard\.Godot/)' -or $Reference -eq 'docs/ARCHITECTURE.md#architectural-boundary'
}

function Write-Section([string]$Name, [object[]]$Items) {
    if (@($Items).Count -eq 0) { return }
    Write-Output $Name
    foreach ($item in @($Items)) { Write-Output ([string]$item) }
    Write-Output ''
}

try {
    if (-not $RepositoryRoot) { $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    $root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $index = Read-Utf8 (Join-Path $root 'docs/tasks/index.json') | ConvertFrom-Json
    $metadata = @($index.tickets | Where-Object { $_.id -eq $Ticket })[0]
    if ($null -eq $metadata) { throw "Ticket '$Ticket' was not found in docs/tasks/index.json." }

    $required = New-Object System.Collections.Generic.List[string]
    Add-References $required (Get-RequiredReferences $metadata)
    $conditional = New-Object System.Collections.Generic.List[string]
    Add-References $conditional (Get-ConditionalReferences $metadata)
    foreach ($reference in @($conditional | Where-Object { $_ -match '^docs/(ARCHITECTURE|INVARIANTS)\.md#' })) {
        $null = $conditional.Remove(($reference -replace '#.*$', ''))
    }
    $review = New-Object System.Collections.Generic.List[string]
    Add-References $review $metadata.review.required
    Add-References $review $metadata.review.conditional
    $verify = New-Object System.Collections.Generic.List[string]
    if ($metadata.verification.headless) { Add-Unique $verify 'headless' }
    if ($metadata.verification.godot_manual) { Add-Unique $verify 'godot_manual' }

    if ($Lane) {
        if (-not $review.Contains($Lane)) { throw "Ticket '$Ticket' does not require review lane '$Lane'." }
        $laneRequired = New-Object System.Collections.Generic.List[string]
        foreach ($reference in @($required)) {
            if ($reference -match "^docs/tasks/$Ticket\.md$" -or (Is-PresentationReference $reference)) { Add-Unique $laneRequired $reference }
        }
        $laneConditional = New-Object System.Collections.Generic.List[string]
        foreach ($reference in @($conditional)) { if (Is-PresentationReference $reference) { Add-Unique $laneConditional $reference } }
        Write-Section 'REQUIRED' @($laneRequired)
        Write-Section 'CONDITIONAL' @($laneConditional)
        Write-Section 'REVIEW' @($Lane)
        exit 0
    }

    Write-Section 'REQUIRED' @($required)
    Write-Section 'CONDITIONAL' @($conditional)
    Write-Section 'REVIEW' @($review)
    Write-Section 'VERIFY' @($verify)
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
