$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

$ProfilePath = Join-Path $ProjectRoot "edgex-config\smart-meter-profile.yaml"
$DevicePath = Join-Path $ProjectRoot "edgex-config\smart-meter-device.json"

$CoreMetadataUrl = "http://localhost:59881"
$CoreCommandUrl = "http://localhost:59882"
$DeviceRestUrl = "http://localhost:59986"

$ProfileName = "smart-meter"
$DeviceName = "smart-meter-1"

function Test-Endpoint {
    param (
        [string]$Name,
        [string]$Url
    )

    Write-Host "Checking $Name..." -ForegroundColor Cyan

    try {
        Invoke-RestMethod -Method Get -Uri "$Url/api/v3/ping" | Out-Null
        Write-Host "$Name is available." -ForegroundColor Green
    }
    catch {
        Write-Host "$Name is not available at $Url" -ForegroundColor Red
        throw
    }
}

function Test-ResourceExists {
    param (
        [string]$Url
    )

    try {
        Invoke-RestMethod -Method Get -Uri $Url | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

Write-Host "=== EdgeX Smart Meter Registration ===" -ForegroundColor Yellow

Test-Endpoint -Name "device-rest" -Url $DeviceRestUrl
Test-Endpoint -Name "core-metadata" -Url $CoreMetadataUrl
Test-Endpoint -Name "core-command" -Url $CoreCommandUrl

if (!(Test-Path $ProfilePath)) {
    throw "Device profile file not found: $ProfilePath"
}

if (!(Test-Path $DevicePath)) {
    throw "Device file not found: $DevicePath"
}

$ProfileExists = Test-ResourceExists -Url "$CoreMetadataUrl/api/v3/deviceprofile/name/$ProfileName"

if ($ProfileExists) {
    Write-Host "Device profile '$ProfileName' already exists. Skipping upload." -ForegroundColor Yellow
}
else {
    Write-Host "Uploading device profile '$ProfileName'..." -ForegroundColor Cyan

    curl.exe -X POST `
        -F "file=@$ProfilePath" `
        "$CoreMetadataUrl/api/v3/deviceprofile/uploadfile"

    Write-Host "Device profile uploaded." -ForegroundColor Green
}

$DeviceExists = Test-ResourceExists -Url "$CoreMetadataUrl/api/v3/device/name/$DeviceName"

if ($DeviceExists) {
    Write-Host "Device '$DeviceName' already exists. Skipping creation." -ForegroundColor Yellow
}
else {
    Write-Host "Creating device '$DeviceName'..." -ForegroundColor Cyan

    Invoke-RestMethod `
        -Method Post `
        -Uri "$CoreMetadataUrl/api/v3/device" `
        -ContentType "application/json" `
        -InFile $DevicePath

    Write-Host "Device created." -ForegroundColor Green
}

Write-Host "Verifying Core Command registration..." -ForegroundColor Cyan

Invoke-RestMethod `
    -Method Get `
    -Uri "$CoreCommandUrl/api/v3/device/name/$DeviceName" | Out-Null

Write-Host "Core Command can see '$DeviceName'." -ForegroundColor Green
Write-Host "=== Registration completed successfully. ===" -ForegroundColor Green