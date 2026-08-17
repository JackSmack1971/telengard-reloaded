[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$DotNetArgs,
    [Parameter(Mandatory = $false)]
    [string]$WorkingDirectory
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$LocalDotNet = Join-Path $RepoRoot '.dotnet\dotnet.exe'

if (Test-Path -LiteralPath $LocalDotNet -PathType Leaf) {
    $DotNetExecutable = $LocalDotNet
} else {
    $system = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
    if (-not $system) {
        throw "No .NET executable found. Expected repository SDK at '$LocalDotNet'. Restore the repo-local .dotnet directory; do not modify machine PATH just for this repository."
    }
    $DotNetExecutable = $system.Source
    Write-Warning "Repository-local .dotnet SDK is missing; falling back to system dotnet at '$DotNetExecutable'."
}

$DotNetRoot = Split-Path -Parent $DotNetExecutable
$env:DOTNET_ROOT = $DotNetRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

$separator = [IO.Path]::PathSeparator
$entries = @($env:PATH -split [Regex]::Escape([string]$separator))
if ($entries -notcontains $DotNetRoot) {
    $env:PATH = "$DotNetRoot$separator$env:PATH"
}

if ($WorkingDirectory) {
    $executionDirectory = (Resolve-Path -LiteralPath $WorkingDirectory -ErrorAction Stop).Path
} else {
    $executionDirectory = $RepoRoot
}

Push-Location $executionDirectory
try {
    & $DotNetExecutable @DotNetArgs
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
exit $exitCode
