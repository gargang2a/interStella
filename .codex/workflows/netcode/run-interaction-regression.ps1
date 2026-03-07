param(
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 240,
    [int]$PostInteractWaitSec = 25,
    [int]$AutoInteractCount = 2,
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

function Start-InteractionClient {
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
        [Parameter(Mandatory = $true)]
        [int]$InteractCount,
        [bool]$UseSteamBootstrap = $false,
        [bool]$StrictSteamRelay = $false,
        [string]$InviteLobbyId = "",
        [string]$InviteHostId = ""
    )

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
        "-interstella-auto-interact", "1",
        "-interstella-auto-interact-count", ([Math]::Max(1, $InteractCount).ToString()),
        "-executeMethod", "InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay",
        "-logFile", $LogPath
    )

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

function Start-InteractionClientWithRetry {
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
        [Parameter(Mandatory = $true)]
        [int]$InteractCount,
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
        $process = Start-InteractionClient `
            -UnityPath $UnityPath `
            -ProjectPath $ProjectPath `
            -HubSessionId $HubSessionId `
            -AccessToken $AccessToken `
            -LogPath $attemptLogPath `
            -InteractCount $InteractCount `
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

function Parse-OwnerBoundaryPass {
    param([string]$HostLogDelta)

    $matches = [Regex]::Matches($HostLogDelta, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\. caller=(\-?\d+), owner=(\-?\d+), committed=(True|False), object=([^\r\n]+)")
    if ($matches.Count -eq 0) {
        return $false
    }

    foreach ($match in $matches) {
        $caller = $match.Groups[1].Value
        $owner = $match.Groups[2].Value
        if ($caller -ne $owner) {
            return $false
        }
    }

    return $true
}

Assert-File -Path $UnityEditorPath -Label "Unity editor"
Assert-Directory -Path $HostProjectPath -Label "Host project"
Assert-Directory -Path $ClientProjectPath -Label "Client project"
Assert-File -Path $HostEditorLogPath -Label "Host editor log"
Assert-Directory -Path $LogDirectory -Label "Log"

$hostPortListening = (netstat -ano | Select-String "UDP\s+.*:7770\s+.*\s+\d+$").Count -gt 0
if (-not $hostPortListening) {
    throw "Host is not listening on UDP 7770. Start host Play mode before running this workflow."
}

$hostCommandLine = Get-HostCommandLine -ProjectPath $HostProjectPath
$hubSessionId = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "hubSessionId"
$accessToken = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "accessToken"
if ([string]::IsNullOrWhiteSpace($hubSessionId) -or [string]::IsNullOrWhiteSpace($accessToken)) {
    throw "Failed to parse hubSessionId/accessToken from host Unity process."
}

$startOffset = (Get-Item $HostEditorLogPath).Length
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$clientLog = Join-Path $LogDirectory "client-interaction-regression-$stamp.log"
$summaryPath = Join-Path $LogDirectory "interaction-regression-summary-$stamp.json"
$client = $null
$summary = $null
$exceptionMessage = ""
$clientStartupAttemptsUsed = 0
$clientStartupLastReason = ""

try {
    $clientStartResult = Start-InteractionClientWithRetry `
        -UnityPath $UnityEditorPath `
        -ProjectPath $ClientProjectPath `
        -HubSessionId $hubSessionId `
        -AccessToken $accessToken `
        -BaseLogPath $clientLog `
        -InteractCount $AutoInteractCount `
        -ClientReadyTimeoutSec $ClientBootTimeoutSec `
        -MaxAttempts $StartupRetryMaxAttempts `
        -RetryDelaySec $StartupRetryDelaySec `
        -UseSteamBootstrap $UseSteamBootstrap `
        -StrictSteamRelay $StrictSteamRelay `
        -InviteLobbyId $SteamInviteLobbyId `
        -InviteHostId $SteamInviteHostId

    $client = $clientStartResult.Process
    $clientLog = $clientStartResult.LogPath
    $clientStartupAttemptsUsed = $clientStartResult.AttemptsUsed
    $clientStartupLastReason = $clientStartResult.Reason
    if (-not $clientStartResult.Ready) {
        throw "Interaction client failed to become ready. Reason=$($clientStartResult.Reason) Attempts=$($clientStartResult.AttemptsUsed) Log=$clientLog"
    }

    Start-Sleep -Seconds $PostInteractWaitSec
    $hostDeltaText = Read-AppendedText -Path $HostEditorLogPath -StartOffset $startOffset
    $clientLogText = if (Test-Path $clientLog -PathType Leaf) { Get-Content $clientLog -Raw } else { "" }

    $assignedMatch = [Regex]::Match($hostDeltaText, "Assigned client (\d+) to slot 1 \(PlayerB\)\.")
    $acceptedAnyMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\. caller=(\-?\d+), owner=(\-?\d+), committed=(True|False), object=([^\r\n]+)")
    $acceptedCommittedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\..*committed=True")
    $acceptedUncommittedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\..*committed=False")
    $deliveryMatches = [Regex]::Matches($hostDeltaText, "\[RepairStationObjective\] Delivery accepted\. delivered=(\d+)/(\d+)")
    $regressionSeedAppliedInHost = $hostDeltaText -match "\[FishNetScenePlayerAssigner\] Regression seed ready for slot \d+\."
    $fuelTransientAcceptedInHost = $hostDeltaText -match "\[PlayerFuelNetworkState\] Transient fuel submit accepted\."
    $fuelDurableAppliedInHost = $hostDeltaText -match "\[PlayerFuelNetworkState\] Durable fuel sync applied\."
    $fuelRejectedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerFuelNetworkState\] Fuel submit rejected\.")
    $repairDurablePublishedInHost = $hostDeltaText -match "\[RepairObjectiveNetworkState\] Durable repair sync published\."
    $tetherDurablePublishedInHost = $hostDeltaText -match "\[TetherNetworkStateReplicator\] Durable tether (sync published|snapshot)\."
    $ownerBoundaryPass = Parse-OwnerBoundaryPass -HostLogDelta $hostDeltaText
    $autoInteractReachedTarget = $clientLogText -match "successes=\d+/$([Math]::Max(1, $AutoInteractCount))"
    $fuelDurableAppliedInClient = $clientLogText -match "\[PlayerFuelNetworkState\] Durable fuel sync applied\."
    $repairTransientMatches = [Regex]::Matches($clientLogText, "\[RepairObjectiveNetworkState\] Transient delivery event received\. delivered=(\d+)/(\d+)")
    $repairTransientEventCountInClient = $repairTransientMatches.Count
    $repairTransientDuplicateDetected = $false
    $repairTransientMonotonic = $true
    $repairLastDeliveredCount = -1
    $repairDeliveredSet = @{}
    foreach ($repairTransientMatch in $repairTransientMatches) {
        $deliveredCount = [int]$repairTransientMatch.Groups[1].Value
        if ($repairDeliveredSet.ContainsKey($deliveredCount)) {
            $repairTransientDuplicateDetected = $true
        }
        else {
            $repairDeliveredSet[$deliveredCount] = $true
        }

        if ($repairLastDeliveredCount -gt $deliveredCount) {
            $repairTransientMonotonic = $false
        }

        $repairLastDeliveredCount = $deliveredCount
    }

    $tetherDurableAppliedInClient = $clientLogText -match "\[TetherNetworkStateReplicator\] Durable tether sync applied\."
    $tetherTransientBreakCountInClient = ([Regex]::Matches($clientLogText, "\[TetherNetworkStateReplicator\] Transient tether break received\.")).Count

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

    $steamBootstrapAppliedInClient = $clientLogText -match "\[SteamSessionService\] Applied Steam bootstrap to FishNet\..*binder=True"
    $steamBinderAppliedInClient = $clientLogText -match "\[FishNetSessionService\] Steam relay transport binder applied\."
    $steamFallbackInClient = $clientLogText -match "Falling back to direct endpoint because _allowSteamFallbackToDirect is enabled\."

    $steamPass = $true
    if ($UseSteamBootstrap) {
        $steamPass = $steamBootstrapAppliedInClient -and $steamBinderAppliedInClient
        if ($StrictSteamRelay) {
            $steamPass = $steamPass -and (-not $steamFallbackInClient)
        }
    }

    $durableTransientPass = $fuelTransientAcceptedInHost `
        -and ($fuelRejectedMatches.Count -eq 0) `
        -and ($fuelDurableAppliedInClient -or $fuelDurableAppliedInHost) `
        -and $repairDurablePublishedInHost `
        -and ($deliveryMatches.Count -ge 1) `
        -and (-not $deliveryDuplicateDetectedInHost) `
        -and $deliveryMonotonicInHost `
        -and $tetherDurablePublishedInHost

    $passed = ($assignedMatch.Success `
        -and $ownerBoundaryPass `
        -and ($authorityMismatchAcceptedCount -eq 0) `
        -and $durableTransientPass `
        -and $acceptedCommittedMatches.Count -ge 1 `
        -and $deliveryMatches.Count -ge 1 `
        -and $steamPass)
    $assignedClientId = if ($assignedMatch.Success) { $assignedMatch.Groups[1].Value } else { "" }

    $summary = [ordered]@{
        timestamp = (Get-Date).ToString("s")
        passed = $passed
        assignedClientDetected = $assignedMatch.Success
        assignedClientId = $assignedClientId
        ownerBoundaryPass = $ownerBoundaryPass
        authorityMismatchAcceptedCount = $authorityMismatchAcceptedCount
        durableTransientPass = $durableTransientPass
        fuelTransientAcceptedInHost = $fuelTransientAcceptedInHost
        fuelDurableAppliedInHost = $fuelDurableAppliedInHost
        fuelRejectedCountInHost = $fuelRejectedMatches.Count
        fuelDurableAppliedInClient = $fuelDurableAppliedInClient
        repairDurablePublishedInHost = $repairDurablePublishedInHost
        deliveryDuplicateDetectedInHost = $deliveryDuplicateDetectedInHost
        deliveryMonotonicInHost = $deliveryMonotonicInHost
        tetherDurablePublishedInHost = $tetherDurablePublishedInHost
        repairTransientEventCountInClient = $repairTransientEventCountInClient
        repairTransientDuplicateDetected = $repairTransientDuplicateDetected
        repairTransientMonotonic = $repairTransientMonotonic
        tetherDurableAppliedInClient = $tetherDurableAppliedInClient
        tetherTransientBreakCountInClient = $tetherTransientBreakCountInClient
        acceptedAnyCount = $acceptedAnyMatches.Count
        acceptedCommittedCount = $acceptedCommittedMatches.Count
        acceptedUncommittedCount = $acceptedUncommittedMatches.Count
        deliveryAcceptedCount = $deliveryMatches.Count
        regressionSeedAppliedInHost = $regressionSeedAppliedInHost
        autoInteractReachedTarget = $autoInteractReachedTarget
        autoInteractTarget = [Math]::Max(1, $AutoInteractCount)
        useSteamBootstrap = [bool]$UseSteamBootstrap
        strictSteamRelay = [bool]$StrictSteamRelay
        clientStartupRetryMaxAttempts = [Math]::Max(1, $StartupRetryMaxAttempts)
        clientStartupRetryDelaySec = [Math]::Max(1, $StartupRetryDelaySec)
        clientStartupAttemptsUsed = $clientStartupAttemptsUsed
        clientStartupLastReason = $clientStartupLastReason
        steamInviteLobbyId = $SteamInviteLobbyId
        steamInviteHostId = $SteamInviteHostId
        steamBootstrapAppliedInClient = $steamBootstrapAppliedInClient
        steamBinderAppliedInClient = $steamBinderAppliedInClient
        steamFallbackInClient = $steamFallbackInClient
        steamPass = $steamPass
        clientLog = $clientLog
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
        clientStartupAttemptsUsed = $clientStartupAttemptsUsed
        clientStartupLastReason = $clientStartupLastReason
        clientLog = $clientLog
        hostEditorLog = $HostEditorLogPath
    }
}
finally {
    if (-not $KeepClientRunning -and $client -ne $null) {
        $client.Refresh()
        if (-not $client.HasExited) {
            Stop-Process -Id $client.Id -Force
        }
    }
}

$summary | ConvertTo-Json | Set-Content -Path $summaryPath -Encoding UTF8
if ($summary.passed) {
    Write-Output "INTERACTION_REGRESSION_PASS SUMMARY=$summaryPath ASSIGNED=$($summary.assignedClientId) COMMITTED=$($summary.acceptedCommittedCount) DELIVERIES=$($summary.deliveryAcceptedCount)"
    exit 0
}

if (-not [string]::IsNullOrWhiteSpace($exceptionMessage)) {
    Write-Output "INTERACTION_REGRESSION_FAIL SUMMARY=$summaryPath ERROR=$exceptionMessage"
}
else {
    Write-Output "INTERACTION_REGRESSION_FAIL SUMMARY=$summaryPath"
}

exit 1
