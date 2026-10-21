@echo off
title Dong goi Single-File Release - Ernastie IT Tools
color 0B
echo ========================================================
echo   DONG GOI SINGLE-FILE RELEASE (WIN-X64 STANDALONE)
echo ========================================================
echo.

set DOTNET_CMD=dotnet
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
        set DOTNET_CMD="%USERPROFILE%\.dotnet\dotnet.exe"
    )
)

echo [1/3] Dang build Single-File Executable (Self-Contained)...
%DOTNET_CMD% publish src\Ernastie.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -o release\single_file

if %ERRORLEVEL% NEQ 0 (
    echo [LOI] Build that bai! Vui long kiem tra lai ma nguon.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Dang tao file nen ZIP de upload len GitHub Releases...
powershell -NoProfile -Command "Compress-Archive -Path 'release\single_file\Ernastie.exe' -DestinationPath 'release\Ernastie_vRelease_x64.zip' -Force"

echo.
echo ========================================================
echo   [THANH CONG] DA DONG GOI XONG!
echo   1. File chay truc tiep: release\single_file\Ernastie.exe
echo   2. File ZIP upload GitHub: release\Ernastie_vRelease_x64.zip
echo ========================================================
echo.
explorer release
pause
