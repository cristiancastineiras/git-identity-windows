@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem ============================================================
rem  unregister.cmd - remove Explorer context menu entries
rem ============================================================

echo Removing Explorer context menu entries ...

reg delete "HKCU\Software\Classes\Directory\shell\git-identity" /f 1>nul 2>nul
reg delete "HKCU\Software\Classes\Directory\Background\shell\git-identity" /f 1>nul 2>nul

echo Done.
exit /b 0
