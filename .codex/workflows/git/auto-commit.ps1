param(
    [string]$Message = "",
    [switch]$Push
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

Invoke-Git add --all
& git diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Output "NO_STAGED_CHANGES"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    $Message = "chore: codex checkpoint $stamp"
}

Invoke-Git commit -m $Message

if (-not $Push) {
    Write-Output "COMMITTED_ONLY"
    exit 0
}

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ([string]::IsNullOrWhiteSpace($branch)) {
    throw "Unable to resolve current branch."
}

if (Has-Upstream) {
    Invoke-Git pull --rebase --autostash
    Invoke-Git push
}
else {
    Invoke-Git push -u origin $branch
}

Write-Output "COMMITTED_AND_PUSHED=$branch"
