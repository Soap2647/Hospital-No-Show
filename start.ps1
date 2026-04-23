# Hospital No-Show -- Startup Script
# API    : http://localhost:5048
# Client : http://localhost:5158

$root    = $PSScriptRoot
$api     = Join-Path $root "src\HospitalNoShow.API"
$client  = Join-Path $root "src\HospitalNoShow.BlazorClient"
$apiUrl  = "http://localhost:5048"
$appUrl  = "http://localhost:5158"

Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "    Hospital No-Show -- Starting Services" -ForegroundColor Cyan
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""

# 1. API
Write-Host "  [1/2] Starting API    : $apiUrl" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "Set-Location '$api'; Write-Host '[API] Running...' -ForegroundColor Green; dotnet run --launch-profile http" `
    -WindowStyle Normal

# 2. Blazor Client
Write-Host "  [2/2] Starting Client : $appUrl" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "Set-Location '$client'; Write-Host '[Client] Running...' -ForegroundColor Green; dotnet run --launch-profile http" `
    -WindowStyle Normal

# 3. Wait for API health, then open browser
Write-Host ""
Write-Host "  Waiting for API to become ready..." -ForegroundColor DarkGray

$maxWait = 60
$elapsed = 0
$ready   = $false

while ($elapsed -lt $maxWait) {
    Start-Sleep -Seconds 2
    $elapsed += 2
    try {
        $resp = Invoke-WebRequest -Uri "$apiUrl/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) { $ready = $true; break }
    } catch { }
    Write-Host "  ...${elapsed}s" -ForegroundColor DarkGray
}

if ($ready) {
    Write-Host "  API is ready!" -ForegroundColor Green
} else {
    Write-Host "  API did not respond in ${maxWait}s -- opening browser anyway." -ForegroundColor DarkYellow
}

Start-Sleep -Seconds 3
Write-Host ""
Write-Host "  Opening browser : $appUrl" -ForegroundColor Cyan
Start-Process $appUrl

Write-Host ""
Write-Host "  Both services started. Close this window when done." -ForegroundColor Green
Write-Host ""
