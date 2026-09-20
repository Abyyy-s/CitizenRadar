@echo off
setlocal enabledelayedexpansion

echo ============================================================
echo   CitizenRadar -- Windows Developer Setup Assistant
echo ============================================================
echo.

:: 1. Check .NET SDK
echo [1/4] Checking .NET 8 SDK...
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [X] .NET SDK is not found in PATH!
    echo     Please install .NET 8.0 SDK:
    echo     Option A (winget): winget install Microsoft.DotNet.SDK.8
    echo     Option B: Download from https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    goto check_python
)

dotnet --list-sdks | findstr /R "^8\." >nul
if %ERRORLEVEL% equ 0 (
    echo [V] Found .NET 8 SDK.
) else (
    echo [!] .NET is installed, but .NET 8 SDK was not detected in dotnet --list-sdks.
    echo     Ensure .NET 8.0 SDK is installed.
)

:check_python
echo.
:: 2. Check Python
echo [2/4] Checking Python (required only for one-time model export)...
where python >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [X] Python was not found in PATH!
    echo     Please install Python 3.9+ from https://www.python.org/downloads/
    echo     Make sure to check "Add python.exe to PATH" during installation.
    echo.
    goto restore_dotnet
)
echo [V] Found Python.

:: 3. Export YOLOv8 ONNX Model
echo.
echo [3/4] Checking YOLOv8n ONNX model...
if exist "CitizenRadar\models\yolov8n.onnx" (
    echo [V] Model already exists at CitizenRadar\models\yolov8n.onnx.
) else (
    echo Model file not found. Would you like to export it now?
    set /p EXPORT_NOW="Install ultralytics and export yolov8n.onnx? (Y/N): "
    if /i "!EXPORT_NOW!"=="Y" (
        echo Installing dependencies...
        python -m pip install --upgrade pip
        pip install -r scripts\requirements-model.txt
        python scripts\export_model.py
    ) else (
        echo [!] Skipped model export. Run 'python scripts\export_model.py' when ready.
    )
)

:restore_dotnet
echo.
:: 4. NuGet Restore
echo [4/4] Checking project file...
if exist "CitizenRadar\CitizenRadar.csproj" (
    echo Restoring NuGet packages...
    dotnet restore
    echo [V] NuGet packages restored.
) else (
    echo Project file not yet present in repository root.
)

echo.
echo ============================================================
echo   Setup Complete!
echo   Open CitizenRadar.sln in Visual Studio 2022 or run:
echo     dotnet build
echo ============================================================
pause
