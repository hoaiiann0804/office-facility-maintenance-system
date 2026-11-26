param(
    [string]$BackupFile = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$EnvFile = Join-Path $RepoRoot ".env.selfhost"

function Get-EnvValue {
    param([string]$Path, [string]$Key, [string]$Default = "")
    foreach ($line in Get-Content $Path) {
        if ($line -match '^\s*#' -or $line -match '^\s*$') { continue }
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2 -and $parts[0].Trim() -eq $Key) { return $parts[1].Trim() }
    }
    return $Default
}

if (-not (Test-Path $EnvFile)) { throw "Khong tim thay $EnvFile." }
$SaPassword = Get-EnvValue -Path $EnvFile -Key "MSSQL_SA_PASSWORD"
$BackupDir = (Get-EnvValue -Path $EnvFile -Key "BACKUP_DIR" -Default "C:\ImmBackups") -replace '/', '\'

if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    $latest = Get-ChildItem -Path (Join-Path $BackupDir "daily") -Filter "*.bak" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $latest) { throw "Khong co file .bak nao trong $BackupDir\daily" }
    $BackupFile = $latest.Name
    $containerPath = "/var/opt/mssql/backup/daily/$BackupFile"
} else {
    $containerPath = "/var/opt/mssql/backup/daily/$(Split-Path $BackupFile -Leaf)"
}

Write-Host "[Restore-Test] Kiem tra tinh toan ven (VERIFYONLY): $containerPath" -ForegroundColor Cyan
docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -C -b -Q "RESTORE VERIFYONLY FROM DISK = N'$containerPath' WITH CHECKSUM;"
if ($LASTEXITCODE -ne 0) { throw "RESTORE VERIFYONLY that bai!" }

$testDb = "InternalMaintenanceDb_RestoreTest"
Write-Host "[Restore-Test] Khoi phuc thu vao DB tam [$testDb] ..." -ForegroundColor Cyan
$restoreSql = @"
IF DB_ID(N'$testDb') IS NOT NULL
BEGIN
    ALTER DATABASE [$testDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$testDb];
END;
RESTORE DATABASE [$testDb]
FROM DISK = N'$containerPath'
WITH MOVE N'InternalMaintenanceDb' TO N'/var/opt/mssql/data/${testDb}.mdf',
     MOVE N'InternalMaintenanceDb_log' TO N'/var/opt/mssql/data/${testDb}_log.ldf',
     REPLACE, RECOVERY;
SELECT COUNT(*) AS TableCount FROM [$testDb].sys.tables;
ALTER DATABASE [$testDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$testDb];
"@

docker exec imm-sqlserver-selfhost /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -C -b -Q $restoreSql
if ($LASTEXITCODE -ne 0) { throw "Restore thu that bai!" }

Write-Host "[Restore-Test] THANH CONG: Ban backup $BackupFile khoi phuc duoc va toan ven!" -ForegroundColor Green

