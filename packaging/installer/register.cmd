@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem ============================================================
rem  register.cmd - register Explorer context menu entries
rem
rem  After this, right-clicking the background of any folder (or any
rem  folder itself) will show "Open git-identity here" which opens a
rem  cmd.exe in that folder with the tool ready to use.
rem
rem  No admin rights required. Writes only to HKCU.
rem ============================================================

set "GID_INSTALL_DIR=%LOCALAPPDATA%\Programs\git-identity"
set "GID_EXE=%GID_INSTALL_DIR%\git-identity.exe"

if not exist "%GID_EXE%" (
    echo [git-identity] ERROR: %GID_EXE% not found. Run install.cmd first. 1>&2
    exit /b 1
)

echo Registering Explorer context menu entries ...

rem 1. Right-click on a folder
reg add "HKCU\Software\Classes\Directory\shell\git-identity" /ve /d "git identity here" /f 1>nul
reg add "HKCU\Software\Classes\Directory\shell\git-identity" /v "Icon" /t REG_SZ /d "\"%GID_EXE%\",0" /f 1>nul
reg add "HKCU\Software\Classes\Directory\shell\git-identity\command" /ve /d "cmd.exe /k cd /d \"%%1\"" /f 1>nul

rem 2. Right-click on the background of a folder
reg add "HKCU\Software\Classes\Directory\Background\shell\git-identity" /ve /d "git identity here" /f 1>nul
reg add "HKCU\Software\Classes\Directory\Background\shell\git-identity" /v "Icon" /t REG_SZ /d "\"%GID_EXE%\",0" /f 1>nul
reg add "HKCU\Software\Classes\Directory\Background\shell\git-identity\command" /ve /d "cmd.exe /k cd /d \"%%V\"" /f 1>nul

echo.
echo == Done ==
echo Right-click any folder (or its background) to see "git identity here".
echo It opens a cmd.exe in that folder so you can run `git identity` directly.
echo.
echo To remove these entries, run unregister.cmd.
echo.
exit /b 0
