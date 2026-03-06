param(
    [switch]$SetToken
)

$ErrorActionPreference = "Stop"

function Get-CodexConfigPath {
    return Join-Path $HOME ".codex\config.toml"
}

function Test-GithubMcpConfigured {
    param([string]$ConfigPath)
    if (-not (Test-Path $ConfigPath)) {
        return $false
    }

    $text = Get-Content -Raw $ConfigPath
    return ($text -match "(?ms)\[mcp_servers\.github\]")
}

$configPath = Get-CodexConfigPath
$isConfigured = Test-GithubMcpConfigured -ConfigPath $configPath

if (-not $isConfigured) {
    Write-Output "GITHUB_MCP_NOT_CONFIGURED"
    Write-Output "Expected config path: $configPath"
    exit 1
}

if ($SetToken) {
    $token = Read-Host -Prompt "Enter GitHub PAT token for GITHUB_PAT_TOKEN"
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Token is empty."
    }

    [Environment]::SetEnvironmentVariable("GITHUB_PAT_TOKEN", $token, "User")
    Write-Output "TOKEN_SET_FOR_USER_ENV=GITHUB_PAT_TOKEN"
}
else {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_PAT_TOKEN)) {
        Write-Output "TOKEN_MISSING_GITHUB_PAT_TOKEN"
    }
    else {
        Write-Output "TOKEN_PRESENT_GITHUB_PAT_TOKEN"
    }
}

Write-Output "NEXT_STEP=Restart Codex desktop app to load updated MCP servers."
