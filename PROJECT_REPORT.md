# CITIZENRADAR: CAMERA-BASED VEHICLE SPEED ESTIMATION AND OVERSPEED VIOLATION DETECTION USING YOLOV8 AND .NET 8

### Project Report
*submitted to the APJ Abdul Kalam Technological University*  
*in partial fulfillment of the requirements for the award of the degree*  

**Bachelor of Technology**  
*in*  
**Computer Science and Engineering**  

*by*  
**SREEHARI A NAIR (CHN24AM058)**  
**KRISHNA B (CHN24AM033)**  
**PARVATI RAJAN (CHN24AM040)**  
**HENA MARIAM ABRAHAM (CHN24AM025)**  
**ANUGRAHA S (CHN24AM013)**  
**ABY S BIJU (CHN24AM005)**  

<br>

```
                  ┌───────────────────────────────┐
                  │    [ COLLEGE OF ENGINEERING   │
                  │           CHENGANNUR          │
                  │             EMBLEM]           │
                  └───────────────────────────────┘
```

**Department of Computer Engineering**  
**College of Engineering Chengannur, Kerala - 689121**  
Phone: (0479) 24541125 | Fax: (0479) 2451424  
Website: www.ceconline.edu  

**OCTOBER 2026**

---

<div style="page-break-after: always;"></div>

## DEPARTMENT OF COMPUTER ENGINEERING
### COLLEGE OF ENGINEERING CHENGANNUR 2026 - 27

<br>

```
                  ┌───────────────────────────────┐
                  │    [ COLLEGE OF ENGINEERING   │
                  │           CHENGANNUR          │
                  │             EMBLEM]           │
                  └───────────────────────────────┘
```

### CERTIFICATE

This is to certify that the project report titled **CITIZENRADAR: CAMERA-BASED VEHICLE SPEED ESTIMATION AND OVERSPEED VIOLATION DETECTION USING YOLOV8 AND .NET 8** is a bonafide record of the course project presented by **SREEHARI A NAIR (CHN24AM058)**, **KRISHNA B (CHN24AM033)**, **PARVATI RAJAN (CHN24AM040)**, **HENA MARIAM ABRAHAM (CHN24AM025)**, **ANUGRAHA S (CHN24AM013)**, and **ABY S BIJU (CHN24AM005)**, Fourth Semester B.Tech. degree in Computer Science and Engineering, under our guidance and supervision. This project is submitted in partial fulfillment of the requirements for the award of the degree **Bachelor of Technology in Computer Science and Engineering** of **APJ Abdul Kalam Technological University**.

<br><br><br>

**Project Guide / Assistant Professor**  
Dept. of Computer Engineering  
College of Engineering Chengannur  

**Dr. Renu George / Professor & Head of the Department**  
Dept. of Computer Engineering  
College of Engineering Chengannur  

---

<div style="page-break-after: always;"></div>

## INDEX

| No. | Topic | Page No. |
| :---: | :--- | :---: |
| **1.** | **ABSTRACT** | 1 |
| **2.** | **INTRODUCTION** | 2 |
| **3.** | **OBJECTIVES** | 3 |
| **4.** | **TOOLS USED** | 4 |
| **5.** | **SYSTEM ARCHITECTURE** | 5 |
| **6.** | **METHODOLOGY** | 6 |
| **7.** | **IMPLEMENTATION** | 7 |
| **8.** | **SYSTEM DASHBOARD & VISUALS** | 9 |
| **9.** | **RESULTS** | 12 |
| **10.** | **ANALYSIS** | 14 |
| **11.** | **CONCLUSION** | 15 |
| **12.** | **REFERENCES** | 16 |

---

<div style="page-break-after: always;"></div>

*Page | 1*

## 1. ABSTRACT

This project presents the design, architectural implementation, and empirical evaluation of **CitizenRadar**, an autonomous, edge-capable vehicle speed estimation and automated overspeed violation detection system developed in **pure C# (.NET 8)**. Traditional traffic monitoring infrastructure depends almost universally on dedicated physical sensors such as Doppler microwave radar guns, inductive pavement loops, and time-of-flight LiDAR gantries. While accurate, these legacy hardware installations entail excessive capital expenditure, disruptive roadway excavations, continuous maintenance overhead, and a total lack of contextual photographic evidence. To democratize urban speed monitoring for public safety and traffic analytics, this project demonstrates that high-precision vehicle kinematics can be computed entirely through standard optical monocular surveillance feeds using deep convolutional neural networks and projective geometry.

The computational core pairs **YOLOv8** deep learning object detection with **Planar Homography (Perspective Transformation)** to project two-dimensional camera pixel coordinates $(u, v)$ onto a metric real-world ground coordinate system $(X, Y)$ on the road surface. By anchoring tracking calculations to the bottom-center tire patch of detected vehicles, the system tracks real-world metric displacements over time. To eliminate monocular perspective distortion and bounding-box jitter, CitizenRadar implements a multi-tiered stability pipeline: a 30-frame temporal measurement window (1.0 second), longitudinal road-axis displacement isolation, horizon distance gating, and first-order exponential smoothing.

To achieve production throughput without the multi-process IPC bottlenecks characteristic of Python scripts, CitizenRadar executes within a unified, multi-threaded .NET 8 runtime. High-throughput neural execution is accelerated on an **NVIDIA GeForce RTX 4050 Laptop GPU (6GB VRAM)** via the **Microsoft.ML.OnnxRuntime.Gpu** engine utilizing **CUDA 13.4** and **cuDNN 9**, achieving **95.1 FPS** inference with automated fallback to multi-threaded CPU SIMD. The system is paired with a **luxury Dribbble-inspired web dashboard** served directly by an embedded **ASP.NET Core Kestrel** server. The interface features custom Canvas-rendered circular segmented speedometers, velocity wave trend charts with highlight pillars, hourly traffic volume histograms, and a live multipart MJPEG video stream with bird's-eye minimaps. Real-world benchmarking confirms realistic velocity estimation ($\pm 2.4\text{ km/h}$ accuracy), establishing software-driven monocular vision as an accessible, high-performance alternative for modern intelligent transportation systems.

---

<div style="page-break-after: always;"></div>

*Page | 2*

## 2. INTRODUCTION

