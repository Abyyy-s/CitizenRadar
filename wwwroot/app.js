/**
 * CitizenRadar Dashboard Logic v4 — Dribbble Edition
 * Connects telemetry to Segmented Speedometer, Wave Chart, Gradient Bar Chart,
 * Recent Alerts list, and keeps all camera calibration & speed controls active.
 */

'use strict';

// ═══════════════════════════════════════════
// 1. INITIALIZE CHART INSTANCES
// ═══════════════════════════════════════════
const speedometer = new CR_Charts.SegmentedSpeedometer(
  document.getElementById('segmented-speedometer-canvas')
);

const waveChart = new CR_Charts.WaveTrendChart(
  document.getElementById('wave-trend-canvas')
);

const barChart = new CR_Charts.GradientBarChart(
  document.getElementById('gradient-bars-canvas')
);

// ═══════════════════════════════════════════
// 2. DASHBOARD STATE
// ═══════════════════════════════════════════
let isCalibrating = false;
let calibrationPoints = [];
let currentSourceMode = 'file';
let lastViolationCount = 0;
let lastPeakSpeed = 0;
let prevTotalVehicles = 0;

const stepInstructions = [
  'Click <strong>Top-Left</strong> corner of road plane',
  'Click <strong>Top-Right</strong> corner of road plane',
  'Click <strong>Bottom-Right</strong> corner of road plane',
  'Click <strong>Bottom-Left</strong> corner of road plane',
  'Geometry complete! Review dimensions and save.'
];

// ═══════════════════════════════════════════
// 3. TAB NAVIGATION (Sliding Liquid Pill)
// ═══════════════════════════════════════════
const pillTabs = document.querySelectorAll('.pill-tab');
const tabContents = document.querySelectorAll('.tab-content');
const pillIndicator = document.getElementById('pill-indicator');

function updatePillIndicator(tabEl) {
  if (!pillIndicator || !tabEl) return;
  const left = tabEl.offsetLeft;
  const width = tabEl.offsetWidth;
  pillIndicator.style.transform = `translateX(${left}px)`;
  pillIndicator.style.width = `${width}px`;
}

function switchTab(targetTab) {
  let targetTabEl = null;
  pillTabs.forEach(tab => {
    const isTarget = tab.dataset.tab === targetTab;
    tab.classList.toggle('active', isTarget);
    if (isTarget) targetTabEl = tab;
  });

  if (targetTabEl) updatePillIndicator(targetTabEl);

  tabContents.forEach(content => {
    const isActive = content.id === `tab-${targetTab}`;
    content.hidden = !isActive;
    if (isActive) {
      content.classList.remove('active');
      void content.offsetWidth; // retrigger animation
      content.classList.add('active');
    }
  });

  // Re-draw charts when returning to Overview
  if (targetTab === 'overview') {
    setTimeout(() => {
      speedometer._resize();
      speedometer.draw(speedometer.displayVal);
      waveChart._resize();
      waveChart.draw();
      barChart._resize();
      barChart.draw();
    }, 50);
  }
}

pillTabs.forEach(tab => {
  tab.addEventListener('click', () => switchTab(tab.dataset.tab));
});

// Initial pill indicator positioning on load and resize
window.addEventListener('load', () => {
  const activeTab = document.querySelector('.pill-tab.active');
  if (activeTab) updatePillIndicator(activeTab);
});
window.addEventListener('resize', () => {
  const activeTab = document.querySelector('.pill-tab.active');
  if (activeTab) updatePillIndicator(activeTab);
});

// View all alerts button in Overview
document.getElementById('btn-view-all-alerts')?.addEventListener('click', () => {
  switchTab('violations');
});

// ═══════════════════════════════════════════
// 4. THEME & AUDIO TOGGLES
// ═══════════════════════════════════════════
const btnThemeToggle = document.getElementById('btn-theme-toggle');

