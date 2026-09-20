# CitizenRadar — Developer Setup Guide 🛠️

This guide will help every team member set up their local development environment for **CitizenRadar**.

> **Quick Summary**:
> - **5 Windows Teammates**: Follow the **[Windows Setup Guide](#-windows-setup-guide)**.
> - **1 Fedora Linux Teammate**: Follow the **[Fedora Linux Setup Guide](#-fedora-linux-setup-guide)**.
> - **Zero Python in runtime**: Python is used *only once* to export the YOLOv8 neural network into `.onnx` format. The application itself runs completely on C# / .NET 8.

---

## 💻 Windows Setup Guide

### 1. Install .NET 8 SDK
CitizenRadar targets **.NET 8.0**. You must have the .NET 8 SDK installed.

* **Option A (winget - fastest)**:
  Open PowerShell or Windows Terminal and run:
  ```powershell
  winget install Microsoft.DotNet.SDK.8
  ```
* **Option B (Official Installer)**:
  Download the **.NET 8.0 SDK (x64)** installer from Microsoft:
  👉 [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

To verify the installation, open a new Command Prompt or PowerShell and type:
```cmd
dotnet --version
```
*(You should see `8.0.xxx`)*

---

### 2. Recommended IDE

* **Visual Studio 2022 (Recommended)**:
  Download [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/community/) (free).
  In the Visual Studio Installer, select the workload:
  * ✅ **.NET desktop development**
* **VS Code (Alternative)**:
  Install [VS Code](https://code.visualstudio.com/) and install the official **C# Dev Kit** extension from Microsoft.

---

### 3. Install Python (One-Time Model Export)
A Python interpreter is needed once to download the YOLOv8 weights and export them to ONNX.

1. Download Python 3.10, 3.11, or 3.12 from [python.org](https://www.python.org/downloads/).
2. ⚠️ **Crucial**: During installation, check the box: **"Add python.exe to PATH"**.

Verify in Command Prompt:
```cmd
python --version
```

---

### 4. Automated Windows Setup (One-Click)

You can run our automated helper script:
1. Open the project folder in Windows Explorer.
2. Double-click `setup-windows.bat` (or open Command Prompt in the folder and type `setup-windows.bat`).
3. Press `Y` when prompted to export the YOLOv8 model.
4. The script will install `ultralytics`, download `yolov8n.pt`, export `yolov8n.onnx` directly into `CitizenRadar/models/`, and restore all NuGet packages.

#### Manual Alternative for Windows:
If you prefer running commands manually:
```cmd
pip install -r scripts\requirements-model.txt
python scripts\export_model.py
```

---

### 5. Open & Build in Visual Studio
1. Double-click `CitizenRadar.sln` to open it in Visual Studio 2022.
2. Press `Ctrl + Shift + B` (or menu **Build $\rightarrow$ Build Solution**).
3. The project will compile cleanly.

---

## 🐧 Fedora Linux Setup Guide

### 1. Install .NET 8 SDK
Fedora provides first-class, official .NET packages in default repositories:

```bash
sudo dnf install -y dotnet-sdk-8.0
```

Verify:
```bash
dotnet --version
```

### 2. OpenCV Graphics Libraries
On Fedora, ensure standard X11 and OpenGL libraries are present so OpenCV can open desktop display windows:

```bash
sudo dnf install -y mesa-libGL glib2 libX11
```

### 3. One-Time YOLOv8 ONNX Export
Run the provided automated script:

```bash
chmod +x setup-fedora.sh
./setup-fedora.sh
```

Or manually:
```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r scripts/requirements-model.txt
python3 scripts/export_model.py
deactivate
```

This places `yolov8n.onnx` into `CitizenRadar/models/yolov8n.onnx`.

### 4. Build the Project
```bash
dotnet build
```

---

## 📋 Verifying Model Export

Ensure the model was placed in the expected path:
```text
CitizenRadar/
└── CitizenRadar/
    └── models/
        └── yolov8n.onnx    (~12 MB)
```

The model tensor specification is:
* **Input**: `[1, 3, 640, 640]` float32 (RGB normalized $0.0 - 1.0$)
* **Output**: `[1, 84, 8400]` float32
  * 4 bounding box coordinates (`center_x`, `center_y`, `width`, `height`)
  * 80 COCO class probability scores

---

## ❓ Troubleshooting & FAQs

| Issue | Cause | Fix |
|:---|:---|:---|
| `'dotnet' is not recognized` (Windows) | .NET SDK was installed while the terminal was open | Close and reopen Command Prompt or PowerShell so PATH refreshes. |
| `'python' is not recognized` (Windows) | Python wasn't added to PATH during installation | Rerun Python installer $\rightarrow$ choose Modify $\rightarrow$ check **Add Python to environment variables**. |
| `DllNotFoundException: Unable to load DLL 'OpenCvSharpExtern'` | Missing native runtime for your OS | Verify `CitizenRadar.csproj` includes `OpenCvSharp4.runtime.win` (for Windows) and `OpenCvSharp4.runtime.linux` (for Linux). |
| Black or empty video window on Linux | Missing OpenGL / X11 dependencies | Run `sudo dnf install mesa-libGL glib2 libX11`. |

