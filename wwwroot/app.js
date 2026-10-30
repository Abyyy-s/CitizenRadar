// CitizenRadar Dashboard Logic v2
// Features: Calibration, Speed Limit, Telemetry, Video Upload, Live Camera, Toasts

// ═══════════════════════════════════════════
// STATE
// ═══════════════════════════════════════════
let isCalibrating = false;
let calibrationPoints = [];
let currentSourceMode = "file"; // "file" | "camera" | "idle"
let lastViolationCount = 0;

const stepInstructions = [
  "Click <strong>Top-Left</strong> corner of the road",
  "Click <strong>Top-Right</strong> corner of the road",
  "Click <strong>Bottom-Right</strong> corner of the road",
  "Click <strong>Bottom-Left</strong> corner of the road",
  "Calibration quadrilateral complete! Review dimensions and click Save."
];

// ═══════════════════════════════════════════
// DOM ELEMENTS
// ═══════════════════════════════════════════
const liveImg = document.getElementById("live-stream");
const calibCanvas = document.getElementById("calibration-canvas");
const ctx = calibCanvas.getContext("2d");
const btnToggleCalib = document.getElementById("btn-toggle-calib");
const btnCalibText = document.getElementById("btn-calib-text");
const btnResetCalib = document.getElementById("btn-reset-calib");
const calibBanner = document.getElementById("calibration-banner");
const calibControls = document.getElementById("calibration-controls");
const calibStepNum = document.getElementById("calib-step-num");
const calibInstruction = document.getElementById("calib-instruction");
const btnSaveCalib = document.getElementById("btn-save-calib");
const inputRoadWidth = document.getElementById("input-road-width");
const inputRoadLength = document.getElementById("input-road-length");

// Telemetry
const statFps = document.getElementById("stat-fps");
const statActive = document.getElementById("stat-active");
const statTotal = document.getElementById("stat-total");
const statAvg = document.getElementById("stat-avg");
const statViolations = document.getElementById("stat-violations");
const statViolationPct = document.getElementById("stat-violation-pct");
const displayLimit = document.getElementById("display-limit");
const sliderSpeedLimit = document.getElementById("slider-speed-limit");
const violationsTbody = document.getElementById("violations-tbody");
const liveClock = document.getElementById("live-clock");

// Source controls
const btnUpload = document.getElementById("btn-upload");
const fileInput = document.getElementById("file-input");
const btnGoLive = document.getElementById("btn-go-live");
const sourceLabel = document.getElementById("source-label");
const sourceIcon = document.getElementById("source-icon");
const liveBadge = document.getElementById("live-badge");
const statusPill = document.getElementById("status-pill");
const statusLabel = document.getElementById("status-label");
const footerSource = document.getElementById("footer-source");
const dropOverlay = document.getElementById("drop-overlay");
const toastContainer = document.getElementById("toast-container");
const calibWarningBanner = document.getElementById("calib-warning-banner");
const calibWarningDismiss = document.getElementById("calib-warning-dismiss");

// ═══════════════════════════════════════════
// TOAST SYSTEM
// ═══════════════════════════════════════════
function showToast(message, type = "success", duration = 3500) {
  const icons = {
    success: "✅",
    error: "❌",
    warning: "⚠️",
    violation: "🚨"
  };

  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `
    <span class="toast-icon">${icons[type] || "ℹ️"}</span>
    <span>${message}</span>
  `;

  toastContainer.appendChild(toast);

  setTimeout(() => {
    toast.classList.add("toast-out");
    toast.addEventListener("animationend", () => toast.remove());
  }, duration);
}

// ═══════════════════════════════════════════
// CANVAS SYNC
// ═══════════════════════════════════════════
function syncCanvasSize() {
  const rect = liveImg.getBoundingClientRect();
  if (rect.width > 0 && rect.height > 0) {
    calibCanvas.width = rect.width;
    calibCanvas.height = rect.height;
    drawCalibrationPoints();
  }
}

window.addEventListener("resize", syncCanvasSize);
liveImg.addEventListener("load", syncCanvasSize);

// Calibration warning banner dismiss
calibWarningDismiss.addEventListener("click", () => {
  calibWarningBanner.style.display = "none";
});

