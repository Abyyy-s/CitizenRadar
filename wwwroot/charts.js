/**
 * CitizenRadar Charts Engine v4 — Dribbble "PricePilotAI" Replica
 * Includes:
 * 1. SegmentedSpeedometer (radiating pill-bar arc with gradient lighting)
 * 2. WaveTrendChart (flowing dual wave with interactive vertical highlight pillar)
 * 3. GradientBarChart (rounded capsule bars with gradient fill & hover tooltips)
 * 4. MicroSparkline (inline sparklines for metrics)
 */

'use strict';

// ═══════════════════════════════════════════
// 1. RADIATING SEGMENTED SPEEDOMETER GAUGE
// ═══════════════════════════════════════════
class SegmentedSpeedometer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.dpr = window.devicePixelRatio || 1;
    this.value = 0;        // current speed (km/h) or compliance %
    this.displayVal = 0;
    this.limit = 40;       // speed limit (km/h)
    this.max = 100;        // max gauge value
    this.numSegments = 22; // number of radiating pill segments
    this.mode = 'speed';   // 'speed' | 'compliance'

    // Dynamic animation states
    this.pulsePhase = 0;
    this.cascadeRatio = 0;
    this._isPulsing = false;

    this._resize();
    this.draw(0);

    const ro = new ResizeObserver(() => { this._resize(); this.draw(this.displayVal); });
    ro.observe(this.canvas.parentElement);
  }

  _resize() {
    const parent = this.canvas.parentElement;
    const w = parent.clientWidth || 240;
    const h = parent.clientHeight || 150;
    this.canvas.width = w * this.dpr;
    this.canvas.height = h * this.dpr;
    this.canvas.style.width = w + 'px';
    this.canvas.style.height = h + 'px';
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    this.w = w;
    this.h = h;
  }

  animateTo(targetVal) {
    const startVal = this.displayVal;
    const delta = targetVal - startVal;
    if (Math.abs(delta) < 0.1 && targetVal <= this.limit) return;

    const dur = 750;
    const t0 = performance.now();
    // Strong natural spring-like cubic ease
    const ease = t => (--t) * t * t + 1;

    const tick = (now) => {
      const p = Math.min((now - t0) / dur, 1);
      this.displayVal = startVal + delta * ease(p);
      this.pulsePhase += 0.08;

      this.draw(this.displayVal);

      // Keep animation loop alive if animating or if vehicle is in overspeed state (for breathing pulse)
      if (p < 1 || this.displayVal > this.limit) {
        requestAnimationFrame(tick);
      } else {
        this.displayVal = targetVal;
        this.draw(this.displayVal);
      }
    };
    requestAnimationFrame(tick);
  }

  draw(val) {
    const { ctx, w, h, numSegments, max, limit } = this;
    ctx.clearRect(0, 0, w, h);

    const isDark = document.body.dataset.theme === 'dark';
    const cx = w / 2;
    const cy = h - 18;
    const outerR = Math.min(w * 0.44, cy - 6);
    const innerR = outerR * 0.64;
    const segmentLength = outerR - innerR;

    // Angle span: from 195 deg to -15 deg (210 deg total arc)
    const startAngle = Math.PI * (195 / 180);
    const endAngle = Math.PI * (-15 / 180);

    // Active segments count
    const ratio = Math.max(0, Math.min(val / max, 1));
    const activeCount = Math.round(ratio * numSegments);
    const isCurrentOverspeed = val > limit;

    // Unlit segment color
    const unlitColor = isDark ? 'rgba(255, 255, 255, 0.08)' : 'rgba(110, 86, 207, 0.12)';

    // Breathing pulse calculation for overspeed
    const breathGlow = Math.sin(this.pulsePhase) * 4 + 8;

    for (let i = 0; i < numSegments; i++) {
      const t = i / (numSegments - 1);
      const angle = startAngle + t * (endAngle + Math.PI * 2 - startAngle);

      const isActive = i < activeCount;
      const speedAtSegment = (i / numSegments) * max;
      const isOverspeed = speedAtSegment > limit;

      // Color computation
      let segColor;
      if (isActive) {
        if (isOverspeed) {
          // Coral / Red warning for overspeed
          segColor = '#EF4444';
        } else {
          // Purple / Violet gradient
          const p = i / numSegments;
          const r = Math.round(155 - p * 45); // 155 -> 110
          const g = Math.round(130 - p * 44); // 130 -> 86
          const b = Math.round(250 - p * 43); // 250 -> 207
          segColor = `rgb(${r}, ${g}, ${b})`;
        }
      } else {
        segColor = unlitColor;
      }

      // Draw rounded radial pill segment
      ctx.save();
      ctx.translate(cx, cy);
      ctx.rotate(angle + Math.PI / 2);

      ctx.fillStyle = segColor;
      if (isActive && !isOverspeed) {
        ctx.shadowColor = 'rgba(110, 86, 207, 0.35)';
        ctx.shadowBlur = 6;
      } else if (isActive && isOverspeed) {
        // Breathing pulse glow when overspeed
        ctx.shadowColor = 'rgba(239, 68, 68, 0.6)';
        ctx.shadowBlur = isCurrentOverspeed ? breathGlow : 8;
      } else {
        ctx.shadowBlur = 0;
      }

      // Pill dimensions
      const pillWidth = Math.max(3.5, (outerR * 0.08));
      const pillHeight = segmentLength;
      const pillY = innerR;

      ctx.beginPath();
      ctx.roundRect(-pillWidth / 2, pillY, pillWidth, pillHeight, pillWidth / 2);
      ctx.fill();

      ctx.restore();
    }

    // Center display readout
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    // Speed / percentage number
    ctx.fillStyle = isDark ? '#FFFFFF' : '#111827';
    ctx.font = '700 28px "Inter", sans-serif';
    const displayNum = val.toFixed(1);
    ctx.fillText(`${displayNum}`, cx, cy - 28);

    // Unit / label
    ctx.fillStyle = isDark ? '#94A3B8' : '#6B7280';
    ctx.font = '500 11px "Inter", sans-serif';
    ctx.fillText(this.mode === 'speed' ? 'km/h Avg Speed' : 'Compliance Rate', cx, cy - 8);

    // Limit indicator subtext with dynamic alert badge
    ctx.font = '600 10.5px "Inter", sans-serif';
    if (val > limit) {
      ctx.fillStyle = '#EF4444';
      ctx.fillText(`OVER LIMIT (+${(val - limit).toFixed(1)} km/h)`, cx, cy + 8);
    } else {
      ctx.fillStyle = isDark ? '#38BDF8' : '#6E56CF';
      ctx.fillText(`Speed Limit: ${limit} km/h`, cx, cy + 6);
    }
  }
}