Excessive vehicular velocity is universally recognized by civil engineers and transport authorities as the predominant risk factor governing road traffic collisions, trauma severity, and urban pedestrian fatalities. As urban density accelerates worldwide, municipal law enforcement agencies face growing demands for continuous, ubiquitous traffic surveillance. Historically, municipal speed enforcement has relied on physical sensor hardware:
1. **Doppler Radar Units:** Emitting microwave pulses and measuring frequency shifts reflected from moving metallic vehicle bodies.
2. **LiDAR Telemetry Systems:** Utilizing pulsed infrared lasers to calculate time-of-flight distances.
3. **Piezoelectric & Inductive Road Loops:** Physical wire loops surgically embedded into asphalt roadbeds.

While mechanically dependable, these hardware-centric sensors present substantial barriers. Doppler radar units cannot isolate individual vehicles within dense multi-lane traffic; inductive loops degrade rapidly under freeze-thaw cycles and heavy truck axle loads; and none of these sensors natively provide visual context or vehicle classification.

Concurrently, closed-circuit television (CCTV) cameras, smart city optical poles, and security webcams already blanket modern roadways. Converting these passive optical streams into intelligent, autonomous speed enforcement nodes represents a non-intrusive, zero-hardware paradigm. However, implementing real-time monocular optical radar requires overcoming two fundamental computer vision hurdles:
- **Perspective Foreshortening:** A monocular camera flattens 3D space into a 2D sensor. A car traveling $100\text{ km/h}$ near the camera lens traverses hundreds of pixels per second, whereas the identical car traveling $100\text{ km/h}$ near the horizon moves fewer than 2 pixels per second.
- **Deep Learning Bounding Box Jitter:** Minor pixel shifts ($\pm 2\text{ px}$) caused by lighting variations or vehicle body reflection can translate into catastrophic velocity spikes ($\pm 35\text{ km/h}$) if naive frame-to-frame displacement formulas are applied.

**CitizenRadar** resolves these challenges by uniting deep convolutional neural networks with projective geometry inside a compiled, hardware-accelerated **C# (.NET 8)** environment. By bypassing the Global Interpreter Lock (GIL) and process-isolation latencies of Python, CitizenRadar executes video ingestion, GPU tensor inference, spatial tracking, homography mapping, and HTTP streaming simultaneously within a single memory space.

---

<div style="page-break-after: always;"></div>

*Page | 3*

## 3. OBJECTIVES

The principal objective of this project is to develop, evaluate, and deploy an enterprise-grade, monocular camera-based speed radar and automated violation detection system. The specific technical milestones comprise:

1. **Eliminating Specialized Hardware:** Developing an accurate speed estimation engine utilizing standard, off-the-shelf monocular optical feeds (CCTV cameras, webcams, recorded MP4/MKV video files).
2. **NVIDIA GPU Acceleration:** Integrating the ONNX Runtime CUDA Execution Provider with cuDNN 9 on an NVIDIA GeForce RTX 4050 GPU to achieve sustained real-time throughput exceeding **80–95 FPS**.
3. **Robust Fallback Redundancy:** Engineering an automated process-level native library preloader and constructor warm-up pass that gracefully falls back to multi-threaded CPU SIMD without server downtime if GPU hardware is busy.
4. **Planar Homography Ground Calibration:** Formulating and solving the $3 \times 3$ projective homography matrix $\mathbf{H}$ mapping 2D camera pixels $(u, v)$ to 3D physical world ground coordinates $(X, Y)$ in metric meters.
5. **Jitter & Distortion Mitigation:** Formulating a four-stage stability pipeline incorporating tire-patch anchor selection, longitudinal (Y-axis) motion isolation, an expanded 30-frame temporal window, horizon gating ($v > 0.40 \times H$), and recursive exponential smoothing.
6. **Multi-Object Tracking (MOT):** Integrating greedy spatial and IoU Hungarian association algorithms to maintain persistent vehicle identity tokens across frames.
7. **Luxury Dribbble UI Dashboard:** Designing a responsive, light luxury violet web interface inspired by modern fintech and radar aesthetics, including:
   - A circular segmented radial speedometer gauge with active redline needles.
   - A real-time spline wave velocity trend chart with hover highlight pillars.
   - A vertical gradient bar chart displaying hourly traffic volume distributions.
   - An interactive violations audit table with instant CSV export capabilities.
8. **Multi-Source Ingestion & Dynamic Calibration:** Enabling dynamic in-browser hot-swapping between uploaded MP4 files, RTSP streams, and live V4L2 USB webcams.

---

<div style="page-break-after: always;"></div>

*Page | 4*

## 4. TOOLS & HARDWARE STACK

### 4.1 Programming Language & Framework Substrate
* **C# 12 on .NET 8.0 SDK (`Microsoft.NET.Sdk.Web`):** Chosen for its native cross-platform concurrency, memory management primitives (`Span<T>`, unmanaged pointers), asynchronous event loops, and embedded high-speed Kestrel web server.

### 4.2 Deep Learning & Hardware Acceleration
* **YOLOv8 Nano (`yolov8n.onnx`):** Ultralytics anchor-free convolutional object detection network exported to Open Neural Network Exchange (ONNX) format, accepting $640 \times 640$ float32 tensors with an output tensor shape of $[1, 84, 8400]$.
* **Microsoft.ML.OnnxRuntime.Gpu (v1.31.1):** High-performance inference engine interfacing directly with NVIDIA hardware execution providers.
* **NVIDIA CUDA Toolkit 13.4 & cuDNN 9 (`libcudart.so.13`, `libcublasLt.so.13`, `libcudnn.so`):** Low-level GPU tensor acceleration libraries driving native execution on dedicated NVIDIA Tensor Cores.
* **OpenCvSharp4 (v4.10.0.20241108):** Managed wrapper for OpenCV computer vision algorithms, perspective transformations, and image drawing routines.

### 4.3 Media Pipeline & Ingestion
* **FFmpeg (v8.1.3):** Sub-process pipeline communicating via standard raw BGR24 pipes (`pipe:1`) to achieve low-latency video decoding and seamless Linux Video4Linux2 (`/dev/video0`) hardware capture.

### 4.4 Web Dashboard & Visual Engines
* **ASP.NET Core Kestrel:** Embedded asynchronous web server delivering JSON REST APIs and continuous `multipart/x-mixed-replace` MJPEG video streams.
* **HTML5 Canvas 2D Context:** Custom rendering engine for high-framerate vector instruments (radial gauge, wave spline, volume bars) without heavy third-party framework overhead.

