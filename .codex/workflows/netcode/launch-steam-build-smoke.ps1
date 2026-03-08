param(
    [ValidateSet("host", "client")]
    [string]$Mode = "client",
    [string]$BuildExePath = "",
    [string]$LogDirectory = "",
    [string]$LobbyId = "",
    [string]$JoinArgs = "",
    [switch]$UseClipboardJoinArgs,
    [int]$AutoInteractCount = 0,
    [switch]$StrictSteamRelay,
    [switch]$WaitForBoot,
    [int]$BootTimeoutSec = 120,
    [switch]$WhatIfLaunch
)

$ErrorActionPreference = "Stop"

function Resolve-DefaultProjectPath {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
}

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

function Wait-BuildBoot {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)]
        [string]$LogPath,
        [Parameter(Mandatory = $true)]
        [string]$ModeName,
        [int]$TimeoutSec = 120
    )

    $readyPatterns = if ($ModeName -eq "host") {
        @(
            "\[SteamSessionService\] Session started\.",
            "Host Steam lobby created",
            "\[FishNetSessionService\] Starting session provider=SteamRelay, mode=Host"
        )
    }
    else {
        @(
            "Steam relay binder configured client mode with FishySteamworks",
            "Steam lobby joined",
            "\[FishNetSessionService\] Starting session provider=SteamRelay, mode=ClientOnly"
        )
    }

    $failurePatterns = @(
        "SteamworksBootstrap failed",
        "Steam lobby join failed",
        "Steam lobby creation failed",
        "ConnectionFailed",
        "No valid Unity Editor license found"
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(10, $TimeoutSec))
    while ((Get-Date) -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) {
            return @{
                Ready = $false
                Reason = "ProcessExited"
            }
        }

        if (Test-Path $LogPath -PathType Leaf) {
            $content = Get-Content $LogPath -Raw
            foreach ($pattern in $readyPatterns) {
                if ($content -match $pattern) {
                    return @{
                        Ready = $true
                        Reason = "Booted"
                    }
                }
            }

            foreach ($pattern in $failurePatterns) {
                if ($content -match $pattern) {
                    return @{
                        Ready = $false
                        Reason = "BootFailed"
                    }
                }
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return @{
        Ready = $false
        Reason = "BootTimeout"
    }
}

$projectPath = Resolve-DefaultProjectPath
if ([string]::IsNullOrWhiteSpace($BuildExePath)) {
    $BuildExePath = Join-Path $projectPath "Builds\SteamSmokeWindows64\interStella-Smoke.exe"
}

if ([string]::IsNullOrWhiteSpace($LogDirectory)) {
    $LogDirectory = Join-Path $projectPath "Logs"
}

if (-not (Test-Path $LogDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = Join-Path $LogDirectory ("steam-build-{0}-{1}.log" -f $Mode, $timestamp)
$arguments = @("-interstella-provider", "steam", "-logFile", $logPath)

if ($StrictSteamRelay) {
    $arguments += @("-interstella-steam-strict-relay", "1")
}

if ($Mode -eq "client") {
    $resolvedLobbyId = Resolve-LobbyId -ExplicitLobbyId $LobbyId -RawJoinArgs $JoinArgs -UseClipboard:$UseClipboardJoinArgs
    if ([string]::IsNullOrWhiteSpace($resolvedLobbyId)) {
        throw "Client mode requires -LobbyId, -JoinArgs, or -UseClipboardJoinArgs."
    }

    $arguments += @(
        "-interstella-mode", "client",
        "-interstella-address", "127.0.0.1",
        "-interstella-port", "7770",
        "+connect_lobby", $resolvedLobbyId
    )

    if ($AutoInteractCount -gt 0) {
        $arguments += @(
            "-interstella-auto-interact", "1",
            "-interstella-auto-interact-count", ([Math]::Max(1, $AutoInteractCount).ToString())
        )
    }
}
else {
    $arguments += @(
        "-interstella-mode", "host",
        "-interstella-address", "127.0.0.1",
        "-interstella-port", "7770"
    )
}

if ($WhatIfLaunch) {
    Write-Output ("STEAM_BUILD_LAUNCH_PREVIEW MODE={0} LOG={1}" -f $Mode, $logPath)
    Write-Output ("EXE_PATH {0}" -f $BuildExePath)
    Write-Output ("ARGUMENTS {0}" -f ($arguments -join " "))
    return
}

Assert-File -Path $BuildExePath -Label "Build executable"

$process = Start-Process -FilePath $BuildExePath -ArgumentList $arguments -PassThru

if ($WaitForBoot) {
    $bootResult = Wait-BuildBoot -Process $process -LogPath $logPath -ModeName $Mode -TimeoutSec $BootTimeoutSec
    Write-Output ("STEAM_BUILD_LAUNCHED MODE={0} PID={1} LOG={2} READY={3} REASON={4}" -f $Mode, $process.Id, $logPath, $bootResult.Ready, $bootResult.Reason)
    return
}

Write-Output ("STEAM_BUILD_LAUNCHED MODE={0} PID={1} LOG={2}" -f $Mode, $process.Id, $logPath)
