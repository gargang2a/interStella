param(
    [switch]$Rebase = $true,
    [switch]$FfOnly
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

function Has-Upstream {
    cmd /c 'git rev-parse --abbrev-ref --symbolic-full-name "@{u}" >nul 2>nul'
    $exitCode = $LASTEXITCODE
    return ($exitCode -eq 0)
}

Assert-GitRepository
Invoke-Git fetch origin --prune

if (Has-Upstream) {
    if ($FfOnly) {
        Invoke-Git pull --ff-only
    }
    elseif ($Rebase) {
        Invoke-Git pull --rebase --autostash
    }
    else {
        Invoke-Git pull
    }
}
else {
    Write-Output "NO_UPSTREAM_TRACKING_BRANCH"
    exit 0
}

Write-Output "PULL_COMPLETED"