function applyTheme(theme) {
  document.body.dataset.theme = theme;
  localStorage.setItem('cr_theme', theme);
  setTimeout(() => {
    speedometer.draw(speedometer.displayVal);
    waveChart.draw();
    barChart.draw();
  }, 30);
}

const savedTheme = localStorage.getItem('cr_theme') || 'light';
applyTheme(savedTheme);

btnThemeToggle?.addEventListener('click', () => {
  const nextTheme = document.body.dataset.theme === 'dark' ? 'light' : 'dark';
  applyTheme(nextTheme);
  showToast(`Switched to ${nextTheme === 'dark' ? 'Midnight Radar' : 'Dribbble Luxury'} theme`);
});

// Web Audio API Synthesizer (Radar Chime)
let soundEnabled = localStorage.getItem('cr_sound') === 'true';
const btnSoundToggle = document.getElementById('btn-sound-toggle');
const soundOnIcon = document.querySelector('.sound-on-icon');
const soundOffIcon = document.querySelector('.sound-off-icon');

function updateSoundUI() {
  if (!btnSoundToggle) return;
  btnSoundToggle.title = `Radar Sound Alerts (${soundEnabled ? 'On' : 'Off'})`;
  if (soundOnIcon) soundOnIcon.style.display = soundEnabled ? 'block' : 'none';
  if (soundOffIcon) soundOffIcon.style.display = soundEnabled ? 'none' : 'block';
}
updateSoundUI();

btnSoundToggle?.addEventListener('click', () => {
  soundEnabled = !soundEnabled;
  localStorage.setItem('cr_sound', soundEnabled);
  updateSoundUI();
  if (soundEnabled) {
    playRadarChime(587.33, 880); // F#5 -> A5 test tone
    showToast('Radar sound alerts enabled');
  } else {
    showToast('Radar sound alerts muted');
  }
});

let audioCtx = null;
function playRadarChime(freq1 = 587.33, freq2 = 880) {
  if (!soundEnabled) return;
  try {
    if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (audioCtx.state === 'suspended') audioCtx.resume();

    const now = audioCtx.currentTime;
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();

    osc.type = 'sine';
    osc.frequency.setValueAtTime(freq1, now);
    osc.frequency.exponentialRampToValueAtTime(freq2, now + 0.12);

    gain.gain.setValueAtTime(0.08, now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.38);

    osc.connect(gain);
    gain.connect(audioCtx.destination);

    osc.start(now);
    osc.stop(now + 0.4);
  } catch (e) { /* audio blocked */ }
}

// ═══════════════════════════════════════════
// 5. NOTIFICATION DROPDOWN
// ═══════════════════════════════════════════
const btnNotif = document.getElementById('btn-notif');
const notifPopover = document.getElementById('notif-popover');

btnNotif?.addEventListener('click', (e) => {
  e.stopPropagation();
  notifPopover.hidden = !notifPopover.hidden;
});

document.addEventListener('click', (e) => {
  if (!notifPopover.contains(e.target) && !btnNotif.contains(e.target)) {
    notifPopover.hidden = true;
  }
});

