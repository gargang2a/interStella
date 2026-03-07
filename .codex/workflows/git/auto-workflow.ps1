param(
    [Parameter(Mandatory = $true)]
    [string]$Task,
    [string]$Base = "main",
    [string]$CommitMessage = "",
    [switch]$Push,
    [switch]$CreatePr,
    [string]$PrBase = "",
    [string]$PrTitle = "",
    [string]$PrBody = "",
    [switch]$PrDraft
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

if ($CreatePr) {
    $effectivePrBase = $PrBase
    if ([string]::IsNullOrWhiteSpace($effectivePrBase)) {
        $effectivePrBase = $Base
    }

    & (Join-Path $root "auto-pr.ps1") -Base $effectivePrBase -Title $PrTitle -Body $PrBody -Draft:$PrDraft
}
