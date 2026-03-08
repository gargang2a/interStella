param(
    [string]$ProjectPath = "",
    [string]$SourceBuildPath = "",
    [string]$PublishPath = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Resolve-DefaultProjectPath {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
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

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Resolve-DefaultProjectPath
}

if ([string]::IsNullOrWhiteSpace($SourceBuildPath)) {
    $SourceBuildPath = Join-Path $ProjectPath "Builds\SteamSmokeWindows64"
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    if ([string]::IsNullOrWhiteSpace($env:OneDrive)) {
        throw "OneDrive environment path was not found. Set -PublishPath explicitly."
    }

    $PublishPath = Join-Path $env:OneDrive "interStellaBuilds\SteamSmokeWindows64"
}

Assert-Directory -Path $SourceBuildPath -Label "Source build"

if ($WhatIf) {
    Write-Output ("STEAM_SMOKE_PUBLISH_PREVIEW SOURCE={0} DEST={1}" -f $SourceBuildPath, $PublishPath)
    return
}

if (-not (Test-Path $PublishPath -PathType Container)) {
    New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
}

$null = robocopy $SourceBuildPath $PublishPath /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -gt 7) {
    throw "robocopy failed with exit code $robocopyExitCode"
}

Write-Output ("STEAM_SMOKE_PUBLISH_COMPLETED SOURCE={0} DEST={1}" -f $SourceBuildPath, $PublishPath)
