[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Basic', 'Standard', 'Advanced', 'Complete')]
    [string]$MutationLevel = 'Standard',
    [ValidateSet('All', 'Telengard.Core', 'Telengard.Content', 'Telengard.Save', 'Telengard.Terminal')]
    [string]$Project = 'All',
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ResultsDirectoryName = 'mutation-baseline',
    [string[]]$AdditionalStrykerArgs = @()
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DotNetScript = Join-Path $RepoRoot 'eng\dotnet.ps1'
$MsBuildPath = Join-Path $RepoRoot '.dotnet\sdk\8.0.100\MSBuild.dll'

if ($ResultsDirectoryName -eq 'mutation-baseline') {
    $scopedOptions = @($AdditionalStrykerArgs | Where-Object {
        $option = ([string]$_ -split '=', 2)[0]
        $option -in @('--since', '--with-baseline')
    })
    if ($scopedOptions.Count -gt 0) {
        throw "Scoped Stryker arguments ($($scopedOptions -join ', ')) require a non-default -ResultsDirectoryName so they cannot overwrite the full mutation baseline."
    }
}

$ResultsDirectory = Join-Path $RepoRoot (Join-Path 'TestResults' $ResultsDirectoryName)
$ResultsPrefix = "TestResults/$ResultsDirectoryName/"

$projects = @(
    [pscustomobject]@{ Name = 'Telengard.Core'; Directory = 'src\Telengard.Core' },
    [pscustomobject]@{ Name = 'Telengard.Content'; Directory = 'src\Telengard.Content' },
    [pscustomobject]@{ Name = 'Telengard.Save'; Directory = 'src\Telengard.Save' },
    [pscustomobject]@{ Name = 'Telengard.Terminal'; Directory = 'src\Telengard.Terminal' }
)
if ($Project -ne 'All') {
    $projects = @($projects | Where-Object Name -eq $Project)
}

function Invoke-RepositoryDotNet {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$WorkingDirectory
    )

    $scriptArguments = @{}
    if ($WorkingDirectory) {
        $scriptArguments.WorkingDirectory = $WorkingDirectory
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $DotNetScript @scriptArguments @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Repository dotnet command failed with exit code $LASTEXITCODE."
    }
}

