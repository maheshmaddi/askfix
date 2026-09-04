# AskFix - build everything (client SPA + API) and produce a deployable folder.
# Usage:  .\scripts\build-all.ps1 [-Output .\artifacts\askfix]
param(
    [string]$Output = ".\artifacts\askfix"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "== 1/3 Building client (React)..." -ForegroundColor Cyan
Push-Location client
npm ci --no-audit --no-fund 2>$null | Out-Null
npm run build
if ($LASTEXITCODE -ne 0) { throw "client build failed" }
Pop-Location

Write-Host "== 2/3 Publishing API (self-contained, win-x64)..." -ForegroundColor Cyan
dotnet publish src/AskFix.Api/AskFix.Api.csproj -c Release -r win-x64 --self-contained true -o $Output
if ($LASTEXITCODE -ne 0) { throw "API publish failed" }

Write-Host "== 3/3 Bundling SPA into wwwroot..." -ForegroundColor Cyan
Copy-Item client/dist/* $Output/wwwroot/ -Recurse -Force
New-Item -ItemType Directory -Force "$Output/wwwroot/uploads" | Out-Null

Write-Host ""
Write-Host "Done. Deployable folder: $((Resolve-Path $Output).Path)" -ForegroundColor Green
Write-Host "Copy it to the server, then run install-service.ps1 there."
