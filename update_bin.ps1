param(
    [switch]$KillRunning
)

if ($KillRunning) {
    Write-Host "Dang dong tien trinh Ernastie..." -ForegroundColor Yellow
    Stop-Process -Name "Ernastie" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
}

$source = Join-Path $PSScriptRoot "src\bin\Publish\*"
$dest = Join-Path $PSScriptRoot "bin"

try {
    Copy-Item -Path $source -Destination $dest -Recurse -Force -ErrorAction Stop
    Write-Host "Cap nhat thu muc bin/ thanh cong!" -ForegroundColor Green
} catch {
    Write-Host "Loi khi sao chep vao bin/: $_" -ForegroundColor Red
    Write-Host "Goi y: Vui long tat Ernastie dang chay roi chay lai script nay hoac dung flag -KillRunning" -ForegroundColor Yellow
}
