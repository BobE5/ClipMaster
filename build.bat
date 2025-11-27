@echo off
echo ========================================
echo ClipMaster Build Script
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed!
    echo Please download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found!
echo.

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Building Release version...
dotnet build --configuration Release
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.
echo Output location:
echo   bin\Release\net8.0-windows\
echo.
echo To run the application:
echo   bin\Release\net8.0-windows\ClipMaster.exe
echo.
pause
