param(
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 240,
    [int]$PostInteractWaitSec = 25,
    [int]$AutoInteractCount = 2,
    [switch]$KeepClientRunning
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
        [int]$InteractCount
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

    return Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru
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

try {
    $client = Start-InteractionClient `
        -UnityPath $UnityEditorPath `
        -ProjectPath $ClientProjectPath `
        -HubSessionId $hubSessionId `
        -AccessToken $accessToken `
        -LogPath $clientLog `
        -InteractCount $AutoInteractCount

    $clientReady = Wait-ClientReady -LogPath $clientLog -ClientProcess $client -TimeoutSec $ClientBootTimeoutSec
    if (-not $clientReady.Ready) {
        throw "Interaction client failed to become ready. Reason=$($clientReady.Reason) Log=$clientLog"
    }

    Start-Sleep -Seconds $PostInteractWaitSec
    $hostDeltaText = Read-AppendedText -Path $HostEditorLogPath -StartOffset $startOffset
    $clientLogText = if (Test-Path $clientLog -PathType Leaf) { Get-Content $clientLog -Raw } else { "" }

    $assignedMatch = [Regex]::Match($hostDeltaText, "Assigned client (\d+) to slot 1 \(PlayerB\)\.")
    $acceptedCommittedMatches = [Regex]::Matches($hostDeltaText, "\[PlayerInteractionNetworkRelay\] Accepted interaction request\..*committed=True")
    $deliveryMatches = [Regex]::Matches($hostDeltaText, "\[RepairStationObjective\] Delivery accepted\. delivered=(\d+)/(\d+)")
    $ownerBoundaryPass = Parse-OwnerBoundaryPass -HostLogDelta $hostDeltaText
    $autoInteractReachedTarget = $clientLogText -match "successes=\d+/$([Math]::Max(1, $AutoInteractCount))"

    $passed = ($assignedMatch.Success -and $ownerBoundaryPass -and $acceptedCommittedMatches.Count -ge 1 -and $deliveryMatches.Count -ge 1)
    $assignedClientId = if ($assignedMatch.Success) { $assignedMatch.Groups[1].Value } else { "" }

    $summary = [ordered]@{
        timestamp = (Get-Date).ToString("s")
        passed = $passed
        assignedClientDetected = $assignedMatch.Success
        assignedClientId = $assignedClientId
        ownerBoundaryPass = $ownerBoundaryPass
        acceptedCommittedCount = $acceptedCommittedMatches.Count
        deliveryAcceptedCount = $deliveryMatches.Count
        autoInteractReachedTarget = $autoInteractReachedTarget
        autoInteractTarget = [Math]::Max(1, $AutoInteractCount)
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
