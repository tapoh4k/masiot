#Requires -Version 5.1
<#
.SYNOPSIS
  Local MAS-IOT: Docker (Postgres+RabbitMQ) + schema sync + backend + frontend.

.PARAMETER ResetDb
  Recreate Postgres volume (clean DB from schema.sql).

.EXAMPLE
  .\start-dev.ps1
  .\start-dev.ps1 -ResetDb
  .\start-dev.cmd
#>
param(
    [switch]$ResetDb
)

$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$Backend = Join-Path $Root "masiot-factoryagent-backend-new-gen-dev"
$Front = Join-Path $Root "masiot-factoryagent-front-new-gen-dev"
$ComposeFile = Join-Path $Backend "docker-compose.yml"
$SchemaFile = Join-Path $Backend "schema.sql"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Wait-ContainerHealthy([string]$Name, [int]$TimeoutSec = 90) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        $status = docker inspect -f "{{.State.Health.Status}}" $Name 2>$null
        if ($status -eq "healthy") { return }
        if ($LASTEXITCODE -ne 0) {
            $running = docker inspect -f "{{.State.Running}}" $Name 2>$null
            if ($running -eq "true") { return }
        }
        Start-Sleep -Seconds 2
    }
    throw "Container $Name not healthy after ${TimeoutSec}s"
}

function Sync-Schema {
    Write-Step "Applying schema.sql (idempotent)"
    $pg = "agenti_postgres"
    docker cp $SchemaFile "${pg}:/tmp/schema.sql"
    docker exec $pg psql -U postgres -d agenti_db -v ON_ERROR_STOP=1 -f /tmp/schema.sql | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to apply schema.sql" }
    Write-Host "Schema OK" -ForegroundColor Green
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker not found. Start Docker Desktop and retry."
}

# Quick daemon check
docker info 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Docker daemon is not running. Start Docker Desktop and wait until Ready."
}

Push-Location $Backend
try {
    if ($ResetDb) {
        Write-Step "ResetDb: recreating Postgres volume"
        docker compose -f $ComposeFile stop postgres 2>$null | Out-Null
        docker compose -f $ComposeFile rm -f postgres 2>$null | Out-Null
        docker volume rm masiot-factoryagent-backend-new-gen-dev_postgres_data 2>$null | Out-Null
    }

    Write-Step "Starting Postgres + RabbitMQ"
    docker compose -f $ComposeFile up -d postgres rabbitmq
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

    Wait-ContainerHealthy "agenti_postgres"
    Wait-ContainerHealthy "agenti_rabbitmq"
    Write-Host "Infrastructure healthy" -ForegroundColor Green

    Sync-Schema
}
finally {
    Pop-Location
}

Write-Step "Starting backend and frontend in separate windows"
$backendCmd = @"
Set-Location '$Backend'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
Write-Host 'Backend: http://localhost:3001  (Swagger: /swagger)' -ForegroundColor Green
dotnet run
"@

$frontCmd = @"
Set-Location '$Front'
if (-not (Test-Path 'node_modules')) { npm install }
Write-Host 'Frontend: http://localhost:5173' -ForegroundColor Green
npm run dev
"@

Start-Process powershell -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-Command", $backendCmd)
Start-Process powershell -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-Command", $frontCmd)

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  UI:     http://localhost:5173"
Write-Host "  API:    http://localhost:3001/api/health"
Write-Host "  Login:  admin / admin123"
Write-Host ""
Write-Host "Stop infra:  .\stop-dev.cmd"
Write-Host "Clean DB:    .\start-dev.cmd -ResetDb"
