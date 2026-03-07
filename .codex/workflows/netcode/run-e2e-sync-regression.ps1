param(
    [string]$SourceRoot = "",
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 360,
    [int]$InteractionPostWaitSec = 40,
    [int]$InteractionAutoInteractCount = 2,
    [int]$ReconnectAutoInteractCount = 0,
    [int]$ReconnectAutoInteractMaxAttempts = 120,
    [double]$ReconnectAutoInteractInitialDelaySec = 10.0,
    [double]$ReconnectAutoInteractIntervalSec = 0.5,
    [int]$StartupRetryMaxAttempts = 2,
    [int]$StartupRetryDelaySec = 8,
    [bool]$RunInteractionRegression = $true,
    [int]$PostReconnectWaitSec = 95,
    [int]$RegressionMaxAttempts = 2,
    [int]$RetryDelaySec = 8,
    [switch]$UseSteamBootstrap,
    [switch]$StrictSteamRelay,
    [string]$SteamInviteLobbyId = "local-regression",
    [string]$SteamInviteHostId = "127.0.0.1:7770",
    [switch]$MirrorSync,
    [switch]$SkipSync,
    [switch]$SyncWhatIf,
    [switch]$KeepClientRunning
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}

$syncScript = Join-Path $PSScriptRoot "..\client\sync-interstella-client.ps1"
$interactionScript = Join-Path $PSScriptRoot "run-interaction-regression.ps1"
$regressionScript = Join-Path $PSScriptRoot "run-reconnect-regression.ps1"

if (-not (Test-Path $syncScript -PathType Leaf)) {
    throw "Sync script not found: $syncScript"
}

if (-not (Test-Path $regressionScript -PathType Leaf)) {
    throw "Regression script not found: $regressionScript"
}

if (-not (Test-Path $interactionScript -PathType Leaf)) {
    throw "Interaction regression script not found: $interactionScript"
}

$hostPortListening = (netstat -ano | Select-String "UDP\s+.*:7770\s+.*\s+\d+$").Count -gt 0
if (-not $hostPortListening) {
    throw "Host is not listening on UDP 7770. Start host Play mode before running this workflow."
}

$syncOutput = @()
if (-not $SkipSync) {
    $syncArgs = @{
        SourceRoot = $SourceRoot
        ClientRoot = $ClientProjectPath
    }

    if ($MirrorSync) {
        $syncArgs["Mirror"] = $true
    }

    if ($SyncWhatIf) {
        $syncArgs["WhatIf"] = $true
    }

    $syncOutput = & $syncScript @syncArgs 2>&1
    if ($syncOutput -notmatch "CLIENT_SYNC_COMPLETED") {
        throw "Client sync did not report completion."
    }
}

$regressionArgs = @{
    HostProjectPath = $HostProjectPath
    ClientProjectPath = $ClientProjectPath
    UnityEditorPath = $UnityEditorPath
    LogDirectory = $LogDirectory
    ClientBootTimeoutSec = $ClientBootTimeoutSec
    PostReconnectWaitSec = $PostReconnectWaitSec
    ReconnectAutoInteractCount = $ReconnectAutoInteractCount
    ReconnectAutoInteractMaxAttempts = $ReconnectAutoInteractMaxAttempts
    ReconnectAutoInteractInitialDelaySec = $ReconnectAutoInteractInitialDelaySec
    ReconnectAutoInteractIntervalSec = $ReconnectAutoInteractIntervalSec
    StartupRetryMaxAttempts = $StartupRetryMaxAttempts
    StartupRetryDelaySec = $StartupRetryDelaySec
}

if (-not [string]::IsNullOrWhiteSpace($HostEditorLogPath)) {
    $regressionArgs["HostEditorLogPath"] = $HostEditorLogPath
}

if ($KeepClientRunning) {
    $regressionArgs["KeepClientRunning"] = $true
}

if ($UseSteamBootstrap) {
    $regressionArgs["UseSteamBootstrap"] = $true
    $regressionArgs["SteamInviteLobbyId"] = $SteamInviteLobbyId
    $regressionArgs["SteamInviteHostId"] = $SteamInviteHostId
}

