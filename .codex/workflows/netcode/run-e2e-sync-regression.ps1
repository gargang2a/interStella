param(
    [string]$SourceRoot = "",
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 360,
    [int]$PostReconnectWaitSec = 95,
    [int]$RegressionMaxAttempts = 2,
    [int]$RetryDelaySec = 8,
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
$regressionScript = Join-Path $PSScriptRoot "run-reconnect-regression.ps1"

if (-not (Test-Path $syncScript -PathType Leaf)) {
    throw "Sync script not found: $syncScript"
}

if (-not (Test-Path $regressionScript -PathType Leaf)) {
    throw "Regression script not found: $regressionScript"
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
}

if (-not [string]::IsNullOrWhiteSpace($HostEditorLogPath)) {
    $regressionArgs["HostEditorLogPath"] = $HostEditorLogPath
}

if ($KeepClientRunning) {
    $regressionArgs["KeepClientRunning"] = $true
}

$regressionOutput = @()
$regressionExitCode = 1
$attempt = 0
$summaryPath = ""

while ($attempt -lt [Math]::Max(1, $RegressionMaxAttempts)) {
    $attempt++
    $regressionOutput = & $regressionScript @regressionArgs 2>&1
    $regressionExitCode = $LASTEXITCODE

    $summaryPath = ""
    foreach ($line in $regressionOutput) {
        $match = [Regex]::Match($line.ToString(), "SUMMARY=([^\s]+)")
        if ($match.Success) {
            $summaryPath = $match.Groups[1].Value
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

if ([string]::IsNullOrWhiteSpace($summaryPath)) {
    throw "Reconnect regression did not return summary path."
}

if (-not $SkipSync) {
    $syncLine = ($syncOutput | Where-Object { $_ -match "CLIENT_SYNC_COMPLETED" } | Select-Object -Last 1)
    Write-Output $syncLine
}

$regressionLine = ($regressionOutput | Where-Object { $_ -match "RECONNECT_REGRESSION_PASS" } | Select-Object -Last 1)
Write-Output $regressionLine
Write-Output "E2E_SYNC_REGRESSION_PASS SUMMARY=$summaryPath"
