param(
    [switch]$SkipGitChecks
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot "docker-compose.selfhost.yml"
$EnvFile = Join-Path $RepoRoot ".env.selfhost"
$LastTagFile = Join-Path $RepoRoot ".last-deployed-tag"
$CurrentTagFile = Join-Path $RepoRoot ".current-deployed-tag"

function Get-EnvValue {
    param([string]$Path, [string]$Key, [string]$Default = "")
    foreach ($line in Get-Content $Path) {
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2 -and $parts[0].Trim() -eq $Key) { return $parts[1].Trim() }
    }
    return $Default
}

if (-not (Test-Path $EnvFile)) {
    throw "Chua co file .env.selfhost. Hay copy tu .env.selfhost.example va dien secrets."
}

# 1. Kiem tra Git an toan
if (-not $SkipGitChecks) {
    $branch = (git rev-parse --abbrev-ref HEAD).Trim()
    if ($branch -ne "main") {
        throw "Chi duoc deploy tu nhanh 'main' (hien tai: $branch). Dung -SkipGitChecks neu muon bo qua."
    }
    $dirty = git status --porcelain
    if (-not [string]::IsNullOrWhiteSpace($dirty)) {
        throw "Working tree dang co thay doi chua commit. Hay commit/stash truoc khi deploy."
    }
    Write-Host "[Deploy] Dang cap nhat code moi nhat tu origin/main (--ff-only)..." -ForegroundColor Cyan
    git pull --ff-only origin main
    if ($LASTEXITCODE -ne 0) { throw "git pull --ff-only that bai." }
}

$NewTag = (git rev-parse --short HEAD).Trim()
$PrevTag = if (Test-Path $CurrentTagFile) { (Get-Content $CurrentTagFile -Raw).Trim() } else { "" }
if (-not [string]::IsNullOrWhiteSpace($PrevTag) -and $PrevTag -ne $NewTag) {
    Set-Content -Path $LastTagFile -Value $PrevTag -NoNewline
}

# 2. Dam bao SQL Server healthy va khoi tao DB + user rieng (khong dung sa cho app)
Write-Host "[Deploy] Khoi dong/kiem tra sqlserver-selfhost..." -ForegroundColor Cyan
docker compose -f $ComposeFile --env-file $EnvFile up -d sqlserver-selfhost

$SaPassword = Get-EnvValue -Path $EnvFile -Key "MSSQL_SA_PASSWORD"
$AppDbUser = Get-EnvValue -Path $EnvFile -Key "APP_DB_USER" -Default "imm_app"
$AppDbPassword = Get-EnvValue -Path $EnvFile -Key "APP_DB_PASSWORD"
$BackupDir = (Get-EnvValue -Path $EnvFile -Key "BACKUP_DIR" -Default "C:\ImmBackups") -replace '/', '\'
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

# Cho sqlserver-selfhost healthy
for ($i = 0; $i -lt 30; $i++) {
    $health = docker inspect -f "{{.State.Health.Status}}" imm-sqlserver-selfhost 2>$null
    if ($health -eq "healthy") { break }
    Start-Sleep -Seconds 3
}
if ((docker inspect -f "{{.State.Health.Status}}" imm-sqlserver-selfhost) -ne "healthy") {
    throw "sqlserver-selfhost khong dat trang thai healthy."
}

# Khoi tao DB, Recovery SIMPLE (tranh phinh log o D), va user rieng imm_app
# Tach thanh 2 batch vi SQL Server compile USE [InternalMaintenanceDb] truoc khi CREATE DATABASE chay
Write-Host "[Deploy] Dam bao DB va user rieng [$AppDbUser] ton tai..." -ForegroundColor Cyan
$initBatch1 = @"
IF DB_ID(N'InternalMaintenanceDb') IS NULL
BEGIN
    CREATE DATABASE [InternalMaintenanceDb];
END;
ALTER DATABASE [InternalMaintenanceDb] SET RECOVERY SIMPLE;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$AppDbUser')
BEGIN
    CREATE LOGIN [$AppDbUser] WITH PASSWORD = N'$AppDbPassword', CHECK_POLICY = ON;
END;
"@
docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -C -b -Q $initBatch1
if ($LASTEXITCODE -ne 0) { throw "Khoi tao DB/Login that bai." }

$initBatch2 = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$AppDbUser')
BEGIN
    CREATE USER [$AppDbUser] FOR LOGIN [$AppDbUser];
END;
ALTER ROLE db_datareader ADD MEMBER [$AppDbUser];
ALTER ROLE db_datawriter ADD MEMBER [$AppDbUser];
ALTER ROLE db_ddladmin ADD MEMBER [$AppDbUser];
"@
docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -d InternalMaintenanceDb -C -b -Q $initBatch2
if ($LASTEXITCODE -ne 0) { throw "Khoi tao user DB that bai." }

# 3. Backup DB truoc khi deploy (neu da co bang du lieu)
$tableCount = docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [InternalMaintenanceDb].sys.tables;"
if ([int]($tableCount.Trim()) -gt 0) {
    & (Join-Path $RepoRoot "backup-selfhost.ps1")
} else {
    Write-Host "[Deploy] DB moi tinh, bo qua pre-deploy backup." -ForegroundColor DarkGray
}

# 4. Build va thay the container voi tag git SHA
Write-Host "[Deploy] Build & khoi chay version $NewTag (downtime ngan co kiem soat)..." -ForegroundColor Cyan
$env:IMM_IMAGE_TAG = $NewTag
docker compose -f $ComposeFile --env-file $EnvFile up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Build/Start that bai!"
    if (-not [string]::IsNullOrWhiteSpace($PrevTag)) {
        & (Join-Path $RepoRoot "rollback-selfhost.ps1") -Tag $PrevTag
    }
    throw "Deploy that bai."
}

# 5. Cho api-selfhost & web-selfhost healthy
$ok = $false
for ($i = 0; $i -lt 30; $i++) {
    $apiH = docker inspect -f "{{.State.Health.Status}}" imm-api-selfhost 2>$null
    $webH = docker inspect -f "{{.State.Health.Status}}" imm-web-selfhost 2>$null
    if ($apiH -eq "healthy" -and $webH -eq "healthy") { $ok = $true; break }
    Start-Sleep -Seconds 3
}

if (-not $ok) {
    Write-Warning "Container khong dat healthy sau khi deploy! Dang tu dong rollback..."
    if (-not [string]::IsNullOrWhiteSpace($PrevTag)) {
        & (Join-Path $RepoRoot "rollback-selfhost.ps1") -Tag $PrevTag
    }
    throw "Deploy that bai do healthcheck."
}

Set-Content -Path $CurrentTagFile -Value $NewTag -NoNewline

# 6. Giu 3 ban image gan nhat, xoa image cu hon & dangling (khong dung builder prune de giu 20GB Engine GC)
foreach ($repo in @("imm-api", "imm-web")) {
    $oldTags = docker images $repo --format "{{.Tag}}|{{.CreatedAt}}" |
        Where-Object { $_ -notmatch '^<none>' -and $_ -notmatch "^$NewTag\|" } |
        Select-Object -Skip 2
    foreach ($entry in $oldTags) {
        $tagToRemove = ($entry -split '\|')[0]
        if (-not [string]::IsNullOrWhiteSpace($tagToRemove) -and $tagToRemove -ne $PrevTag) {
            docker rmi "${repo}:${tagToRemove}" 2>$null | Out-Null
        }
    }
}
docker image prune -f | Out-Null

Write-Host "[Deploy] THANH CONG! Phien ban $NewTag dang chay tai http://127.0.0.1:5173" -ForegroundColor Green
docker system df

