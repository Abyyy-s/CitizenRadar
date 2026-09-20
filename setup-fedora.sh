#!/usr/bin/env bash
set -e

echo "============================================================"
echo "  CitizenRadar — Fedora Linux Setup Assistant"
echo "============================================================"
echo ""

# 1. Check .NET 8 SDK
echo "[1/4] Checking .NET 8 SDK..."
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
    echo "        sudo dnf install dotnet-sdk-8.0"
fi
echo ""

# 2. Check Python 3
echo "[2/4] Checking Python 3..."
if command -v python3 >/dev/null 2>&1; then
    echo "[✓] Found Python 3: $(python3 --version)"
else
    echo "[X] Python 3 not found. Install via: sudo dnf install python3 python3-pip"
fi
echo ""

# 3. Check Fedora graphics libraries for OpenCV
echo "[3/4] Recommended Fedora packages for OpenCV window display:"
echo "    If you encounter libGL/display errors, ensure these packages are installed:"
echo "        sudo dnf install mesa-libGL glib2 libX11"
echo ""

# 4. Check & Export YOLOv8 ONNX Model
echo "[4/4] Checking YOLOv8n ONNX model..."
MODEL_PATH="CitizenRadar/models/yolov8n.onnx"
if [ -f "$MODEL_PATH" ]; then
    echo "[✓] Model already exists at $MODEL_PATH."
else
    echo "Model file not found at $MODEL_PATH."
    read -p "Would you like to export it now using a temporary python venv? (y/N): " choice
    case "$choice" in 
        [yY][eE][sS]|[yY])
            echo "Creating virtual environment in .venv-export..."
            python3 -m venv .venv-export
            source .venv-export/bin/activate
            pip install --upgrade pip
            pip install -r scripts/requirements-model.txt
            python scripts/export_model.py
            deactivate
            rm -rf .venv-export
            echo "[✓] Done and temporary venv cleaned up."
            ;;
        *)
            echo "[!] Skipped model export. Run 'python3 scripts/export_model.py' when ready."
            ;;
    esac
fi

echo ""
echo "============================================================"
echo "  Fedora Setup Check Finished!"
echo "============================================================"
