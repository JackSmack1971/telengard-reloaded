$ErrorActionPreference = 'Stop'

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }
try { $inputObject = $raw | ConvertFrom-Json } catch { exit 0 }
$toolName = [string]$inputObject.tool_name
if ([string]::IsNullOrWhiteSpace($toolName)) { $toolName = [string]$inputObject.toolName }
if ($toolName -notin @('Bash', 'exec', 'exec_command')) { exit 0 }
$toolInput = $inputObject.tool_input
if ($null -eq $toolInput) { $toolInput = $inputObject.toolArgs }
$command = if ($toolInput -is [string]) { [string]$toolInput } else { [string]$toolInput.command }
if ([string]::IsNullOrWhiteSpace($command) -and $toolInput) { $command = [string]$toolInput.commandLine }
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

function Deny([string]$reason) {
    $result = @{
        hookSpecificOutput = @{
            hookEventName = 'PreToolUse'
            permissionDecision = 'deny'
            permissionDecisionReason = $reason
        }
    }
    $result | ConvertTo-Json -Depth 5 -Compress
    exit 0
}

# Catch command-segment invocations of a global/bare dotnet before PowerShell can fail on PATH.
$bareDotnet = '(?im)(^|[;&|({]\s*)dotnet(?:\.exe)?(?=\s|$)'
if ($command -match $bareDotnet) {
    Deny 'Bare dotnet is disabled in this repository because the SDK is repo-local. Re-run the command through ./eng/dotnet.ps1 <arguments>. Do not install a global SDK or edit machine PATH.'
}

# Preserve user work and the checked-in/local SDK from broad destructive cleanup operations.
if ($command -match '(?im)(^|[;&|]\s*)git\s+clean(?:\s|$)') {
    Deny 'Repository-wide git clean is blocked by the Telengard project hook because it can delete untracked work or the local .dotnet SDK. Remove only explicitly targeted files after inspecting git status.'
}
if ($command -match '(?im)(^|[;&|]\s*)git\s+reset\s+--hard(?:\s|$)') {
    Deny 'git reset --hard is blocked by the Telengard project hook because it can destroy user changes. Use a non-destructive, explicitly scoped alternative.'
}
if ($command -match '(?im)(^|[;&|]\s*)git\s+(?:restore|checkout)\s+(?:--\s+)?\.(?:\s|$)') {
    Deny 'Repo-wide git restore/checkout is blocked because it can overwrite unrelated user changes. Restore only explicit paths after inspecting git status.'
}
if ($command -match '(?im)(?:Remove-Item|rm|rmdir|del)[^\r\n;&|]*[\\/]?\.dotnet(?:[\\/\s]|$)') {
    Deny 'Modification/deletion of the repository .dotnet SDK is blocked. SDK maintenance requires an explicit user request.'
}

exit 0
