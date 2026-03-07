param(
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 240,
    [int]$PostReconnectWaitSec = 95,
    [int]$ReconnectAutoInteractCount = 0,
    [int]$ReconnectAutoInteractMaxAttempts = 120,
    [double]$ReconnectAutoInteractInitialDelaySec = 10.0,
    [double]$ReconnectAutoInteractIntervalSec = 0.5,
    [switch]$UseSteamBootstrap,
    [switch]$StrictSteamRelay,
    [string]$SteamInviteLobbyId = "local-regression",
    [string]$SteamInviteHostId = "127.0.0.1:7770",
    [switch]$KeepClientRunning,
    [int]$StartupRetryMaxAttempts = 2,
    [int]$StartupRetryDelaySec = 8
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($HostEditorLogPath)) {
    $HostEditorLogPath = Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"
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

function Read-AppendedText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [long]$StartOffset
    )

    $fileInfo = Get-Item $Path
    if ($fileInfo.Length -le $StartOffset) {
        return ""
    }

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        [void]$stream.Seek($StartOffset, [System.IO.SeekOrigin]::Begin)
        $remaining = [int]($stream.Length - $StartOffset)
        if ($remaining -le 0) {
            return ""
        }

        $buffer = New-Object byte[] $remaining
        [void]$stream.Read($buffer, 0, $remaining)
        return [System.Text.Encoding]::UTF8.GetString($buffer)
    }
    finally {
        $stream.Dispose()
    }
}

function Wait-ClientReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LogPath,
        [System.Diagnostics.Process]$ClientProcess = $null,
        [int]$TimeoutSec = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if ($ClientProcess -ne $null) {
            $ClientProcess.Refresh()
            if ($ClientProcess.HasExited) {
                return @{
                    Ready = $false
                    Reason = "ClientProcessExited"
                }
            }
        }

        if (Test-Path $LogPath -PathType Leaf) {
            $content = Get-Content $LogPath -Raw
            if ($content -match "Local client is started for Tugboat\.") {
                return @{
                    Ready = $true
                    Reason = "ClientStarted"
                }
            }

            if ($content -match "No valid Unity Editor license found|Timed-out after 60\.00s|ConnectionFailed") {
                return @{
                    Ready = $false
                    Reason = "ClientStartupFailed"
                }
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return @{
        Ready = $false
        Reason = "ClientStartupTimeout"
    }
}

function Get-AttemptLogPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseLogPath,
        [Parameter(Mandatory = $true)]
        [int]$Attempt
    )

    if ($Attempt -le 1) {
        return $BaseLogPath
    }

    $directory = [System.IO.Path]::GetDirectoryName($BaseLogPath)
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($BaseLogPath)
    $extension = [System.IO.Path]::GetExtension($BaseLogPath)
    return [System.IO.Path]::Combine($directory, "$baseName-attempt$Attempt$extension")
}

function Start-ReconnectClient {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UnityPath,
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$HubSessionId,
        [Parameter(Mandatory = $true)]
        [string]$AccessToken,
        [Parameter(Mandatory = $true)]
        [string]$LogPath,
        [int]$InteractCount = 0,
        [int]$AutoInteractMaxAttempts = 24,
        [double]$AutoInteractInitialDelaySec = 1.25,
        [double]$AutoInteractIntervalSec = 0.5,
        [bool]$UseSteamBootstrap = $false,
        [bool]$StrictSteamRelay = $false,
        [string]$InviteLobbyId = "",
        [string]$InviteHostId = ""
    )

    $autoInteractEnabled = $InteractCount -gt 0
    $autoInteractTarget = [Math]::Max(1, $InteractCount)

    $arguments = @(
        "-projectPath", $ProjectPath,
        "-useHub",
        "-hubIPC",
        "-cloudEnvironment", "production",
        "-licensingIpc", "LicenseClient-gar",
        "-hubSessionId", $HubSessionId,
        "-accessToken", $AccessToken,
        "-interstella-mode", "client",
        "-interstella-address", "127.0.0.1",
        "-interstella-port", "7770",
        "-interstella-auto-interact", ($(if ($autoInteractEnabled) { "1" } else { "0" })),
        "-interstella-auto-interact-count", $autoInteractTarget.ToString(),
        "-executeMethod", "InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay",
        "-logFile", $LogPath
    )

    if ($autoInteractEnabled) {
        $arguments += @(
            "-interstella-auto-interact-max-attempts", ([Math]::Max(1, $AutoInteractMaxAttempts).ToString()),
            "-interstella-auto-interact-initial-delay", ([Math]::Max(0d, $AutoInteractInitialDelaySec).ToString("0.###", [System.Globalization.CultureInfo]::InvariantCulture)),
            "-interstella-auto-interact-interval", ([Math]::Max(0.1d, $AutoInteractIntervalSec).ToString("0.###", [System.Globalization.CultureInfo]::InvariantCulture))
        )
    }

    if ($UseSteamBootstrap) {
        $arguments += @("-interstella-provider", "steam")

        if (-not [string]::IsNullOrWhiteSpace($InviteLobbyId)) {
            $arguments += @("-interstella-invite-lobby-id", $InviteLobbyId)
        }

        if (-not [string]::IsNullOrWhiteSpace($InviteHostId)) {
            $arguments += @("-interstella-invite-host-id", $InviteHostId)
        }

        if ($StrictSteamRelay) {
            $arguments += @("-interstella-steam-strict-relay", "1")
        }
    }

    return Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru
}

