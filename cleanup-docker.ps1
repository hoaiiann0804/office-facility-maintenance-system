$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot "docker-compose.selfhost.yml"
$EnvFile = Join-Path $RepoRoot ".env.selfhost"

Write-Host "[Cleanup] Dang don dep image dangling va container dung qua 7 ngay (KHONG xoa volume)..." -ForegroundColor Cyan
docker image prune -f
docker container prune -f --filter "until=168h"

if (Test-Path $EnvFile) {
    Write-Host "[Cleanup] Kiem tra va dam bao stack Self-Host van dang chay..." -ForegroundColor Cyan
    docker compose -f $ComposeFile --env-file $EnvFile up -d --no-build
}

Write-Host "[Cleanup] Tinh trang dung luong Docker:" -ForegroundColor Green
docker system df

