$ErrorActionPreference = 'SilentlyContinue'
$RepoRoot = (git rev-parse --show-toplevel 2>$null).Trim()
if (-not $RepoRoot) { exit 0 }

$localSdk = Test-Path -LiteralPath (Join-Path $RepoRoot '.dotnet\dotnet.exe')
$globalJson = Join-Path $RepoRoot 'global.json'
$sdkVersion = 'unknown'
if (Test-Path -LiteralPath $globalJson) {
    try { $sdkVersion = (Get-Content -LiteralPath $globalJson -Raw | ConvertFrom-Json).sdk.version } catch { }
}

$dirty = @(git -C $RepoRoot status --porcelain 2>$null).Count
$activePlans = 0
$activeDir = Join-Path $RepoRoot 'docs\exec-plans\active'
if (Test-Path -LiteralPath $activeDir) {
    $activePlans = @(Get-ChildItem -LiteralPath $activeDir -File -Filter '*.md' -ErrorAction SilentlyContinue).Count
}

Write-Output "Telengard session context: repo-local .NET SDK present=$localSdk; global.json SDK=$sdkVersion; dirty paths=$dirty; active ExecPlans=$activePlans. Never run bare dotnet. Use ./eng/dotnet.ps1 for .NET commands and ./eng/verify.ps1 -Mode Full before completing code changes. Read docs/AGENT_INDEX.md to route deeper context."
exit 0
