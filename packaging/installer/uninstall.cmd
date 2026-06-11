@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem ============================================================
rem  uninstall.cmd - remove git-identity for the current user
rem
rem  Reverses install.cmd. Does NOT touch your identity profiles
rem  in %APPDATA%\git-identity\profiles.json; delete that file
rem  manually if you want a full reset.
rem ============================================================

set "GID_INSTALL_DIR=%USERPROFILE%\bin"

echo.
echo == git-identity uninstaller ==
echo.

if not exist "%GID_INSTALL_DIR%" (
    echo Nothing installed at %GID_INSTALL_DIR%.
    goto :envpath
)

rem Remove the git alias
git config --global --unset alias.identity 1>nul 2>nul

rem Remove the exe
del /F /Q "%GID_INSTALL_DIR%\git-identity.exe" 1>nul 2>nul
del /F /Q "%GID_INSTALL_DIR%\uninstall.cmd" 1>nul 2>nul

rem Try to remove the install dir if empty. We don't fail if it
rem still has other files (e.g. other tools in user bin).
rmdir "%GID_INSTALL_DIR%" 2>nul

echo Removed git-identity files.

:envpath
rem Strip the install dir from PATH
set "_GID_OLDPATH="
for /f "tokens=2,*" %%A in ('reg query "HKCU\Environment" /v PATH 2^>nul') do (
    if /i "%%A"=="REG_SZ" set "_GID_OLDPATH=%%B"
)

setlocal EnableDelayedExpansion
set "_GID_DEST=%GID_INSTALL_DIR%"
if "!_GID_DEST:~-1!"=="\" set "_GID_DEST=!_GID_DEST:~0,-1!"

if not "!_GID_OLDPATH!"=="" (
    set "_GID_NEWPATH=!_GID_OLDPATH!"
    call set "_GID_NEWPATH=%%_GID_NEWPATH:;%GID_INSTALL_DIR%=%%"
    call set "_GID_NEWPATH=%%_GID_NEWPATH:%GID_INSTALL_DIR%;=%%"
    call set "_GID_NEWPATH=%%_GID_NEWPATH:%GID_INSTALL_DIR%=%%"
    if not "!_GID_NEWPATH!"=="!_GID_OLDPATH!" (
        if "!_GID_NEWPATH!"=="" (
            reg delete "HKCU\Environment" /v PATH /f 1>nul 2>nul
        ) else (
            reg add "HKCU\Environment" /v PATH /t REG_SZ /d "!_GID_NEWPATH!" /f 1>nul
        )
        echo   Removed %GID_INSTALL_DIR% from PATH.
    )
)
endlocal

echo.
echo == Done ==
echo Your identity profiles in %%APPDATA%%\git-identity\profiles.json are untouched.
echo Delete that file manually if you want a full reset.
echo.
exit /b 0
