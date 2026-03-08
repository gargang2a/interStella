@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%sync-and-build-steam-smoke.ps1"

if not exist "%PS_SCRIPT%" (
  echo Script not found: "%PS_SCRIPT%"
  pause
  exit /b 1
)

powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
  echo.
  echo sync-and-build-steam-smoke failed with exit code %EXIT_CODE%.
  pause
  exit /b %EXIT_CODE%
)

echo.
echo sync-and-build-steam-smoke completed.
pause
