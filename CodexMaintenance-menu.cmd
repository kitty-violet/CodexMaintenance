@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
set "TOOL=%ROOT%CodexMaintenance.exe"
set "CONFIG=%ROOT%CodexMaintenance.config"
set "BACKUPS=%ROOT%backups"

if not exist "%TOOL%" (
  echo CodexMaintenance.exe was not found next to this menu.
  echo Please build or download a release package first.
  pause
  exit /b 1
)

:menu
cls
echo.
echo  Codex Maintenance Menu
echo  ================================
echo  Tool   : %TOOL%
echo  Config : %CONFIG%
echo.
if exist "%CONFIG%" (
  type "%CONFIG%"
) else (
  echo  No local config yet. Choose [3] to configure first.
)
echo.
echo  [1] Dry run preview
echo  [2] Run cleanup with backup
echo  [3] Configure folders
echo  [4] Open backup folder
echo  [5] Show version
echo  [0] Exit
echo.
choice /c 123450 /n /m "Choose: "
if errorlevel 6 exit /b 0
if errorlevel 5 goto version
if errorlevel 4 goto open_backups
if errorlevel 3 goto configure
if errorlevel 2 goto run_cleanup
if errorlevel 1 goto dry_run
goto menu

:dry_run
cls
"%TOOL%" --dry-run --no-pause
echo.
pause
goto menu

:run_cleanup
cls
echo This will clean low-level Codex logs after creating a safety backup.
choice /c YN /n /m "Continue? [Y/N] "
if errorlevel 2 goto menu
"%TOOL%" --no-pause
echo.
pause
goto menu

:configure
cls
"%TOOL%" --configure --no-pause
echo.
pause
goto menu

:open_backups
if exist "%CONFIG%" (
  for /f "usebackq tokens=1,* delims==" %%A in ("%CONFIG%") do (
    if /i "%%A"=="BackupRoot" set "BACKUPS=%%B"
  )
)
if not exist "%BACKUPS%" mkdir "%BACKUPS%" >nul 2>nul
start "" "%BACKUPS%"
goto menu

:version
cls
"%TOOL%" --version
echo.
pause
goto menu
