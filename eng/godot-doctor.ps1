[CmdletBinding()]
param()

$ErrorActionPreference = 'SilentlyContinue'
$candidatePaths = [System.Collections.Generic.List[string]]::new()

foreach ($commandName in @('godot', 'godot4')) {
    $command = Get-Command $commandName -CommandType Application
    if ($command) { $candidatePaths.Add($command.Source) }
}

$candidateRoots = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Godot'),
    (Join-Path $env:ProgramFiles 'Godot'),
    (Join-Path ${env:ProgramFiles(x86)} 'Godot'),
    (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages')
)
foreach ($root in $candidateRoots) {
    if (Test-Path -LiteralPath $root) {
        Get-ChildItem -LiteralPath $root -Filter 'godot*.exe' -File -Recurse | ForEach-Object { $candidatePaths.Add($_.FullName) }
    }
}

$wingetMatches = @()
$winget = Get-Command winget -CommandType Application
if ($winget) {
    $wingetMatches = @( & $winget.Source list --name Godot --accept-source-agreements --disable-interactivity 2>$null )
}

$executables = @($candidatePaths | Sort-Object -Unique | ForEach-Object {
    $version = & $_ --version 2>$null | Select-Object -First 1
    [ordered]@{ path = $_; version = [string]$version }
})

[ordered]@{
    available = $executables.Count -gt 0
    executables = $executables
    winget_matches = $wingetMatches
} | ConvertTo-Json -Depth 4

if ($executables.Count -eq 0) { exit 1 }
