param(
    [string]$ProjectPath = "C:\Unity\interStella",
    [switch]$SkipPull,
    [switch]$SkipBuild,
    [switch]$AllowDirty,
    [switch]$ReleaseBuild,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )

    & git -C $RepositoryPath @Args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Args -join ' ') failed."
    }
}

function Assert-GitRepository {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath
    )

    cmd /c "git -C `"$RepositoryPath`" rev-parse --is-inside-work-tree >nul 2>nul"
    if ($LASTEXITCODE -ne 0) {
        throw "Project path is not a Git repository: $RepositoryPath"
    }
}

function Get-GitOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )

    $output = & git -C $RepositoryPath @Args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Args -join ' ') failed."
    }

    return ($output | Out-String).Trim()
}

function Has-Upstream {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath
    )

    cmd /c "git -C `"$RepositoryPath`" rev-parse --abbrev-ref --symbolic-full-name ""@{u}"" >nul 2>nul"
    return ($LASTEXITCODE -eq 0)
}

function Get-DirtyEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath
    )

    $statusLines = & git -C $RepositoryPath status --short
    if ($LASTEXITCODE -ne 0) {
        throw "git status --short failed."
    }

    return @($statusLines)
}

function Get-BuildInfoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath
    )

    return Join-Path $RepositoryPath "Builds\SteamSmokeWindows64\build-info.txt"
}

Assert-GitRepository -RepositoryPath $ProjectPath

$branchName = Get-GitOutput -RepositoryPath $ProjectPath rev-parse --abbrev-ref HEAD
$commitHash = Get-GitOutput -RepositoryPath $ProjectPath rev-parse HEAD
$dirtyEntries = Get-DirtyEntries -RepositoryPath $ProjectPath

if (-not $AllowDirty -and $dirtyEntries.Count -gt 0) {
    throw @"
Workspace has local changes. Commit/stash them first or rerun with -AllowDirty.
Branch: $branchName
Dirty entries:
$($dirtyEntries -join [Environment]::NewLine)
"@
}

if ($WhatIf) {
    Write-Output ("STEAM_SMOKE_SYNC_PREVIEW BRANCH={0} COMMIT={1} PULL={2} BUILD={3} DIRTY={4}" -f $branchName, $commitHash, (-not $SkipPull.IsPresent), (-not $SkipBuild.IsPresent), $dirtyEntries.Count)
    return
}

if (-not $SkipPull) {
    Invoke-Git -RepositoryPath $ProjectPath fetch origin --prune
    if (Has-Upstream -RepositoryPath $ProjectPath) {
        Invoke-Git -RepositoryPath $ProjectPath pull --ff-only
    }
    else {
        Write-Output "NO_UPSTREAM_TRACKING_BRANCH"
    }

    $branchName = Get-GitOutput -RepositoryPath $ProjectPath rev-parse --abbrev-ref HEAD
    $commitHash = Get-GitOutput -RepositoryPath $ProjectPath rev-parse HEAD
    Write-Output ("PULL_COMPLETED BRANCH={0} COMMIT={1}" -f $branchName, $commitHash)
}

if ($SkipBuild) {
    $buildInfoPath = Get-BuildInfoPath -RepositoryPath $ProjectPath
    if (Test-Path $buildInfoPath -PathType Leaf) {
        Write-Output ("BUILD_INFO {0}" -f $buildInfoPath)
        Get-Content $buildInfoPath
    }
    else {
        Write-Output "BUILD_SKIPPED_NO_BUILD_INFO"
    }

    return
}

$buildScriptPath = Join-Path $ProjectPath ".codex\workflows\netcode\build-steam-smoke.ps1"
if (-not (Test-Path $buildScriptPath -PathType Leaf)) {
    throw "Build script not found: $buildScriptPath"
}

$buildArguments = @{
    ProjectPath = $ProjectPath
}

if ($ReleaseBuild) {
    $buildArguments["ReleaseBuild"] = $true
}

& $buildScriptPath @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Steam smoke build script failed."
}

$buildInfoPath = Get-BuildInfoPath -RepositoryPath $ProjectPath
if (Test-Path $buildInfoPath -PathType Leaf) {
    Write-Output ("BUILD_INFO {0}" -f $buildInfoPath)
    Get-Content $buildInfoPath
}
else {
    Write-Output "BUILD_INFO_MISSING"
}