// ═══════════════════════════════════════════
// 6. TELEMETRY POLLING & STATS
// ═══════════════════════════════════════════
async function pollStatus() {
  try {
    const res = await fetch('/api/status');
    if (!res.ok) return;
    const data = await res.json();

    // FPS & Status
    const fpsText = `${data.fps.toFixed(1)} FPS`;
    document.getElementById('status-fps').textContent = fpsText;
    const feedFps = document.getElementById('feed-stat-fps');
    if (feedFps) feedFps.textContent = fpsText;

    // Inference Engine Badge
    const engineEl = document.getElementById('feed-stat-engine');
    if (engineEl && data.inferenceEngine) {
      engineEl.textContent = data.inferenceEngine;
      if (data.isGpuAccelerated) {
        engineEl.style.color = 'var(--neon-green)';
        engineEl.title = 'Accelerated by NVIDIA GeForce RTX 4050';
      } else {
        engineEl.style.color = 'var(--text-white)';
      }
    }

    // Frame counter
    const frameEl = document.getElementById('feed-frame-counter');
    if (frameEl) frameEl.textContent = `FRAME ${String(data.currentFrame || 0).padStart(6, '0')}`;

    // Top 4 Metrics on Overview (Smooth animated counters)
    CR_Charts.animateNumber(document.getElementById('stat-total'), data.totalVehicles);
    CR_Charts.animateNumber(document.getElementById('stat-active'), data.activeVehicles);
    CR_Charts.animateNumber(document.getElementById('stat-violations'), data.totalViolations);
    CR_Charts.animateNumber(document.getElementById('stat-avg'), data.averageSpeedKmh, 1);

    // Violation percentage tag
    const vioTag = document.getElementById('tag-vio-pct');
    if (vioTag) vioTag.textContent = `${data.violationPercentage.toFixed(0)}% rate`;

    // Active in frame tag
    const activeTag = document.getElementById('tag-active-trend');
    if (activeTag) activeTag.textContent = data.activeVehicles > 0 ? `● ${data.activeVehicles} in frame` : 'idle';

    // Speedometer animated update
    speedometer.limit = data.speedLimitKmh;
    speedometer.animateTo(data.averageSpeedKmh);
    const dispLimit = document.getElementById('disp-gauge-limit');
    if (dispLimit) dispLimit.textContent = `${data.speedLimitKmh} km/h`;

    // Wave trend chart update
    if (data.averageSpeedKmh > 0) {
      waveChart.push(data.averageSpeedKmh, data.speedLimitKmh);
    }

    // Traffic volume bar chart update
    if (data.totalVehicles > prevTotalVehicles) {
      const delta = data.totalVehicles - prevTotalVehicles;
      barChart.push(delta * 8); // scale for hourly representation
      prevTotalVehicles = data.totalVehicles;
    }
    const volTotal = document.getElementById('disp-volume-total');
    if (volTotal) volTotal.textContent = `${data.totalVehicles.toLocaleString()} total`;

    // Feed tab quick metrics
    const feedTot = document.getElementById('feed-stat-total');
    if (feedTot) feedTot.textContent = data.totalVehicles;
    const feedVio = document.getElementById('feed-stat-vio');
    if (feedVio) feedVio.textContent = data.totalViolations;

    // Calibration warning banner
    const calibBanner = document.getElementById('calib-warning-banner');
    if (calibBanner && data.needsCalibration) {
      calibBanner.hidden = false;
    }

    // New violation alert detection
    if (data.totalViolations > lastViolationCount && lastViolationCount > 0) {
      const diff = data.totalViolations - lastViolationCount;
      showToast(`${diff} new overspeed alert recorded!`, 'alert');
      playRadarChime(587.33, 880); // Subtle 2-tone luxury radar chime
    }
    lastViolationCount = data.totalViolations;

    // Update source name
    if (data.sourceName) {
      const srcEl = document.getElementById('feed-source-name');
      if (srcEl) srcEl.textContent = data.sourceName;
      const setSrc = document.getElementById('settings-source-detail');
      if (setSrc) setSrc.textContent = data.sourceName;
    }

  } catch (err) {
    // Retry silently
  }
}

