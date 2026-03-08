param(
    [string]$SourceRoot = "",
    [string]$ClientRoot = "C:\Unity\interStellaClient",
    [switch]$Mirror,
    [switch]$WhatIf,
    [string[]]$Include = @(
        "Assets/Game",
        "Assets/Game.meta",
        "Assets/FishNet",
        "Assets/FishNet.meta",
        "Packages/manifest.json",
        "Packages/packages-lock.json",
        "ProjectSettings/ProjectSettings.asset",
        "ProjectSettings/EditorBuildSettings.asset",
        "ProjectSettings/TagManager.asset",
        "ProjectSettings/InputManager.asset",
        "ProjectSettings/DynamicsManager.asset",
        "steam_appid.txt"
    )
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
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

function Sync-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        [switch]$MirrorMode,
        [switch]$DryRun
    )

    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null

    $robocopyArgs = @(
        $SourcePath,
        $TargetPath
    )

    if ($MirrorMode) {
        $robocopyArgs += "/MIR"
    }
    else {
        $robocopyArgs += "/E"
    }

    $robocopyArgs += @(
        "/R:2",
        "/W:2",
        "/XJ",
        "/NFL",
        "/NDL",
        "/NP",
        "/NJH",
        "/NJS"
    )

    if ($DryRun) {
        $robocopyArgs += "/L"
    }

    & robocopy @robocopyArgs | Out-Null
    $code = $LASTEXITCODE
    if ($code -gt 7) {
        throw "robocopy failed for '$SourcePath' -> '$TargetPath' (exit code: $code)"
    }
}

function Sync-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        [switch]$DryRun
    )

    $targetDirectory = Split-Path -Parent $TargetPath
    if (-not (Test-Path $targetDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if ($DryRun) {
        Write-Output "DRYRUN_FILE_COPY $SourcePath -> $TargetPath"
        return
    }

    Copy-Item -Path $SourcePath -Destination $TargetPath -Force
}

Assert-Directory -Path $SourceRoot -Label "Source root"
Assert-Directory -Path $ClientRoot -Label "Client root"

$syncedItems = 0
$skippedItems = 0

foreach ($relativePath in $Include) {
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        continue
    }

    $normalizedRelativePath = $relativePath.Trim()
    $sourcePath = Join-Path $SourceRoot $normalizedRelativePath
    $targetPath = Join-Path $ClientRoot $normalizedRelativePath

    if (-not (Test-Path $sourcePath)) {
        Write-Warning "Skipped missing source path: $normalizedRelativePath"
        $skippedItems++
        continue
    }

    if (Test-Path $sourcePath -PathType Container) {
        Sync-Directory -SourcePath $sourcePath -TargetPath $targetPath -MirrorMode:$Mirror -DryRun:$WhatIf
        $syncedItems++
        continue
    }

    Sync-File -SourcePath $sourcePath -TargetPath $targetPath -DryRun:$WhatIf
    $syncedItems++
}

$mode = if ($Mirror) { "MIRROR" } else { "INCREMENTAL" }
$dryRunFlag = if ($WhatIf) { "TRUE" } else { "FALSE" }
Write-Output "CLIENT_SYNC_COMPLETED MODE=$mode DRYRUN=$dryRunFlag SYNCED=$syncedItems SKIPPED=$skippedItems SOURCE=$SourceRoot TARGET=$ClientRoot"
