# CitizenRadar — Developer Setup Guide 🛠️

This guide will help every team member set up their local development environment for **CitizenRadar**.

> **100% Pure C# and .NET 8**:
> - **Zero Python Required**: No Python, no `pip`, and no virtual environments needed.
> - The AI model (`yolov8n.onnx`) is downloaded directly as a pre-built binary.
> - **5 Windows Teammates**: Follow the **[Windows Setup Guide](#-windows-setup-guide)**.
> - **1 Fedora Linux Teammate**: Follow the **[Fedora Linux Setup Guide](#-fedora-linux-setup-guide)**.

---

## 💻 Windows Setup Guide

### 1. Install .NET 8 SDK
CitizenRadar requires **.NET 8.0 SDK**.

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
  In the installer, check:
  * ✅ **.NET desktop development**
* **VS Code (Alternative)**:
  Install [VS Code](https://code.visualstudio.com/) and install the official **C# Dev Kit** extension from Microsoft.

---

### 3. Automated One-Click Setup (Download Model & Restore Packages)

You can run our automated Windows setup script:
1. Open the cloned folder in Windows Explorer.
2. Double-click **`setup-windows.bat`**.
3. It will:
   * Verify your `.NET 8 SDK` installation.
   * Automatically download `yolov8n.onnx` (~12 MB) via `curl` directly into `CitizenRadar\models\`.
   * Run `dotnet restore` to download OpenCvSharp4 and dependencies.

#### Manual Alternative for Windows:
If you prefer downloading manually, run this in PowerShell or Command Prompt:
```cmd
curl -L -o CitizenRadar\models\yolov8n.onnx https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx
dotnet restore
```

---

### 4. Build in Visual Studio
1. Open `CitizenRadar.sln` in Visual Studio 2022.
2. Press `Ctrl + Shift + B` (or menu **Build $\rightarrow$ Build Solution**).
3. The project will compile cleanly with zero extra dependencies.

---

## 🐧 Fedora Linux Setup Guide

### 1. Install .NET 8 SDK & Graphics Libraries
Fedora provides first-class .NET packages and native OpenCV dependencies:

```bash
sudo dnf install -y dotnet-sdk-8.0 mesa-libGL glib2 libX11
```

Verify:
```bash
dotnet --version
```

### 2. One-Click Setup (Download Model & Restore Packages)
Run the provided automated script:

```bash
chmod +x setup-fedora.sh
./setup-fedora.sh
```

Or manually with curl:
```bash
mkdir -p CitizenRadar/models
curl -L -o CitizenRadar/models/yolov8n.onnx https://github.com/ultralytics/assets/releases/download/v8.4.0/yolov8n.onnx
```

### 3. Build the Project
```bash
dotnet build
```

---

## 📋 Verifying Model File

Ensure `yolov8n.onnx` is located at:
```text
CitizenRadar/
└── CitizenRadar/
    └── models/
        └── yolov8n.onnx    (~12.2 MB)
```

---

## ❓ Troubleshooting & FAQs

| Issue | Cause | Fix |
|:---|:---|:---|
| `'dotnet' is not recognized` (Windows) | .NET SDK was installed while terminal was open | Close and reopen Command Prompt or PowerShell so PATH updates. |
| `DllNotFoundException: Unable to load DLL 'OpenCvSharpExtern'` | Missing native runtime for your OS | Check that `CitizenRadar.csproj` includes `OpenCvSharp4.runtime.win` (for Windows) and `OpenCvSharp4.runtime.linux` (for Linux). Both are configured by default. |
| Black or empty video window on Linux | Missing OpenGL / X11 dependencies | Run `sudo dnf install -y mesa-libGL glib2 libX11`. |