function Start-ReconnectClientWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UnityPath,
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$HubSessionId,
        [Parameter(Mandatory = $true)]
        [string]$AccessToken,
        [Parameter(Mandatory = $true)]
        [string]$BaseLogPath,
        [int]$InteractCount = 0,
        [int]$AutoInteractMaxAttempts = 24,
        [double]$AutoInteractInitialDelaySec = 1.25,
        [double]$AutoInteractIntervalSec = 0.5,
        [int]$ClientReadyTimeoutSec = 90,
        [int]$MaxAttempts = 1,
        [int]$RetryDelaySec = 2,
        [bool]$UseSteamBootstrap = $false,
        [bool]$StrictSteamRelay = $false,
        [string]$InviteLobbyId = "",
        [string]$InviteHostId = ""
    )

    $attemptLimit = [Math]::Max(1, $MaxAttempts)
    $retryDelay = [Math]::Max(1, $RetryDelaySec)
    for ($attempt = 1; $attempt -le $attemptLimit; $attempt++) {
        $attemptLogPath = Get-AttemptLogPath -BaseLogPath $BaseLogPath -Attempt $attempt
        $process = Start-ReconnectClient `
            -UnityPath $UnityPath `
            -ProjectPath $ProjectPath `
            -HubSessionId $HubSessionId `
            -AccessToken $AccessToken `
            -LogPath $attemptLogPath `
            -InteractCount $InteractCount `
            -AutoInteractMaxAttempts $AutoInteractMaxAttempts `
            -AutoInteractInitialDelaySec $AutoInteractInitialDelaySec `
            -AutoInteractIntervalSec $AutoInteractIntervalSec `
            -UseSteamBootstrap $UseSteamBootstrap `
            -StrictSteamRelay $StrictSteamRelay `
            -InviteLobbyId $InviteLobbyId `
            -InviteHostId $InviteHostId
        $readyResult = Wait-ClientReady -LogPath $attemptLogPath -ClientProcess $process -TimeoutSec $ClientReadyTimeoutSec
        if ($readyResult.Ready) {
            return @{
                Ready = $true
                Process = $process
                LogPath = $attemptLogPath
                Reason = $readyResult.Reason
                AttemptsUsed = $attempt
            }
        }

        $process.Refresh()
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }

        $retryableFailure = $readyResult.Reason -eq "ClientStartupFailed" `
            -or $readyResult.Reason -eq "ClientStartupTimeout" `
            -or $readyResult.Reason -eq "ClientProcessExited"
        if (-not $retryableFailure -or $attempt -ge $attemptLimit) {
            return @{
                Ready = $false
                Process = $process
                LogPath = $attemptLogPath
                Reason = $readyResult.Reason
                AttemptsUsed = $attempt
            }
        }

        Start-Sleep -Seconds $retryDelay
    }

    return @{
        Ready = $false
        Process = $null
        LogPath = $BaseLogPath
        Reason = "ClientStartupUnknown"
        AttemptsUsed = $attemptLimit
    }
}

Assert-File -Path $UnityEditorPath -Label "Unity editor"
Assert-Directory -Path $HostProjectPath -Label "Host project"
Assert-Directory -Path $ClientProjectPath -Label "Client project"
Assert-File -Path $HostEditorLogPath -Label "Host editor log"
Assert-Directory -Path $LogDirectory -Label "Log"

$hostCommandLine = Get-HostCommandLine -ProjectPath $HostProjectPath
$hubSessionId = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "hubSessionId"
$accessToken = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "accessToken"
if ([string]::IsNullOrWhiteSpace($hubSessionId) -or [string]::IsNullOrWhiteSpace($accessToken)) {
    throw "Failed to parse hubSessionId/accessToken from host Unity process."
}

$startOffset = (Get-Item $HostEditorLogPath).Length
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$clientALog = Join-Path $LogDirectory "client-regression-A-$stamp.log"
$clientBLog = Join-Path $LogDirectory "client-regression-B-$stamp.log"
$clientA = $null
$clientB = $null
$summary = $null
$summaryPath = Join-Path $LogDirectory "reconnect-regression-summary-$stamp.json"
$exceptionMessage = ""
$clientAStartupAttemptsUsed = 0
$clientBStartupAttemptsUsed = 0
$clientAStartupLastReason = ""
$clientBStartupLastReason = ""

try {
    $clientAStartResult = Start-ReconnectClientWithRetry `
        -UnityPath $UnityEditorPath `
        -ProjectPath $ClientProjectPath `
        -HubSessionId $hubSessionId `
        -AccessToken $accessToken `
        -BaseLogPath $clientALog `
        -InteractCount 0 `
        -AutoInteractMaxAttempts $ReconnectAutoInteractMaxAttempts `
        -AutoInteractInitialDelaySec $ReconnectAutoInteractInitialDelaySec `
        -AutoInteractIntervalSec $ReconnectAutoInteractIntervalSec `
        -ClientReadyTimeoutSec $ClientBootTimeoutSec `
        -MaxAttempts $StartupRetryMaxAttempts `
        -RetryDelaySec $StartupRetryDelaySec `
        -UseSteamBootstrap $UseSteamBootstrap `
        -StrictSteamRelay $StrictSteamRelay `
        -InviteLobbyId $SteamInviteLobbyId `
        -InviteHostId $SteamInviteHostId
    $clientA = $clientAStartResult.Process
    $clientALog = $clientAStartResult.LogPath
    $clientAStartupAttemptsUsed = $clientAStartResult.AttemptsUsed
    $clientAStartupLastReason = $clientAStartResult.Reason
    if (-not $clientAStartResult.Ready) {
        throw "Client A failed to become ready. Reason=$($clientAStartResult.Reason) Attempts=$($clientAStartResult.AttemptsUsed) Log=$clientALog"
    }

    if (-not $clientA.HasExited) {
        Stop-Process -Id $clientA.Id -Force
    }

    $clientBStartResult = Start-ReconnectClientWithRetry `
        -UnityPath $UnityEditorPath `
        -ProjectPath $ClientProjectPath `
        -HubSessionId $hubSessionId `
        -AccessToken $accessToken `
        -BaseLogPath $clientBLog `
        -InteractCount $ReconnectAutoInteractCount `
        -AutoInteractMaxAttempts $ReconnectAutoInteractMaxAttempts `
        -AutoInteractInitialDelaySec $ReconnectAutoInteractInitialDelaySec `
        -AutoInteractIntervalSec $ReconnectAutoInteractIntervalSec `
        -ClientReadyTimeoutSec $ClientBootTimeoutSec `
        -MaxAttempts $StartupRetryMaxAttempts `
        -RetryDelaySec $StartupRetryDelaySec `
        -UseSteamBootstrap $UseSteamBootstrap `
        -StrictSteamRelay $StrictSteamRelay `
        -InviteLobbyId $SteamInviteLobbyId `
        -InviteHostId $SteamInviteHostId
    $clientB = $clientBStartResult.Process
    $clientBLog = $clientBStartResult.LogPath
    $clientBStartupAttemptsUsed = $clientBStartResult.AttemptsUsed
    $clientBStartupLastReason = $clientBStartResult.Reason
    if (-not $clientBStartResult.Ready) {
        throw "Client B failed to become ready. Reason=$($clientBStartResult.Reason) Attempts=$($clientBStartResult.AttemptsUsed) Log=$clientBLog"
    }

    Start-Sleep -Seconds $PostReconnectWaitSec

    $hostDeltaText = Read-AppendedText -Path $HostEditorLogPath -StartOffset $startOffset
    $clientBLogText = if (Test-Path $clientBLog -PathType Leaf) { Get-Content $clientBLog -Raw } else { "" }

    $queueMatch = [Regex]::Match($hostDeltaText, "No available slot for client \d+\. Queued for reassignment\.")
    $releaseMatches = [Regex]::Matches($hostDeltaText, "Released slot 1 from client (\d+); ownership removed from PlayerB\.")
    $assignMatches = [Regex]::Matches($hostDeltaText, "Assigned client (\d+) to slot 1 \(PlayerB\)\.")
    $acceptedAnyMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\. caller=(\-?\d+), owner=(\-?\d+), committed=(True|False), object=([^\r\n]+)")
    $acceptedCommittedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\..*committed=True")
    $deliveryMatches = [Regex]::Matches($hostDeltaText, "\[RepairStationObjective\] Delivery accepted\. delivered=(\d+)/(\d+)")
    $fuelTransientAcceptedInHost = $hostDeltaText -match "\[PlayerFuelNetworkState\] Transient fuel submit accepted\."
    $fuelRejectedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerFuelNetworkState\] Fuel submit rejected\.")
    $repairDurablePublishedInHost = $hostDeltaText -match "\[RepairObjectiveNetworkState\] Durable repair sync published\."
    $tetherDurablePublishedInHost = $hostDeltaText -match "\[TetherNetworkStateReplicator\] Durable tether (sync published|snapshot)\."

    $fuelDurableAppliedInClientB = $clientBLogText -match "\[PlayerFuelNetworkState\] Durable fuel sync applied\."
    $repairTransientMatchesInClientB = [Regex]::Matches($clientBLogText, "\[RepairObjectiveNetworkState\] Transient delivery event received\. delivered=(\d+)/(\d+)")
    $repairTransientEventCountInClientB = $repairTransientMatchesInClientB.Count
    $tetherDurableAppliedInClientB = $clientBLogText -match "\[TetherNetworkStateReplicator\] Durable tether sync applied\."
    $steamBootstrapAppliedInClientB = $clientBLogText -match "\[SteamSessionService\] Applied Steam bootstrap to FishNet\..*binder=True"
    $steamBinderAppliedInClientB = $clientBLogText -match "\[FishNetSessionService\] Steam relay transport binder applied\."
    $steamFallbackInClientB = $clientBLogText -match "Falling back to direct endpoint because _allowSteamFallbackToDirect is enabled\."

    $repairTransientDuplicateDetectedInClientB = $false
    $repairTransientMonotonicInClientB = $true
    $repairLastDeliveredInClientB = -1
    $repairDeliveredSetInClientB = @{}
    foreach ($repairTransientMatch in $repairTransientMatchesInClientB) {
        $deliveredCount = [int]$repairTransientMatch.Groups[1].Value
        if ($repairDeliveredSetInClientB.ContainsKey($deliveredCount)) {
            $repairTransientDuplicateDetectedInClientB = $true
        }
        else {
            $repairDeliveredSetInClientB[$deliveredCount] = $true
        }

        if ($repairLastDeliveredInClientB -gt $deliveredCount) {
            $repairTransientMonotonicInClientB = $false
        }

        $repairLastDeliveredInClientB = $deliveredCount
    }

    $deliveryDuplicateDetectedInHost = $false
    $deliveryMonotonicInHost = $true
    $lastDeliveredInHost = -1
    $deliverySetInHost = @{}
    foreach ($deliveryMatch in $deliveryMatches) {
        $deliveredInHost = [int]$deliveryMatch.Groups[1].Value
        if ($deliverySetInHost.ContainsKey($deliveredInHost)) {
            $deliveryDuplicateDetectedInHost = $true
        }
        else {
            $deliverySetInHost[$deliveredInHost] = $true
        }

        if ($lastDeliveredInHost -gt $deliveredInHost) {
            $deliveryMonotonicInHost = $false
        }

        $lastDeliveredInHost = $deliveredInHost
    }

    $authorityMismatchAcceptedCount = 0
    foreach ($acceptedAnyMatch in $acceptedAnyMatches) {
        if ($acceptedAnyMatch.Groups[1].Value -ne $acceptedAnyMatch.Groups[2].Value) {
            $authorityMismatchAcceptedCount++
        }
    }

    $releaseMatch = $null
    if ($releaseMatches.Count -gt 0) {
        $releaseMatch = $releaseMatches[$releaseMatches.Count - 1]
    }

    $assignAfterRelease = $null
    if ($releaseMatch -ne $null) {
        foreach ($match in $assignMatches) {
            if ($match.Index -gt $releaseMatch.Index) {
                $assignAfterRelease = $match
                break
            }
        }
    }

    $latestAssignMatch = $null
    if ($assignMatches.Count -gt 0) {
        $latestAssignMatch = $assignMatches[$assignMatches.Count - 1]
    }

    $racePathDetected = ($queueMatch.Success -and $releaseMatch -ne $null -and $assignAfterRelease -ne $null)
    $directAssignDetected = ($latestAssignMatch -ne $null)
    $reconnectPass = ($racePathDetected -or $directAssignDetected)

    $interactionTarget = [Math]::Max(1, $ReconnectAutoInteractCount)
    $autoInteractReachedTargetInClientB = if ($ReconnectAutoInteractCount -gt 0) {
        $clientBLogText -match "successes=\d+/$interactionTarget"
    }
    else {
        $true
    }

    $interactionPass = $ReconnectAutoInteractCount -le 0 `
        -or (($acceptedCommittedMatches.Count -ge 1) -and ($deliveryMatches.Count -ge 1) -and $autoInteractReachedTargetInClientB)

    $durableTransientPass = $fuelTransientAcceptedInHost `
        -and ($fuelRejectedMatches.Count -eq 0) `
        -and $fuelDurableAppliedInClientB `
        -and $tetherDurablePublishedInHost `
        -and $tetherDurableAppliedInClientB

    if ($ReconnectAutoInteractCount -gt 0) {
        $durableTransientPass = $durableTransientPass `
            -and $repairDurablePublishedInHost `
            -and ($repairTransientEventCountInClientB -ge 1) `
            -and (-not $deliveryDuplicateDetectedInHost) `
            -and $deliveryMonotonicInHost `
            -and (-not $repairTransientDuplicateDetectedInClientB) `
            -and $repairTransientMonotonicInClientB
    }

    $steamPass = $true
    if ($UseSteamBootstrap) {
        $steamPass = $steamBootstrapAppliedInClientB -and $steamBinderAppliedInClientB
        if ($StrictSteamRelay) {
            $steamPass = $steamPass -and (-not $steamFallbackInClientB)
        }
    }

    $passed = $reconnectPass `
        -and $interactionPass `
        -and $durableTransientPass `
        -and $steamPass `
        -and ($authorityMismatchAcceptedCount -eq 0)
    $releasedClientId = if ($releaseMatch -ne $null) { $releaseMatch.Groups[1].Value } else { "" }
    $reassignedClientId = if ($assignAfterRelease -ne $null) {
        $assignAfterRelease.Groups[1].Value
    }
    elseif ($latestAssignMatch -ne $null) {
        $latestAssignMatch.Groups[1].Value
    }
    else {
        ""
    }

    $summary = [ordered]@{
        timestamp = (Get-Date).ToString("s")
        passed = $passed
        reconnectPass = $reconnectPass
        racePathDetected = $racePathDetected
        directAssignDetected = $directAssignDetected
        interactionPass = $interactionPass
        durableTransientPass = $durableTransientPass
        steamPass = $steamPass
        queueDetected = $queueMatch.Success
        releaseDetected = ($releaseMatch -ne $null)
        reassignedAfterReleaseDetected = ($assignAfterRelease -ne $null)
        releasedClientId = $releasedClientId
        reassignedClientId = $reassignedClientId
        authorityMismatchAcceptedCount = $authorityMismatchAcceptedCount
        acceptedCommittedCount = $acceptedCommittedMatches.Count
        deliveryAcceptedCount = $deliveryMatches.Count
        deliveryDuplicateDetectedInHost = $deliveryDuplicateDetectedInHost
        deliveryMonotonicInHost = $deliveryMonotonicInHost
        fuelTransientAcceptedInHost = $fuelTransientAcceptedInHost
        fuelRejectedCountInHost = $fuelRejectedMatches.Count
        fuelDurableAppliedInClientB = $fuelDurableAppliedInClientB
        repairDurablePublishedInHost = $repairDurablePublishedInHost
        repairTransientEventCountInClientB = $repairTransientEventCountInClientB
        repairTransientDuplicateDetectedInClientB = $repairTransientDuplicateDetectedInClientB
        repairTransientMonotonicInClientB = $repairTransientMonotonicInClientB
        tetherDurablePublishedInHost = $tetherDurablePublishedInHost
        tetherDurableAppliedInClientB = $tetherDurableAppliedInClientB
        reconnectAutoInteractCount = $ReconnectAutoInteractCount
        reconnectAutoInteractMaxAttempts = $ReconnectAutoInteractMaxAttempts
        reconnectAutoInteractInitialDelaySec = $ReconnectAutoInteractInitialDelaySec
        reconnectAutoInteractIntervalSec = $ReconnectAutoInteractIntervalSec
        clientStartupRetryMaxAttempts = [Math]::Max(1, $StartupRetryMaxAttempts)
        clientStartupRetryDelaySec = [Math]::Max(1, $StartupRetryDelaySec)
        clientAStartupAttemptsUsed = $clientAStartupAttemptsUsed
        clientAStartupLastReason = $clientAStartupLastReason
        clientBStartupAttemptsUsed = $clientBStartupAttemptsUsed
        clientBStartupLastReason = $clientBStartupLastReason
        repairCheckRequired = ($ReconnectAutoInteractCount -gt 0)
        autoInteractReachedTargetInClientB = $autoInteractReachedTargetInClientB
        useSteamBootstrap = [bool]$UseSteamBootstrap
        strictSteamRelay = [bool]$StrictSteamRelay
        steamInviteLobbyId = $SteamInviteLobbyId
        steamInviteHostId = $SteamInviteHostId
        steamBootstrapAppliedInClientB = $steamBootstrapAppliedInClientB
        steamBinderAppliedInClientB = $steamBinderAppliedInClientB
        steamFallbackInClientB = $steamFallbackInClientB
        clientALog = $clientALog
        clientBLog = $clientBLog
        hostEditorLog = $HostEditorLogPath
        hostDeltaLength = $hostDeltaText.Length
    }
}
catch {
    $exceptionMessage = $_.Exception.Message
    $summary = [ordered]@{
        timestamp = (Get-Date).ToString("s")
        passed = $false
        exception = $exceptionMessage
        clientStartupRetryMaxAttempts = [Math]::Max(1, $StartupRetryMaxAttempts)
        clientStartupRetryDelaySec = [Math]::Max(1, $StartupRetryDelaySec)
        clientAStartupAttemptsUsed = $clientAStartupAttemptsUsed
        clientAStartupLastReason = $clientAStartupLastReason
        clientBStartupAttemptsUsed = $clientBStartupAttemptsUsed
        clientBStartupLastReason = $clientBStartupLastReason
        clientALog = $clientALog
        clientBLog = $clientBLog
        hostEditorLog = $HostEditorLogPath
    }
}
finally {
    if (-not $KeepClientRunning) {
        if ($clientA -ne $null) {
            $clientA.Refresh()
            if (-not $clientA.HasExited) {
                Stop-Process -Id $clientA.Id -Force
            }
        }

        if ($clientB -ne $null) {
            $clientB.Refresh()
            if (-not $clientB.HasExited) {
                Stop-Process -Id $clientB.Id -Force
            }
        }
    }
}

$summary | ConvertTo-Json | Set-Content -Path $summaryPath -Encoding UTF8
if ($summary.passed) {
    Write-Output "RECONNECT_REGRESSION_PASS SUMMARY=$summaryPath RELEASED=$($summary.releasedClientId) REASSIGNED=$($summary.reassignedClientId)"
    exit 0
}

if (-not [string]::IsNullOrWhiteSpace($exceptionMessage)) {
    Write-Output "RECONNECT_REGRESSION_FAIL SUMMARY=$summaryPath ERROR=$exceptionMessage"
}
else {
    Write-Output "RECONNECT_REGRESSION_FAIL SUMMARY=$summaryPath"
}

exit 1
