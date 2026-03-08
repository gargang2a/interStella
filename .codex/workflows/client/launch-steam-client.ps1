param(
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [string]$HubSessionId = "",
    [string]$AccessToken = "",
    [string]$JoinArgs = "",
    [string]$LobbyId = "",
    [string]$ClientLogPath = "",
    [int]$AutoInteractCount = 0,
    [int]$BootTimeoutSec = 120,
    [switch]$UseClipboardJoinArgs,
    [switch]$StrictSteamRelay,
    [switch]$WaitForBoot,
    [switch]$WhatIfLaunch
)

$ErrorActionPreference = "Stop"

function Assert-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        throw "$Label file does not exist: $Path"
    }
}

function Assert-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path $Path -PathType Container)) {
        throw "$Label directory does not exist: $Path"
    }
}

function Get-HostCommandLine {
    param([string]$ProjectPath)

    $escapedPath = [Regex]::Escape($ProjectPath)
    $hostProcess = Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq "Unity.exe" -and $_.CommandLine -match "-projectpath\s+(`"|)$escapedPath(`"|)" } |
        Select-Object -First 1

    if ($null -eq $hostProcess) {
        throw "Host Unity process for project '$ProjectPath' was not found."
    }

    return $hostProcess.CommandLine
}

function Extract-ArgumentValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandLine,
        [Parameter(Mandatory = $true)]
        [string]$ArgumentName
    )

    $pattern = "-$ArgumentName\s+(`"([^`"]+)`"|([^\s`"]+))"
    $match = [Regex]::Match($CommandLine, $pattern)
    if (-not $match.Success) {
        return ""
    }

    if (-not [string]::IsNullOrWhiteSpace($match.Groups[2].Value)) {
        return $match.Groups[2].Value
    }

    return $match.Groups[3].Value
}

function Resolve-LobbyId {
    param(
        [string]$ExplicitLobbyId,
        [string]$RawJoinArgs,
        [bool]$UseClipboard
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitLobbyId)) {
        return $ExplicitLobbyId.Trim()
    }

    $candidateArgs = $RawJoinArgs
    if ([string]::IsNullOrWhiteSpace($candidateArgs) -and $UseClipboard) {
        $candidateArgs = Get-Clipboard -Raw
    }

    if ([string]::IsNullOrWhiteSpace($candidateArgs)) {
        return ""
    }

    $match = [Regex]::Match($candidateArgs, "\+connect_lobby(?:\s+|=)(`"([^`"]+)`"|([^\s`"]+))")
    if (-not $match.Success) {
        return ""
    }

    if (-not [string]::IsNullOrWhiteSpace($match.Groups[2].Value)) {
        return $match.Groups[2].Value.Trim()
    }

    return $match.Groups[3].Value.Trim()
}

function Wait-ClientBoot {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)]
        [string]$LogPath,
        [int]$TimeoutSec = 120
    )

    $readyPatterns = @(
        "\[interStella\] ClientAutoPlayBootstrap entered play mode\.",
        "Steam relay binder configured client mode with FishySteamworks",
        "\[FishNetSessionService\] Starting session provider=SteamRelay",
        "Local client is started"
    )

    $failurePatterns = @(
        "No valid Unity Editor license found",
        "Timed-out after 60\.00s",
        "ConnectionFailed",
        "SteamworksBootstrap failed",
        "Steam lobby join failed"
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(10, $TimeoutSec))
    while ((Get-Date) -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) {
            return @{
                Ready = $false
                Reason = "ClientProcessExited"
            }
        }

        if (Test-Path $LogPath -PathType Leaf) {
            $content = Get-Content $LogPath -Raw
            foreach ($pattern in $readyPatterns) {
                if ($content -match $pattern) {
                    return @{
                        Ready = $true
                        Reason = "ClientBooted"
                    }
                }
            }

            foreach ($pattern in $failurePatterns) {
                if ($content -match $pattern) {
                    return @{
                        Ready = $false
                        Reason = "ClientBootFailed"
                    }
                }
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return @{
        Ready = $false
        Reason = "ClientBootTimeout"
    }
}

Assert-Directory -Path $ClientProjectPath -Label "Client project"
Assert-Directory -Path $LogDirectory -Label "Log directory"
Assert-File -Path $UnityEditorPath -Label "Unity editor"

if ([string]::IsNullOrWhiteSpace($HubSessionId) -or [string]::IsNullOrWhiteSpace($AccessToken)) {
    $hostCommandLine = Get-HostCommandLine -ProjectPath $HostProjectPath

    if ([string]::IsNullOrWhiteSpace($HubSessionId)) {
        $HubSessionId = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "hubSessionId"
    }

    if ([string]::IsNullOrWhiteSpace($AccessToken)) {
        $AccessToken = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "accessToken"
    }
}

if ([string]::IsNullOrWhiteSpace($HubSessionId)) {
    throw "HubSessionId could not be resolved."
}

if ([string]::IsNullOrWhiteSpace($AccessToken)) {
    throw "AccessToken could not be resolved."
}

$resolvedLobbyId = Resolve-LobbyId -ExplicitLobbyId $LobbyId -RawJoinArgs $JoinArgs -UseClipboard:$UseClipboardJoinArgs
if ([string]::IsNullOrWhiteSpace($resolvedLobbyId)) {
    throw "LobbyId could not be resolved. Provide -LobbyId, -JoinArgs, or -UseClipboardJoinArgs."
}

if ([string]::IsNullOrWhiteSpace($ClientLogPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $ClientLogPath = Join-Path $LogDirectory "steam-manual-client-$timestamp.log"
}

$arguments = @(
    "-projectPath", $ClientProjectPath,
    "-useHub",
    "-hubIPC",
    "-cloudEnvironment", "production",
    "-licensingIpc", "LicenseClient-gar",
    "-hubSessionId", $HubSessionId,
    "-accessToken", $AccessToken,
    "-interstella-mode", "client",
    "-interstella-provider", "steam",
    "-interstella-address", "127.0.0.1",
    "-interstella-port", "7770",
    "+connect_lobby", $resolvedLobbyId,
    "-executeMethod", "InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay",
    "-logFile", $ClientLogPath
)

if ($StrictSteamRelay) {
    $arguments += @("-interstella-steam-strict-relay", "1")
}

if ($AutoInteractCount -gt 0) {
    $arguments += @(
        "-interstella-auto-interact", "1",
        "-interstella-auto-interact-count", ([Math]::Max(1, $AutoInteractCount).ToString())
    )
}

if ($WhatIfLaunch) {
    Write-Output ("STEAM_CLIENT_LAUNCH_PREVIEW LOBBY_ID={0} LOG={1}" -f $resolvedLobbyId, $ClientLogPath)
    Write-Output ("UNITY_PATH {0}" -f $UnityEditorPath)
    Write-Output ("ARGUMENTS {0}" -f ($arguments -join " "))
    return
}

$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -PassThru

if ($WaitForBoot) {
    $bootResult = Wait-ClientBoot -Process $process -LogPath $ClientLogPath -TimeoutSec $BootTimeoutSec
    Write-Output ("STEAM_CLIENT_LAUNCHED PID={0} LOBBY_ID={1} LOG={2} READY={3} REASON={4}" -f $process.Id, $resolvedLobbyId, $ClientLogPath, $bootResult.Ready, $bootResult.Reason)
    return
}

Write-Output ("STEAM_CLIENT_LAUNCHED PID={0} LOBBY_ID={1} LOG={2}" -f $process.Id, $resolvedLobbyId, $ClientLogPath)
