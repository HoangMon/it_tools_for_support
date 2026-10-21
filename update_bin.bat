@echo off
echo ========================================================
echo CAP NHAT ERNASTIE TOOL MOI NHAT VAO THU MUC BIN/
echo ========================================================
xcopy /Y /S /E "src\bin\Publish\*" "bin\"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [THANH CONG] Da cap nhat toan bo file vao bin/!
) else (
    echo.
    echo [LUU Y] Neu file dang bi khoa, vui long tat Ernastie dang chay roi chay lai file nay.
)
pause
