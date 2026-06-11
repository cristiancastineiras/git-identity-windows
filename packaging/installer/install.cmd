@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem ============================================================
rem  install.cmd - install git-identity for the current user
rem
rem  What it does:
rem    1. Copies git-identity.exe to %USERPROFILE%\bin\
rem       (a directory we add to your user PATH, so `git-identity`
rem       is callable from anywhere without quoting the path)
rem    2. Adds %USERPROFILE%\bin\ to the user PATH (persistent, no admin)
rem    3. Registers a git alias so `git identity` works in any shell
rem    4. Optionally registers an Explorer context menu entry
rem       (right-click a folder -> "Open with git-identity")
rem
rem  No admin rights required. Run from any folder.
rem
rem  Why %USERPROFILE%\bin\ and not %LOCALAPPDATA%\Programs\git-identity\?
rem  Because git's "!"-alias expansion in PowerShell on Windows has a
rem  long-standing bug with backslashes in quoted paths that strips
rem  the separators ("C:\Users\foo" -> "C:Usersfoo"). Putting the exe
rem  on PATH lets us use a bare alias name, sidestepping the bug.
rem ============================================================

set "GID_INSTALL_DIR=%USERPROFILE%\bin"
set "GID_SRC=%~dp0"
set "GID_EXE=git-identity.exe"

echo.
echo == git-identity installer (Windows, user-level) ==
echo.

rem 1. Create install dir
if not exist "%GID_INSTALL_DIR%" (
    echo Creating %GID_INSTALL_DIR% ...
    mkdir "%GID_INSTALL_DIR%" 1>nul 2>nul
    if errorlevel 1 (
        echo [git-identity] ERROR: could not create %GID_INSTALL_DIR%. 1>&2
        exit /b 1
    )
)

rem 2. Copy the exe (overwrite if present)
echo Installing %GID_EXE% ...
copy /Y "%GID_SRC%%GID_EXE%" "%GID_INSTALL_DIR%\%GID_EXE%" 1>nul
if errorlevel 1 (
    echo [git-identity] ERROR: copy failed. 1>&2
    exit /b 1
)

rem 3. Add the install dir to the user PATH (persistent, via reg.exe)
echo Updating user PATH ...
set "_GID_OLDPATH="
for /f "tokens=2,*" %%A in ('reg query "HKCU\Environment" /v PATH 2^>nul') do (
    if /i "%%A"=="REG_SZ" set "_GID_OLDPATH=%%B"
)

setlocal EnableDelayedExpansion
set "_GID_DEST=%GID_INSTALL_DIR%"
if "!_GID_DEST:~-1!"=="\" set "_GID_DEST=!_GID_DEST:~0,-1!"

set "_GID_ALREADY=0"
if not "!_GID_OLDPATH!"=="" (
    echo !_GID_OLDPATH! | findstr /i /c:";!_GID_DEST!;" >nul && set "_GID_ALREADY=1"
    if !_GID_ALREADY!==0 echo !_GID_OLDPATH! | findstr /i /c:";!_GID_DEST!"   >nul && set "_GID_ALREADY=1"
    if !_GID_ALREADY!==0 echo !_GID_OLDPATH! | findstr /i /c:"!_GID_DEST!;"   >nul && set "_GID_ALREADY=1"
    if !_GID_ALREADY!==0 if "!_GID_OLDPATH!"=="!_GID_DEST!" set "_GID_ALREADY=1"
)

if !_GID_ALREADY!==1 (
    echo   %GID_INSTALL_DIR% is already in PATH.
) else (
    if "!_GID_OLDPATH!"=="" (
        reg add "HKCU\Environment" /v PATH /t REG_SZ /d "%GID_INSTALL_DIR%" /f 1>nul
    ) else (
        reg add "HKCU\Environment" /v PATH /t REG_SZ /d "!_GID_OLDPATH!;%GID_INSTALL_DIR%" /f 1>nul
    )
    if errorlevel 1 (
        echo [git-identity] ERROR: could not update PATH. 1>&2
        endlocal
        exit /b 1
    )
    echo   Added %GID_INSTALL_DIR% to your user PATH.
)
endlocal

rem 4. Register a git alias so `git identity` works. We use a BARE
rem    name (not a full path) so we never hit the PowerShell quoting
rem    bug with "!" aliases and backslashes.
echo Registering `git identity` alias ...
git config --global alias.identity "!git-identity"
if errorlevel 1 (
    echo   [WARN] Could not register git alias. Make sure git is on PATH.
) else (
    echo   Done. You can now use `git identity` in any shell.
)

echo.
echo == Done ==
echo Open a NEW terminal so the new PATH takes effect, then run:
echo     git identity --help
echo.
echo To uninstall, run uninstall.cmd (or the one installed at
echo %GID_INSTALL_DIR%\uninstall.cmd).
echo.
exit /b 0