// ═══════════════════════════════════════════
// CALIBRATION
// ═══════════════════════════════════════════
btnToggleCalib.addEventListener("click", () => {
  isCalibrating = !isCalibrating;
  if (isCalibrating) {
    btnToggleCalib.classList.replace("btn-secondary", "btn-primary");
    btnCalibText.innerText = "Exit Calibration";
    btnResetCalib.style.display = "inline-block";
    calibBanner.style.display = "flex";
    calibControls.style.display = "flex";
    // FIX 1: Add 'calibrating' to the video-container div, not 'active' to the canvas.
    // The CSS rule '.video-container.calibrating #calibration-canvas' sets
    // pointer-events: auto and cursor: crosshair on the canvas.
    videoWrapper.classList.add("calibrating");
    console.log("Calibration active:", isCalibrating);
    console.log("Canvas pointer events:", getComputedStyle(calibCanvas).pointerEvents);
    console.log("Video wrapper classes:", videoWrapper.className);
    calibrationPoints = [];
    updateStepGuide();
    syncCanvasSize();
  } else {
    resetCalibrationUI();
  }
});

btnResetCalib.addEventListener("click", () => {
  calibrationPoints = [];
  updateStepGuide();
  drawCalibrationPoints();
});

function resetCalibrationUI() {
  isCalibrating = false;
  btnToggleCalib.classList.replace("btn-primary", "btn-secondary");
  btnCalibText.innerText = "Calibrate Road Points";
  btnResetCalib.style.display = "none";
  calibBanner.style.display = "none";
  calibControls.style.display = "none";
  videoWrapper.classList.remove("calibrating"); // FIX 1 (matching remove)
  calibrationPoints = [];
  ctx.clearRect(0, 0, calibCanvas.width, calibCanvas.height);
}

calibCanvas.addEventListener("click", (e) => {
  console.log("CANVAS CLICK RECEIVED", e.clientX, e.clientY);
  if (!isCalibrating || calibrationPoints.length >= 4) return;

  const rect = calibCanvas.getBoundingClientRect();
  const clickX = e.clientX - rect.left;
  const clickY = e.clientY - rect.top;

  // FIX 2: Use the stream's actual frame dimensions rather than hardcoded
  // 960×540.  liveImg.naturalWidth/naturalHeight reflect the real resolution
  // of whatever video is currently streaming (traffic.mp4 is 960×540, but
  // an uploaded video may differ).  Fall back to 960×540 if not yet loaded.
  const videoW = liveImg.naturalWidth  || 960;
  const videoH = liveImg.naturalHeight || 540;
  const normX = (clickX / rect.width)  * videoW;
  const normY = (clickY / rect.height) * videoH;

  calibrationPoints.push({
    canvasX: clickX,
    canvasY: clickY,
    videoX: normX,
    videoY: normY
  });

  updateStepGuide();
  drawCalibrationPoints();
});

function updateStepGuide() {
  const count = calibrationPoints.length;
  calibStepNum.innerText = `Step ${Math.min(count + 1, 4)}`;
  calibInstruction.innerHTML = stepInstructions[count] || stepInstructions[4];

  for (let i = 1; i <= 4; i++) {
    const el = document.getElementById(`pt-ind-${i}`);
    if (i <= count) {
      el.classList.add("done");
    } else {
      el.classList.remove("done");
    }
  }

  btnSaveCalib.disabled = count < 4;
}