if (Test-Path -LiteralPath $ResultsDirectory) {
    $resolvedResults = (Resolve-Path -LiteralPath $ResultsDirectory).Path
    if ($resolvedResults -ne $ResultsDirectory) {
        throw "Mutation output path resolved outside the repository: '$resolvedResults'."
    }

    Remove-Item -LiteralPath $ResultsDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

Write-Host '== Mutation tool restore =='
Invoke-RepositoryDotNet @('tool', 'restore')

$summaries = [System.Collections.Generic.List[object]]::new()
foreach ($targetProject in $projects) {
    $projectDirectory = Join-Path $RepoRoot $targetProject.Directory
    $projectOutput = Join-Path $ResultsDirectory $targetProject.Name
    New-Item -ItemType Directory -Path $projectOutput -Force | Out-Null

    Write-Host "== Mutation run ($MutationLevel): $($targetProject.Name) =="
    Push-Location $projectDirectory
    try {
        Invoke-RepositoryDotNet -WorkingDirectory $projectDirectory -Arguments (@(
            'stryker',
            '--config-file', 'stryker-config.json',
            '--configuration', $Configuration,
            '--mutation-level', $MutationLevel,
            '--msbuild-path', $MsBuildPath,
            '--concurrency', '1',
            '--output', $projectOutput,
            '--reporter', 'progress',
            '--reporter', 'json',
            '--reporter', 'html',
            '--reporter', 'markdown',
            '--skip-version-check'
        ) + @($AdditionalStrykerArgs))
    } finally {
        Pop-Location
    }

    $jsonReport = Get-ChildItem -LiteralPath $projectOutput -Filter 'mutation-report.json' -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $markdownReport = Get-ChildItem -LiteralPath $projectOutput -Filter 'mutation-report.md' -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $htmlReport = Get-ChildItem -LiteralPath $projectOutput -Filter 'mutation-report.html' -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1

    if (-not $jsonReport -or -not $markdownReport -or -not $htmlReport) {
        throw "Stryker did not produce the expected JSON, Markdown, and HTML reports for $($targetProject.Name) under '$projectOutput'."
    }

    $report = Get-Content -Raw -LiteralPath $jsonReport.FullName | ConvertFrom-Json
    $markdownText = Get-Content -Raw -LiteralPath $markdownReport.FullName
    $scoreMatch = [regex]::Match($markdownText, 'final mutation score is\s+([0-9.]+)%', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $mutationScore = if ($scoreMatch.Success) { [double]$scoreMatch.Groups[1].Value / 100 } else { $null }
    $mutants = @($report.files.PSObject.Properties | ForEach-Object { $_.Value.mutants })
    $knownStatuses = @('Killed', 'Survived', 'NoCoverage', 'Timeout', 'CompileError', 'Ignored')
    $statusCounts = [ordered]@{}
    foreach ($status in @($mutants | Select-Object -ExpandProperty status -Unique)) {
        $statusCounts[$status] = @($mutants | Where-Object status -eq $status).Count
    }
    $summaries.Add([pscustomobject]@{
        Project = $targetProject.Name
        JsonReport = $jsonReport.FullName.Substring($RepoRoot.Length + 1).Replace('\', '/')
        MarkdownReport = $markdownReport.FullName.Substring($RepoRoot.Length + 1).Replace('\', '/')
        HtmlReport = $htmlReport.FullName.Substring($RepoRoot.Length + 1).Replace('\', '/')
        MutationScore = $mutationScore
        Mutants = $mutants.Count
        Killed = @($mutants | Where-Object status -eq 'Killed').Count
        Survived = @($mutants | Where-Object status -eq 'Survived').Count
        NoCoverage = @($mutants | Where-Object status -eq 'NoCoverage').Count
        Timeout = @($mutants | Where-Object status -eq 'Timeout').Count
        CompileError = @($mutants | Where-Object status -eq 'CompileError').Count
        Ignored = @($mutants | Where-Object status -eq 'Ignored').Count
        Other = @($mutants | Where-Object { $_.status -notin $knownStatuses }).Count
        StatusCounts = $statusCounts
    })
}

$summaryJson = Join-Path $ResultsDirectory 'mutation-summary.json'
$summaryMarkdown = Join-Path $ResultsDirectory 'mutation-summary.md'
$summaries | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryJson -Encoding UTF8
$manifest = [ordered]@{
    Tool = 'dotnet-stryker'
    ToolVersion = '4.14.2'
    SdkVersion = '8.0.100'
    MutationLevel = $MutationLevel
    Configuration = $Configuration
    AdditionalStrykerArgs = @($AdditionalStrykerArgs)
    ResultsDirectory = "TestResults/$ResultsDirectoryName"
    TestProjects = @('tests/Telengard.Architecture.Tests/Telengard.Architecture.Tests.csproj')
    ProductionProjects = @($projects | ForEach-Object { $_.Name })
    Exclusions = @(
        'tools/Telengard.TestHarness is tooling and is outside the production mutation scope.'
        'src/Telengard.Godot contains no C# source in the solution scope.'
    )
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ResultsDirectory 'mutation-manifest.json') -Encoding UTF8

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add("# Telengard mutation-testing $MutationLevel results")
$markdown.Add('')
$markdown.Add("Stryker.NET 4.14.2, $MutationLevel mutation level, $Configuration configuration. Repository-local SDK 8.0.100. No score gate or mutation exclusions were applied.")
$markdown.Add('')
$markdown.Add('| Project | Score | Total | Killed | Survived | No coverage | Timeout | Compile error | Ignored | Other | Reports |')
$markdown.Add('| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |')
foreach ($summary in $summaries) {
    $score = if ($null -eq $summary.MutationScore) { 'n/a' } else { '{0:P2}' -f [double]$summary.MutationScore }
    $jsonLink = $summary.JsonReport.Substring($ResultsPrefix.Length)
    $markdownLink = $summary.MarkdownReport.Substring($ResultsPrefix.Length)
    $htmlLink = $summary.HtmlReport.Substring($ResultsPrefix.Length)
    $markdown.Add("| $($summary.Project) | $score | $($summary.Mutants) | $($summary.Killed) | $($summary.Survived) | $($summary.NoCoverage) | $($summary.Timeout) | $($summary.CompileError) | $($summary.Ignored) | $($summary.Other) | [$($summary.Project) JSON]($jsonLink); [$($summary.Project) Markdown]($markdownLink); [$($summary.Project) HTML]($htmlLink) |")
}
$markdown.Add('')
$markdown.Add('`Telengard.TestHarness` is a tooling project and is not part of the production mutation scope. The Godot project contains no C# source in the solution scope.')
$markdown -join [Environment]::NewLine | Set-Content -LiteralPath $summaryMarkdown -Encoding UTF8

$auditEntries = [System.Collections.Generic.List[object]]::new()
foreach ($summary in $summaries) {
    $report = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot ($summary.JsonReport -replace '/', '\')) | ConvertFrom-Json
    foreach ($file in $report.files.PSObject.Properties) {
        $sourceLines = [string]$file.Value.source -split "`r?`n"
        foreach ($mutant in @($file.Value.mutants) | Where-Object status -ne 'Killed') {
            $line = [int]$mutant.location.start.line
            $sourceText = if ($line -le $sourceLines.Count) { $sourceLines[$line - 1].Trim() } else { '' }
            $category = switch ($mutant.status) {
                'Survived' {
                    if ($mutant.mutatorName -like '*String*' -and $sourceText -match 'throw new|Exception') {
                        'Intentionally unobservable implementation detail'
                    } else {
                        'Actionable test weakness'
                    }
                }
                'NoCoverage' { 'Actionable test weakness' }
                'Timeout' { 'Timeout' }
                'CompileError' { 'Tooling limitation' }
                'Ignored' { 'Equivalent mutation / Stryker covered-block optimization' }
                default { 'Other justified category' }
            }
            $rationale = switch ($category) {
                'Actionable test weakness' { 'The mutation survived or had no covering test; behavior remains unprotected by the existing suite.' }
                'Intentionally unobservable implementation detail' { 'The mutation changes only an exception diagnostic string; no diagnostic wording is a game, save, or API contract.' }
                'Timeout' { 'Stryker could not complete the mutated test run within its configured timeout; retain for a tooling/test-isolation follow-up.' }
                'Tooling limitation' { 'The generated mutation did not compile; record it as a Stryker compile-error result rather than treating it as killed.' }
                'Equivalent mutation / Stryker covered-block optimization' { [string]$mutant.statusReason }
                default { [string]$mutant.statusReason }
            }
            $auditEntries.Add([pscustomobject]@{
                Project = $summary.Project
                SourceFile = $file.Name.Substring($RepoRoot.Length + 1).Replace('\', '/')
                Line = $line
                Column = [int]$mutant.location.start.column
                MutantId = [string]$mutant.id
                Mutator = [string]$mutant.mutatorName
                Replacement = [string]$mutant.replacement
                Status = [string]$mutant.status
                StatusReason = [string]$mutant.statusReason
                Category = $category
                Rationale = $rationale
            })
        }
    }
}

$auditJson = Join-Path $ResultsDirectory 'mutation-audit.json'
$auditMarkdown = Join-Path $ResultsDirectory 'mutation-audit.md'
$auditEntries | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $auditJson -Encoding UTF8
$audit = [System.Collections.Generic.List[string]]::new()
$audit.Add("# Telengard mutation $MutationLevel audit")
$audit.Add('')
$audit.Add("This audit lists every non-killed mutant from the $MutationLevel run. No mutant category, method, or hand-written production file was excluded by repository configuration.")
$audit.Add('')
$audit.Add('| Category | Count |')
$audit.Add('| --- | ---: |')
foreach ($group in @($auditEntries | Group-Object Category | Sort-Object Name)) {
    $audit.Add("| $($group.Name) | $($group.Count) |")
}
$audit.Add('')
$audit.Add('| Project | Source location | Status | Category | Mutator | Replacement | Reason |')
$audit.Add('| --- | --- | --- | --- | --- | --- | --- |')
foreach ($entry in @($auditEntries | Sort-Object Project,SourceFile,Line,MutantId)) {
    $replacement = ([string]$entry.Replacement).Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
    $reason = ([string]$entry.Rationale).Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
    $audit.Add("| $($entry.Project) | $($entry.SourceFile):$($entry.Line):$($entry.Column) | $($entry.Status) | $($entry.Category) | $($entry.Mutator) | $replacement | $reason |")
}
$audit.Add('')
$audit.Add('`Telengard.Terminal` has a hand-written but empty `Main` method and produced zero mutants. `Telengard.TestHarness` is tooling and is outside the production mutation scope. The Godot project contains no C# source in the solution scope.')
$audit -join [Environment]::NewLine | Set-Content -LiteralPath $auditMarkdown -Encoding UTF8

Write-Host "Mutation baseline reports: $summaryJson, $summaryMarkdown, $auditJson, and $auditMarkdown"
