Set-StrictMode -Version Latest

function Get-TelengardRepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-TelengardVerificationFiles {
    param([Parameter(Mandatory=$true)][string]$RepoRoot)

    $gitDir = git -C $RepoRoot rev-parse --git-dir 2>$null
    if ($LASTEXITCODE -ne 0) { return @() }

    $paths = @()
    $headPath = Join-Path $RepoRoot '.git\HEAD'
    $hasHead = $false
    if (Test-Path -LiteralPath $headPath -PathType Leaf) {
        $head = Get-Content -Raw -LiteralPath $headPath
        $hasHead = if ($head -match '^ref: (.+)\s*$') {
            Test-Path -LiteralPath (Join-Path $RepoRoot (Join-Path '.git' $Matches[1])) -PathType Leaf
        } else {
            $true
        }
    }

    if ($hasHead) {
        $paths += @(git -C $RepoRoot diff --name-only HEAD -- 2>$null)
    } else {
        $paths += @(git -C $RepoRoot diff --name-only -- 2>$null)
    }
    $paths += @(git -C $RepoRoot ls-files --others --exclude-standard 2>$null)
    $paths = $paths | Where-Object { $_ } | Sort-Object -Unique

    $relevant = foreach ($p in $paths) {
        $normalized = $p -replace '\\','/'
        if (
            $normalized -match '^(src|tests|tools)/' -and $normalized -match '\.(cs|csproj|props|targets|json|godot)$' -or
            $normalized -match '^(Telengard\.sln|global\.json|Directory\.Build\.(props|targets)|Directory\.Packages\.props)$'
        ) { $normalized }
    }
    return @($relevant | Sort-Object -Unique)
}

function Get-TelengardVerificationFingerprint {
    param([Parameter(Mandatory=$true)][string]$RepoRoot)

    $files = @(Get-TelengardVerificationFiles -RepoRoot $RepoRoot)
    if ($files.Count -eq 0) { return '' }

    $lines = foreach ($relative in $files) {
        $full = Join-Path $RepoRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $full -PathType Leaf) {
            $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $full).Hash.ToLowerInvariant()
            "$relative`t$hash"
        } else {
            "$relative`t<deleted>"
        }
    }

    $payload = [string]::Join("`n", $lines)
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}
