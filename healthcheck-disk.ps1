param(
    [int]$MinFreeGb = 20
)

$ErrorActionPreference = "Stop"
$drive = Get-PSDrive -Name D -ErrorAction SilentlyContinue
if (-not $drive) {
    Write-Warning "Khong tim thay o dia D."
    exit 0
}

$freeGb = [math]::Round($drive.Free / 1GB, 2)
Write-Host "[DiskCheck] O D con trong: $freeGb GB (nguong canh bao: $MinFreeGb GB)" -ForegroundColor Cyan

if ($freeGb -lt $MinFreeGb) {
    $msg = "CANH BAO: O D chi con trong $freeGb GB (duoi nguong $MinFreeGb GB)! Hay chay cleanup-docker.ps1 hoac kiem tra docker_data.vhdx."
    Write-Warning $msg

    # Hien thong bao Windows (BalloonTip) neu dang co session desktop
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $notify = New-Object System.Windows.Forms.NotifyIcon
        $notify.Icon = [System.Drawing.SystemIcons]::Warning
        $notify.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Warning
        $notify.BalloonTipTitle = "Internal Maintenance - Canh bao o dia D"
        $notify.BalloonTipText = $msg
        $notify.Visible = $true
        $notify.ShowBalloonTip(10000)
        Start-Sleep -Seconds 2
        $notify.Dispose()
    } catch {
        # Bo qua neu chay trong session khong co UI
    }
    exit 1
}

