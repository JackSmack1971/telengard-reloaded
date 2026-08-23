$ErrorActionPreference = 'SilentlyContinue'

if ($env:CODEX_SKIP_VERIFY_GUARD -eq '1') { exit 0 }

$RepoRoot = (git rev-parse --show-toplevel 2>$null).Trim()
if (-not $RepoRoot) { exit 0 }
. (Join-Path $RepoRoot 'eng\common.ps1')

$changed = @(Get-TelengardVerificationFiles -RepoRoot $RepoRoot)
if ($changed.Count -eq 0) { exit 0 }

$fingerprint = Get-TelengardVerificationFingerprint -RepoRoot $RepoRoot
$stampPath = Join-Path $RepoRoot '.codex\.verify-stamp.json'
$verified = $false
$stampPresent = Test-Path -LiteralPath $stampPath
$verifiedAtUtc = $null
if ($stampPresent) {
    try {
        $stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
        $verified = ($stamp.mode -eq 'Full' -and $stamp.fingerprint -eq $fingerprint)
        if ($stamp.verifiedAtUtc) { $verifiedAtUtc = [DateTime]::Parse($stamp.verifiedAtUtc).ToUniversalTime() }
    } catch { $verified = $false }
}
if (-not $verified -and $verifiedAtUtc) {
    $verified = $true
    foreach ($relative in $changed) {
        $full = Join-Path $RepoRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $full -PathType Leaf -and (Get-Item -LiteralPath $full).LastWriteTimeUtc -gt $verifiedAtUtc) {
            $verified = $false
            break
        }
    }
}
if ($verified) { exit 0 }

$raw = [Console]::In.ReadToEnd()
try { $event = $raw | ConvertFrom-Json } catch { $event = $null }

$alreadyContinued = $false
if ($event -and $event.stop_hook_active) { $alreadyContinued = [bool]$event.stop_hook_active }

if ($alreadyContinued) {
    @{ systemMessage = 'Telengard verification guard: source/build/test changes remain unverified or changed after verification. Report this explicitly if verification cannot be completed.' } | ConvertTo-Json -Compress
    exit 0
}

$preview = ($changed | Select-Object -First 8) -join ', '
if ($changed.Count -gt 8) { $preview += ", +$($changed.Count - 8) more" }
$verificationState = if ($stampPresent) {
    'the existing Full verification stamp does not match the current code-relevant files'
} else {
    'no successful Full verification stamp exists for the current code-relevant files'
}
@{
    decision = 'block'
    reason = "Telengard verification required: $verificationState ($preview). Run ./eng/verify.ps1 -Mode Full, fix failures, inspect the final diff, then conclude. If verification is genuinely impossible, make one best-effort pass and report exactly what could not run."
} | ConvertTo-Json -Compress
exit 0