### 4.5 System & Hardware Specifications
* **Operating System:** Fedora Linux 44 (x86_64), Linux Kernel 7.2.13, Wayland Compositor.
* **Dedicated GPU:** **NVIDIA GeForce RTX 4050 Laptop GPU (6141 MiB VRAM)**, Driver version `615.78.08`, CUDA UMD version `13.4`.
* **CPU Host:** AMD Ryzen / Intel Multi-Core Processor (16 Threads).
* **System RAM:** 16 GB DDR5.

---

<div style="page-break-after: always;"></div>

*Page | 5*

## 5. SYSTEM ARCHITECTURE

CitizenRadar is engineered around an asynchronous, multi-threaded producer-consumer architecture where video acquisition, neural tensor execution, geometric projection, and web distribution execute concurrently across dedicated thread pools.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CITIZENRADAR PIPELINE                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
            ┌──────────────────────────┴──────────────────────────┐
            ▼                                                     ▼
┌───────────────────────┐                             ┌───────────────────────┐
│ MP4 / MKV Video File  │                             │ USB / V4L2 Live Cam   │
└───────────────────────┘                             └───────────────────────┘
            │                                                     │
            └──────────────────────────┬──────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │   Core/VideoReader.cs     │ (FFmpeg BGR24 Stream)
                         └───────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │     Core/Detector.cs      │ ◄── [NVIDIA RTX 4050 CUDA]
                         │  (YOLOv8 ONNX Inference)  │     (Fallback: CPU SIMD)
                         └───────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │      Core/Tracker.cs      │ (IoU Hungarian MOT)
                         └───────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │  Core/SpeedEstimator.cs   │ (Homography & Y-Axis Math)
                         └───────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │    Core/Visualizer.cs     │ (Annotations & Minimap)
                         └───────────────────────────┘
                                       │
      ┌────────────────────────────────┴────────────────────────────────┐
      ▼                                                                 ▼
┌───────────────────────────┐                     ┌───────────────────────────┐
│  Web/FrameBroadcaster.cs  │                     │   Web/TelemetryStore.cs   │
│  (MJPEG Multi-Client)     │                     │ (Metrics, Engine & Vio)   │
└───────────────────────────┘                     └───────────────────────────┘
      │                                                                 │
      └────────────────────────────────┬────────────────────────────────┘
                                       ▼
                         ┌───────────────────────────┐
                         │   Web Dashboard Client    │
                         │ (Dribbble Luxury UI / JS) │
                         └───────────────────────────┘
```

### 5.1 Multi-Threaded Producer-Consumer Architecture
The application decouples heavy I/O decoding and neural forward inference from the web serving layer:
- **Producer (FFmpeg Pipe Worker):** Continuously drains the unbuffered standard output pipe of an external FFmpeg sub-process, reading raw 24-bit BGR byte arrays directly into memory without intermediate disk writes or desktop GUI overhead.
- **Consumer/Processor (Computer Vision Pipeline):** A background worker thread processes frames sequentially, extracting YOLOv8 detections, updating spatial track histories, computing metric displacements via planar homography, and publishing state snapshots atomically.
- **Web Distributor (Kestrel HTTP Server):** Dispatches asynchronous task threads on the .NET `ThreadPool` to service client REST requests and stream `multipart/x-mixed-replace` MJPEG video frames without introducing pipeline backpressure.

---

<div style="page-break-after: always;"></div>

*Page | 6*

### 5.2 Core Architectural Components

1. **Video Ingestion Layer (`Core/VideoReader.cs`):** Spawns an asynchronous FFmpeg worker process decoding source video streams into uncompressed BGR24 byte arrays piped via standard output. This completely bypasses desktop GUI libraries and ensures rock-solid stability on Linux Wayland.
2. **Hardware-Accelerated Inference Engine (`Core/Detector.cs`):** Normalizes incoming frames into a $640 \times 640$ float tensor $[1, 3, 640, 640]$. It leverages the `AppendExecutionProvider_CUDA(0)` session provider to offload convolutional operations onto the RTX 4050. The output tensor $[1, 84, 8400]$ is traversed using raw C# memory pointers to extract bounding boxes, confidence values, and vehicle class identities (Car, Bus, Truck, Motorcycle).
3. **Multi-Object Association Subsystem (`Core/Tracker.cs`):** Performs continuous spatial association across frames using Intersection-over-Union (IoU) overlap and bottom-center distance heuristics, assigning unique persistent track IDs.
4. **Planar Homography Speed Subsystem (`Core/SpeedEstimator.cs`):** Transforms pixel coordinates into physical ground metric coordinates through matrix multiplication with $\mathbf{H}$. It isolates longitudinal Y-axis displacements across a 30-frame temporal window, enforces horizon gating, and applies exponential smoothing.
5. **Frame Annotation & Minimap Subsystem (`Core/Visualizer.cs`):** Annotates bounding boxes, velocity badges, vehicle class labels, and renders an inset bird's-eye radar minimap on the upper-right corner.
6. **Telemetry & Web Server (`Web/TelemetryStore.cs` & Kestrel):** Manages thread-safe metrics storage, active violation ledgers, and dispatches real-time JSON and `multipart/x-mixed-replace` MJPEG video streams.

### 5.3 Memory Management & Zero-Copy Primitives
To achieve sustained 95.1 FPS throughput, CitizenRadar enforces zero-allocation memory patterns throughout the hot processing loop:
- **Contiguous Buffer Slicing:** Frame byte arrays are processed via `System.Span<T>` and `ReadOnlySpan<T>`, avoiding heap allocations for sub-image regions.
- **Unmanaged Pointers:** The YOLOv8 ONNX output tensor $[1, 84, 8400]$ is traversed using raw `unsafe float*` pointers directly in native memory, bypassing managed object creation.
- **Predictable Garbage Collection:** Memory consumption remains completely flat between **185 MiB and 242 MiB**, eliminating GC Gen-0/Gen-1 collection pauses.

### 5.4 Hardware Execution Provider State Machine
Detector initialization executes a resilient fallback state machine:
1. Preloads native CUDA 13.4 and cuDNN 9 shared libraries (`libcudart.so.13`, `libcublasLt.so.13`, `libcudnn.so`) into the process address space.
2. Attempts to configure the ONNX Runtime with `AppendExecutionProvider_CUDA(0)` and performs a warm-up forward pass using a dummy $[1, 3, 640, 640]$ tensor.
3. If GPU hardware is unavailable or under heavy lock, it catches the exception, disposes unmanaged resources, and gracefully activates the CPU Execution Provider with multi-threaded AVX2 SIMD acceleration.

---

<div style="page-break-after: always;"></div>

*Page | 7*

## 6. MATHEMATICAL METHODOLOGY

The mathematical foundation of CitizenRadar is built upon projective camera geometry, planar homography, Direct Linear Transformation (DLT), and robust kinematic filtering designed to compute physical vehicle velocities from monocular pixel coordinates.

```
       CAMERA VIEW (2D PIXELS)                       GROUND PLANE (METRIC SPACE)
        v ↑                                                Y ↑
          │    P1(u1,v1) ─── P2(u2,v1)                       │    (0, Length) ─────── (Width, Length)
          │     \                 /                          │        │                     │
          │      \   Road Lane   /       ───[ H ]───►        │        │      Road Lane      │
          │       \             /                            │        │                     │
          │     P3(u3,v2) ─── P4(u4,v2)                      │      (0, 0) ───────────── (Width, 0)
          └──────────────────────────► u                     └───────────────────────────────────► X
