@echo off
setlocal enabledelayedexpansion

echo ============================================================
echo   CitizenRadar -- Windows Developer Setup Assistant
echo   (100%% Pure C# / .NET 8 -- Zero Python Required)
echo ============================================================
echo.

:: 1. Check .NET SDK
echo [1/3] Checking .NET 8 SDK...
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [X] .NET SDK not found in PATH!
    echo     Please install .NET 8.0 SDK:
    echo     Option A (PowerShell): winget install Microsoft.DotNet.SDK.8
    echo     Option B: Download from https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

dotnet --list-sdks | findstr /R "^8\." >nul
if %ERRORLEVEL% equ 0 (
    echo [V] Found .NET 8 SDK.
) else (
    echo [!] Warning: .NET is installed, but version 8.0 was not detected in 'dotnet --list-sdks'.
    echo     Please make sure .NET 8.0 SDK is installed.
)

:: 2. Download YOLOv8n ONNX Model
echo.
echo [2/3] Checking YOLOv8n ONNX model...
if not exist "CitizenRadar\models" mkdir "CitizenRadar\models"

if exist "CitizenRadar\models\yolov8n.onnx" (
    echo [V] Model already exists at CitizenRadar\models\yolov8n.onnx.
) else (
    echo Downloading pre-built yolov8n.onnx (approx 12 MB)...
    curl -L -o "CitizenRadar\models\yolov8n.onnx" "https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx"
    if exist "CitizenRadar\models\yolov8n.onnx" (
        echo [V] Successfully downloaded yolov8n.onnx!
    ) else (
        echo [X] Download failed. Please download manually from:
        echo     https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx
        echo     and save to CitizenRadar\models\yolov8n.onnx
    )
)

:: 3. Restore NuGet Packages
echo.
echo [3/3] Checking project dependencies...
if exist "CitizenRadar\CitizenRadar.csproj" (
    echo Restoring OpenCvSharp4 and dependencies...
    dotnet restore
    echo [V] NuGet packages restored.
) else (
    echo Project file not found in current folder.
)

echo.
echo ============================================================
echo   Setup Complete!
echo   Open CitizenRadar.sln in Visual Studio 2022 or run:
echo     dotnet build
echo ============================================================
echo.
pause
