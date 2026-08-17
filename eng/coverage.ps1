[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DotNetScript = Join-Path $RepoRoot 'eng\dotnet.ps1'
$ResultsDirectory = Join-Path $RepoRoot 'TestResults\coverage'

function Invoke-RepositoryDotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $DotNetScript @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Repository dotnet command failed with exit code $LASTEXITCODE."
    }
}

if (Test-Path -LiteralPath $ResultsDirectory) {
    $resolvedResults = (Resolve-Path -LiteralPath $ResultsDirectory).Path
    if ($resolvedResults -ne $ResultsDirectory) {
        throw "Coverage output path resolved outside the repository: '$resolvedResults'."
    }

    Remove-Item -LiteralPath $ResultsDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

Write-Host '== Coverage restore =='
Invoke-RepositoryDotNet @('restore', 'Telengard.sln')

Write-Host '== Coverage test run =='
Invoke-RepositoryDotNet @(
    'test', 'Telengard.sln',
    '--configuration', $Configuration,
    '--no-restore',
    '--collect:XPlat Code Coverage',
    '--results-directory', $ResultsDirectory,
    '--logger', 'console;verbosity=minimal'
)

$coverageFile = Get-ChildItem -LiteralPath $ResultsDirectory -Filter 'coverage.cobertura.xml' -Recurse |
    Select-Object -First 1
if (-not $coverageFile) {
    throw "Coverage collector did not produce coverage.cobertura.xml under '$ResultsDirectory'."
}

$coverage = [xml](Get-Content -Raw -LiteralPath $coverageFile.FullName)
$projects = @(
    @{ Name = 'Telengard.Core'; Root = Join-Path $RepoRoot 'src\Telengard.Core' },
    @{ Name = 'Telengard.Content'; Root = Join-Path $RepoRoot 'src\Telengard.Content' },
    @{ Name = 'Telengard.Save'; Root = Join-Path $RepoRoot 'src\Telengard.Save' },
    @{ Name = 'Telengard.Terminal'; Root = Join-Path $RepoRoot 'src\Telengard.Terminal' },
    @{ Name = 'Telengard.TestHarness'; Root = Join-Path $RepoRoot 'tools\Telengard.TestHarness' }
)

function Normalize-Path([string]$Path) {
    return $Path.Replace('\', '/').TrimStart('./')
}

function Get-CoverageNumbers($Lines) {
    $lineList = @($Lines)
    $lineCovered = @($lineList | Where-Object { [int]$_.hits -gt 0 }).Count
    $branchLines = @($lineList | Where-Object { $_.branch -eq 'true' })
    $branchesValid = 0
    $branchesCovered = 0
    foreach ($line in $branchLines) {
        $match = [regex]::Match([string]$line.'condition-coverage', '\((\d+)\/(\d+)\)')
        if (-not $match.Success) {
            throw "Cannot parse branch coverage '$($line.'condition-coverage')' at line $($line.number)."
        }

        $branchesCovered += [int]$match.Groups[1].Value
        $branchesValid += [int]$match.Groups[2].Value
    }

    return [ordered]@{
        LinesValid = $lineList.Count
        LinesCovered = $lineCovered
        BranchesValid = $branchesValid
        BranchesCovered = $branchesCovered
    }
}

$classes = @($coverage.coverage.packages.package | ForEach-Object { $_.classes.class })
$rows = [System.Collections.Generic.List[object]]::new()

foreach ($project in $projects) {
    $sourceFiles = @(Get-ChildItem -LiteralPath $project.Root -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

    if ($sourceFiles.Count -eq 0) {
        $rows.Add([pscustomobject]@{
            Project = $project.Name
            File = '(no hand-written C# files)'
            Lines = 'n/a'
            Branches = 'n/a'
            Status = 'PASS'
        })
        continue
    }

    foreach ($sourceFile in $sourceFiles) {
        $relative = Normalize-Path $sourceFile.FullName.Substring($RepoRoot.Length + 1)
        $matchingClasses = @($classes | Where-Object { (Normalize-Path $_.filename) -eq $relative })
        $lineNodes = @($matchingClasses | ForEach-Object { $_.methods.method | ForEach-Object { $_.lines.line } })
        $numbers = Get-CoverageNumbers $lineNodes
        $lineStatus = "$($numbers.LinesCovered)/$($numbers.LinesValid)"
        $branchStatus = "$($numbers.BranchesCovered)/$($numbers.BranchesValid)"
        $pass = ($numbers.LinesCovered -eq $numbers.LinesValid) -and
            ($numbers.BranchesCovered -eq $numbers.BranchesValid)
        $rows.Add([pscustomobject]@{
            Project = $project.Name
            File = $relative
            Lines = $lineStatus
            Branches = $branchStatus
            Status = if ($pass) { 'PASS' } else { 'FAIL' }
        })
    }
}

$measuredRows = @($rows | Where-Object { $_.Lines -ne 'n/a' })
$linesValid = 0
$linesCovered = 0
$branchesValid = 0
$branchesCovered = 0
foreach ($row in $measuredRows) {
    $lineParts = $row.Lines -split '/'
    $branchParts = $row.Branches -split '/'
    $linesCovered += [int]$lineParts[0]
    $linesValid += [int]$lineParts[1]
    $branchesCovered += [int]$branchParts[0]
    $branchesValid += [int]$branchParts[1]
}

$lineRate = if ($linesValid -eq 0) { 1 } else { $linesCovered / $linesValid }
$branchRate = if ($branchesValid -eq 0) { 1 } else { $branchesCovered / $branchesValid }
$summary = [ordered]@{
    Configuration = $Configuration
    CoverageFile = $coverageFile.FullName
    LinesCovered = $linesCovered
    LinesValid = $linesValid
    LineRate = $lineRate
    BranchesCovered = $branchesCovered
    BranchesValid = $branchesValid
    BranchRate = $branchRate
    Files = @($rows)
}

$jsonPath = Join-Path $ResultsDirectory 'coverage-summary.json'
$markdownPath = Join-Path $ResultsDirectory 'coverage-summary.md'
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add('# Telengard coverage')
$markdown.Add('')
$markdown.Add("Overall: **$linesCovered/$linesValid lines ($('{0:P2}' -f $lineRate))**, **$branchesCovered/$branchesValid branches ($('{0:P2}' -f $branchRate))**")
$markdown.Add('')
$markdown.Add('| Project | Source file | Lines | Branches | Status |')
$markdown.Add('| --- | --- | ---: | ---: | --- |')
foreach ($row in $rows) {
    $markdown.Add("| $($row.Project) | $($row.File) | $($row.Lines) | $($row.Branches) | $($row.Status) |")
}
$markdown -join [Environment]::NewLine | Set-Content -LiteralPath $markdownPath -Encoding UTF8

$rows | Format-Table -AutoSize | Out-String | Write-Host
Write-Host "Overall: $linesCovered/$linesValid lines ($('{0:P2}' -f $lineRate)); $branchesCovered/$branchesValid branches ($('{0:P2}' -f $branchRate))."

if ($linesCovered -ne $linesValid -or $branchesCovered -ne $branchesValid) {
    throw 'Coverage target failed: every in-scope hand-written production line and branch must be covered.'
}

Write-Host "Coverage passed. Reports: $jsonPath and $markdownPath"
