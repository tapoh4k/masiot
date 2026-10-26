#Requires -Version 5.1
<#
.SYNOPSIS
  Stop MAS-IOT Docker infra (Postgres + RabbitMQ).
  Close backend/frontend windows manually (Ctrl+C).
#>
$ErrorActionPreference = "Stop"
$Backend = Join-Path $PSScriptRoot "masiot-factoryagent-backend-new-gen-dev"
$ComposeFile = Join-Path $Backend "docker-compose.yml"

Write-Host "==> Stopping Postgres + RabbitMQ" -ForegroundColor Cyan
docker compose -f $ComposeFile stop postgres rabbitmq
Write-Host "Done." -ForegroundColor Green