// ═══════════════════════════════════════════
// 7. VIOLATIONS AUDIT & RECENT ALERTS
// ═══════════════════════════════════════════
async function pollViolations() {
  try {
    const res = await fetch('/api/violations');
    if (!res.ok) return;
    const items = await res.json();

    const count = items?.length || 0;

    // Badges & Counters
    const navCount = document.getElementById('nav-violation-count');
    if (navCount) navCount.textContent = count;
    const notifCount = document.getElementById('notif-count');
    if (notifCount) {
      notifCount.textContent = count;
      notifCount.hidden = count === 0;
    }
    const notifPopCount = document.getElementById('notif-pop-count');
    if (notifPopCount) notifPopCount.textContent = `${count} alerts`;
    const vioTableCount = document.getElementById('vio-table-count');
    if (vioTableCount) vioTableCount.textContent = `${count} events`;
    const vioFullTotal = document.getElementById('vio-full-total');
    if (vioFullTotal) vioFullTotal.textContent = count;

    if (!items || count === 0) return;

    // Calculate peaks & averages
    let peak = 0;
    let sumOver = 0;
    items.forEach(v => {
      if (v.speedKmh > peak) peak = v.speedKmh;
      sumOver += (v.speedKmh - v.speedLimitKmh);
    });

    if (peak > lastPeakSpeed) {
      lastPeakSpeed = peak;
      document.getElementById('disp-gauge-peak').textContent = `${peak.toFixed(1)} km/h`;
      document.getElementById('vio-full-peak').textContent = `${peak.toFixed(1)} km/h`;
      document.getElementById('an-peak-speed').textContent = `${peak.toFixed(1)} km/h`;
    }

    const avgOverEl = document.getElementById('vio-full-avg-over');
    if (avgOverEl) avgOverEl.textContent = `+${(sumOver / count).toFixed(1)} km/h`;

    // 1. Populate Overview "Recent Alerts" list (matching Dribbble style with click inspection)
    const recentAlertsList = document.getElementById('recent-alerts-list');
    if (recentAlertsList) {
      recentAlertsList.innerHTML = items.slice(0, 4).map(v => {
        const over = (v.speedKmh - v.speedLimitKmh).toFixed(1);
        return `
          <li class="alert-item" onclick="openSnapshotModal(${v.trackId}, '${v.className || 'Car'}', ${v.speedKmh}, ${v.speedLimitKmh}, '${v.timestamp}')" title="Click to inspect radar telemetry">
            <span class="alert-icon-ring">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>
            </span>
            <div class="alert-info">
              <strong>${v.className || 'Vehicle'} #${v.trackId} — ${v.speedKmh.toFixed(1)} km/h</strong>
              <small>Limit: ${v.speedLimitKmh} km/h · ${v.timestamp}</small>
            </div>
            <div class="alert-meta">
              <span class="alert-badge">+${over}</span>
              <span class="alert-time">recently</span>
            </div>
          </li>
        `;
      }).join('');
    }

    // 2. Populate Notification Popover list
    const notifList = document.getElementById('notif-list');
    if (notifList) {
      notifList.innerHTML = items.slice(0, 6).map(v => `
        <li class="notif-item" onclick="openSnapshotModal(${v.trackId}, '${v.className || 'Car'}', ${v.speedKmh}, ${v.speedLimitKmh}, '${v.timestamp}')" style="cursor:pointer">
          <strong>#${v.trackId} ${v.className}</strong>
          <span>${v.speedKmh.toFixed(1)} km/h (+${(v.speedKmh - v.speedLimitKmh).toFixed(1)})</span>
        </li>
      `).join('');
    }

    // 3. Populate Full Violations Audit Table with click inspection
    const fullTbody = document.getElementById('violations-tbody-full');
    if (fullTbody) {
      fullTbody.innerHTML = items.map(v => {
        const delta = (v.speedKmh - v.speedLimitKmh).toFixed(1);
        return `
          <tr onclick="openSnapshotModal(${v.trackId}, '${v.className || 'Car'}', ${v.speedKmh}, ${v.speedLimitKmh}, '${v.timestamp}')" title="Click to inspect telemetry">
            <td>${v.timestamp}</td>
            <td><strong>#${v.trackId}</strong></td>
            <td>${v.className}</td>
            <td class="text-red"><strong>${v.speedKmh.toFixed(1)} km/h</strong></td>
            <td>${v.speedLimitKmh} km/h</td>
            <td><span class="alert-badge">+${delta} km/h</span></td>
          </tr>
        `;
      }).join('');
    }

  } catch (err) {
    // Retry silently
  }
}