if ($StrictSteamRelay) {
    $regressionArgs["StrictSteamRelay"] = $true
}

$regressionOutput = @()
$regressionExitCode = 1
$attempt = 0
$reconnectSummaryPath = ""
$interactionSummaryPath = "SKIPPED"

if ($RunInteractionRegression) {
    $interactionArgs = @{
        HostProjectPath = $HostProjectPath
        ClientProjectPath = $ClientProjectPath
        UnityEditorPath = $UnityEditorPath
        LogDirectory = $LogDirectory
        ClientBootTimeoutSec = $ClientBootTimeoutSec
        PostInteractWaitSec = $InteractionPostWaitSec
        AutoInteractCount = $InteractionAutoInteractCount
        StartupRetryMaxAttempts = $StartupRetryMaxAttempts
        StartupRetryDelaySec = $StartupRetryDelaySec
    }

    if (-not [string]::IsNullOrWhiteSpace($HostEditorLogPath)) {
        $interactionArgs["HostEditorLogPath"] = $HostEditorLogPath
    }

    if ($UseSteamBootstrap) {
        $interactionArgs["UseSteamBootstrap"] = $true
        $interactionArgs["SteamInviteLobbyId"] = $SteamInviteLobbyId
        $interactionArgs["SteamInviteHostId"] = $SteamInviteHostId
    }

    if ($StrictSteamRelay) {
        $interactionArgs["StrictSteamRelay"] = $true
    }

    $interactionOutput = & $interactionScript @interactionArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Interaction regression failed. $($interactionOutput -join ' ')"
    }

    foreach ($line in $interactionOutput) {
        $match = [Regex]::Match($line.ToString(), "SUMMARY=([^\s]+)")
        if ($match.Success) {
            $interactionSummaryPath = $match.Groups[1].Value
        }
    }

    if ([string]::IsNullOrWhiteSpace($interactionSummaryPath)) {
        throw "Interaction regression did not return summary path."
    }
}

while ($attempt -lt [Math]::Max(1, $RegressionMaxAttempts)) {
    $attempt++
    $regressionOutput = & $regressionScript @regressionArgs 2>&1
    $regressionExitCode = $LASTEXITCODE

    $reconnectSummaryPath = ""
    foreach ($line in $regressionOutput) {
        $match = [Regex]::Match($line.ToString(), "SUMMARY=([^\s]+)")
        if ($match.Success) {
            $reconnectSummaryPath = $match.Groups[1].Value
        }
    }

    if ($regressionExitCode -eq 0) {
        break
    }

    $retryableFailure =
        ($regressionOutput -match "ClientStartupFailed|ClientStartupTimeout|ClientProcessExited") -or
        ($regressionOutput -match "ConnectionFailed")
    if (-not $retryableFailure -or $attempt -ge $RegressionMaxAttempts) {
        break
    }

    Start-Sleep -Seconds ([Math]::Max(1, $RetryDelaySec))
}

if ($regressionExitCode -ne 0) {
    throw "Reconnect regression failed after $attempt attempt(s). $($regressionOutput -join ' ')"
}

if ([string]::IsNullOrWhiteSpace($reconnectSummaryPath)) {
    throw "Reconnect regression did not return summary path."
}

if (-not $SkipSync) {
    $syncLine = ($syncOutput | Where-Object { $_ -match "CLIENT_SYNC_COMPLETED" } | Select-Object -Last 1)
    Write-Output $syncLine
}

$regressionLine = ($regressionOutput | Where-Object { $_ -match "RECONNECT_REGRESSION_PASS" } | Select-Object -Last 1)
if ($RunInteractionRegression) {
    $interactionLine = "INTERACTION_REGRESSION_PASS SUMMARY=$interactionSummaryPath"
    Write-Output $interactionLine
}

Write-Output $regressionLine
Write-Output "E2E_SYNC_REGRESSION_PASS RECONNECT_SUMMARY=$reconnectSummaryPath INTERACTION_SUMMARY=$interactionSummaryPath"
