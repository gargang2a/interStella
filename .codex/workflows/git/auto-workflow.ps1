param(
    [Parameter(Mandatory = $true)]
    [string]$Task,
    [string]$Base = "main",
    [string]$CommitMessage = "",
    [switch]$Push
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$stamp = Get-Date -Format "yyMMdd-HHmm"
$branchSeed = "$stamp-$Task"

& (Join-Path $root "auto-branch.ps1") -Name $branchSeed -Base $Base -SyncBase

if (-not [string]::IsNullOrWhiteSpace($CommitMessage)) {
    & (Join-Path $root "auto-commit.ps1") -Message $CommitMessage -Push:$Push
}
else {
    Write-Output "BRANCH_READY_NO_COMMIT"
}
