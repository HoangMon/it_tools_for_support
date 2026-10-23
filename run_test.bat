@echo off
title Chay Test E-TOOLBOX
set DOTNET_CMD=dotnet
if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    set DOTNET_CMD="%USERPROFILE%\.dotnet\dotnet.exe"
)

taskkill /f /im E-TOOLBOX.exe 2>nul
taskkill /f /im Ernastie.exe 2>nul
echo Dang bien dich va khoi chay ban test...
%DOTNET_CMD% publish src\Ernastie.csproj -c Debug -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin >nul 2>&1
if exist "bin\E-TOOLBOX.exe" (
    copy /Y "bin\E-TOOLBOX.exe" "bin\Ernastie.exe" >nul
)
if exist "src\bin" rmdir /s /q "src\bin"
if exist "src\obj" rmdir /s /q "src\obj"

if exist "bin\E-TOOLBOX.exe" (
    start "" "bin\E-TOOLBOX.exe"
) else if exist "bin\Ernastie.exe" (
    start "" "bin\Ernastie.exe"
) else (
    echo [LOI] Khong tim thay bin\E-TOOLBOX.exe de khoi chay!
    pause
)
