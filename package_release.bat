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

echo [2/3] Dang chuan bi tai lieu Patch Notes...
copy /Y "PATCH_NOTES.md" "release\single_file\PATCH_NOTES.md" >nul
copy /Y "PATCH_NOTES.md" "release\PATCH_NOTES_v1.0.1.md" >nul

echo [3/3] Dang tao file nen ZIP de upload len GitHub Releases...
powershell -NoProfile -Command "Compress-Archive -Path 'release\single_file\Ernastie.exe', 'release\single_file\PATCH_NOTES.md' -DestinationPath 'release\Ernastie_v1.0.1_x64.zip' -Force"

echo.
echo ========================================================
echo   [THANH CONG] DA DONG GOI BAN RELEASE 1.0.1 XONG!
echo   1. File chay truc tiep: release\single_file\Ernastie.exe
echo   2. File ZIP upload GitHub: release\Ernastie_v1.0.1_x64.zip
echo   3. Patch Notes: release\PATCH_NOTES_v1.0.1.md
echo ========================================================
pause