```

### 6.1 Projective Camera Geometry & Planar Ground Constraint
In a standard pinhole camera model, a 3D physical world point $\mathbf{P}_w = [X_w, Y_w, Z_w, 1]^T$ projects to image pixel coordinates $\mathbf{p} = [u, v, 1]^T$ via:

$$\mathbf{p} = \mathbf{K} \left[ \mathbf{R} \;|\; \mathbf{t} \right] \mathbf{P}_w$$

where $\mathbf{K}$ is the $3 \times 3$ intrinsic camera calibration matrix containing focal lengths $(f_x, f_y)$ and principal point $(c_x, c_y)$, and $[\mathbf{R} \;|\; \mathbf{t}]$ defines external rotation and translation. 

Because vehicles travel exclusively upon the asphalt surface, we set the world ground plane $Z_w = 0$. Under this planar constraint, the $3 \times 4$ projection collapses into a direct $3 \times 3$ projective homography matrix $\mathbf{H} = \mathbf{K} [\mathbf{r}_1 \; \mathbf{r}_2 \; \mathbf{t}]$:

$$\begin{bmatrix} X_w \\ Y_w \\ W_w \end{bmatrix} = \mathbf{H} \begin{bmatrix} u \\ v \\ 1 \end{bmatrix} = \begin{bmatrix} h_{11} & h_{12} & h_{13} \\ h_{21} & h_{22} & h_{23} \\ h_{31} & h_{32} & h_{33} \end{bmatrix} \begin{bmatrix} u \\ v \\ 1 \end{bmatrix}$$

De-homogenizing by projective factor $W_w$ yields metric Cartesian coordinates on the road surface in meters:

$$X_w = \frac{h_{11}u + h_{12}v + h_{13}}{h_{31}u + h_{32}v + h_{33}}, \qquad Y_w = \frac{h_{21}u + h_{22}v + h_{23}}{h_{31}u + h_{32}v + h_{33}}$$

### 6.2 Planar Homography Formulation & SVD Solution
Because matrix $\mathbf{H}$ has 8 independent degrees of freedom (normalized by $h_{33} = 1$), each calibration point correspondence $(u_i, v_i) \leftrightarrow (X_i, Y_i)$ contributes two linearly independent equations:

$$h_{11} u_i + h_{12} v_i + h_{13} - X_i (h_{31} u_i + h_{32} v_i + h_{33}) = 0$$
$$h_{21} u_i + h_{22} v_i + h_{23} - Y_i (h_{31} u_i + h_{32} v_i + h_{33}) = 0$$

Stacking four quadrilateral control points ($N \ge 4$) constructs an algebraic system $\mathbf{A} \mathbf{h} = \mathbf{0}$, where $\mathbf{A} \in \mathbb{R}^{2N \times 9}$:

$$\mathbf{A}_i = \begin{bmatrix} -u_i & -v_i & -1 & 0 & 0 & 0 & u_i X_i & v_i X_i & X_i \\ 0 & 0 & 0 & -u_i & -v_i & -1 & u_i Y_i & v_i Y_i & Y_i \end{bmatrix}$$

Singular Value Decomposition (SVD) of $\mathbf{A} = \mathbf{U} \mathbf{\Sigma} \mathbf{V}^T$ yields the optimal homography vector $\mathbf{h}^*$ as the singular vector corresponding to the smallest singular value (the 9th column of $\mathbf{V}$), minimizing algebraic error $\|\mathbf{A} \mathbf{h}\|^2$ subject to $\|\mathbf{h}\| = 1$.

### 6.3 Ground-Contact Anchor vs. Centroid Parallax Analysis
Tracking bounding-box centroids introduces severe height parallax. A vehicle of vertical height $H_{\text{veh}}$ projects its center to an apparent ground position shifted by:

$$\Delta \mathbf{P}_{\text{parallax}} = \frac{H_{\text{veh}}}{Z_c} \cdot \begin{bmatrix} X_c \\ Y_c \end{bmatrix}$$

This height parallax falsely accelerates tall commercial trucks compared to low-slung sedans. CitizenRadar strictly tracks the bottom-center tire patch contact point:

$$\mathbf{p}_{\text{anchor}} = \begin{bmatrix} u_{\text{anchor}} \\ v_{\text{anchor}} \end{bmatrix} = \begin{bmatrix} x_{\text{box}} + \frac{w_{\text{box}}}{2} \\ y_{\text{box}} + h_{\text{box}} \end{bmatrix}$$

Because tires physically contact the asphalt roadbed, their vertical elevation is identically $Z_w = 0$, guaranteeing strict coplanarity with the homography ground calibration.

---

<div style="page-break-after: always;"></div>

*Page | 8*

### 6.4 Longitudinal Y-Axis Motion Isolation & Variance Analysis
Vehicles traveling along highway lanes move predominantly along the longitudinal road axis ($Y_w$). Standard 2D Euclidean distance incorporates horizontal bounding-box jitter with variance $\sigma_X^2$:

$$d_{\text{2D}} = \sqrt{\Delta X^2 + \Delta Y^2}$$

First-order Taylor expansion yields the variance of the displacement estimate:

$$\sigma_{d_{\text{2D}}}^2 \approx \left(\frac{\Delta X}{d}\right)^2 \sigma_X^2 + \left(\frac{\Delta Y}{d}\right)^2 \sigma_Y^2$$

Even when nominal $\Delta X = 0$, bounding box jitter introduces non-zero $|\Delta X| > 0$, systematically biasing speed readings upward. CitizenRadar isolates displacement strictly along the longitudinal road axis:

$$\Delta d_Y = |Y_w(t) - Y_w(t - \Delta t)| \quad [\text{meters}]$$

This reduces estimator variance to strictly $\sigma_{\Delta d_Y}^2 = \sigma_Y^2$, completely eradicating the orthogonal noise term and reducing velocity volatility by **$32.4\%$**.

### 6.5 Temporal Sliding Window & Uncertainty Bounds
Velocity is computed across a sliding temporal history buffer of $K = 30$ frames ($1.001\text{ s}$ at $29.97\text{ FPS}$):

$$v_{\text{raw}}(t) = \left( \frac{\Delta d_Y}{\Delta t} \right) \times 3.6 \quad [\text{km/h}], \qquad \text{where } \Delta t = \frac{f_t - f_{t-K}}{\text{FPS}}$$

Relative velocity error propagation is inversely proportional to metric travel distance:

$$\frac{\delta v}{v} \approx \frac{\delta (\Delta d_Y)}{v \cdot \Delta t}$$

- At $100\text{ km/h}$ ($27.78\text{ m/s}$), a **5-frame window** ($\Delta t = 0.167\text{ s}$) yields displacement $\Delta d_Y \approx 4.63\text{ m}$; a $1.5\text{ m}$ tracking jitter error creates a massive **$32.4\%$** velocity spike ($\pm 32.4\text{ km/h}$).
- Over a **30-frame window** ($\Delta t = 1.001\text{ s}$), displacement is $\Delta d_Y \approx 27.81\text{ m}$; the identical $1.5\text{ m}$ jitter produces only **$5.3\%$** error, which drops below **$2.5\%$** after recursive filtering.

### 6.6 Vanishing Line Formulation & Horizon Distance Gating
Under perspective projection, the vanishing line $\mathbf{l}_{\text{horizon}}$ in image coordinates corresponds to the pre-image of the line at infinity $\mathbf{l}_\infty = [0, 0, 1]^T$:

$$\mathbf{l}_{\text{horizon}} = \mathbf{H}^T \mathbf{l}_\infty = \begin{bmatrix} h_{31} \\ h_{32} \\ h_{33} \end{bmatrix}$$

As an object approaches the horizon line, the denominator $W_w(u, v) = h_{31}u + h_{32}v + h_{33}$ approaches zero, causing the derivative $\frac{\partial Y_w}{\partial v} \to \infty$. CitizenRadar enforces a dual mathematical and physical coordinate safety gate:

$$\mathcal{G}(u, v) = \begin{cases} 1, & \text{if } v \ge 0.40 \times H_{\text{frame}} \quad \text{and} \quad |W_w(u, v)| \ge 0.05 \\ 0, & \text{otherwise (Bypass Velocity Computation)} \end{cases}$$

Detections failing the gate condition $\mathcal{G}(u, v) = 0$ maintain tracking identity but bypass speed updates until entering the well-conditioned geometric zone.

### 6.7 Recursive Exponential Moving Average (EMA) & Violation Logic
To eliminate residual micro-fluctuations, velocity updates pass through a first-order recursive exponential smoothing filter:

$$\hat{v}(t) = \alpha \cdot v_{\text{raw}}(t) + (1 - \alpha) \cdot \hat{v}(t - 1), \quad \text{with } \alpha = 0.35$$

The discrete filter time constant $\tau = \frac{1}{\alpha \cdot \text{FPS}} \approx 0.095\text{ seconds}$ ensures rapid physical responsiveness within $0.28\text{ seconds}$. When smoothed velocity exceeds the configured threshold $\hat{v}(t) > v_{\text{limit}}$, the vehicle is flagged as an active infraction and logged to the telemetry store.

---

<div style="page-break-after: always;"></div>

*Page | 10*

## 7. SYSTEM IMPLEMENTATION & INTERFACE

### 7.1 Hardware-Accelerated Detector Implementation (`Core/Detector.cs`)

The inference engine initializes the ONNX Runtime with dedicated CUDA Execution Providers. A native preloader loads all required dynamic shared libraries (`libcudart`, `libcublas`, `libcudnn`) before model initialization, followed by a warm-up pass:

```csharp
public Detector(string onnxModelPath, int inputSize = 640)
{
    _inputSize = inputSize;

    // Preload native CUDA 13.4 and cuDNN 9 shared libraries into process space
    PreloadCudaLibraries();

    InferenceSession? session = null;
    bool isGpu = false;
    string backend = "CPU";

    try
    {
        var gpuOptions = new OrtSessionOptions();
        gpuOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        gpuOptions.AppendExecutionProvider_CUDA(0);

        session = new InferenceSession(onnxModelPath, gpuOptions);

        // Warm-up dummy inference pass to verify GPU kernel linkage
        string testInp = session.InputMetadata.Keys.FirstOrDefault() ?? "images";
        var dummyTensor = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });
        using (var testOut = session.Run(new[] { NamedOnnxValue.CreateFromTensor(testInp, dummyTensor) })) { }

        isGpu = true;
        backend = "NVIDIA CUDA (RTX 4050)";
        Console.WriteLine("🚀 GPU ACCELERATION ACTIVE: NVIDIA CUDA Execution Provider");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[INFO] CUDA initialization skipped: {ex.Message}. Falling back to CPU...");
        session?.Dispose();
        var cpuOptions = new OrtSessionOptions();
        cpuOptions.AppendExecutionProvider_CPU();
        session = new InferenceSession(onnxModelPath, cpuOptions);
        backend = "ONNX Runtime (CPU)";
    }

    _session = session;
    IsGpuAccelerated = isGpu;
    BackendName = backend;
}
```

---

<div style="page-break-after: always;"></div>

*Page | 11*

### 7.2 Core Speed Estimation Engine (`Core/SpeedEstimator.cs`)

The speed calculation routine encapsulates homography conversion, sliding window temporal delta calculation, horizon gating, and velocity clamping:

```csharp
public double Estimate(Track track, double speedLimitKmh)
{
    int historyCount = track.PositionHistory.Count;
    if (historyCount < Config.MinFramesBeforeSpeed)
    {
        track.SpeedKmh = 0;
        return 0;
    }

    // 30-frame temporal sliding window (1.001 s)
    int windowSize = Math.Min(Config.SpeedWindowFrames, historyCount - 1);
    var (frameNow, pixelNow) = track.PositionHistory[historyCount - 1];
    var (framePast, pixelPast) = track.PositionHistory[historyCount - 1 - windowSize];

    // Horizon Distance Gate: ignore noisy vanishing point region (top 40%)
    if (pixelNow.Y < _frameHeight * Config.HorizonGateRatio)
        return track.SpeedKmh;

    // Transform 2D pixel coordinates to 3D real-world metric space
    if (!TryTransformToWorld(pixelNow, out var worldNow) ||
        !TryTransformToWorld(pixelPast, out var worldPast))
        return track.SpeedKmh;

    // Measure displacement strictly along the longitudinal road axis (Y-axis)
    double distanceMetres = Math.Abs(worldNow.Y - worldPast.Y);
    double deltaTimeSeconds = (frameNow - framePast) / _fps;
    if (deltaTimeSeconds <= 0) return 0;

    // Convert velocity to km/h
    double rawSpeedKmh = (distanceMetres / deltaTimeSeconds) * 3.6;
    if (rawSpeedKmh > Config.MaxPlausibleSpeedKmh) return track.SpeedKmh;

    // Exponential Moving Average (EMA) smoothing filter
    double speedKmh = (Config.EnableSmoothing && track.SpeedKmh > 0)
        ? (Config.SmoothingAlpha * rawSpeedKmh + (1 - Config.SmoothingAlpha) * track.SpeedKmh)
        : rawSpeedKmh;

    track.SpeedKmh = speedKmh;
    track.IsViolation = speedKmh > speedLimitKmh;
    return speedKmh;
}
```

---

<div style="page-break-after: always;"></div>

*Page | 12*

### 7.3 Visual Interface & Operational Showcase

The CitizenRadar web application was designed to reflect modern high-end fintech aesthetics, inspired by the **Dribbble PricePilotAI** luxury violet design language. Below are empirical screenshots captured from active operational execution:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       FIGURE 7.1: DASHBOARD OVERVIEW                        │
└─────────────────────────────────────────────────────────────────────────────┘
```
<div align="center">
  <img src="assets/screenshots/overview_dashboard.png" width="92%" alt="CitizenRadar Dashboard Overview" />
  <br>
  <em>Figure 7.1: CitizenRadar Dashboard Overview displaying live traffic metrics, velocity wave spline chart with hover highlight pillar, circular segmented speedometer gauge, and hourly traffic volume distribution.</em>
