param(
    [string]$Base = "main",
    [string]$Head = "",
    [string]$Title = "",
    [string]$Body = "",
    [switch]$Draft
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    $output = & git @Args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Args -join ' ') failed."
    }

    return $output
}

function Assert-GitRepository {
    cmd /c "git rev-parse --is-inside-work-tree >nul 2>nul"
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Current directory is not a Git repository."
    }
}

function Resolve-CurrentBranch {
    $branch = (Invoke-Git rev-parse --abbrev-ref HEAD | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($branch)) {
        throw "Unable to resolve current branch."
    }

    return $branch
}

function Resolve-OriginRepository {
    $originUrl = (Invoke-Git remote get-url origin | Select-Object -First 1).Trim()

    $patterns = @(
        "^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?$",
        "^git@github\.com:(?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?$",
        "^ssh://git@github\.com/(?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?$"
    )

    foreach ($pattern in $patterns) {
        if ($originUrl -match $pattern) {
            return @{
                Owner = $Matches["owner"]
                Repo  = $Matches["repo"]
            }
        }
    }

    throw "Unsupported origin URL for GitHub repository: $originUrl"
}

function Ensure-RemoteBranch {
    param([string]$BranchName)

    $existing = Invoke-Git ls-remote --heads origin $BranchName
    if ([string]::IsNullOrWhiteSpace(($existing -join "").Trim())) {
        Invoke-Git push -u origin $BranchName
        Write-Output "PUSHED_HEAD_BRANCH=$BranchName"
    }
}

function Resolve-Token {
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_PAT_TOKEN)) {
        return $env:GITHUB_PAT_TOKEN
    }

    return [Environment]::GetEnvironmentVariable("GITHUB_PAT_TOKEN", "User")
}

Assert-GitRepository

$headBranch = $Head
if ([string]::IsNullOrWhiteSpace($headBranch)) {
    $headBranch = Resolve-CurrentBranch
}

if ($headBranch -eq $Base) {
    throw "Head branch and base branch are identical: $headBranch"
}

$repoInfo = Resolve-OriginRepository
Ensure-RemoteBranch -BranchName $headBranch

if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = (Invoke-Git log -1 --pretty=%s | Select-Object -First 1).Trim()
}

if ([string]::IsNullOrWhiteSpace($Body)) {
    $Body = "Automated PR from $headBranch to $Base."
}

$token = Resolve-Token
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Output "TOKEN_MISSING_GITHUB_PAT_TOKEN"
    Write-Output ("PR_CREATE_URL=https://github.com/{0}/{1}/compare/{2}...{3}?expand=1" -f $repoInfo.Owner, $repoInfo.Repo, $Base, $headBranch)
    exit 0
}

$headers = @{
    Authorization         = "Bearer $token"
    Accept                = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"          = "interStella-auto-pr"
}

$payload = @{
    title = $Title
    head  = $headBranch
    base  = $Base
    body  = $Body
    draft = [bool]$Draft
} | ConvertTo-Json -Depth 5

try {
    $response = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$($repoInfo.Owner)/$($repoInfo.Repo)/pulls" -Headers $headers -ContentType "application/json" -Body $payload
    Write-Output "PR_NUMBER=$($response.number)"
    Write-Output "PR_URL=$($response.html_url)"
}
catch {
    $existingPrUrl = $null
    try {
        $existing = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$($repoInfo.Owner)/$($repoInfo.Repo)/pulls?state=open&head=$($repoInfo.Owner):$headBranch&base=$Base" -Headers $headers
        if ($existing -and $existing.Count -gt 0) {
            $existingPrUrl = $existing[0].html_url
        }
    }
    catch {
        # Ignore lookup failure and rethrow original error below.
    }

    if (-not [string]::IsNullOrWhiteSpace($existingPrUrl)) {
        Write-Output "PR_ALREADY_EXISTS=$existingPrUrl"
        exit 0
    }

    throw
}