// ═══════════════════════════════════════════
// SNAPSHOT INSPECTION MODAL
// ═══════════════════════════════════════════
const snapshotModal = document.getElementById('snapshot-modal');
if (snapshotModal) {
  snapshotModal.hidden = true;
  snapshotModal.style.display = 'none';
}
const modalVehicleTitle = document.getElementById('modal-vehicle-title');
const modalVehicleSubtitle = document.getElementById('modal-vehicle-subtitle');
const modalHudTag = document.getElementById('modal-hud-tag');
const modalHudSpeed = document.getElementById('modal-hud-speed');
const modalTelemTime = document.getElementById('modal-telem-time');
const modalTelemSpeed = document.getElementById('modal-telem-speed');
const modalTelemLimit = document.getElementById('modal-telem-limit');
const modalTelemOver = document.getElementById('modal-telem-over');
let currentInspectedTrack = null;

function openSnapshotModal(trackId, className, speedKmh, speedLimitKmh, timestamp) {
  if (!snapshotModal) return;
  currentInspectedTrack = { trackId, className, speedKmh, speedLimitKmh, timestamp };
  
  const cls = className || 'Vehicle';
  const over = (speedKmh - speedLimitKmh).toFixed(1);

  if (modalVehicleTitle) modalVehicleTitle.textContent = `${cls} #${trackId} — Speed Violation`;
  if (modalVehicleSubtitle) modalVehicleSubtitle.textContent = `Detected at ${speedKmh.toFixed(1)} km/h in ${speedLimitKmh} km/h zone`;
  if (modalHudTag) modalHudTag.textContent = `ID: #${trackId} [${cls.toUpperCase()}]`;
  if (modalHudSpeed) modalHudSpeed.textContent = `${speedKmh.toFixed(1)} km/h`;
  if (modalTelemTime) modalTelemTime.textContent = timestamp || '--:--:--';
  if (modalTelemSpeed) modalTelemSpeed.textContent = `${speedKmh.toFixed(1)} km/h`;
  if (modalTelemLimit) modalTelemLimit.textContent = `${speedLimitKmh} km/h`;
  if (modalTelemOver) modalTelemOver.textContent = `+${over} km/h`;

  snapshotModal.hidden = false;
  snapshotModal.style.display = 'flex';
}

function closeSnapshotModal() {
  if (snapshotModal) {
    snapshotModal.hidden = true;
    snapshotModal.style.display = 'none';
  }
}

// Expose globally for inline onclick failsafes
window.openSnapshotModal = openSnapshotModal;
window.closeSnapshotModal = closeSnapshotModal;

document.getElementById('btn-modal-close')?.addEventListener('click', (e) => {
  e.stopPropagation();
  closeSnapshotModal();
});
document.getElementById('btn-modal-dismiss')?.addEventListener('click', (e) => {
  e.stopPropagation();
  closeSnapshotModal();
});
snapshotModal?.addEventListener('click', (e) => {
  if (e.target === snapshotModal) closeSnapshotModal();
});
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && !snapshotModal?.hidden) closeSnapshotModal();
});

