@echo off
rem One-shot PATH fix. Run this ONCE in any cmd window (no admin needed).
reg add "HKCU\Environment" /v PATH /t REG_SZ /d "%USERPROFILE%\bin;%PATH%" /f
echo.
echo Done. Open a NEW terminal and verify with: where git-identity
echo (expected: C:\Users\carana\bin\git-identity.exe)
