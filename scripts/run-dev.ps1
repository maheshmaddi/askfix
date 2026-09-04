# AskFix - run everything locally for development.
# Starts the API (Dev auth mode, seeded SQLite) on :8080 and the Vite dev server on :5173.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Starting API on http://localhost:8080 (Dev mode)..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
Start-Process dotnet -ArgumentList "run --project src/AskFix.Api/AskFix.Api.csproj" -WorkingDirectory $root

Write-Host "Starting Vite dev server on http://localhost:5173 ..." -ForegroundColor Cyan
Push-Location client
npm run dev
Pop-Location
