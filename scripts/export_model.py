#!/usr/bin/env python3
"""
CitizenRadar — One-Time YOLOv8 ONNX Model Exporter
Cross-platform helper for Windows and Linux teammates.
"""
import os
import sys
import shutil

def main():
    print("=" * 60)
    print("  CitizenRadar: YOLOv8n ONNX Model Exporter")
    print("=" * 60)

    try:
        from ultralytics import YOLO
    except ImportError:
        print("\n[ERROR] 'ultralytics' is not installed in your Python environment.")
        print("Please install dependencies by running:")
        print("    pip install -r scripts/requirements-model.txt")
        sys.exit(1)

    # Resolve target directory: CitizenRadar/models/
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    target_dir = os.path.join(project_root, "CitizenRadar", "models")
    os.makedirs(target_dir, exist_ok=True)
    target_onnx = os.path.join(target_dir, "yolov8n.onnx")

    print(f"\n[1/3] Downloading & loading pretrained yolov8n.pt...")
    model = YOLO("yolov8n.pt")

    print(f"\n[2/3] Exporting to ONNX format (imgsz=640, static batch=1)...")
    exported_path = model.export(format="onnx", imgsz=640, dynamic=False, opset=12)

    print(f"\n[3/3] Placing model file into project...")
    if os.path.exists(exported_path) and os.path.abspath(exported_path) != os.path.abspath(target_onnx):
        shutil.move(exported_path, target_onnx)

    # Clean up local .pt file if left in root
    pt_file = os.path.join(os.getcwd(), "yolov8n.pt")
    if os.path.exists(pt_file):
        try:
            os.remove(pt_file)
        except OSError:
            pass

    if os.path.exists(target_onnx):
        file_size_mb = os.path.getsize(target_onnx) / (1024 * 1024)
        print("\n" + "=" * 60)
        print(f"[SUCCESS] Model exported successfully!")
        print(f"Location: {target_onnx}")
        print(f"File Size: {file_size_mb:.2f} MB")
        print("=" * 60)
    else:
        print(f"\n[WARNING] Expected file at {target_onnx} not found.")

if __name__ == "__main__":
    main()
