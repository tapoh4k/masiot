@echo off
REM Bypass ExecutionPolicy for local unsigned scripts. Requires Docker Desktop running.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-dev.ps1" %*
