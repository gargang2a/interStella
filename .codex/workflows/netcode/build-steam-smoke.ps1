param(
    [string]$ProjectPath = "C:\Unity\interStella",
    [string]$UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [string]$OutputPath = "Builds/SteamSmokeWindows64/interStella-Smoke.exe",
    [string]$LogDirectory = "C:\Unity\interStella\Logs",
    [string]$HubSessionId = "",
    [string]$AccessToken = "",
    [string]$LicensingIpc = "",
    [string]$CloudEnvironment = "",
    [switch]$ReleaseBuild,
    [switch]$WhatIfBuild
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
    param([string]$ResolvedProjectPath)

    try {
        $escapedPath = [Regex]::Escape($ResolvedProjectPath)
        $hostProcess = Get-CimInstance Win32_Process |
            Where-Object { $_.Name -eq "Unity.exe" -and $_.CommandLine -match "-projectpath\s+(`"|)$escapedPath(`"|)" } |
            Select-Object -First 1
    }
    catch {
        return ""
    }

    if ($null -eq $hostProcess) {
        return ""
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

Assert-Directory -Path $ProjectPath -Label "Project"
Assert-Directory -Path $LogDirectory -Label "Log directory"
Assert-File -Path $UnityEditorPath -Label "Unity editor"

$hostCommandLine = Get-HostCommandLine -ResolvedProjectPath $ProjectPath
$projectOpenInEditor = -not [string]::IsNullOrWhiteSpace($hostCommandLine)
if (-not [string]::IsNullOrWhiteSpace($hostCommandLine)) {
    if ([string]::IsNullOrWhiteSpace($HubSessionId)) {
        $HubSessionId = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "hubSessionId"
    }

    if ([string]::IsNullOrWhiteSpace($AccessToken)) {
        $AccessToken = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "accessToken"
    }

    if ([string]::IsNullOrWhiteSpace($LicensingIpc)) {
        $LicensingIpc = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "licensingIpc"
    }

    if ([string]::IsNullOrWhiteSpace($CloudEnvironment)) {
        $CloudEnvironment = Extract-ArgumentValue -CommandLine $hostCommandLine -ArgumentName "cloudEnvironment"
    }
}

if ([string]::IsNullOrWhiteSpace($CloudEnvironment)) {
    $CloudEnvironment = "production"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$batchLogPath = Join-Path $LogDirectory "build-steam-smoke-$timestamp.log"
$developmentFlag = if ($ReleaseBuild) { "0" } else { "1" }

$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "InterStella.EditorTools.InterStellaBuildSmoke.BuildSteamSmokeWindows64FromCommandLine",
    "-interstella-build-output", $OutputPath,
    "-interstella-build-development", $developmentFlag,
    "-logFile", $batchLogPath
)

if (-not [string]::IsNullOrWhiteSpace($HubSessionId) -and
    -not [string]::IsNullOrWhiteSpace($AccessToken) -and
    -not [string]::IsNullOrWhiteSpace($LicensingIpc)) {
    $arguments = @(
        "-useHub",
        "-hubIPC",
        "-cloudEnvironment", $CloudEnvironment,
        "-licensingIpc", $LicensingIpc,
        "-hubSessionId", $HubSessionId,
        "-accessToken", $AccessToken
    ) + $arguments
}

if ($WhatIfBuild) {
    Write-Output ("STEAM_BUILD_PREVIEW OUTPUT={0} LOG={1} RELEASE={2} HUB={3} PROJECT_OPEN={4}" -f $OutputPath, $batchLogPath, $ReleaseBuild.IsPresent, (-not [string]::IsNullOrWhiteSpace($HubSessionId)), $projectOpenInEditor)
    Write-Output ("UNITY_PATH {0}" -f $UnityEditorPath)
    Write-Output ("ARGUMENTS {0}" -f ($arguments -join " "))
    return
}

if ($projectOpenInEditor) {
    throw @"
Batch build cannot run while the same project is open in Unity.
Use the Unity menu instead:
Tools/InterStella/Build/Build Steam Smoke Windows64
Or close the editor and rerun this script.
"@
}

if ([string]::IsNullOrWhiteSpace($HubSessionId) -or
    [string]::IsNullOrWhiteSpace($AccessToken) -or
    [string]::IsNullOrWhiteSpace($LicensingIpc)) {
    throw @"
Batch build is missing Unity Hub licensing arguments.
Provide -HubSessionId, -AccessToken, and -LicensingIpc explicitly,
or run the build from the Unity editor menu:
Tools/InterStella/Build/Build Steam Smoke Windows64
"@
}

& $UnityEditorPath @arguments
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Steam smoke build failed with exit code $exitCode. See log: $batchLogPath"
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $ProjectPath $OutputPath
}

if (-not (Test-Path $resolvedOutputPath -PathType Leaf)) {
    throw "Steam smoke build did not produce expected output: $resolvedOutputPath"
}

Write-Output ("STEAM_BUILD_COMPLETED OUTPUT={0} LOG={1} RELEASE={2}" -f $resolvedOutputPath, $batchLogPath, $ReleaseBuild.IsPresent)