function drawCalibrationPoints() {
  ctx.clearRect(0, 0, calibCanvas.width, calibCanvas.height);
  if (calibrationPoints.length === 0) return;

  ctx.lineWidth = 2;

  // Draw lines
  ctx.strokeStyle = "#00e5ff";
  ctx.beginPath();
  for (let i = 0; i < calibrationPoints.length; i++) {
    const pt = calibrationPoints[i];
    if (i === 0) ctx.moveTo(pt.canvasX, pt.canvasY);
    else ctx.lineTo(pt.canvasX, pt.canvasY);
  }
  if (calibrationPoints.length === 4) {
    ctx.closePath();
    ctx.fillStyle = "rgba(0, 229, 255, 0.12)";
    ctx.fill();
  }
  ctx.stroke();

  // Draw points
  for (let i = 0; i < calibrationPoints.length; i++) {
    const pt = calibrationPoints[i];
    ctx.fillStyle = "#ff3d71";
    ctx.beginPath();
    ctx.arc(pt.canvasX, pt.canvasY, 5, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = "#ffffff";
    ctx.font = "bold 11px 'JetBrains Mono', monospace";
    ctx.fillText(`P${i + 1}`, pt.canvasX + 9, pt.canvasY - 5);
  }
}

// Save Calibration
btnSaveCalib.addEventListener("click", async () => {
  if (calibrationPoints.length < 4) return;

  const payload = {
    sourcePoints: calibrationPoints.map(p => ({ x: p.videoX, y: p.videoY })),
    realWorldWidthMetres: parseFloat(inputRoadWidth.value) || 10.0,
    realWorldHeightMetres: parseFloat(inputRoadLength.value) || 50.0
  };

  try {
    const res = await fetch("/api/calibrate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      showToast("Road calibration updated and applied!", "success");
      calibWarningBanner.style.display = "none";
      resetCalibrationUI();
    } else {
      showToast("Failed to update calibration: " + (await res.text()), "error");
    }
  } catch (err) {
    showToast("Error sending calibration: " + err.message, "error");
  }
});

// ═══════════════════════════════════════════
// SPEED LIMIT CONTROLS
// ═══════════════════════════════════════════
sliderSpeedLimit.addEventListener("input", (e) => {
  updateSpeedLimit(parseFloat(e.target.value));
});

document.querySelectorAll(".preset-btn").forEach(btn => {
  btn.addEventListener("click", () => {
    document.querySelectorAll(".preset-btn").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");
    updateSpeedLimit(parseFloat(btn.dataset.speed));
  });
});

async function updateSpeedLimit(newLimit) {
  displayLimit.innerText = newLimit;
  sliderSpeedLimit.value = newLimit;

  try {
    await fetch("/api/config", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ speedLimitKmh: newLimit })
    });
  } catch (e) {
    console.error("Failed to update speed limit:", e);
  }
}

// ═══════════════════════════════════════════
// VIDEO UPLOAD
// ═══════════════════════════════════════════
btnUpload.addEventListener("click", () => {
  fileInput.click();
});

fileInput.addEventListener("change", async (e) => {
  const file = e.target.files[0];
  if (!file) return;
  await uploadVideoFile(file);
  fileInput.value = ""; // reset for re-upload
});

async function uploadVideoFile(file) {
  const formData = new FormData();
  formData.append("file", file);

  showToast(`Uploading ${file.name}...`, "warning", 5000);

  try {
    const res = await fetch("/api/source/upload", {
      method: "POST",
      body: formData
    });

    if (res.ok) {
      const data = await res.json();
      showToast(`Now analyzing: ${data.source}`, "success");
      setSourceMode("file", data.source);
    } else {
      const errText = await res.text();
      showToast("Upload failed: " + errText, "error");
    }
  } catch (err) {
    showToast("Upload error: " + err.message, "error");
  }
}

// Drag and drop
const videoWrapper = document.getElementById("video-wrapper");

videoWrapper.addEventListener("dragenter", (e) => {
  e.preventDefault();
  dropOverlay.classList.add("visible");
});

videoWrapper.addEventListener("dragover", (e) => {
  e.preventDefault();
});

dropOverlay.addEventListener("dragleave", (e) => {
  e.preventDefault();
  dropOverlay.classList.remove("visible");
});

dropOverlay.addEventListener("drop", async (e) => {
  e.preventDefault();
  dropOverlay.classList.remove("visible");

  const file = e.dataTransfer.files[0];
  if (!file) return;

  const ext = file.name.split(".").pop().toLowerCase();
  const allowed = ["mp4", "avi", "mkv", "mov", "webm"];
  if (!allowed.includes(ext)) {
    showToast(`Unsupported format: .${ext}`, "error");
    return;
  }

  await uploadVideoFile(file);
});

// ═══════════════════════════════════════════
// LIVE CAMERA MODE
// ═══════════════════════════════════════════
btnGoLive.addEventListener("click", async () => {
  if (currentSourceMode === "camera") {
    // Already live — this could be a "stop" action in the future
    showToast("Already in live camera mode", "warning");
    return;
  }

  showToast("Switching to live camera...", "warning", 3000);

  try {
    const res = await fetch("/api/source/live", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ device: 0 })
    });

    if (res.ok) {
      showToast("Live camera active!", "success");
      setSourceMode("camera", "Webcam /dev/video0");
    } else {
      const errText = await res.text();
      showToast("Failed to start camera: " + errText, "error");
    }
  } catch (err) {
    showToast("Camera error: " + err.message, "error");
  }
});

