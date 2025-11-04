Param(
    [string]$ImageTag = "teams-bot:latest",
    [string]$Dockerfile = "apps/teams-bot/docker/Dockerfile.windows"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path "$repoRoot\.."

Write-Host "Building Teams bot image: $ImageTag"
docker build `
    --file "$repoRoot\$Dockerfile" `
    --tag $ImageTag `
    "$repoRoot\apps\teams-bot"
