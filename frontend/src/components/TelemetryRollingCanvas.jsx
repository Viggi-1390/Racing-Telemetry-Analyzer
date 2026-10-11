import React, { useRef, useEffect, useState, useCallback } from 'react';

const SCRUBBER_X_RATIO = 0.80; // Pinned scrubber at 80% across canvas

export default function TelemetryRollingCanvas({
  telemetryData = [],
  selectedLapNumber = 1,
  onLapChange,
  onTelemetryTick,
}) {
  const canvasRef = useRef(null);
  const containerRef = useRef(null);
  const tooltipRef = useRef(null);

  // Direct HUD readout DOM refs (prevents 60 FPS React re-renders)
  const timeDisplayRef = useRef(null);
  const lapBadgeRef = useRef(null);
  const distDisplayRef = useRef(null);
  const speedDisplayRef = useRef(null);
  const sliderRef = useRef(null);

  // Top channel live value spans
  const tbSpeedRef = useRef(null);
  const tbRpmRef = useRef(null);
  const tbThrRef = useRef(null);
  const tbBrkRef = useRef(null);
  const tbStrRef = useRef(null);

  // Tooltip HUD readout refs
  const thLapRef = useRef(null);
  const thDistRef = useRef(null);
  const thTimeRef = useRef(null);
  const thSpeedRef = useRef(null);
  const thRpmRef = useRef(null);
  const thThrRef = useRef(null);
  const thThrBarRef = useRef(null);
  const thBrkRef = useRef(null);
  const thBrkBarRef = useRef(null);
  const thSteerRef = useRef(null);

  // React state for user controls
  const [isPlaying, setIsPlaying] = useState(false);
  const [playbackRate, setPlaybackRate] = useState(1);
  const [isRollingMode, setIsRollingMode] = useState(true);
  const [windowDistance, setWindowDistance] = useState(1000.0);
  const [channels, setChannels] = useState({
    speed: true,
    rpm: true,
    throttle: true,
    brake: true,
    steering: true,
  });

  // Mutable animation engine state kept in refs for 60 FPS performance
  const engineRef = useRef({
    currentIndex: 0,
    fractionalIndex: 0,
    lastFrameTime: 0,
    animFrameId: null,
    isPlaying: false,
    playbackRate: 1,
    isRollingMode: true,
    windowDistance: 1000.0,
    currentLap: selectedLapNumber || 1,
    activeLapPoints: [],
    lapTotalDistance: 2802,
    windowStartDist: 0,
    windowEndDist: 2802,
    isHovering: false,
    hoverCanvasX: null,
    hoverPoint: null,
    channels: { speed: true, rpm: true, throttle: true, brake: true, steering: true },
  });

  // Sync state to ref
  useEffect(() => {
    engineRef.current.channels = channels;
    renderCanvas();
  }, [channels]);

  useEffect(() => {
    engineRef.current.playbackRate = playbackRate;
  }, [playbackRate]);

  useEffect(() => {
    engineRef.current.isRollingMode = isRollingMode;
    renderCanvas();
  }, [isRollingMode]);

  useEffect(() => {
    engineRef.current.windowDistance = windowDistance;
    renderCanvas();
  }, [windowDistance]);

  // Format seconds to mm:ss.fff
  function formatSeconds(sec) {
    if (isNaN(sec) || sec < 0) sec = 0;
    const mins = Math.floor(sec / 60);
    const remainderSec = (sec % 60).toFixed(3);
    return (mins < 10 ? '0' : '') + mins + ':' + (remainderSec < 10 ? '0' : '') + remainderSec;
  }

  // Binary search utilities
  function binarySearchLower(dataset, targetDist) {
    let low = 0, high = dataset.length - 1, ans = 0;
    while (low <= high) {
      const mid = (low + high) >> 1;
      if (dataset[mid].lapDist >= targetDist) {
        ans = mid;
        high = mid - 1;
      } else {
        low = mid + 1;
      }
    }
    return ans;
  }

  function binarySearchUpper(dataset, targetDist) {
    let low = 0, high = dataset.length - 1, ans = dataset.length - 1;
    while (low <= high) {
      const mid = (low + high) >> 1;
      if (dataset[mid].lapDist <= targetDist) {
        ans = mid;
        low = mid + 1;
      } else {
        high = mid - 1;
      }
    }
    return ans;
  }

  // Linear interpolation for hover inspection
  function getInterpolatedPointAtDist(activePoints, dist) {
    if (!activePoints || activePoints.length === 0) return null;
    if (dist <= activePoints[0].lapDist) return activePoints[0];
    if (dist >= activePoints[activePoints.length - 1].lapDist) return activePoints[activePoints.length - 1];

    let low = 0, high = activePoints.length - 1;
    while (low <= high) {
      const mid = (low + high) >> 1;
      if (activePoints[mid].lapDist < dist) {
        low = mid + 1;
      } else {
        high = mid - 1;
      }
    }
    const idx2 = Math.min(activePoints.length - 1, Math.max(0, low));
    const idx1 = Math.max(0, idx2 - 1);
    const p1 = activePoints[idx1];
    const p2 = activePoints[idx2];
    if (p1 === p2 || p2.lapDist === p1.lapDist) return p1;

    const frac = Math.max(0, Math.min(1, (dist - p1.lapDist) / (p2.lapDist - p1.lapDist)));
    const lerp = (a, b) => a + (b - a) * frac;

    return {
      lap: p1.lap,
      lapDist: dist,
      t: lerp(p1.t ?? 0, p2.t ?? 0),
      speed: lerp(p1.speed, p2.speed),
      rpm: lerp(p1.rpm, p2.rpm),
      thr: lerp(p1.thr, p2.thr),
      brk: lerp(p1.brk, p2.brk),
      steer: lerp(p1.steer, p2.steer),
      gear: frac < 0.5 ? p1.gear : p2.gear,
      gLat: p1.gLat != null ? lerp(p1.gLat, p2.gLat ?? p1.gLat) : null,
      gLon: p1.gLon != null ? lerp(p1.gLon, p2.gLon ?? p1.gLon) : null,
      gVert: p1.gVert != null ? lerp(p1.gVert, p2.gVert ?? p1.gVert) : null,
      suspFL: p1.suspFL != null ? lerp(p1.suspFL, p2.suspFL ?? p1.suspFL) : null,
      suspFR: p1.suspFR != null ? lerp(p1.suspFR, p2.suspFR ?? p1.suspFR) : null,
      suspRL: p1.suspRL != null ? lerp(p1.suspRL, p2.suspRL ?? p1.suspRL) : null,
      suspRR: p1.suspRR != null ? lerp(p1.suspRR, p2.suspRR ?? p1.suspRR) : null,
    };
  }

  // Update active lap data
  const updateLapFilter = useCallback((lapNum, resetIndex = false) => {
    const e = engineRef.current;
    e.currentLap = lapNum || 1;
    e.activeLapPoints = telemetryData.filter((p) => p.lap === e.currentLap);
    if (e.activeLapPoints.length === 0) {
      e.activeLapPoints = telemetryData;
    }

    const maxD = e.activeLapPoints.length > 0 ? e.activeLapPoints[e.activeLapPoints.length - 1].lapDist : 0;
    e.lapTotalDistance = maxD > 0 ? maxD : 2802;

    if (resetIndex) {
      const startIdx = telemetryData.findIndex((p) => p.lap === e.currentLap);
      e.currentIndex = startIdx !== -1 ? startIdx : 0;
      e.fractionalIndex = e.currentIndex;
    }

    if (sliderRef.current) {
      sliderRef.current.max = Math.max(1, telemetryData.length - 1);
      sliderRef.current.value = e.currentIndex;
    }

    if (lapBadgeRef.current) {
      lapBadgeRef.current.textContent = e.currentLap;
    }
  }, [telemetryData]);

  // Initial and data update setup
  useEffect(() => {
    if (telemetryData.length > 0) {
      updateLapFilter(selectedLapNumber, false);
      renderCanvas();
    }
  }, [telemetryData, selectedLapNumber, updateLapFilter]);

  // Handle Resize
  function resizeCanvas() {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    canvas.width = Math.round(rect.width * dpr);
    canvas.height = Math.round(rect.height * dpr);
    renderCanvas();
  }

  useEffect(() => {
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);
    return () => window.removeEventListener('resize', resizeCanvas);
  }, []);

  // Main Canvas Rendering Method (Runs at 60 FPS)
  function renderCanvas() {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const dpr = window.devicePixelRatio || 1;
    const width = canvas.width / dpr;
    const height = canvas.height / dpr;

    ctx.save();
    ctx.scale(dpr, dpr);

    // Clear background
    ctx.fillStyle = '#08080d';
    ctx.fillRect(0, 0, width, height);

    const e = engineRef.current;
    if (!telemetryData || telemetryData.length === 0 || e.activeLapPoints.length === 0) {
      ctx.restore();
      return;
    }

    const currentPt = telemetryData[e.currentIndex] || e.activeLapPoints[0] || { lapDist: 0, speed: 0, rpm: 0, thr: 0, brk: 0, steer: 0 };
    const currentDist = currentPt.lapDist;

    let windowStartDist, windowEndDist;
    if (e.isRollingMode) {
      windowStartDist = currentDist - (e.windowDistance * SCRUBBER_X_RATIO);
      windowEndDist = windowStartDist + e.windowDistance;
    } else {
      windowStartDist = 0;
      windowEndDist = e.lapTotalDistance;
    }

    e.windowStartDist = windowStartDist;
    e.windowEndDist = windowEndDist;

    // Draw Grid & X-Axis
    drawGrid(ctx, width, height, windowStartDist, windowEndDist, e.currentLap, e.lapTotalDistance);

    // Viewport Virtualization
    const startIdx = binarySearchLower(e.activeLapPoints, windowStartDist);
    const endIdx = binarySearchUpper(e.activeLapPoints, windowEndDist);

    if (startIdx <= endIdx && startIdx < e.activeLapPoints.length) {
      const slice = e.activeLapPoints.slice(startIdx, endIdx + 1);
      const span = Math.max(1, windowEndDist - windowStartDist);
      const getX = (dist) => ((dist - windowStartDist) / span) * width;

      // Channel 1: Speed (Green #00ff41, 0-400 km/h) -> Top 45%
      if (e.channels.speed) {
        drawCurve(ctx, slice, getX, (p) => (height * 0.44) * (1.0 - (p.speed / 400.0)) + 4, '#00ff41', 2.0);
      }

      // Channel 2: RPM (Purple #a200ff, 0-15000 RPM) -> Top 45%
      if (e.channels.rpm) {
        drawCurve(ctx, slice, getX, (p) => (height * 0.44) * (1.0 - (p.rpm / 15000.0)) + 4, '#a200ff', 1.5);
      }

      // Channel 3: Throttle (Cyan #00d2ff, 0-100%) -> Bottom 48%
      if (e.channels.throttle) {
        drawCurve(ctx, slice, getX, (p) => (height * 0.52) + (height * 0.42) * (1.0 - (p.thr / 100.0)), '#00d2ff', 1.5);
      }

      // Channel 4: Brake (Red #ff0000, 0-100%) -> Bottom 48%
      if (e.channels.brake) {
        drawCurve(ctx, slice, getX, (p) => (height * 0.52) + (height * 0.42) * (1.0 - (p.brk / 100.0)), '#ff0000', 1.8);
      }

      // Channel 5: Steering (Magenta #ff00ff, ±50°)
      if (e.channels.steering) {
        drawCurve(ctx, slice, getX, (p) => (height * 0.52) + (height * 0.42) * (0.5 - (p.steer / 100.0)), '#ff00ff', 1.5);
      }
    }

    // Scrubber vertical line
    const viewSpan = Math.max(1, windowEndDist - windowStartDist);
    const scrubberX = e.isRollingMode
      ? (SCRUBBER_X_RATIO * width)
      : (((currentDist - windowStartDist) / viewSpan) * width);

    ctx.strokeStyle = '#ff5a1f';
    ctx.lineWidth = 2.0;
    ctx.shadowColor = '#ff5a1f';
    ctx.shadowBlur = 8;
    ctx.beginPath();
    ctx.moveTo(scrubberX, 0);
    ctx.lineTo(scrubberX, height);
    ctx.stroke();
    ctx.shadowBlur = 0; // reset

    // Oscilloscope Hover Guide Line & Curve Target Dots
    if (e.isHovering && e.hoverCanvasX !== null && e.hoverPoint) {
      const hX = e.hoverCanvasX;
      const hp = e.hoverPoint;

      // Vertical guide line
      ctx.strokeStyle = 'rgba(0, 229, 255, 0.85)';
      ctx.lineWidth = 1.5;
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(hX, 0);
      ctx.lineTo(hX, height);
      ctx.stroke();
      ctx.setLineDash([]);

      // Target Dots on active curves
      if (e.channels.speed) drawHoverDot(ctx, hX, (height * 0.44) * (1.0 - (hp.speed / 400.0)) + 4, '#00ff41');
      if (e.channels.rpm) drawHoverDot(ctx, hX, (height * 0.44) * (1.0 - (hp.rpm / 15000.0)) + 4, '#a200ff');
      if (e.channels.throttle) drawHoverDot(ctx, hX, (height * 0.52) + (height * 0.42) * (1.0 - (hp.thr / 100.0)), '#00d2ff');
      if (e.channels.brake) drawHoverDot(ctx, hX, (height * 0.52) + (height * 0.42) * (1.0 - (hp.brk / 100.0)), '#ff0000');
      if (e.channels.steering) drawHoverDot(ctx, hX, (height * 0.52) + (height * 0.42) * (0.5 - (hp.steer / 100.0)), '#ff00ff');

      // Top distance badge
      const distStr = hp.lapDist >= 1000
        ? (hp.lapDist / 1000.0).toFixed(2) + ' km'
        : Math.round(hp.lapDist) + ' m';
      const badgeWidth = 66;
      const badgeX = Math.max(2, Math.min(width - badgeWidth - 2, hX - badgeWidth / 2));
      ctx.fillStyle = '#00e5ff';
      ctx.fillRect(badgeX, 2, badgeWidth, 16);
      ctx.fillStyle = '#0a0c12';
      ctx.font = 'bold 10px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(distStr, badgeX + badgeWidth / 2, 14);
    }

    ctx.restore();

    // Direct DOM sync for high-frequency stats
    syncDomStats(e.isHovering && e.hoverPoint ? e.hoverPoint : currentPt);
  }

  function drawHoverDot(ctx, x, y, color) {
    ctx.save();
    ctx.shadowColor = color;
    ctx.shadowBlur = 10;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x, y, 4.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 1.5;
    ctx.stroke();
    ctx.restore();
  }

  function drawCurve(ctx, slice, getX, getY, color, lineWidth) {
    if (slice.length < 2) return;
    ctx.strokeStyle = color;
    ctx.lineWidth = lineWidth;
    ctx.beginPath();
    let first = true;
    for (let i = 0; i < slice.length; i++) {
      const x = getX(slice[i].lapDist);
      const y = getY(slice[i]);
      if (isNaN(x) || isNaN(y)) continue;
      if (first) { ctx.moveTo(x, y); first = false; }
      else { ctx.lineTo(x, y); }
    }
    ctx.stroke();
  }

  function drawGrid(ctx, width, height, startDist, endDist, currentLap, lapTotalDistance) {
    ctx.strokeStyle = '#14141e';
    ctx.lineWidth = 1;

    // Horizontal lines
    const hLines = [0.15, 0.30, 0.44, 0.52, 0.73, 0.94];
    hLines.forEach((ratio) => {
      const y = height * ratio;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(width, y);
      ctx.stroke();
    });

    // Category separator
    ctx.strokeStyle = '#222234';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(0, height * 0.48);
    ctx.lineTo(width, height * 0.48);
    ctx.stroke();

    // X-Axis distance labels
    const span = endDist - startDist;
    const step = span <= 1200 ? 200 : (span <= 3500 ? 500 : 1000);
    const firstTick = Math.ceil(startDist / step) * step;

    ctx.font = 'bold 11px monospace';
    ctx.textAlign = 'center';

    for (let d = firstTick; d <= endDist; d += step) {
      const x = ((d - startDist) / span) * width;
      ctx.strokeStyle = '#181826';
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, height);
      ctx.stroke();

      const distLabel = d >= 1000 ? (d / 1000.0).toFixed(1) + ' km' : d + ' m';
      ctx.fillStyle = '#9ca3af';
      ctx.fillText(`LAP ${currentLap} • ${distLabel}`, x, height - 7);
    }

    // Finish line marker
    if (lapTotalDistance >= startDist && lapTotalDistance <= endDist) {
      const endX = ((lapTotalDistance - startDist) / span) * width;
      ctx.save();
      ctx.setLineDash([4, 4]);
      ctx.strokeStyle = '#00ff41';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(endX, 0);
      ctx.lineTo(endX, height);
      ctx.stroke();
      ctx.restore();

      ctx.fillStyle = '#00ff41';
      ctx.fillRect(Math.max(40, endX - 45), height - 22, 90, 16);
      ctx.fillStyle = '#000000';
      ctx.font = 'bold 10px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(`LAP ${currentLap} FINISH`, Math.max(40, endX - 45) + 45, height - 10);
    }
  }

  // Sync DOM elements directly (zero React re-render overhead)
  function syncDomStats(pt) {
    if (!pt) return;
    const e = engineRef.current;

    if (tbSpeedRef.current) tbSpeedRef.current.textContent = `${Math.round(pt.speed)} km/h`;
    if (tbRpmRef.current) tbRpmRef.current.textContent = Math.round(pt.rpm).toLocaleString();
    if (tbThrRef.current) tbThrRef.current.textContent = `${Math.round(pt.thr)}%`;
    if (tbBrkRef.current) tbBrkRef.current.textContent = `${Math.round(pt.brk)}%`;
    if (tbStrRef.current) tbStrRef.current.textContent = `${pt.steer >= 0 ? '+' : ''}${Number(pt.steer).toFixed(1)}°`;

    if (speedDisplayRef.current) speedDisplayRef.current.textContent = `${Math.round(pt.speed)} KM/H`;
    if (distDisplayRef.current) distDisplayRef.current.textContent = `${Math.round(pt.lapDist).toLocaleString()} m / ${Math.round(e.lapTotalDistance).toLocaleString()} m`;
    if (sliderRef.current) sliderRef.current.value = e.currentIndex;

    if (timeDisplayRef.current && telemetryData.length > 0) {
      const startPt = telemetryData[0];
      const endPt = telemetryData[telemetryData.length - 1];
      const startT = startPt ? (startPt.t ?? 0) : 0;
      const endT = endPt ? (endPt.t ?? 0) : 0;
      const curT = pt.t ?? 0;
      const elapsedSec = Math.max(0, curT - startT);
      const totalSec = Math.max(0, endT - startT);
      timeDisplayRef.current.textContent = `${formatSeconds(elapsedSec)} / ${formatSeconds(totalSec)}`;
    }

    if (onTelemetryTick) {
      onTelemetryTick(pt);
    }
  }

  // Animation Loop for Replay
  function animLoop(now) {
    const e = engineRef.current;
    if (!e.isPlaying) return;

    if (!now) now = performance.now();
    const dt = Math.min(0.1, (now - e.lastFrameTime) / 1000.0);
    e.lastFrameTime = now;

    const startPt = telemetryData[0];
    const endPt = telemetryData[telemetryData.length - 1];
    const totalDurationSec = Math.max(1, (endPt ? endPt.t : 0) - (startPt ? startPt.t : 0));
    const avgSampleRate = (telemetryData.length > 1 && totalDurationSec > 0)
      ? (telemetryData.length - 1) / totalDurationSec
      : 50.0;

    const deltaPoints = dt * e.playbackRate * avgSampleRate;
    e.fractionalIndex += deltaPoints;
    e.currentIndex = Math.floor(e.fractionalIndex);

    // Stop at end of complete session
    if (e.currentIndex >= telemetryData.length - 1) {
      e.currentIndex = telemetryData.length - 1;
      e.fractionalIndex = e.currentIndex;
      pausePlayback();
      renderCanvas();
      return;
    }

    // Continuous Chronological Replay across laps
    const pt = telemetryData[e.currentIndex];
    if (pt && pt.lap !== e.currentLap) {
      e.currentLap = pt.lap;
      updateLapFilter(e.currentLap, false);
      if (onLapChange) onLapChange(e.currentLap);
    }

    renderCanvas();
    e.animFrameId = requestAnimationFrame(animLoop);
  }

  function startPlayback() {
    const e = engineRef.current;
    e.isPlaying = true;
    setIsPlaying(true);

    if (e.currentIndex >= telemetryData.length - 1) {
      e.currentIndex = 0;
      e.fractionalIndex = 0;
      updateLapFilter(1, true);
      if (onLapChange) onLapChange(1);
    } else {
      e.fractionalIndex = e.currentIndex;
    }

    e.lastFrameTime = performance.now();
    if (!e.animFrameId) e.animFrameId = requestAnimationFrame(animLoop);
  }

  function pausePlayback() {
    const e = engineRef.current;
    e.isPlaying = false;
    setIsPlaying(false);
    if (e.animFrameId) {
      cancelAnimationFrame(e.animFrameId);
      e.animFrameId = null;
    }
    renderCanvas();
  }

  function resetPlayback() {
    pausePlayback();
    const e = engineRef.current;
    e.currentIndex = 0;
    e.fractionalIndex = 0;
    updateLapFilter(1, true);
    if (onLapChange) onLapChange(1);
    renderCanvas();
  }

  function seekToSlider(val) {
    pausePlayback();
    const e = engineRef.current;
    e.currentIndex = Math.max(0, Math.min(telemetryData.length - 1, parseInt(val, 10)));
    e.fractionalIndex = e.currentIndex;

    const pt = telemetryData[e.currentIndex];
    if (pt && pt.lap !== e.currentLap) {
      e.currentLap = pt.lap;
      updateLapFilter(e.currentLap, false);
      if (onLapChange) onLapChange(e.currentLap);
    }
    renderCanvas();
  }

  // External lap jump (called by parent when user clicks lap list or lap analysis table)
  const jumpToLap = useCallback((lapNum) => {
    pausePlayback();
    updateLapFilter(lapNum, true);
    renderCanvas();
  }, [updateLapFilter]);

  // Expose jumpToLap to window/parent if needed
  useEffect(() => {
    window.__rtaJumpToLap = jumpToLap;
    return () => { delete window.__rtaJumpToLap; };
  }, [jumpToLap]);

  // Clean up animation on unmount
  useEffect(() => {
    return () => {
      const e = engineRef.current;
      if (e.animFrameId) cancelAnimationFrame(e.animFrameId);
    };
  }, []);

  // Hover Engine Event Handlers
  function onMouseMove(e) {
    const canvas = canvasRef.current;
    const tooltip = tooltipRef.current;
    const eng = engineRef.current;
    if (!canvas || eng.activeLapPoints.length === 0) return;

    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    if (mouseX < 0 || mouseX > rect.width || mouseY < 0 || mouseY > rect.height) {
      onMouseLeave();
      return;
    }

    eng.isHovering = true;
    eng.hoverCanvasX = mouseX;

    const span = eng.windowEndDist - eng.windowStartDist;
    if (span <= 0) return;
    const ratio = Math.max(0, Math.min(1, mouseX / rect.width));
    const hoveredDist = eng.windowStartDist + ratio * span;

    eng.hoverPoint = getInterpolatedPointAtDist(eng.activeLapPoints, hoveredDist);
    if (!eng.hoverPoint) return;

    // Update Tooltip HUD DOM
    if (tooltip) {
      tooltip.style.display = 'block';

      if (thLapRef.current) thLapRef.current.textContent = eng.hoverPoint.lap;
      if (thDistRef.current) {
        thDistRef.current.textContent = eng.hoverPoint.lapDist >= 1000
          ? `${(eng.hoverPoint.lapDist / 1000.0).toFixed(3)} km`
          : `${Math.round(eng.hoverPoint.lapDist)} m`;
      }
      if (thTimeRef.current) {
        const lapStartPt = eng.activeLapPoints[0];
        const lapElapsed = Math.max(0, eng.hoverPoint.t - (lapStartPt ? lapStartPt.t : 0));
        thTimeRef.current.textContent = formatSeconds(lapElapsed);
      }

      if (thSpeedRef.current) thSpeedRef.current.textContent = `${Math.round(eng.hoverPoint.speed)} km/h`;
      if (thRpmRef.current) thRpmRef.current.textContent = Math.round(eng.hoverPoint.rpm).toLocaleString();
      if (thThrRef.current) thThrRef.current.textContent = `${Math.round(eng.hoverPoint.thr)}%`;
      if (thThrBarRef.current) thThrBarRef.current.style.width = `${Math.min(100, Math.max(0, eng.hoverPoint.thr))}%`;
      if (thBrkRef.current) thBrkRef.current.textContent = `${Math.round(eng.hoverPoint.brk)}%`;
      if (thBrkBarRef.current) thBrkBarRef.current.style.width = `${Math.min(100, Math.max(0, eng.hoverPoint.brk))}%`;
      if (thSteerRef.current) thSteerRef.current.textContent = `${eng.hoverPoint.steer >= 0 ? '+' : ''}${Number(eng.hoverPoint.steer).toFixed(1)}°`;

      // Position Tooltip HUD
      const tooltipWidth = tooltip.offsetWidth || 360;
      const tooltipHeight = tooltip.offsetHeight || 260;
      let tipX = mouseX + 18;
      let tipY = mouseY - 25;

      if (tipX + tooltipWidth > rect.width - 8) {
        tipX = mouseX - tooltipWidth - 18;
      }
      if (tipX < 8) tipX = 8;
      if (tipY < 8) tipY = 8;
      if (tipY + tooltipHeight > rect.height - 8) {
        tipY = rect.height - tooltipHeight - 8;
      }

      tooltip.style.transform = `translate(${tipX}px, ${tipY}px)`;
    }

    if (!eng.isPlaying) {
      renderCanvas();
    }
  }

  function onMouseLeave() {
    const eng = engineRef.current;
    eng.isHovering = false;
    eng.hoverCanvasX = null;
    eng.hoverPoint = null;
    if (tooltipRef.current) {
      tooltipRef.current.style.display = 'none';
    }
    if (!eng.isPlaying) {
      renderCanvas();
    }
  }

  function onCanvasClick() {
    const eng = engineRef.current;
    if (!eng.hoverPoint || telemetryData.length === 0) return;
    const targetLap = eng.hoverPoint.lap;
    const targetDist = eng.hoverPoint.lapDist;

    let closestIdx = 0;
    let minDiff = Infinity;
    for (let i = 0; i < telemetryData.length; i++) {
      if (telemetryData[i].lap === targetLap) {
        const diff = Math.abs(telemetryData[i].lapDist - targetDist);
        if (diff < minDiff) {
          minDiff = diff;
          closestIdx = i;
        }
      }
    }
    seekToSlider(closestIdx);
  }

  return (
    <div className="dash-card graph-main-card">
      {/* Graph Header Bar with Toggles & Mode */}
      <div className="graph-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
          <span className="graph-title">TELEMETRY GRAPHS</span>

          <div className="channel-toggles">
            <button
              type="button"
              className={`ch-btn ch-speed ${channels.speed ? 'active' : 'disabled-ch'}`}
              onClick={() => setChannels((c) => ({ ...c, speed: !c.speed }))}
              title="Toggle Speed curve (0-400 km/h)"
            >
              <span className="ch-dot" style={{ background: '#00ff41' }} />
              SPEED <span ref={tbSpeedRef} style={{ marginLeft: '4px', fontWeight: 'bold' }} />
            </button>
            <button
              type="button"
              className={`ch-btn ch-rpm ${channels.rpm ? 'active' : 'disabled-ch'}`}
              onClick={() => setChannels((c) => ({ ...c, rpm: !c.rpm }))}
              title="Toggle RPM curve (0-15000 RPM)"
            >
              <span className="ch-dot" style={{ background: '#a200ff' }} />
              RPM <span ref={tbRpmRef} style={{ marginLeft: '4px', fontWeight: 'bold' }} />
            </button>
            <button
              type="button"
              className={`ch-btn ch-throttle ${channels.throttle ? 'active' : 'disabled-ch'}`}
              onClick={() => setChannels((c) => ({ ...c, throttle: !c.throttle }))}
              title="Toggle Throttle curve (0-100%)"
            >
              <span className="ch-dot" style={{ background: '#00d2ff' }} />
              THROTTLE <span ref={tbThrRef} style={{ marginLeft: '4px', fontWeight: 'bold' }} />
            </button>
            <button
              type="button"
              className={`ch-btn ch-brake ${channels.brake ? 'active' : 'disabled-ch'}`}
              onClick={() => setChannels((c) => ({ ...c, brake: !c.brake }))}
              title="Toggle Brake curve (0-100%)"
            >
              <span className="ch-dot" style={{ background: '#ff0000' }} />
              BRAKE <span ref={tbBrkRef} style={{ marginLeft: '4px', fontWeight: 'bold' }} />
            </button>
            <button
              type="button"
              className={`ch-btn ch-steering ${channels.steering ? 'active' : 'disabled-ch'}`}
              onClick={() => setChannels((c) => ({ ...c, steering: !c.steering }))}
              title="Toggle Steering curve (±50°)"
            >
              <span className="ch-dot" style={{ background: '#ff00ff' }} />
              STEERING <span ref={tbStrRef} style={{ marginLeft: '4px', fontWeight: 'bold' }} />
            </button>
          </div>
        </div>

        {/* View Mode & Window Size */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <button
            type="button"
            className={`btn-mode-toggle ${isRollingMode ? 'active' : ''}`}
            onClick={() => setIsRollingMode((m) => !m)}
            title="Toggle between Rolling Viewport and Full Lap View"
          >
            <span style={{ marginRight: '4px' }}>📡</span>
            {isRollingMode ? 'ROLLING VIEWPORT' : 'FULL LAP'}
          </button>

          <select
            value={windowDistance}
            onChange={(e) => setWindowDistance(parseFloat(e.target.value))}
            className="vehicle-quick-select"
            style={{ width: '95px', fontSize: '0.72rem', padding: '2px 6px' }}
            title="Rolling Window Distance"
          >
            <option value="500">500 m</option>
            <option value="1000">1.0 km</option>
            <option value="1500">1.5 km</option>
            <option value="2500">2.5 km</option>
          </select>
        </div>
      </div>

      {/* Progressive-Reveal Viewport Canvas */}
      <div
        ref={containerRef}
        className="graph-viewport-wrapper"
        onMouseMove={onMouseMove}
        onMouseLeave={onMouseLeave}
        onClick={onCanvasClick}
      >
        <canvas ref={canvasRef} style={{ width: '100%', height: '100%', display: 'block' }} />

        {/* Oscilloscope Hover Tooltip HUD */}
        <div ref={tooltipRef} className="telemetry-hover-hud" style={{ display: 'none' }}>
          <div className="th-header">
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span className="th-badge-lap">LAP <span ref={thLapRef}>1</span></span>
              <span ref={thDistRef} className="th-dist">0.000 km</span>
            </div>
            <div ref={thTimeRef} className="th-time">00:00.000</div>
          </div>

          <div className="th-grid">
            <div className={`th-item ${!channels.speed ? 'disabled-ch' : ''}`}>
              <div className="th-meta">
                <span className="th-dot" style={{ background: '#00ff41' }} />
                <span className="th-label">SPEED</span>
              </div>
              <span ref={thSpeedRef} className="th-val" style={{ color: '#00ff41' }}>0 km/h</span>
            </div>

            <div className={`th-item ${!channels.rpm ? 'disabled-ch' : ''}`}>
              <div className="th-meta">
                <span className="th-dot" style={{ background: '#a200ff' }} />
                <span className="th-label">RPM</span>
              </div>
              <span ref={thRpmRef} className="th-val" style={{ color: '#c084fc' }}>0</span>
            </div>

            <div className={`th-item ${!channels.throttle ? 'disabled-ch' : ''}`}>
              <div className="th-meta">
                <span className="th-dot" style={{ background: '#00d2ff' }} />
                <span className="th-label">THROTTLE</span>
                <div className="th-bar-track">
                  <div ref={thThrBarRef} className="th-bar-fill" style={{ background: '#00d2ff' }} />
                </div>
              </div>
              <span ref={thThrRef} className="th-val" style={{ color: '#00d2ff' }}>0%</span>
            </div>

            <div className={`th-item ${!channels.brake ? 'disabled-ch' : ''}`}>
              <div className="th-meta">
                <span className="th-dot" style={{ background: '#ff0000' }} />
                <span className="th-label">BRAKE</span>
                <div className="th-bar-track">
                  <div ref={thBrkBarRef} className="th-bar-fill" style={{ background: '#ff0000' }} />
                </div>
              </div>
              <span ref={thBrkRef} className="th-val" style={{ color: '#ff4444' }}>0%</span>
            </div>

            <div className={`th-item ${!channels.steering ? 'disabled-ch' : ''}`}>
              <div className="th-meta">
                <span className="th-dot" style={{ background: '#ff00ff' }} />
                <span className="th-label">STEERING</span>
              </div>
              <span ref={thSteerRef} className="th-val" style={{ color: '#ff66ff' }}>0.0°</span>
            </div>
          </div>

          <div className="th-hint">Click anywhere on curve to jump replay</div>
        </div>
      </div>

      {/* Replay Timeline Controls Bar */}
      <div className="replay-control-bar">
        <div className="playback-buttons">
          <button
            type="button"
            className={`btn-replay ${isPlaying ? 'active' : ''}`}
            onClick={startPlayback}
            title="Start Telemetry Playback"
          >
            <span>▶</span> PLAY
          </button>
          <button
            type="button"
            className={`btn-replay ${!isPlaying ? '' : ''}`}
            onClick={pausePlayback}
            title="Pause Telemetry Playback"
          >
            <span>❚❚</span> PAUSE
          </button>
          <button
            type="button"
            className="btn-replay"
            onClick={resetPlayback}
            title="Reset to Lap 1 Start"
          >
            <span>↺</span> RESET
          </button>
        </div>

        <input
          ref={sliderRef}
          type="range"
          min="0"
          max={Math.max(1, telemetryData.length - 1)}
          defaultValue="0"
          className="timeline-slider"
          onInput={(e) => seekToSlider(e.target.value)}
        />

        <div className="speed-multiplier-group">
          <button
            type="button"
            className={`btn-speed-mult ${playbackRate === 1 ? 'active' : ''}`}
            onClick={() => setPlaybackRate(1)}
          >
            1x
          </button>
          <button
            type="button"
            className={`btn-speed-mult ${playbackRate === 2 ? 'active' : ''}`}
            onClick={() => setPlaybackRate(2)}
          >
            2x
          </button>
          <button
            type="button"
            className={`btn-speed-mult ${playbackRate === 4 ? 'active' : ''}`}
            onClick={() => setPlaybackRate(4)}
          >
            4x
          </button>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <span ref={timeDisplayRef} className="hud-time">00:00.000 / 00:00.000</span>
          <span className="hud-lap-badge">
            Lap <strong ref={lapBadgeRef} style={{ color: '#ff5a1f' }}>{selectedLapNumber || 1}</strong>
          </span>
          <span className="hud-stat">
            <span ref={distDisplayRef} style={{ color: '#fff', fontWeight: 'bold' }}>0 m</span>
          </span>
          <span className="hud-stat">
            <span ref={speedDisplayRef} style={{ color: '#00ff41', fontWeight: 'bold' }}>0 KM/H</span>
          </span>
        </div>
      </div>
    </div>
  );
}

