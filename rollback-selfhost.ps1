param(
    [string]$Tag = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot "docker-compose.selfhost.yml"
$EnvFile = Join-Path $RepoRoot ".env.selfhost"
$LastTagFile = Join-Path $RepoRoot ".last-deployed-tag"

if ([string]::IsNullOrWhiteSpace($Tag)) {
    if (Test-Path $LastTagFile) {
        $Tag = (Get-Content $LastTagFile -Raw).Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($Tag)) {
    Write-Host "Cac image imm-api hien co:" -ForegroundColor Yellow
    docker images imm-api --format "table {{.Repository}}\t{{.Tag}}\t{{.CreatedAt}}"
    throw "Vui long truyen -Tag <git-sha> de rollback."
}

$apiExists = docker images -q "imm-api:$Tag"
$webExists = docker images -q "imm-web:$Tag"
if ([string]::IsNullOrWhiteSpace($apiExists) -or [string]::IsNullOrWhiteSpace($webExists)) {
    throw "Khong tim thay image imm-api:$Tag hoac imm-web:$Tag tren may."
}

Write-Host "[Rollback] Dang quay lai phien ban $Tag ..." -ForegroundColor Yellow
$env:IMM_IMAGE_TAG = $Tag
docker compose -f $ComposeFile --env-file $EnvFile up -d --no-build

Write-Host "[Rollback] Da khoi chay lai tag $Tag." -ForegroundColor Green
Write-Warning "Luu y: Neu lan deploy vua roi da chay EF Migration thay doi schema DB, hay restore lai ban backup truoc deploy trong C:\ImmBackups\daily\."

