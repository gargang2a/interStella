param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [string]$Base = "main",
    [switch]$SyncBase
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    & git @Args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Args -join ' ') failed."
    }
}

function Assert-GitRepository {
    cmd /c "git rev-parse --is-inside-work-tree >nul 2>nul"
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Current directory is not a Git repository."
    }
}

function Normalize-BranchName {
    param([string]$Raw)

    $normalized = $Raw.Trim().ToLowerInvariant()
    $normalized = $normalized -replace "[^a-z0-9/_-]+", "-"
    $normalized = $normalized -replace "-{2,}", "-"
    $normalized = $normalized.Trim("-")
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw "Branch name is empty after normalization."
    }

    if (-not $normalized.StartsWith("codex/")) {
        $normalized = "codex/$normalized"
    }

    return $normalized
}

Assert-GitRepository
$branchName = Normalize-BranchName -Raw $Name

if ($SyncBase) {
    Invoke-Git fetch origin --prune
    Invoke-Git checkout $Base
    Invoke-Git pull --ff-only origin $Base
}

& git show-ref --verify --quiet "refs/heads/$branchName"
if ($LASTEXITCODE -eq 0) {
    Invoke-Git checkout $branchName
}
else {
    Invoke-Git checkout -b $branchName
}

Write-Output "ACTIVE_BRANCH=$branchName"
