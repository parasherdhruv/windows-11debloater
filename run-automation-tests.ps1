# ==============================================================================
# Win11 Control Center — Automated Test & Validation Runner
# Exercises and validates Safety Boundaries, Unit Tests, and STA UI Automation
# ==============================================================================

param(
    [switch]$SkipReleaseBuild = $false
)

$ErrorActionPreference = "Stop"

# Setup portable .NET 10 LTS environment
$DotNetRoot = "$env:USERPROFILE\.dotnet"
if (Test-Path $DotNetRoot) {
    $env:DOTNET_ROOT = $DotNetRoot
    $env:PATH = "$DotNetRoot;$env:PATH"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Win11 Control Center — Full Feature Automation Suite   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Step 1: Clean lingering test hosts
Write-Host "`n[1/4] Cleaning test runner processes..." -ForegroundColor DarkGray
Get-Process -Name "*testhost*" -ErrorAction SilentlyContinue | Stop-Process -Force

# Step 2: Safety Boundary Tests (91 tests)
Write-Host "`n[2/4] Executing Safety Boundary Tests (91 tests)..." -ForegroundColor Yellow
dotnet test tests/Win11ControlCenter.SafetyTests/Win11ControlCenter.SafetyTests.csproj --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Safety Boundary Tests Failed!" -ForegroundColor Red
    exit 1
}

# Step 3: Full Feature Automation & Unit Tests
Write-Host "`n[3/4] Executing Full Feature Automation & Unit Tests..." -ForegroundColor Yellow
dotnet test tests/Win11ControlCenter.UnitTests/Win11ControlCenter.UnitTests.csproj --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Unit & Automation Feature Tests Failed!" -ForegroundColor Red
    exit 1
}

# Step 4: Release Compilation Verification
if (-not $SkipReleaseBuild) {
    Write-Host "`n[4/4] Verifying Release Build Compilation..." -ForegroundColor Yellow
    dotnet build src/Win11ControlCenter.App/Win11ControlCenter.App.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Release Build Compilation Failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  ✅ ALL AUTOMATION TESTS & BOUNDARIES PASSED (100% SUCCESS) " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