// ═══════════════════════════════════════════
// 2. WAVE TREND CHART WITH MAGNETIC GLIDE
// ═══════════════════════════════════════════
class WaveTrendChart {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.dpr = window.devicePixelRatio || 1;

    // Time series (e.g. 16 data points like in Dribbble reference)
    this.labels = ['10:00', '10:05', '10:10', '10:15', '10:20', '10:25', '10:30', '10:35', '10:40', '10:45', '10:50', '10:55', '11:00'];
    this.currentSpeed = [32, 38, 41, 36, 45, 52, 68, 74, 58, 42, 39, 44, 48];
    this.avgLimit = [40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40];

    // Highlight pillar index & smooth glide position
    this.highlightIndex = 7;
    this.currentHX = 0;
    this.targetHX = 0;
    this.isGlideActive = false;

    this._resize();
    this._bindEvents();
    this.draw();

    const ro = new ResizeObserver(() => { this._resize(); this.draw(); });
    ro.observe(this.canvas.parentElement);
  }

  _resize() {
    const parent = this.canvas.parentElement;
    const w = parent.clientWidth || 500;
    const h = parent.clientHeight || 220;
    this.canvas.width = w * this.dpr;
    this.canvas.height = h * this.dpr;
    this.canvas.style.width = w + 'px';
    this.canvas.style.height = h + 'px';
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    this.w = w;
    this.h = h;
  }

  _bindEvents() {
    this.canvas.addEventListener('mousemove', (e) => {
      const rect = this.canvas.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const pad = { l: 24, r: 24 };
      const cw = this.w - pad.l - pad.r;
      const step = cw / (this.currentSpeed.length - 1);
      const idx = Math.max(0, Math.min(Math.round((mouseX - pad.l) / step), this.currentSpeed.length - 1));
      
      if (idx !== this.highlightIndex) {
        this.highlightIndex = idx;
        this.triggerGlide();
      }
    });
  }

  triggerGlide() {
    if (this.isGlideActive) return;
    this.isGlideActive = true;

    const pad = { l: 24, r: 24 };
    const cw = this.w - pad.l - pad.r;
    const toX = (i) => pad.l + (i / (this.currentSpeed.length - 1)) * cw;

    const tickGlide = () => {
      this.targetHX = toX(this.highlightIndex);
      if (this.currentHX === 0) this.currentHX = this.targetHX;

      const diff = this.targetHX - this.currentHX;
      this.currentHX += diff * 0.24; // silky spring follow

      this.draw();

      if (Math.abs(diff) > 0.4) {
        requestAnimationFrame(tickGlide);
      } else {
        this.currentHX = this.targetHX;
        this.isGlideActive = false;
        this.draw();
      }
    };
    requestAnimationFrame(tickGlide);
  }

  push(currentVal, limitVal = 40) {
    this.currentSpeed.push(Math.round(currentVal));
    this.avgLimit.push(limitVal);
    if (this.currentSpeed.length > 16) {
      this.currentSpeed.shift();
      this.avgLimit.shift();
    }
    // Update labels to current time
    const now = new Date();
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    this.labels.push(timeStr);
    if (this.labels.length > 16) this.labels.shift();

    // Default highlight to max speed point
    let maxIdx = 0, maxVal = 0;
    this.currentSpeed.forEach((v, i) => { if (v > maxVal) { maxVal = v; maxIdx = i; } });
    this.highlightIndex = maxIdx;

    this.triggerGlide();
  }

  draw() {
    const { ctx, w, h, currentSpeed, avgLimit, labels, highlightIndex } = this;
    ctx.clearRect(0, 0, w, h);

    const isDark = document.body.dataset.theme === 'dark';
    const pad = { t: 36, r: 24, b: 32, l: 24 };
    const cw = w - pad.l - pad.r;
    const ch = h - pad.t - pad.b;

    const maxVal = Math.max(...currentSpeed, ...avgLimit, 80) * 1.15;
    const minVal = 0;

    const toX = (i) => pad.l + (i / (currentSpeed.length - 1)) * cw;
    const toY = (v) => pad.t + ch - ((v - minVal) / (maxVal - minVal)) * ch;

    // 1. Draw subtle horizontal grid lines
    ctx.strokeStyle = isDark ? 'rgba(255, 255, 255, 0.05)' : 'rgba(0, 0, 0, 0.04)';
    ctx.lineWidth = 1;
    for (let i = 0; i <= 3; i++) {
      const y = pad.t + (i / 3) * ch;
      ctx.beginPath();
      ctx.moveTo(pad.l, y);
      ctx.lineTo(w - pad.r, y);
      ctx.stroke();
    }

    // 2. Draw Highlight Pillar Column (the rounded purple capsule from the Dribbble design!)
    if (highlightIndex >= 0 && highlightIndex < currentSpeed.length) {
      const hx = this.currentHX || toX(highlightIndex);
      const hy = toY(currentSpeed[highlightIndex]);
      const pillW = Math.max(28, cw / currentSpeed.length * 0.9);

      // Gradient capsule background
      const pillarGrad = ctx.createLinearGradient(0, hy - 8, 0, pad.t + ch);
      pillarGrad.addColorStop(0, isDark ? 'rgba(110, 86, 207, 0.45)' : 'rgba(110, 86, 207, 0.25)');
      pillarGrad.addColorStop(1, isDark ? 'rgba(110, 86, 207, 0.05)' : 'rgba(110, 86, 207, 0.02)');

      ctx.fillStyle = pillarGrad;
      ctx.beginPath();
      ctx.roundRect(hx - pillW / 2, hy - 6, pillW, (pad.t + ch) - (hy - 6), pillW / 2);
      ctx.fill();

      // Border around pillar
      ctx.strokeStyle = isDark ? 'rgba(147, 128, 255, 0.4)' : 'rgba(110, 86, 207, 0.35)';
      ctx.lineWidth = 1;
      ctx.stroke();

      // Tooltip pill at top of highlight pillar
      const spdVal = currentSpeed[highlightIndex];
      const limVal = avgLimit[highlightIndex];
      const tooltipW = 76;
      const tooltipH = 34;
      const tooltipX = Math.min(Math.max(hx - tooltipW / 2, pad.l), w - pad.r - tooltipW);
      const tooltipY = Math.max(10, hy - tooltipH - 8);

      ctx.save();
      ctx.fillStyle = isDark ? '#1E1B4B' : '#FFFFFF';
      ctx.shadowColor = isDark ? 'rgba(0,0,0,0.5)' : 'rgba(110, 86, 207, 0.15)';
      ctx.shadowBlur = 10;
      ctx.shadowOffsetY = 4;
      ctx.beginPath();
      ctx.roundRect(tooltipX, tooltipY, tooltipW, tooltipH, 8);
      ctx.fill();

      ctx.strokeStyle = isDark ? '#4338CA' : '#E0E7FF';
      ctx.lineWidth = 1;
      ctx.stroke();
      ctx.restore();

      ctx.textAlign = 'center';
      ctx.fillStyle = isDark ? '#94A3B8' : '#6B7280';
      ctx.font = '500 8px "Inter", sans-serif';
      ctx.fillText(labels[highlightIndex] || 'Peak', tooltipX + tooltipW / 2, tooltipY + 11);

      ctx.fillStyle = spdVal > limVal ? '#EF4444' : '#6E56CF';
      ctx.font = '700 11px "Inter", sans-serif';
      ctx.fillText(`${spdVal} km/h`, tooltipX + tooltipW / 2, tooltipY + 24);
    }

    // 3. Draw Avg Speed Limit Dotted Line (light blue / grey)
    ctx.setLineDash([4, 4]);
    ctx.strokeStyle = isDark ? 'rgba(148, 163, 184, 0.4)' : '#94A3B8';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    for (let i = 0; i < avgLimit.length; i++) {
      const x = toX(i);
      const y = toY(avgLimit[i]);
      i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
    }
    ctx.stroke();
    ctx.setLineDash([]); // reset dash

    // 4. Draw Current Speed Wave Line (Rich Violet / Lavender)
    // Smooth bezier curve
    const pts = currentSpeed.map((v, i) => ({ x: toX(i), y: toY(v) }));

    // Area fill under current wave
    const waveGrad = ctx.createLinearGradient(0, pad.t, 0, pad.t + ch);
    waveGrad.addColorStop(0, isDark ? 'rgba(110, 86, 207, 0.28)' : 'rgba(110, 86, 207, 0.14)');
    waveGrad.addColorStop(1, 'rgba(110, 86, 207, 0.00)');

    ctx.beginPath();
    ctx.moveTo(pts[0].x, pad.t + ch);
    ctx.lineTo(pts[0].x, pts[0].y);
    for (let i = 1; i < pts.length; i++) {
      const midX = (pts[i - 1].x + pts[i].x) / 2;
      ctx.bezierCurveTo(midX, pts[i - 1].y, midX, pts[i].y, pts[i].x, pts[i].y);
    }
    ctx.lineTo(pts[pts.length - 1].x, pad.t + ch);
    ctx.closePath();
    ctx.fillStyle = waveGrad;
    ctx.fill();

    // The Stroke line
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < pts.length; i++) {
      const midX = (pts[i - 1].x + pts[i].x) / 2;
      ctx.bezierCurveTo(midX, pts[i - 1].y, midX, pts[i].y, pts[i].x, pts[i].y);
    }
    ctx.strokeStyle = '#6E56CF';
    ctx.lineWidth = 2.5;
    ctx.stroke();

    // Dotted drop indicators on points (subtle detail like in reference)
    ctx.fillStyle = isDark ? '#A78BFA' : '#6E56CF';
    pts.forEach((pt, idx) => {
      if (idx % 2 === 0 || idx === highlightIndex) {
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, idx === highlightIndex ? 4.5 : 2.5, 0, Math.PI * 2);
        ctx.fill();
      }
    });

    // 5. X-Axis Labels
    ctx.fillStyle = isDark ? '#94A3B8' : '#9CA3AF';
    ctx.font = '500 9px "Inter", sans-serif';
    ctx.textAlign = 'center';
    labels.forEach((lbl, i) => {
      if (i % 2 === 0 || i === labels.length - 1) {
        ctx.fillText(lbl, toX(i), h - 8);
      }
    });
  }
}