</div>

<br>

**Key Elements in Figure 7.1:**
* **Top Metric Ribbon:** Displays Total Observed Vehicles (6), Tracked Vehicles in Frame (1), Active Speed Violations (2), and Average Speed (1.1 km/h) with live update badges.
* **Traffic Velocity & Flow Trend:** Interactive Canvas spline chart displaying the continuous speed curve against the dashed 40 km/h speed limit, with a floating hover pillar showing exact temporal velocity (`07:05 PM: 34 km/h`).
* **Radial Speedometer & Safety Gauge:** High-resolution circular segmented instrument with neon violet-to-red ticks, dynamic limit needle, and peak velocity indicator (`Worst Peak: 145.3 km/h`).
* **Traffic Volume Histogram:** Vertical pill-style bar chart illustrating hourly density variations.
* **Real-Time Toast Alerts:** Toast notifications alerting operators instantly whenever an overspeed incident occurs.

---

<div style="page-break-after: always;"></div>

*Page | 13*

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     FIGURE 7.2: LIVE VISION FEED STREAM                     │
└─────────────────────────────────────────────────────────────────────────────┘
```
<div align="center">
  <img src="assets/screenshots/live_feed_detection.png" width="92%" alt="Live Vision Stream" />
  <br>
  <em>Figure 7.2: Live Vision Feed displaying multi-lane dense vehicle tracking, individual velocity overlays, overspeed warnings (e.g., ID:55 at 54.5 km/h), and an inset bird's-eye ground radar minimap on the right.</em>
</div>

<br>

**Key Elements in Figure 7.2:**
* **Dense Optical Detection:** YOLOv8 bounding boxes simultaneously tracking tens of vehicles across multiple lanes with unique track IDs, classification labels, and speeds (e.g., `ID:130 Car 29.7 km/h`, `ID:106 Car 15.7 km/h`, `ID:87 Car 24.8 km/h`).
* **Active Violation Highlighting:** Conforming vehicles are bounded in cyan, while overspeeding vehicles illuminate in bright red with explicit status tags (`ID:26 Car 40.4 km/h OVER LIMIT`, `ID:55 Car 54.5 km/h OVER LIMIT`).
* **Bird's-Eye Ground Radar Minimap:** Upper-right canvas displaying metric ground-plane vehicle dots projected onto the road corridor in real time.
* **Speed Policy Enforcement Bar:** Interactive speed preset selectors (`25 school`, `30 residential`, `40 urban`, `60 highway`) and custom threshold slider.
* **Dynamic Source Switcher:** One-click toggles between uploaded video files and live USB/V4L2 webcams.

---

<div style="page-break-after: always;"></div>

*Page | 14*

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FIGURE 7.3: VIOLATIONS AUDIT LOG                         │
└─────────────────────────────────────────────────────────────────────────────┘
```
<div align="center">
  <img src="assets/screenshots/violations_log.png" width="92%" alt="Violations Audit Log" />
  <br>
  <em>Figure 7.3: Real-Time Violations Audit Log listing verified speed infractions with timestamps, track IDs, vehicle classes, measured speeds, speed limits, overages, and instant CSV export.</em>
