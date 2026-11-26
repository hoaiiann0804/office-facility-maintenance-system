param(
    [switch]$Weekly,
    [string]$RcloneRemote = $env:RCLONE_BACKUP_REMOTE
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$EnvFile = Join-Path $RepoRoot ".env.selfhost"

function Get-EnvValue {
    param([string]$Path, [string]$Key, [string]$Default = "")
    if (-not (Test-Path $Path)) { return $Default }
    foreach ($line in Get-Content $Path) {
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2 -and $parts[0].Trim() -eq $Key) {
            return $parts[1].Trim()
        }
    }
    return $Default
}

if (-not (Test-Path $EnvFile)) {
    throw "Khong tim thay $EnvFile. Hay copy tu .env.selfhost.example truoc."
}

$SaPassword = Get-EnvValue -Path $EnvFile -Key "MSSQL_SA_PASSWORD"
$BackupDir = (Get-EnvValue -Path $EnvFile -Key "BACKUP_DIR" -Default "C:\ImmBackups") -replace '/', '\'

if ([string]::IsNullOrWhiteSpace($SaPassword)) {
    throw "Thieu MSSQL_SA_PASSWORD trong .env.selfhost"
}

$DailyDir = Join-Path $BackupDir "daily"
$WeeklyDir = Join-Path $BackupDir "weekly"
New-Item -ItemType Directory -Force -Path $DailyDir, $WeeklyDir | Out-Null

$containerRunning = docker inspect -f "{{.State.Running}}" imm-sqlserver-selfhost 2>$null
if ($containerRunning -ne "true") {
    throw "Container imm-sqlserver-selfhost khong dang chay. Khong the backup."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$fileName = "imm_${timestamp}.bak"
$containerBakPath = "/var/opt/mssql/backup/daily/$fileName"
$hostBakPath = Join-Path $DailyDir $fileName

Write-Host "[Backup] Dang tao backup SQL Server -> $hostBakPath ..." -ForegroundColor Cyan
# Express Edition khong ho tro WITH COMPRESSION, van giu CHECKSUM de kiem tra toan ven
$sql = "BACKUP DATABASE [InternalMaintenanceDb] TO DISK = N'$containerBakPath' WITH CHECKSUM, INIT, NAME = N'IMM Full Backup $timestamp';"
docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -C -b -Q $sql
if ($LASTEXITCODE -ne 0) {
    throw "Backup SQL Server that bai (exit code $LASTEXITCODE)."
}

if (-not (Test-Path $hostBakPath)) {
    throw "Khong tim thay file backup sau khi chay sqlcmd: $hostBakPath"
}

# Ban Chu Nhat hoac khi truyen -Weekly: luu them ban tuan
if ($Weekly -or ((Get-Date).DayOfWeek -eq [DayOfWeek]::Sunday)) {
    $weeklyPath = Join-Path $WeeklyDir $fileName
    Copy-Item -Path $hostBakPath -Destination $weeklyPath -Force
    Write-Host "[Backup] Da luu ban backup tuan -> $weeklyPath" -ForegroundColor Green
}

# Giu 7 ban ngay gan nhat
Get-ChildItem -Path $DailyDir -Filter "*.bak" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 7 |
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "[Backup] Da xoa ban ngay cu: $($_.Name)" -ForegroundColor DarkGray
    }

# Giu 4 ban tuan gan nhat
Get-ChildItem -Path $WeeklyDir -Filter "*.bak" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 4 |
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "[Backup] Da xoa ban tuan cu: $($_.Name)" -ForegroundColor DarkGray
    }

# Tuy chon day len Cloud (Cloudflare R2 / Google Drive) bang rclone
if (-not [string]::IsNullOrWhiteSpace($RcloneRemote)) {
    if (Get-Command rclone -ErrorAction SilentlyContinue) {
        Write-Host "[Backup] Dang dong bo backup len $RcloneRemote ..." -ForegroundColor Cyan
        rclone sync $BackupDir $RcloneRemote --transfers 2
    } else {
        Write-Warning "Da cau hinh RCLONE_BACKUP_REMOTE nhung may chua cai rclone."
    }
}

$sizeMb = [math]::Round((Get-Item $hostBakPath).Length / 1MB, 2)
Write-Host "[Backup] Hoan tat: $fileName ($sizeMb MB)" -ForegroundColor Green