// ═══════════════════════════════════════════
// 3. GRADIENT BAR CHART (Rounded Pill Bars)
// ═══════════════════════════════════════════
class GradientBarChart {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.dpr = window.devicePixelRatio || 1;

    // 12 bars (e.g. hourly traffic volume)
    this.data = [42, 65, 88, 70, 95, 120, 160, 140, 195, 175, 130, 110];
    this.labels = ['8a', '9a', '10a', '11a', '12p', '1p', '2p', '3p', '4p', '5p', '6p', '7p'];
    this.hoverIdx = -1;

    this._resize();
    this._bindEvents();
    this.draw();

    const ro = new ResizeObserver(() => { this._resize(); this.draw(); });
    ro.observe(this.canvas.parentElement);
  }

  _resize() {
    const parent = this.canvas.parentElement;
    const w = parent.clientWidth || 360;
    const h = parent.clientHeight || 160;
    this.canvas.width = w * this.dpr;
    this.canvas.height = h * this.dpr;
    this.canvas.style.width = w + 'px';
    this.canvas.style.height = h + 'px';
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    this.w = w;
    this.h = h;
  }

  _bindEvents() {
    this.canvas.addEventListener('mousemove', (e) => {
      const rect = this.canvas.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const pad = { l: 16, r: 16 };
      const cw = this.w - pad.l - pad.r;
      const barSlot = cw / this.data.length;
      const idx = Math.floor((mouseX - pad.l) / barSlot);
      if (idx >= 0 && idx < this.data.length && idx !== this.hoverIdx) {
        this.hoverIdx = idx;
        this.draw();
      }
    });
    this.canvas.addEventListener('mouseleave', () => {
      this.hoverIdx = -1;
      this.draw();
    });
  }

  push(val) {
    this.data.push(val);
    if (this.data.length > 12) this.data.shift();
    this.draw();
  }

  draw() {
    const { ctx, w, h, data, labels, hoverIdx } = this;
    ctx.clearRect(0, 0, w, h);

    const isDark = document.body.dataset.theme === 'dark';
    const pad = { t: 20, r: 16, b: 24, l: 16 };
    const cw = w - pad.l - pad.r;
    const ch = h - pad.t - pad.b;

    const maxVal = Math.max(...data, 100) * 1.15;
    const slotW = cw / data.length;
    const barW = Math.max(8, slotW * 0.48);

    data.forEach((val, i) => {
      const bx = pad.l + i * slotW + (slotW - barW) / 2;
      const barH = (val / maxVal) * ch;
      const by = pad.t + ch - barH;

      const isHovered = i === hoverIdx;

      // Gradient: top is rich violet/purple, bottom is soft lavender
      const barGrad = ctx.createLinearGradient(0, by, 0, by + barH);
      if (isHovered) {
        barGrad.addColorStop(0, '#5B21B6');
        barGrad.addColorStop(1, '#A78BFA');
      } else {
        barGrad.addColorStop(0, isDark ? '#8B5CF6' : '#7C3AED');
        barGrad.addColorStop(1, isDark ? 'rgba(139, 92, 246, 0.25)' : '#DDD6FE');
      }

      ctx.fillStyle = barGrad;
      ctx.beginPath();
      // Fully rounded capsule on top
      ctx.roundRect(bx, by, barW, barH, [barW / 2, barW / 2, 4, 4]);
      ctx.fill();

      // If hovered, draw small tooltip
      if (isHovered) {
        ctx.fillStyle = isDark ? '#FFFFFF' : '#111827';
        ctx.font = '700 9px "Inter", sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(`${val}`, bx + barW / 2, by - 6);
      }

      // X labels
      ctx.fillStyle = isDark ? '#94A3B8' : '#9CA3AF';
      ctx.font = '500 8.5px "Inter", sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(labels[i] || '', bx + barW / 2, h - 8);
    });
  }
}

// ═══════════════════════════════════════════
// 4. ANIMATED NUMBER COUNTER HELPER
// ═══════════════════════════════════════════
function animateNumber(el, target, decimals = 0, prefix = '', suffix = '') {
  if (!el) return;
  const current = parseFloat(el.dataset.val || 0);
  if (Math.abs(current - target) < 0.05) return;

  const dur = 600;
  const t0 = performance.now();
  const ease = t => (--t) * t * t + 1; // easeOutCubic

  const tick = (now) => {
    const p = Math.min((now - t0) / dur, 1);
    const val = current + (target - current) * ease(p);
    el.textContent = `${prefix}${decimals > 0 ? val.toFixed(decimals) : Math.round(val)}${suffix}`;
    el.dataset.val = val;
    if (p < 1) requestAnimationFrame(tick);
    else {
      el.textContent = `${prefix}${decimals > 0 ? target.toFixed(decimals) : Math.round(target)}${suffix}`;
      el.dataset.val = target;
    }
  };
  requestAnimationFrame(tick);
}

// Expose on window
window.CR_Charts = {
  SegmentedSpeedometer,
  WaveTrendChart,
  GradientBarChart,
  animateNumber
};
