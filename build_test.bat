@echo off
title Build Ban Test (1 File .exe Duy Nhat) - E-TOOLBOX
color 0A
echo ========================================================
echo   BUILD BAN KIEM THU (1 FILE .EXE DUY NHAT)
echo ========================================================
echo.

set DOTNET_CMD=dotnet
if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    set DOTNET_CMD="%USERPROFILE%\.dotnet\dotnet.exe"
)

echo [1/3] Dong tien trinh E-TOOLBOX neu dang chay...
taskkill /f /im E-TOOLBOX.exe 2>nul
taskkill /f /im Ernastie.exe 2>nul

echo [2/3] Dang bien dich ban test (Single-File vao bin/)...
%DOTNET_CMD% publish src\Ernastie.csproj -c Debug -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Bien dich that bai!
    pause
    exit /b %ERRORLEVEL%
)

if exist "bin\E-TOOLBOX.exe" (
    copy /Y "bin\E-TOOLBOX.exe" "bin\Ernastie.exe" >nul
)

echo [3/3] Don dep cache trung gian, chi giu lai 1 file .exe duy nhat...
if exist "bin\E-TOOLBOX.exe.old" del /f /q "bin\E-TOOLBOX.exe.old" 2>nul
if exist "bin\Ernastie.exe.old" del /f /q "bin\Ernastie.exe.old" 2>nul
if exist "bin\E-TOOLBOX.pdb" del /f /q "bin\E-TOOLBOX.pdb" 2>nul
if exist "bin\Ernastie.pdb" del /f /q "bin\Ernastie.pdb" 2>nul
if exist "src\bin" rmdir /s /q "src\bin"
if exist "src\obj" rmdir /s /q "src\obj"

echo.
echo ========================================================
echo   [THANH CONG] DA TAO 1 FILE .EXE DUY NHAT DE KIEM THU:
echo   File: bin\E-TOOLBOX.exe
echo ========================================================
pause