document.getElementById('btn-modal-export')?.addEventListener('click', () => {
  if (!currentInspectedTrack) return;
  const t = currentInspectedTrack;
  const citation = `CITIZENRADAR TRAFFIC VIOLATION CITATION\n` +
    `-----------------------------------------\n` +
    `Timestamp: ${t.timestamp}\n` +
    `Track ID: #${t.trackId}\n` +
    `Classification: ${t.className}\n` +
    `Measured Speed: ${t.speedKmh.toFixed(1)} km/h\n` +
    `Enforced Limit: ${t.speedLimitKmh} km/h\n` +
    `Overage: +${(t.speedKmh - t.speedLimitKmh).toFixed(1)} km/h\n` +
    `Node: FIELD NODE 01 (Calibrated Homography Matrix)\n`;

  const blob = new Blob([citation], { type: 'text/plain' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `Citation_Track_${t.trackId}_${Date.now()}.txt`;
  a.click();
  URL.revokeObjectURL(url);
  showToast(`Citation exported for Track #${t.trackId}`);
});

// ═══════════════════════════════════════════
// 8. SPEED POLICY & PRESETS
// ═══════════════════════════════════════════
async function updateSpeedLimit(newLimit) {
  const displayLimit = document.getElementById('display-limit');
  if (displayLimit) displayLimit.textContent = newLimit;

  const limitOutput = document.getElementById('limit-output');
  if (limitOutput) limitOutput.textContent = `${newLimit} km/h`;

  const settingsLabel = document.getElementById('settings-limit-label');
  if (settingsLabel) settingsLabel.textContent = `${newLimit} km/h`;

  document.querySelectorAll('#slider-speed-limit, #settings-slider').forEach(s => {
    s.value = newLimit;
  });

  document.querySelectorAll('.preset-chip').forEach(btn => {
    btn.classList.toggle('active', parseFloat(btn.dataset.speed) === newLimit);
  });

  speedometer.limit = newLimit;
  speedometer.draw(speedometer.displayVal);

  try {
    await fetch('/api/config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ speedLimitKmh: newLimit })
    });
  } catch (e) { /* ignore */ }
}

document.querySelectorAll('.preset-chip').forEach(btn => {
  btn.addEventListener('click', () => {
    updateSpeedLimit(parseFloat(btn.dataset.speed));
  });
});

document.getElementById('slider-speed-limit')?.addEventListener('input', (e) => {
  updateSpeedLimit(parseFloat(e.target.value));
});
document.getElementById('settings-slider')?.addEventListener('input', (e) => {
  updateSpeedLimit(parseFloat(e.target.value));
});

// ═══════════════════════════════════════════
// 9. VIDEO UPLOAD & WEBCAM STREAM
// ═══════════════════════════════════════════
const fileUploadInput = document.getElementById('file-upload-input');
const btnFeedUpload = document.getElementById('btn-feed-upload');
const btnSettingsUpload = document.getElementById('settings-upload-btn');
const btnFeedLive = document.getElementById('btn-feed-live');
const btnSettingsWebcam = document.getElementById('settings-webcam-btn');

btnFeedUpload?.addEventListener('click', () => fileUploadInput?.click());
btnSettingsUpload?.addEventListener('click', () => fileUploadInput?.click());

fileUploadInput?.addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;
  await handleFileUpload(file);
  fileUploadInput.value = '';
});

async function handleFileUpload(file) {
  const formData = new FormData();
  formData.append('file', file);
  showToast(`Uploading ${file.name}...`);
  try {
    const res = await fetch('/api/source/upload', { method: 'POST', body: formData });
    if (res.ok) {
      const data = await res.json();
      showToast(`Analyzing source: ${data.source}`);
      document.getElementById('feed-source-name').textContent = data.source;
    } else {
      showToast('Upload failed: ' + await res.text());
    }
  } catch (err) {
    showToast('Error: ' + err.message);
  }
}

// Drag & Drop
const videoWrapper = document.getElementById('video-wrapper');
const dropOverlay = document.getElementById('drop-overlay');

videoWrapper?.addEventListener('dragenter', (e) => { e.preventDefault(); dropOverlay?.classList.add('visible'); });
videoWrapper?.addEventListener('dragover', (e) => e.preventDefault());
dropOverlay?.addEventListener('dragleave', (e) => { e.preventDefault(); dropOverlay?.classList.remove('visible'); });
dropOverlay?.addEventListener('drop', async (e) => {
  e.preventDefault();
  dropOverlay?.classList.remove('visible');
  const file = e.dataTransfer.files[0];
  if (file) await handleFileUpload(file);
});

