$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$CoverageScript = Join-Path $RepoRoot 'eng\coverage.ps1'
$MutationScript = Join-Path $RepoRoot 'eng\mutation.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Invoke-RepositoryScript {
    param(
        [Parameter(Mandatory = $true)][string]$Script,
        [Parameter(Mandatory = $true)][object[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Script @Arguments 2>&1 | Out-String)
        [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output = $output
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

Push-Location $RepoRoot
try {
    Write-Host '== Coverage role aggregation =='
    $coverageRun = Invoke-RepositoryScript -Script $CoverageScript -Arguments @('-Configuration', 'Release')
    $coverageSummaryPath = Join-Path $RepoRoot 'TestResults\coverage\coverage-summary.json'
    Assert-True (Test-Path -LiteralPath $coverageSummaryPath) 'Coverage did not produce coverage-summary.json.'

    $coverageSummary = Get-Content -Raw -LiteralPath $coverageSummaryPath | ConvertFrom-Json
    $rows = @($coverageSummary.Files)
    $productionRows = @($rows | Where-Object Role -eq 'Production')
    $testSupportRows = @($rows | Where-Object Role -eq 'TestSupport')
    Assert-True ($productionRows.Count -gt 0) 'Coverage reported no production rows.'
    Assert-True ($testSupportRows.Count -gt 0) 'Coverage did not report TestHarness rows.'
    Assert-True (($testSupportRows | Select-Object -ExpandProperty Project -Unique) -eq 'Telengard.TestHarness') 'Test-support rows were not limited to TestHarness.'

    $allLinesValid = 0
    $productionLinesValid = 0
    foreach ($row in $rows | Where-Object Lines -ne 'n/a') {
        $allLinesValid += [int](($row.Lines -split '/')[1])
    }
    foreach ($row in $productionRows | Where-Object Lines -ne 'n/a') {
        $productionLinesValid += [int](($row.Lines -split '/')[1])
    }
    Assert-True ($coverageSummary.LinesValid -eq $productionLinesValid) 'The top-level coverage total includes non-production rows.'
    Assert-True ($coverageSummary.ProductionSummary.LinesValid -eq $productionLinesValid) 'The named production coverage summary is missing or incorrect.'
    Assert-True ($coverageSummary.TestSupportSummary.LinesValid -gt 0) 'Test-support coverage totals are not visible separately.'
    Assert-True ($allLinesValid -gt $coverageSummary.LinesValid) 'TestHarness coverage was not excluded from the production total.'

    $productionComplete = $coverageSummary.LinesCovered -eq $coverageSummary.LinesValid -and
        $coverageSummary.BranchesCovered -eq $coverageSummary.BranchesValid
    $expectedCoverageExitCode = if ($productionComplete) { 0 } else { 1 }
    Assert-True ($coverageRun.ExitCode -eq $expectedCoverageExitCode) "Coverage gate exit code $($coverageRun.ExitCode) did not match the production totals. Output: $($coverageRun.Output)"

    $coverageMarkdown = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'TestResults\coverage\coverage-summary.md')
    Assert-True ($coverageMarkdown.Contains('Production (gated)')) 'Coverage Markdown omitted the production aggregate.'
    Assert-True ($coverageMarkdown.Contains('Test-support (informational)')) 'Coverage Markdown omitted the test-support aggregate.'

    Write-Host '== Scoped mutation guards =='
    $sinceGuard = Invoke-RepositoryScript -Script $MutationScript -Arguments @(
        '-Project', 'Telengard.Terminal',
        '-MutationLevel', 'Basic',
        '-AdditionalStrykerArgs', '--since=HEAD~1'
    )
    Assert-True ($sinceGuard.ExitCode -ne 0) '--since with the default mutation-baseline directory was accepted.'
    Assert-True ($sinceGuard.Output -match 'Scoped Stryker arguments') 'The --since guard did not explain the baseline-directory restriction.'

    $baselineGuard = Invoke-RepositoryScript -Script $MutationScript -Arguments @(
        '-Project', 'Telengard.Terminal',
        '-MutationLevel', 'Basic',
        '-AdditionalStrykerArgs', '--with-baseline=mutation-baseline'
    )
    Assert-True ($baselineGuard.ExitCode -ne 0) '--with-baseline with the default mutation-baseline directory was accepted.'
    Assert-True ($baselineGuard.Output -match 'Scoped Stryker arguments') 'The --with-baseline guard did not explain the baseline-directory restriction.'

    Write-Host '== Valid scoped mutation run =='
    $scopedDirectory = 'mutation-tel-117-scoped'
    $scopedTarget = (git rev-parse HEAD^).Trim()
    $scopedRun = Invoke-RepositoryScript -Script $MutationScript -Arguments @(
        '-Project', 'Telengard.Terminal',
        '-MutationLevel', 'Basic',
        '-ResultsDirectoryName', $scopedDirectory,
        '-AdditionalStrykerArgs', "--since=$scopedTarget"
    )
    Assert-True ($scopedRun.ExitCode -eq 0) "Valid scoped Stryker invocation failed. Output: $($scopedRun.Output)"
    $manifestPath = Join-Path $RepoRoot "TestResults\$scopedDirectory\mutation-manifest.json"
    Assert-True (Test-Path -LiteralPath $manifestPath) 'Scoped mutation run did not produce its manifest.'
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    Assert-True ($manifest.AdditionalStrykerArgs -contains "--since=$scopedTarget") 'Scoped mutation metadata did not record the forwarded argument.'
}
finally {
    Pop-Location
}

Write-Host 'TEL-117 tooling regression tests passed.'
