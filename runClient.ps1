#!/usr/bin/env pwsh
# Run Vintage Story with the LiveMap mod from the Release folder

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$ReleaseDir = Join-Path $ProjectRoot "Release"
$RunDataDir = Join-Path $ProjectRoot "runDataClient"

# Check for VINTAGE_STORY environment variable
if (-not $env:VINTAGE_STORY) {
    Write-Host "Error: VINTAGE_STORY environment variable is not set." -ForegroundColor Red
    Write-Host "Please set it to your Vintage Story installation directory."
    exit 1
}

$VintageStoryExe = Join-Path $env:VINTAGE_STORY "Vintagestory.exe"

if (-not (Test-Path $VintageStoryExe)) {
    Write-Host "Error: Vintagestory.exe not found at: $VintageStoryExe" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ReleaseDir)) {
    Write-Host "Error: Release directory not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "Starting Vintage Story with LiveMap mod..." -ForegroundColor Cyan
Write-Host "  Exe: $VintageStoryExe" -ForegroundColor Gray
Write-Host "  Mod Path: $ReleaseDir" -ForegroundColor Gray
Write-Host "  Data Path: $RunDataDir" -ForegroundColor Gray
Write-Host ""

& $VintageStoryExe --tracelog --addModPath $ReleaseDir --dataPath $RunDataDir