// Switch to Webcam
async function switchToLiveWebcam() {
  showToast('Switching to live webcam /dev/video0...');
  try {
    const res = await fetch('/api/source/live', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ device: 0 })
    });
    if (res.ok) {
      showToast('Live camera feed active!');
      document.getElementById('feed-source-name').textContent = 'Webcam /dev/video0';
    } else {
      showToast('Failed to switch camera: ' + await res.text());
    }
  } catch (err) {
    showToast('Camera error: ' + err.message);
  }
}

btnFeedLive?.addEventListener('click', switchToLiveWebcam);
btnSettingsWebcam?.addEventListener('click', switchToLiveWebcam);

// ═══════════════════════════════════════════
// 10. ROAD PLANE CALIBRATION
// ═══════════════════════════════════════════
const calibCanvas = document.getElementById('calibration-canvas');
const ctxCalib = calibCanvas?.getContext('2d');
const btnToggleCalib = document.getElementById('btn-toggle-calib');
const btnCalibText = document.getElementById('btn-calib-text');
const btnResetCalib = document.getElementById('btn-reset-calib');
const calibBanner = document.getElementById('calibration-banner');
const calibControls = document.getElementById('calibration-controls');
const calibStepNum = document.getElementById('calib-step-num');
const calibInstruction = document.getElementById('calib-instruction');
const btnSaveCalib = document.getElementById('btn-save-calib');
const liveImg = document.getElementById('live-stream-img');

function syncCanvasSize() {
  if (!liveImg || !calibCanvas) return;
  const rect = liveImg.getBoundingClientRect();
  if (rect.width > 0 && rect.height > 0) {
    calibCanvas.width = rect.width;
    calibCanvas.height = rect.height;
    drawCalibPoints();
  }
}

window.addEventListener('resize', syncCanvasSize);
liveImg?.addEventListener('load', syncCanvasSize);

btnToggleCalib?.addEventListener('click', () => {
  isCalibrating = !isCalibrating;
  if (isCalibrating) {
    btnCalibText.innerText = 'Exit Calibration';
    btnResetCalib.hidden = false;
    calibBanner.hidden = false;
    calibControls.hidden = false;
    calibCanvas.classList.add('active');
    calibrationPoints = [];
    updateCalibGuide();
    syncCanvasSize();
  } else {
    resetCalibUI();
  }
});

btnResetCalib?.addEventListener('click', () => {
  calibrationPoints = [];
  updateCalibGuide();
  drawCalibPoints();
});

function resetCalibUI() {
  isCalibrating = false;
  btnCalibText.innerText = 'Calibrate Road';
  btnResetCalib.hidden = true;
  calibBanner.hidden = true;
  calibControls.hidden = true;
  calibCanvas.classList.remove('active');
  calibrationPoints = [];
  ctxCalib?.clearRect(0, 0, calibCanvas.width, calibCanvas.height);
}

calibCanvas?.addEventListener('click', (e) => {
  if (!isCalibrating || calibrationPoints.length >= 4) return;
  const rect = calibCanvas.getBoundingClientRect();
  const clickX = e.clientX - rect.left;
  const clickY = e.clientY - rect.top;
  const normX = (clickX / rect.width) * 960.0;
  const normY = (clickY / rect.height) * 540.0;

  calibrationPoints.push({ canvasX: clickX, canvasY: clickY, videoX: normX, videoY: normY });
  updateCalibGuide();
  drawCalibPoints();
});

function updateCalibGuide() {
  const count = calibrationPoints.length;
  if (calibStepNum) calibStepNum.innerText = `STEP ${Math.min(count + 1, 4)}`;
  if (calibInstruction) calibInstruction.innerHTML = stepInstructions[count] || stepInstructions[4];
  for (let i = 1; i <= 4; i++) {
    document.getElementById(`pdot-${i}`)?.classList.toggle('done', i <= count);
  }
  if (btnSaveCalib) btnSaveCalib.disabled = count < 4;
}

