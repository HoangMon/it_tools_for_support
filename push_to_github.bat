@echo off
title Day code len GitHub - it_tools_for_support
color 0A
echo ========================================================
echo   DANG TIEN HANH DAY CODE LEN GITHUB (IT TOOLS FOR SUPPORT)
echo   Repo: https://github.com/HoangMon/it_tools_for_support
echo ========================================================
echo.

git push -u origin main
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [THONG BAO] Neu kho tren GitHub da khoi tao san file (README/License),
    echo            dang thu day de de dong bo toan bo source code...
    echo.
    git push -u origin main --force
)

echo.
echo ========================================================
if %ERRORLEVEL% EQU 0 (
    echo   [THANH CONG] DA DAY TOAN BO CODE LEN GITHUB 100%%!
) else (
    echo   [LUU Y] Neu can dang nhap, vui long dang nhap tren trinh duyet roi thu lai.
)
echo ========================================================
pause
