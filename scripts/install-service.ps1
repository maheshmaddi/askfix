# AskFix - install (or update) the Windows service on the intranet server.
# Run ON THE SERVER, from inside the deployed folder, as Administrator.
# Usage:  .\install-service.ps1 [-Port 8080]
param(
    [int]$Port = 8080,
    [string]$ServiceName = "AskFix"
)

$ErrorActionPreference = "Stop"

# must be running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw "Run this script as Administrator." }

$appDir = Split-Path -Parent $PSScriptRoot   # scripts/ sits inside the deployed folder
$exe = Join-Path $appDir "AskFix.Api.exe"
if (-not (Test-Path $exe)) { throw "AskFix.Api.exe not found in $appDir" }

# stop + remove previous installation, keep data
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating service '$ServiceName' on port $Port..." -ForegroundColor Cyan
$binPath = "`"$exe`" --urls http://0.0.0.0:$Port"
sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= "AskFix Q&A" | Out-Null
sc.exe description $ServiceName "AskFix - internal Q&A for tool and setup problems (SQLite, Windows auth)"
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$svc = Get-Service -Name $ServiceName
Write-Host ""
Write-Host "Service status: $($svc.Status)" -ForegroundColor Green
Write-Host "App:      http://localhost:$Port"
Write-Host "Database: $appDir\askfix.db  (back this single file up)"
Write-Host "Logs:     Windows Event Viewer > Application (source: AskFix)"
Write-Host ""
Write-Host "Before first use, edit $appDir\appsettings.Production.json:" -ForegroundColor Yellow
Write-Host '  "Auth": { "Mode": "Ldap", "DefaultDomain": "YOURDOMAIN" }'
Write-Host "  then restart the service:  Restart-Service $ServiceName"
