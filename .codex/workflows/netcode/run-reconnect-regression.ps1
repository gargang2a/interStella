param(
    [string]$HostProjectPath = "C:\Unity\interStella",
    [string]$ClientProjectPath = "C:\Unity\interStellaClient",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$HostEditorLogPath = "",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [int]$ClientBootTimeoutSec = 240,
    [int]$PostReconnectWaitSec = 95,
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
        [string]$LogPath
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
        "-interstella-auto-interact", "0",
        "-executeMethod", "InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay",
        "-logFile", $LogPath
    )

    return Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru
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

try {
    $clientA = Start-ReconnectClient -UnityPath $UnityEditorPath -ProjectPath $ClientProjectPath -HubSessionId $hubSessionId -AccessToken $accessToken -LogPath $clientALog
    $clientAReady = Wait-ClientReady -LogPath $clientALog -ClientProcess $clientA -TimeoutSec $ClientBootTimeoutSec
    if (-not $clientAReady.Ready) {
        throw "Client A failed to become ready. Reason=$($clientAReady.Reason) Log=$clientALog"
    }

    if (-not $clientA.HasExited) {
        Stop-Process -Id $clientA.Id -Force
    }

    $clientB = Start-ReconnectClient -UnityPath $UnityEditorPath -ProjectPath $ClientProjectPath -HubSessionId $hubSessionId -AccessToken $accessToken -LogPath $clientBLog
    $clientBReady = Wait-ClientReady -LogPath $clientBLog -ClientProcess $clientB -TimeoutSec $ClientBootTimeoutSec
    if (-not $clientBReady.Ready) {
        throw "Client B failed to become ready. Reason=$($clientBReady.Reason) Log=$clientBLog"
    }

    Start-Sleep -Seconds $PostReconnectWaitSec

    $hostDeltaText = Read-AppendedText -Path $HostEditorLogPath -StartOffset $startOffset

    $queueMatch = [Regex]::Match($hostDeltaText, "No available slot for client \d+\. Queued for reassignment\.")
    $releaseMatches = [Regex]::Matches($hostDeltaText, "Released slot 1 from client (\d+); ownership removed from PlayerB\.")
    $assignMatches = [Regex]::Matches($hostDeltaText, "Assigned client (\d+) to slot 1 \(PlayerB\)\.")

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

    $passed = ($queueMatch.Success -and $releaseMatch -ne $null -and $assignAfterRelease -ne $null)
    $releasedClientId = if ($releaseMatch -ne $null) { $releaseMatch.Groups[1].Value } else { "" }
    $reassignedClientId = if ($assignAfterRelease -ne $null) { $assignAfterRelease.Groups[1].Value } else { "" }

    $summary = [ordered]@{
        timestamp = (Get-Date).ToString("s")
        passed = $passed
        queueDetected = $queueMatch.Success
        releaseDetected = ($releaseMatch -ne $null)
        reassignedAfterReleaseDetected = ($assignAfterRelease -ne $null)
        releasedClientId = $releasedClientId
        reassignedClientId = $reassignedClientId
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