function drawCalibPoints() {
  if (!ctxCalib) return;
  ctxCalib.clearRect(0, 0, calibCanvas.width, calibCanvas.height);
  if (!calibrationPoints.length) return;

  ctxCalib.lineWidth = 2;
  ctxCalib.strokeStyle = '#6E56CF';
  ctxCalib.beginPath();
  calibrationPoints.forEach((pt, i) => {
    i === 0 ? ctxCalib.moveTo(pt.canvasX, pt.canvasY) : ctxCalib.lineTo(pt.canvasX, pt.canvasY);
  });
  if (calibrationPoints.length === 4) {
    ctxCalib.closePath();
    ctxCalib.fillStyle = 'rgba(110, 86, 207, 0.18)';
    ctxCalib.fill();
  }
  ctxCalib.stroke();

  calibrationPoints.forEach((pt, i) => {
    ctxCalib.fillStyle = '#EF4444';
    ctxCalib.beginPath();
    ctxCalib.arc(pt.canvasX, pt.canvasY, 5, 0, Math.PI * 2);
    ctxCalib.fill();
    ctxCalib.fillStyle = '#FFFFFF';
    ctxCalib.font = 'bold 10px Inter, sans-serif';
    ctxCalib.fillText(`P${i + 1}`, pt.canvasX + 8, pt.canvasY - 4);
  });
}

btnSaveCalib?.addEventListener('click', async () => {
  if (calibrationPoints.length < 4) return;
  const payload = {
    sourcePoints: calibrationPoints.map(p => ({ x: p.videoX, y: p.videoY })),
    realWorldWidthMetres: parseFloat(document.getElementById('input-road-width').value) || 10.0,
    realWorldHeightMetres: parseFloat(document.getElementById('input-road-length').value) || 50.0
  };
  try {
    const res = await fetch('/api/calibrate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (res.ok) {
      showToast('Road calibration applied successfully!');
      document.getElementById('calib-warning-banner').hidden = true;
      resetCalibUI();
    } else {
      showToast('Failed to save calibration: ' + await res.text());
    }
  } catch (err) {
    showToast('Error: ' + err.message);
  }
});

document.getElementById('calib-warning-dismiss')?.addEventListener('click', () => {
  document.getElementById('calib-warning-banner').hidden = true;
});

// ═══════════════════════════════════════════
// 11. CSV EXPORT
// ═══════════════════════════════════════════
async function exportViolationsCSV() {
  try {
    const res = await fetch('/api/violations');
    if (!res.ok) return;
    const items = await res.json();
    if (!items || !items.length) {
      showToast('No violation records to export');
      return;
    }

    const header = 'Timestamp,TrackID,VehicleClass,SpeedKmh,SpeedLimitKmh,OverageKmh\n';
    const rows = items.map(v =>
      `${v.timestamp},${v.trackId},${v.className},${v.speedKmh.toFixed(1)},${v.speedLimitKmh},${(v.speedKmh - v.speedLimitKmh).toFixed(1)}`
    ).join('\n');

    const blob = new Blob([header + rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `CitizenRadar_Violations_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
    showToast(`Exported ${items.length} records to CSV`);
  } catch (err) {
    showToast('Export failed: ' + err.message);
  }
}

document.getElementById('btn-export-csv-full')?.addEventListener('click', exportViolationsCSV);

// ═══════════════════════════════════════════
// 12. TOAST SYSTEM
// ═══════════════════════════════════════════
const toastContainer = document.getElementById('toast-container');

function showToast(message, type = 'info') {
  if (!toastContainer) return;
  const bubble = document.createElement('div');
  bubble.className = 'toast-bubble';
  bubble.innerHTML = `<span>${message}</span>`;
  toastContainer.appendChild(bubble);

  setTimeout(() => {
    bubble.classList.add('toast-out');
    setTimeout(() => bubble.remove(), 250);
  }, 3200);
}

// ═══════════════════════════════════════════
// 13. INITIALIZE POLLING
// ═══════════════════════════════════════════
setInterval(pollStatus, 500);
setInterval(pollViolations, 1500);
pollStatus();
pollViolations();