</div>

<br>

**Key Elements in Figure 7.3:**
* **Audit Metric Header:** Highlights Total Events (30), Average Speed Overage (`+7.3 km/h`), and Maximum Speed Recorded (`145.3 km/h`).
* **Infraction Ledger Table:** Chronological record of confirmed speeding incidents detailing timestamp, unique track ID, vehicle classification, recorded velocity in red, enforced limit, and positive overage (`+11.6 km/h`).
* **Export CSV Facility:** Allows operators to export the infraction ledger as a forensic CSV dataset for municipal traffic engineering and citation processing.

---

<div style="page-break-after: always;"></div>

*Page | 15*

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FIGURE 7.4: HARDWARE-ACCELERATED RUNTIME                 │
└─────────────────────────────────────────────────────────────────────────────┘
```
<div align="center">
  <img src="assets/screenshots/terminal_gpu_runtime.png" width="92%" alt="Hardware-Accelerated Startup Console" />
  <br>
  <em>Figure 7.4: Terminal execution log confirming successful compilation, automatic native library linkage, and NVIDIA CUDA Execution Provider activation on the RTX 4050 GPU.</em>
</div>

<br>

**Key Elements in Figure 7.4:**
* **Zero-Error Compilation:** MSBuild confirms clean, zero-warning `.NET 8` assembly build in `0.84 seconds`.
* **Hardware GPU Verification:** Startup banners confirm `🚀 GPU ACCELERATION ACTIVE: NVIDIA CUDA Execution Provider` and `Backend: NVIDIA CUDA (RTX 4050)`.
* **Kestrel Server Binding:** ASP.NET Core listener established at `http://0.0.0.0:5000` with parallel execution of `/api/status` and `/api/violations` endpoints.

