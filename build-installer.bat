@echo off
setlocal enabledelayedexpansion

echo =======================================================
echo   Bible Study App - Compile & Package Installer Wizard
echo   Waitaminute Digital (Native ARM64 / x64 Windows)
echo =======================================================
echo.

:: Detect architecture or default to win-arm64
set TARGET_ARCH=%1
if "%TARGET_ARCH%"=="" (
    if "%PROCESSOR_ARCHITECTURE%"=="ARM64" (
        set TARGET_ARCH=win-arm64
    ) else (
        set TARGET_ARCH=win-arm64
    )
)

echo [1/3] Publishing .NET MAUI Release Bundle for %TARGET_ARCH%...
dotnet publish LumenScriptura.csproj -c Release -f net10.0-windows10.0.19041.0 -r %TARGET_ARCH% --self-contained true -p:UseMonoRuntime=false -p:WindowsPackageType=MSIX -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -o .\artifacts\%TARGET_ARCH%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Dotnet publish failed! Check build output above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Compiling Inno Setup Installer...
set ISCC_PATH=""
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set ISCC_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set ISCC_PATH="C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set ISCC_PATH="%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"

where iscc >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    iscc installer.iss
) else if not "%ISCC_PATH%"=="" (
    %ISCC_PATH% installer.iss
) else (
    echo.
    echo [WARNING] Inno Setup compiler (iscc.exe) was not found in standard paths.
    echo You can install it via: choco install innosetup -y  OR  winget install JRSoftware.InnoSetup
    echo The published binaries are available at: .\artifacts\%TARGET_ARCH%
    pause
    exit /b 0
)

echo.
echo [3/3] Build Complete!
echo Installer created at: .\artifacts\installer\BibleStudyApp-Setup.exe
echo.
pause
