#!/usr/bin/env bash
set -e

echo "============================================================"
echo "  CitizenRadar — Fedora Linux Setup Assistant"
echo "  (100% Pure C# / .NET 8 — Zero Python Required)"
echo "============================================================"
echo ""

# 1. Check .NET 8 SDK
echo "[1/3] Checking .NET 8 SDK..."
if command -v dotnet >/dev/null 2>&1; then
    if dotnet --list-sdks | grep -E "^8\." >/dev/null 2>&1; then
        echo "[✓] Found .NET 8 SDK."
    else
        echo "[!] .NET is installed, but .NET 8.0 SDK was not found."
        echo "    Install via: sudo dnf install dotnet-sdk-8.0"
    fi
else
    echo "[X] .NET SDK is not installed."
    echo "    To install on Fedora, run:"
    echo "        sudo dnf install -y dotnet-sdk-8.0"
fi
echo ""

# 2. Check Fedora graphics libraries for OpenCV
echo "[2/3] Checking Fedora graphics libraries for OpenCV GUI..."
echo "    If you encounter display errors when opening windows, ensure these are installed:"
echo "        sudo dnf install -y mesa-libGL glib2 libX11"
echo ""

# 3. Download YOLOv8 ONNX Model
echo "[3/3] Checking YOLOv8n ONNX model..."
mkdir -p CitizenRadar/models
MODEL_PATH="CitizenRadar/models/yolov8n.onnx"
if [ -f "$MODEL_PATH" ]; then
    echo "[✓] Model already exists at $MODEL_PATH."
else
    echo "Downloading pre-built yolov8n.onnx (approx 12 MB)..."
    curl -L -o "$MODEL_PATH" "https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx"
    if [ -f "$MODEL_PATH" ]; then
        echo "[✓] Successfully downloaded yolov8n.onnx!"
    else
        echo "[X] Download failed. Please download manually from:"
        echo "    https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx"
        echo "    and save to $MODEL_PATH"
    fi
fi

echo ""
echo "============================================================"
echo "  Fedora Setup Finished!"
echo "  Run 'dotnet build' to compile the project."
echo "============================================================"