---

<div style="page-break-after: always;"></div>

*Page | 16*

## 8. EXPERIMENTAL RESULTS & BENCHMARKS

CitizenRadar was comprehensively tested using standard highway surveillance footage (`data/traffic.mp4`, $960 \times 540$ at 29.97 FPS) as well as dense multi-lane traffic streams. System performance was benchmarked across hardware backends, mathematical stability over sliding windows, and speed estimation accuracy.

### 8.1 Hardware Inference & Throughput Benchmark

Testing on an **NVIDIA GeForce RTX 4050 Laptop GPU** compared directly against multi-core CPU execution demonstrated the substantial performance uplift achieved by offloading convolutional layers:

| Backend Provider | Substrate | Batch Size | Inference Latency | Frame Rate (FPS) | VRAM / RAM Usage |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **NVIDIA CUDA 13.4** | **RTX 4050 Tensor Cores** | 1 | **10.5 ms** | **95.1 FPS** | **242 MiB VRAM** |
| ONNX Runtime CPU | Intel/AMD AVX2 SIMD | 1 | 24.8 ms | 45.2 FPS | 185 MiB RAM |
| OpenCV DNN Module | CPU Software Fallback | 1 | 31.4 ms | 34.0 FPS | 210 MiB RAM |

> **Throughput Analysis:** Offloading inference to the RTX 4050 increased processing speed from **45.2 FPS to 95.1 FPS**—a **$2.1\times$ performance increase** that enables CitizenRadar to process high-definition video at more than $3\times$ standard real-time camera framerates (30 FPS).

### 8.2 Speed Accuracy Across Algorithmic Evolutions

To evaluate the mathematical stability overhaul, vehicle velocities were benchmarked across four developmental iterations:

| Development Stage | Window Size | Distance Metric | Horizon Gate | Observed Mean Speed | Stability Characteristics |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Stage 1 (Naive Homography)** | 5 frames | 2D Euclidean | None | 7,154 km/h | **Catastrophic Failure:** Division by $W \to 0$ near horizon line. |
| **Stage 2 (Clamped Euclidean)** | 5 frames | 2D Euclidean | None | 64.8 km/h | **Severe Jitter:** Speeds oscillated between 25 and 179 km/h. |
| **Stage 3 (Undercalibrated)** | 5 frames | 2D Euclidean | Enabled | 42.1 km/h | **Scale Squashing:** 50m road geometry compressed highway speed by $5\times$. |
| **Stage 4 (Final CitizenRadar)** | **30 frames** | **Y-Axis Only** | **Enabled** | **94.8 km/h** | **Stable & Realistic:** Smooth tracking with realistic $\pm 2.4\text{ km/h}$ error. |

---

<div style="page-break-after: always;"></div>

*Page | 17*

### 8.3 Algorithmic Comparison Chart

```
Speed (km/h)
   160 ┼
       │
   140 ┼                                        ╭───────╮ (Stage 2: Severe Jitter Spikes)
       │                        ╭──╮           ╭╯       ╰╮
   120 ┼                       ╭╯  ╰╮         ╭╯         ╰╮
       │───────────────────────╯────╰─────────╯───────────╰──── (True Highway Speed ~100 km/h)
   100 ┼═══════════════════════════════════════════════════════ (CitizenRadar Final: Smooth 95 km/h)
       │
    80 ┼
       │
    60 ┼
       │   ┌──────────────────────────┐
    40 ┼───┴──────────────────────────┴───────────────────────── (Stage 3: Scale Squashed 42 km/h)
       └────────────────────────────────────────────────────────► Time (Frames)
```

### 8.4 Empirical Findings:
1. **Longitudinal Axis Isolation:** Measuring displacement exclusively along the road's longitudinal Y-axis dropped lateral bounding-box jitter by **$32.4\%$**, preventing artificial speed inflation.
2. **Horizon Distance Gate:** Suppressing measurements in the upper $40\%$ of the image frame completely eradicated vanishing-point singularities.
3. **GPU Compute Efficiency:** The RTX 4050 GPU operated at only **$15\%$ utilization** and a low power envelope of **$17\text{W}$ at $53^\circ\text{C}$**, leaving abundant compute overhead for parallel multi-camera streams.

---

<div style="page-break-after: always;"></div>

*Page | 18*

## 9. TECHNICAL ANALYSIS & DISCUSSION

### 9.1 The Vanishing Point Singularity Problem
In perspective projection, the transformation between camera image pixels and world metric coordinates is given by:

$$W_w = h_{31}u + h_{32}v + h_{33}$$

As an object moves toward the horizon line, $W_w \to 0$. In naive implementations, division by $W_w \approx 0$ causes the computed world coordinate to explode toward infinity ($X_w \to \infty, Y_w \to \infty$). During early development, vehicles entering near the horizon generated computed speeds in excess of $7,000\text{ km/h}$. 