// ═══════════════════════════════════════════
// SOURCE MODE UI
// ═══════════════════════════════════════════
function setSourceMode(mode, name) {
  currentSourceMode = mode;

  if (mode === "camera") {
    sourceIcon.textContent = "🔴";
    sourceLabel.textContent = name || "Live Camera";
    liveBadge.style.display = "flex";
    btnGoLive.classList.add("active");
    statusPill.classList.add("live-mode");
    statusLabel.textContent = "LIVE TRACKING";
    footerSource.textContent = "Live Camera Mode";
  } else {
    sourceIcon.textContent = "📁";
    sourceLabel.textContent = name || "data/traffic.mp4";
    liveBadge.style.display = "none";
    btnGoLive.classList.remove("active");
    statusPill.classList.remove("live-mode");
    statusLabel.textContent = "RADAR ACTIVE";
    footerSource.textContent = "File Mode";
  }
}

// ═══════════════════════════════════════════
// TELEMETRY POLLING
// ═══════════════════════════════════════════
async function pollStatus() {
  try {
    const res = await fetch("/api/status");
    if (!res.ok) return;

    const data = await res.json();
    statFps.innerText = data.fps.toFixed(1);
    statActive.innerText = data.activeVehicles;
    statTotal.innerText = data.totalVehicles;
    statAvg.innerHTML = `${data.averageSpeedKmh.toFixed(1)} <span class="unit">km/h</span>`;
    statViolations.innerHTML = `${data.totalViolations} <span class="sub">(${data.violationPercentage.toFixed(0)}%)</span>`;
    displayLimit.innerText = data.speedLimitKmh;
    sliderSpeedLimit.value = data.speedLimitKmh;

    // Update source info from server
    if (data.sourceName && data.sourceMode) {
      if (data.sourceMode !== currentSourceMode) {
        setSourceMode(data.sourceMode, data.sourceName);
      }
    }

    // Show/hide calibration warning banner
    if (data.needsCalibration) {
      calibWarningBanner.style.display = "flex";
    }

    // New violation toast (throttled — only on count increase)
    if (data.totalViolations > lastViolationCount && lastViolationCount > 0) {
      const newCount = data.totalViolations - lastViolationCount;
      if (newCount <= 3) {
        showToast(`${newCount} new speed violation${newCount > 1 ? "s" : ""} detected!`, "violation", 2500);
      }
    }
    lastViolationCount = data.totalViolations;
  } catch (e) {
    // server starting up
  }
}

// ═══════════════════════════════════════════
// VIOLATIONS POLLING
// ═══════════════════════════════════════════
async function pollViolations() {
  try {
    const res = await fetch("/api/violations");
    if (!res.ok) return;

    const items = await res.json();
    if (!items || items.length === 0) return;

    violationsTbody.innerHTML = items.map(v => {
      const delta = (v.speedKmh - v.speedLimitKmh).toFixed(1);
      const deltaNum = parseFloat(delta);

      // Severity classification
      let severity = "low";
      if (deltaNum > 20) severity = "high";
      else if (deltaNum > 10) severity = "medium";

      const deltaClass = deltaNum > 15 ? "delta-tag severe" : "delta-tag";

      return `
        <tr class="severity-${severity}">
          <td>${v.timestamp}</td>
          <td>#${v.trackId}</td>
          <td>${v.className}</td>
          <td class="speed-tag-violation">${v.speedKmh.toFixed(1)} km/h</td>
          <td><span class="${deltaClass}">+${delta} km/h</span></td>
        </tr>
      `;
    }).join("");
  } catch (e) {
    // network retry
  }
}

// ═══════════════════════════════════════════
// CLOCK & POLLING INIT
// ═══════════════════════════════════════════
setInterval(() => {
  liveClock.innerText = new Date().toLocaleTimeString();
}, 1000);

setInterval(pollStatus, 400);
setInterval(pollViolations, 1000);
pollStatus();
pollViolations();

// Initial clock
liveClock.innerText = new Date().toLocaleTimeString();
