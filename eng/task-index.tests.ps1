[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$generator = Join-Path $repoRoot 'eng\task-index.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("telengard-task-index-" + [Guid]::NewGuid().ToString('N'))
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
            -LedgerPath (Join-Path $tempRoot 'README.md') `
            -IndexPath (Join-Path $tempRoot 'index.json') `
            -OverridesPath (Join-Path $tempRoot 'overrides.json') 2>&1
    } finally {
        $ErrorActionPreference = $previousErrorAction
    }
    return $LASTEXITCODE
}

try {
    Copy-Item (Join-Path $repoRoot 'docs\tasks\README.md') (Join-Path $tempRoot 'README.md')
    Copy-Item (Join-Path $repoRoot 'docs\tasks\index-overrides.json') (Join-Path $tempRoot 'overrides.json')

    $firstCode = Invoke-Generator -Mode Generate
    Assert-Condition ($firstCode -eq 0) 'Initial task-index generation failed.'
    $first = Read-Utf8 (Join-Path $tempRoot 'index.json')
    $firstBytes = [System.IO.File]::ReadAllBytes((Join-Path $tempRoot 'index.json'))

    $secondCode = Invoke-Generator -Mode Generate
    Assert-Condition ($secondCode -eq 0) 'Repeated task-index generation failed.'
    Assert-Condition ([System.Linq.Enumerable]::SequenceEqual($firstBytes, [System.IO.File]::ReadAllBytes((Join-Path $tempRoot 'index.json')))) 'Repeated task-index generation is not byte-stable.'

    $index = $first | ConvertFrom-Json
    Assert-Condition ($index.schema_version -eq 1) 'Generated index schema version is incorrect.'
    Assert-Condition ($index.completed.Count -ge 70) 'Generated index omitted ledger scheduling data.'
    $tel001 = @($index.tickets | Where-Object { $_.id -eq 'TEL-001' })
    $tel111 = @($index.tickets | Where-Object { $_.id -eq 'TEL-111' })
    $tel112 = @($index.tickets | Where-Object { $_.id -eq 'TEL-112' })
    $tel113 = @($index.tickets | Where-Object { $_.id -eq 'TEL-113' })
    $tel114 = @($index.tickets | Where-Object { $_.id -eq 'TEL-114' })
    $tel116 = @($index.tickets | Where-Object { $_.id -eq 'TEL-116' })
    $tel120 = @($index.tickets | Where-Object { $_.id -eq 'TEL-120' })
    $tel121 = @($index.tickets | Where-Object { $_.id -eq 'TEL-121' })
    $tel127 = @($index.tickets | Where-Object { $_.id -eq 'TEL-127' })
    Assert-Condition ($tel001.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-001' }).Count -eq 1) 'Completed ledger entry was not compacted.'
    Assert-Condition ($tel111.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-111' }).Count -eq 1) 'Completed monster-roster entry was not compacted.'
    Assert-Condition ($tel112.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-112' }).Count -eq 1) 'Completed encounter-ecology entry was not compacted.'
    Assert-Condition ($tel113.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-113' }).Count -eq 1) 'Completed item-roster entry was not compacted.'
    Assert-Condition ($tel114.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-114' }).Count -eq 1) 'Completed loot-table entry was not compacted.'
    Assert-Condition ($tel116.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-116' }).Count -eq 1) 'Completed feature-definition entry was not compacted.'
    Assert-Condition ($tel120.Count -eq 0 -and @($index.completed | Where-Object { $_ -eq 'TEL-120' }).Count -eq 1) 'Completed host ticket was not compacted.'
    Assert-Condition ($tel121.Count -eq 1 -and $tel121[0].track -eq 'presentation' -and $tel121[0].decision_state -eq 'ready') 'Playable-client scheduling metadata was not projected.'
    Assert-Condition (@($tel121[0].risk_tags).Count -gt 0 -and @($tel121[0].review.conditional) -contains 'presentation' -and $tel121[0].verification.godot_manual) 'Effective risk, review, and verification policy was not serialized.'
    Assert-Condition ($tel127.Count -eq 1 -and $tel127[0].decision_state -eq 'blocked' -and $tel127[0].blocker -match 'TEL-121') 'Dependency-derived blocker metadata was not projected.'
    Assert-Condition ($index.context_template -eq 'docs/tasks/{id}.md') 'Ticket context template is missing.'
    Assert-Condition ($index.conditional_context_by_risk.'save-compatibility' -eq 'save') 'Risk-to-context routing is missing.'

    $badOverrides = '{"schema_version":1,"milestone":"core-alpha","tickets":{"TEL-109":{"dependencies":["TEL-999"]}}}'
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'overrides.json'), $badOverrides, (New-Object System.Text.UTF8Encoding($false)))
    $badCode = Invoke-Generator -Mode Generate
    Assert-Condition ($badCode -ne 0) 'Invalid dependency metadata did not fail generation.'
    Copy-Item (Join-Path $repoRoot 'docs\tasks\index-overrides.json') (Join-Path $tempRoot 'overrides.json') -Force

    $emptyRiskOverrides = '{"schema_version":1,"milestone":"core-alpha","tickets":{"TEL-109":{"risk_tags":[]}}}'
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'overrides.json'), $emptyRiskOverrides, (New-Object System.Text.UTF8Encoding($false)))
    $emptyRiskCode = Invoke-Generator -Mode Generate
    Assert-Condition ($emptyRiskCode -ne 0) 'Empty risk-tag metadata did not fail generation.'
    Copy-Item (Join-Path $repoRoot 'docs\tasks\index-overrides.json') (Join-Path $tempRoot 'overrides.json') -Force

    $traversalOverrides = '{"schema_version":1,"milestone":"core-alpha","tickets":{"TEL-109":{"context":{"required":["../../AGENTS.md"],"conditional":{}}}}}'
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'overrides.json'), $traversalOverrides, (New-Object System.Text.UTF8Encoding($false)))
    $traversalCode = Invoke-Generator -Mode Generate
    Assert-Condition ($traversalCode -ne 0) 'Traversal context metadata did not fail generation.'
    Copy-Item (Join-Path $repoRoot 'docs\tasks\index-overrides.json') (Join-Path $tempRoot 'overrides.json') -Force

    $rootedOverrides = '{"schema_version":1,"milestone":"core-alpha","tickets":{"TEL-109":{"context":{"required":["C:\\outside.md"],"conditional":{}}}}}'
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'overrides.json'), $rootedOverrides, (New-Object System.Text.UTF8Encoding($false)))
    $rootedCode = Invoke-Generator -Mode Generate
    Assert-Condition ($rootedCode -ne 0) 'Rooted context metadata did not fail generation.'
    Copy-Item (Join-Path $repoRoot 'docs\tasks\index-overrides.json') (Join-Path $tempRoot 'overrides.json') -Force

    $stale = $first.Replace('"milestone":"core-alpha"', '"milestone":"stale"')
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'index.json'), $stale, (New-Object System.Text.UTF8Encoding($false)))
    $checkCode = Invoke-Generator -Mode Check
    Assert-Condition ($checkCode -ne 0) 'Stale task index did not fail check mode.'

    $crlf = $first.Replace("`n", "`r`n")
    [System.IO.File]::WriteAllText((Join-Path $tempRoot 'index.json'), $crlf, (New-Object System.Text.UTF8Encoding($false)))
    $lineEndingCode = Invoke-Generator -Mode Check
    Assert-Condition ($lineEndingCode -eq 0) 'Equivalent CRLF task index did not pass check mode.'

    $restoreCode = Invoke-Generator -Mode Generate
    Assert-Condition ($restoreCode -eq 0) 'Task-index restoration failed.'
    Write-Host 'Task-index tests passed: parsing, compact projection, deterministic generation, and stale detection.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