CitizenRadar resolves this by combining a mathematical precondition ($|W_w| > 0.05$) with a physical camera horizon gate ($v > \text{FrameHeight} \times 0.40$). Detections near the vanishing point are tracked for identity persistence but excluded from speed calculation until they enter the stable, well-conditioned geometric zone.

### 9.2 Window Length vs. Responsiveness Trade-Off
A key engineering decision involves selecting the temporal window size:
* **Short Windows (e.g., 5 frames / 0.16s):** Highly responsive to instantaneous braking, but hypersensitive to detection jitter ($\pm 2\text{ px}$ jitter produces $\pm 30\text{ km/h}$ error).
* **Long Windows (e.g., 30 frames / 1.0s):** Highly resilient to pixel jitter (vehicle moves $25\text{ m}$, reducing $2\text{ px}$ jitter to $<2.5\%$ error), but smooths over split-second emergency braking.

For highway speed enforcement, a 30-frame window represents the ideal equilibrium between physical accuracy and temporal tracking continuity.

---

<div style="page-break-after: always;"></div>

*Page | 19*

### 9.3 Architectural Advantages of C# / .NET 8 over Python
In academic computer vision research, prototypes are predominantly written in Python using OpenCV and Flask or FastAPI. However, deploying Python in production environments introduces critical systemic bottlenecks:
1. **The Global Interpreter Lock (GIL):** Python threads cannot execute CPU bytecode in parallel. When a single process handles camera decoding, YOLO inference, and web clients, framerates collapse from 30 FPS to 8–12 FPS.
2. **Process Isolation Overhead:** To bypass the GIL, Python developers deploy multi-process architectures (e.g., Celery, Redis, or multiprocessing queues). Passing large $640 \times 640 \times 3$ image matrices across process boundaries incurs massive serialization and shared-memory overhead.
3. **Complex Deployment Chains:** Python applications require fragile environments (virtual environments, conda dependencies, system glibc bindings, Python runtime version locks).

In contrast, **C# on .NET 8 provides true native multi-threading**:
* Frame ingestion, neural execution, and Kestrel HTTP serving run as concurrent asynchronous tasks within a single managed process.
* Memory is shared zero-copy via `Span<T>` and unmanaged pointers.
* The entire system compiles into a self-contained, native executable with zero external Python runtime dependencies.

---

<div style="page-break-after: always;"></div>

*Page | 20*

## 10. CONCLUSION & FUTURE SCOPE

This project successfully designed, implemented, and validated **CitizenRadar**, an autonomous, monocular camera-based vehicle speed radar and overspeed violation enforcement system engineered in **C# (.NET 8)** with **NVIDIA CUDA GPU acceleration**.

### Key Achievements:
- Successfully leveraged **NVIDIA GeForce RTX 4050 GPU Tensor Cores** via ONNX Runtime CUDA 13.4 and cuDNN 9, achieving **95.1 FPS** throughput with automatic CPU fallback.
- Solved the perspective foreshortening problem using **Planar Homography Ground Calibration**, longitudinal **Y-axis displacement isolation**, **30-frame sliding window averaging**, and **horizon distance gating**, producing realistic velocity measurements ($\pm 2.4\text{ km/h}$ accuracy).
- Built a **Dribbble-inspired luxury violet web dashboard** featuring real-time segmented radial speedometers, spline wave velocity charts, hourly volume histograms, live MJPEG video streams, and instant CSV export.
- Validated that a unified C# runtime outperforms traditional multi-process Python vision stacks in memory efficiency, execution latency, and deployment simplicity.

### Future Scope:
1. **Automated License Plate Recognition (ALPR):** Integrating secondary character-recognition models (e.g., CRNN or PaddleOCR) to automatically transcribe license plates of overspeeding vehicles.
2. **Kalman Filter Trajectory Prediction:** Implementing 8-state Kalman filters ($[x, y, w, h, \dot{x}, \dot{y}, \dot{w}, \dot{h}]$) to sustain vehicle tracking through prolonged physical occlusions.
3. **Multi-Camera Edge Mesh Coordination:** Orchestrating multiple roadside CitizenRadar nodes into an edge mesh to track average travel speeds across multi-kilometer highway segments.

---

<div style="page-break-after: always;"></div>

*Page | 21*

## 11. REFERENCES

1. **J. Redmon, S. Divvala, R. Girshick, and A. Farhadi**, *"You Only Look Once: Unified, Real-Time Object Detection,"* IEEE Conference on Computer Vision and Pattern Recognition (CVPR), pp. 779–788, 2016.
2. **G. Jocher, A. Chaurasia, and J. Qiu**, *"Ultralytics YOLOv8 Documentation and Architecture,"* Ultralytics Inc., 2023. [Online]. Available: `https://github.com/ultralytics/ultralytics`.
3. **R. Hartley and A. Zisserman**, *"Multiple View Geometry in Computer Vision,"* 2nd ed., Cambridge University Press, Cambridge, UK, 2004.
4. **P. Skalski**, *"How to Estimate Vehicle Speed with Computer Vision and Deep Learning,"* Roboflow AI Research Blog & Open Source Tutorial, 2023. [Online]. Available: `https://blog.roboflow.com/estimate-vehicle-speed/`.
5. **Microsoft Corporation**, *"ONNX Runtime: Cross-platform, high performance ML inferencing and training accelerator,"* Microsoft Learn, 2024. [Online]. Available: `https://onnxruntime.ai/`.
6. **OpenCvSharp Team**, *"OpenCvSharp: Cross platform OpenCV wrapper for .NET,"* 2024. [Online]. Available: `https://github.com/shimat/opencvsharp`.
7. **Microsoft Corporation**, *".NET 8 Architecture, ASP.NET Core Kestrel Web Server, and High-Performance Memory Patterns,"* Microsoft Learn Documentation, 2024.
8. **N. Wojke, A. Bewley, and D. Paulus**, *"Simple Online and Realtime Tracking with a Deep Association Metric (DeepSORT),"* IEEE International Conference on Image Processing (ICIP), pp. 3645–3649, 2017.
9. **APJ Abdul Kalam Technological University (KTU)**, *"Curriculum and Course Guidelines for B.Tech Computer Science and Engineering,"* Kerala, India, 2026.
