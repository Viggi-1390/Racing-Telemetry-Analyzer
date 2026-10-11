import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import {
  getVehicles,
  getTracks,
  getSessions,
  getSessionTelemetry,
  generateSyntheticTelemetry,
} from '../api/client';
import TelemetryRollingCanvas from '../components/TelemetryRollingCanvas';
import TrackMapSvg from '../components/TrackMapSvg';
import CsvImportModal from '../components/CsvImportModal';
import '../styles/telemetry.css';

export default function TelemetryPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();

  // URL parameters
  const urlVehicleId = searchParams.get('vehicleId');
  const urlTrackId = searchParams.get('trackId');
  const urlSessionId = searchParams.get('sessionId');

  // Application Data States
  const [vehicles, setVehicles] = useState([]);
  const [tracks, setTracks] = useState([]);
  const [sessions, setSessions] = useState([]);
  const [currentSessionData, setCurrentSessionData] = useState(null);

  // Active selections
  const [activeVehicle, setActiveVehicle] = useState(null);
  const [activeTrack, setActiveTrack] = useState(null);
  const [activeSessionId, setActiveSessionId] = useState(null);
  const [activeLapNumber, setActiveLapNumber] = useState(1);

  // UI state
  const [loading, setLoading] = useState(true);
  const [generatingDemo, setGeneratingDemo] = useState(false);
  const [demoMessage, setDemoMessage] = useState(null);
  const [error, setError] = useState(null);
  const [isCsvModalOpen, setIsCsvModalOpen] = useState(false);
  const [noDataSessionType, setNoDataSessionType] = useState('PRACTICE 1');

  // Live G-force direct DOM refs for 60 FPS playback
  const gforceDotRef = useRef(null);
  const gforceLatRef = useRef(null);
  const gforceLongRef = useRef(null);
  const gforceVertRef = useRef(null);

  // Live Suspension travel refs
  const suspFLRef = useRef(null);
  const suspFRRef = useRef(null);
  const suspRLRef = useRef(null);
  const suspRRRef = useRef(null);

  // Load Vehicles & Tracks initial catalog
  useEffect(() => {
    let isMounted = true;

    async function loadCatalog() {
      try {
        setLoading(true);
        setError(null);
        const [vList, tList] = await Promise.all([getVehicles(), getTracks()]);
        if (!isMounted) return;

        setVehicles(vList);
        setTracks(tList);

        // Resolve vehicle
        let v = null;
        if (urlVehicleId) {
          const vId = parseInt(urlVehicleId, 10);
          v = vList.find((x) => x.id === vId || x.vehicleId === vId);
        }
        if (!v && vList.length > 0) {
          // Default to first vehicle if none requested
          v = vList[0];
        }
        setActiveVehicle(v);

        // Resolve track
        let t = null;
        if (urlTrackId) {
          const tId = parseInt(urlTrackId, 10);
          t = tList.find((x) => x.id === tId || x.trackId === tId);
        }
        if (!t && tList.length > 0) {
          // Default to first track if none requested
          t = tList[0];
        }
        setActiveTrack(t);
      } catch (err) {
        if (isMounted) setError(err.message || 'Failed to load vehicle and track catalog.');
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    loadCatalog();
    return () => { isMounted = false; };
  }, [urlVehicleId, urlTrackId]);

  // Load Sessions whenever activeVehicle or activeTrack changes
  const loadSessionsAndTelemetry = useCallback(async (vId, tId, targetSessionId = null) => {
    if (!vId || !tId) return;
    try {
      setLoading(true);
      setError(null);
      setDemoMessage(null);

      // Fetch sessions strictly isolated by vehicle & track
      const sessionList = await getSessions(vId, tId);
      setSessions(sessionList);

      if (sessionList && sessionList.length > 0) {
        // Resolve active session
        let sId = targetSessionId;
        if (!sId || !sessionList.some((s) => s.sessionId === sId)) {
          sId = sessionList[0].sessionId;
        }
        setActiveSessionId(sId);

        // Load complete telemetry payload for session
        const teleData = await getSessionTelemetry(sId, vId, tId);
        setCurrentSessionData(teleData);
        setActiveLapNumber(1);
      } else {
        // No sessions recorded for this vehicle + track combo
        setActiveSessionId(null);
        setCurrentSessionData(null);
      }
    } catch (err) {
      setError(err.message || 'Failed to load telemetry session.');
      setCurrentSessionData(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeVehicle && activeTrack) {
      const vId = activeVehicle.id || activeVehicle.vehicleId;
      const tId = activeTrack.id || activeTrack.trackId;
      const sId = urlSessionId ? parseInt(urlSessionId, 10) : null;
      loadSessionsAndTelemetry(vId, tId, sId);
    }
  }, [activeVehicle, activeTrack, urlSessionId, loadSessionsAndTelemetry]);

  // Handle vehicle switch
  function handleSwitchVehicle(newVehicleId) {
    const v = vehicles.find((x) => (x.id || x.vehicleId) === parseInt(newVehicleId, 10));
    if (v && activeTrack) {
      setActiveVehicle(v);
      const tId = activeTrack.id || activeTrack.trackId;
      navigate(`/telemetry?vehicleId=${v.id || v.vehicleId}&trackId=${tId}`, { replace: true });
    }
  }

  // Handle track switch
  function handleSwitchTrack(newTrackId) {
    const t = tracks.find((x) => (x.id || x.trackId) === parseInt(newTrackId, 10));
    if (t && activeVehicle) {
      setActiveTrack(t);
      const vId = activeVehicle.id || activeVehicle.vehicleId;
      navigate(`/telemetry?vehicleId=${vId}&trackId=${t.id || t.trackId}`, { replace: true });
    }
  }

  // Handle session switch
  async function handleSwitchSession(newSessionId) {
    const sId = parseInt(newSessionId, 10);
    setActiveSessionId(sId);
    const vId = activeVehicle ? (activeVehicle.id || activeVehicle.vehicleId) : null;
    const tId = activeTrack ? (activeTrack.id || activeTrack.trackId) : null;
    if (vId && tId) {
      navigate(`/telemetry?vehicleId=${vId}&trackId=${tId}&sessionId=${sId}`, { replace: true });
      try {
        setLoading(true);
        const teleData = await getSessionTelemetry(sId, vId, tId);
        setCurrentSessionData(teleData);
        setActiveLapNumber(1);
      } catch (err) {
        setError(err.message || 'Failed to load telemetry data for selected session.');
      } finally {
        setLoading(false);
      }
    }
  }

  // Handle explicit demo telemetry generation
  async function handleGenerateDemoTelemetry() {
    if (!activeVehicle || !activeTrack) return;
    try {
      setGeneratingDemo(true);
      setError(null);
      setDemoMessage(null);

      const vId = activeVehicle.id || activeVehicle.vehicleId;
      const tId = activeTrack.id || activeTrack.trackId;

      const res = await generateSyntheticTelemetry(vId, tId, noDataSessionType);
      if (res && res.success) {
        setDemoMessage(`✓ Successfully generated 3 laps demonstration telemetry for ${activeVehicle.name} at ${activeTrack.name}.`);
        await loadSessionsAndTelemetry(vId, tId, res.sessionId);
      }
    } catch (err) {
      setError(err.message || 'Failed to generate demo telemetry.');
    } finally {
      setGeneratingDemo(false);
    }
  }

  // Handle CSV import success
  function handleCsvSuccess(newSessionId) {
    if (activeVehicle && activeTrack) {
      const vId = activeVehicle.id || activeVehicle.vehicleId;
      const tId = activeTrack.id || activeTrack.trackId;
      loadSessionsAndTelemetry(vId, tId, newSessionId);
    }
  }

  // Handle jump to lap (called when user clicks lap list or table)
  function handleSelectLap(lapNum) {
    setActiveLapNumber(lapNum);
    if (window.__rtaJumpToLap) {
      window.__rtaJumpToLap(lapNum);
    }
  }

  // Telemetry tick callback: updates G-Force radar and suspension travel without React re-renders
  function handleTelemetryTick(pt) {
    if (!pt) return;
    const speedMs = Number(pt.speed || 0) / 3.6;
    const steerVal = Number(pt.steer || 0);
    const thrVal = Number(pt.thr || 0);
    const brkVal = Number(pt.brk || 0);

    let latG = pt.gLat != null ? Number(pt.gLat) : null;
    let longG = pt.gLon != null ? Number(pt.gLon) : null;
    let vertG = pt.gVert != null ? Number(pt.gVert) : null;

    if (latG === null) {
      latG = (speedMs > 4 && Math.abs(steerVal) > 0.3)
        ? (speedMs * speedMs * Math.sin(Math.abs(steerVal) * Math.PI / 180.0)) / (9.81 * 75.0)
        : 0;
      latG = Math.min(4.5, Math.max(0, latG));
      if (steerVal < 0) latG = -latG;
    }

    if (longG === null) {
      longG = (thrVal / 100.0 * 1.35) - (brkVal / 100.0 * 2.85);
    }

    if (vertG === null) {
      vertG = 0.20 + (speedMs / 100.0 * 0.12);
    }

    // Direct DOM updates
    if (gforceLatRef.current) gforceLatRef.current.textContent = `${latG >= 0 ? '+' : ''}${latG.toFixed(2)} G`;
    if (gforceLongRef.current) gforceLongRef.current.textContent = `${longG >= 0 ? '+' : ''}${longG.toFixed(2)} G`;
    if (gforceVertRef.current) gforceVertRef.current.textContent = `${vertG >= 0 ? '+' : ''}${vertG.toFixed(2)} G`;

    if (gforceDotRef.current) {
      const topPct = Math.max(10, Math.min(90, 50 - (longG * 18)));
      const leftPct = Math.max(10, Math.min(90, 50 + (latG * 18)));
      gforceDotRef.current.style.top = `${topPct}%`;
      gforceDotRef.current.style.left = `${leftPct}%`;
    }

    // Suspension updates
    if (suspFLRef.current && pt.suspFL != null) suspFLRef.current.textContent = Math.round(pt.suspFL);
    if (suspFRRef.current && pt.suspFR != null) suspFRRef.current.textContent = Math.round(pt.suspFR);
    if (suspRLRef.current && pt.suspRL != null) suspRLRef.current.textContent = Math.round(pt.suspRL);
    if (suspRRRef.current && pt.suspRR != null) suspRRRef.current.textContent = Math.round(pt.suspRR);
  }

  // Derived properties from active session
  const isBike = useMemo(() => {
    return activeVehicle?.vehicleType === 1 || activeVehicle?.vehicleType === 'Bike';
  }, [activeVehicle]);

  const allLaps = useMemo(() => {
    return currentSessionData?.laps || [];
  }, [currentSessionData]);

  const bestLap = useMemo(() => {
    if (!allLaps || allLaps.length === 0) return null;
    const valid = allLaps.filter((l) => l.isValid);
    if (valid.length > 0) {
      return [...valid].sort((a, b) => a.lapTimeSec - b.lapTimeSec)[0];
    }
    return allLaps[0];
  }, [allLaps]);

  const selectedLap = useMemo(() => {
    if (!allLaps || allLaps.length === 0) return null;
    return allLaps.find((l) => l.lapNumber === activeLapNumber) || allLaps[0];
  }, [allLaps, activeLapNumber]);

  // Derived Tyre Status using proven formulas
  const tyreStatus = useMemo(() => {
    const trackTemp = currentSessionData?.condition?.trackTemperature || 28.0;
    const power = activeVehicle?.power || 650;
    const lapCount = allLaps.length || 1;

    if (isBike) {
      const fTemp = (trackTemp + 46.0 + (power / 250.0) * 8.0).toFixed(1);
      const rTemp = (trackTemp + 52.0 + (power / 200.0) * 10.0).toFixed(1);
      const fWear = Math.max(80, Math.min(100, Math.round(99.0 - (lapCount * 0.4))));
      const rWear = Math.max(75, Math.min(100, Math.round(98.0 - (lapCount * 0.6))));

      return {
        isBike: true,
        front: { temp: fTemp, press: 2.15, wear: fWear },
        rear: { temp: rTemp, press: 1.65, wear: rWear },
        maxLean: 54.0,
      };
    } else {
      const flTemp = (trackTemp + 49.0 + (power / 800.0) * 10.0).toFixed(1);
      const frTemp = (trackTemp + 51.0 + (power / 800.0) * 10.0).toFixed(1);
      const rlTemp = (trackTemp + 47.0 + (power / 800.0) * 9.0).toFixed(1);
      const rrTemp = (trackTemp + 49.0 + (power / 800.0) * 9.0).toFixed(1);

      return {
        isBike: false,
        fl: { temp: flTemp, press: 1.72, wear: Math.max(80, 99 - lapCount) },
        fr: { temp: frTemp, press: 1.73, wear: Math.max(78, 99 - lapCount) },
        rl: { temp: rlTemp, press: 1.71, wear: Math.max(82, 99 - lapCount) },
        rr: { temp: rrTemp, press: 1.72, wear: Math.max(80, 99 - lapCount) },
      };
    }
  }, [currentSessionData, activeVehicle, allLaps, isBike]);

  // Derived analysis recommendations
  const analysisResults = useMemo(() => {
    return currentSessionData?.analysisResults || [];
  }, [currentSessionData]);

  const brakeMetric = useMemo(() => {
    return analysisResults.find((r) => r.recommendationType === 'Braking') || { brakingPercentage: 85, minimumSpeed: 60 };
  }, [analysisResults]);

  const throttleMetric = useMemo(() => {
    return analysisResults.find((r) => r.recommendationType === 'Throttle') || { throttleResponseTime: 0.25, exitSpeed: 253 };
  }, [analysisResults]);

  // Recovery Card if vehicle or track is invalid
  if (!loading && (!activeVehicle || !activeTrack)) {
    return (
      <div className="telemetry-workstation" style={{ padding: '60px 20px', textAlign: 'center' }}>
        <div className="no-data-panel" style={{ maxWidth: '640px' }}>
          <div style={{ color: '#ff5a1f', fontSize: '2rem', marginBottom: '12px' }}>⚠️</div>
          <h2 style={{ color: '#fff', fontSize: '1.4rem', fontWeight: 800, letterSpacing: '1px', marginBottom: '10px' }}>
            TELEMETRY RECOVERY // MACHINE OR TRACK REQUIRED
          </h2>
          <p style={{ color: '#9ca3af', fontSize: '0.85rem', lineHeight: 1.6, marginBottom: '24px' }}>
            Telemetry acquisition requires an active machine and circuit selection from the garage to establish strict telemetry isolation.
          </p>
          <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
            <Link to="/" className="rta-btn-primary" style={{ padding: '10px 20px', background: '#ff5a1f' }}>
              ◀ GO TO GARAGE
            </Link>
            <Link to="/tracks" className="rta-btn-secondary" style={{ padding: '10px 20px' }}>
              CHOOSE TRACK
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="telemetry-workstation">
      {/* Top Header Bar */}
      <div className="telemetry-top-header">
        <div className="telemetry-title-group">
          <div className="rta-tag-micro">// MOTORSPORT TELEMETRY WORKSTATION</div>
          <h1>
            TELEMETRY <span>WORKSTATION</span>
            {activeVehicle && (
              <span className="telemetry-title-badge">
                {activeVehicle.name?.toUpperCase()} @ {activeTrack?.name?.toUpperCase()}
              </span>
            )}
          </h1>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <button
            type="button"
            className="rta-btn-secondary"
            onClick={() => setIsCsvModalOpen(true)}
            style={{ fontSize: '0.78rem', padding: '6px 14px', borderColor: '#00e5ff', color: '#00e5ff' }}
          >
            📁 IMPORT CSV
          </button>
          <Link
            to={`/tracks?vehicleId=${activeVehicle?.id || activeVehicle?.vehicleId}`}
            className="rta-btn-secondary"
            style={{ fontSize: '0.78rem', padding: '6px 14px' }}
          >
            SWITCH TRACK
          </Link>
          <Link
            to="/"
            className="rta-btn-secondary"
            style={{ fontSize: '0.78rem', padding: '6px 14px' }}
          >
            ◀ RETURN TO GARAGE
          </Link>
        </div>
      </div>

      {/* Notifications / Alerts */}
      {demoMessage && (
        <div style={{
          background: 'rgba(0, 230, 118, 0.15)',
          border: '1px solid #00e676',
          borderRadius: '6px',
          padding: '10px 16px',
          color: '#00e676',
          fontSize: '0.85rem',
          marginBottom: '14px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center'
        }}>
          <span>{demoMessage}</span>
          <button onClick={() => setDemoMessage(null)} style={{ background: 'none', border: 'none', color: '#00e676', cursor: 'pointer' }}>✕</button>
        </div>
      )}

      {error && (
        <div style={{
          background: 'rgba(255, 23, 68, 0.15)',
          border: '1px solid #ff1744',
          borderRadius: '6px',
          padding: '12px 18px',
          color: '#ff1744',
          fontSize: '0.85rem',
          marginBottom: '14px'
        }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Loading state indicator */}
      {loading && !currentSessionData && (
        <div style={{ textAlign: 'center', padding: '80px 20px', color: '#9ca3af' }}>
          <div style={{ fontSize: '1.8rem', color: '#00e5ff', marginBottom: '12px' }}>⚙</div>
          <div style={{ fontSize: '1rem', fontWeight: 700, letterSpacing: '1px', color: '#fff' }}>
            ACQUIRING TELEMETRY STREAMS...
          </div>
          <div style={{ fontSize: '0.8rem', marginTop: '6px' }}>
            Establishing vehicle and session context from SQL Server LocalDB
          </div>
        </div>
      )}

      {/* NO DATA STATE (When vehicle + track has 0 recorded sessions) */}
      {!loading && (!currentSessionData || sessions.length === 0) && (
        <div className="no-data-panel">
          <div className="no-data-header">
            <div style={{ textAlign: 'left' }}>
              <span className="rta-badge rta-badge-orange" style={{ marginBottom: '6px', display: 'inline-block' }}>
                {isBike ? 'RACE BIKE' : 'RACE CAR'}
              </span>
              <h2 style={{ color: '#fff', margin: 0, fontSize: '1.6rem', fontWeight: 800, letterSpacing: '1px' }}>
                {activeVehicle?.name?.toUpperCase()}
              </h2>
              <span style={{ color: '#9ca3af', fontSize: '0.78rem', textTransform: 'uppercase' }}>
                {activeVehicle?.manufacturer?.toUpperCase()} &bull; {activeVehicle?.class?.toUpperCase()} &bull; {activeVehicle?.power} HP
              </span>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <div style={{ textAlign: 'right' }}>
                <label style={{ display: 'block', fontSize: '0.7rem', color: '#9ca3af', marginBottom: '2px' }}>SWITCH MACHINE</label>
                <select
                  value={activeVehicle?.id || activeVehicle?.vehicleId || ''}
                  onChange={(e) => handleSwitchVehicle(e.target.value)}
                  className="vehicle-quick-select"
                >
                  {vehicles.map((v) => (
                    <option key={v.id || v.vehicleId} value={v.id || v.vehicleId}>
                      {v.name} ({v.vehicleType === 1 || v.vehicleType === 'Bike' ? 'Bike' : 'Car'})
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ textAlign: 'right' }}>
                <label style={{ display: 'block', fontSize: '0.7rem', color: '#9ca3af', marginBottom: '2px' }}>SWITCH TRACK</label>
                <select
                  value={activeTrack?.id || activeTrack?.trackId || ''}
                  onChange={(e) => handleSwitchTrack(e.target.value)}
                  className="vehicle-quick-select"
                >
                  {tracks.map((t) => (
                    <option key={t.id || t.trackId} value={t.id || t.trackId}>
                      {t.name?.toUpperCase()}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          <div style={{ margin: '20px 0' }}>
            <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#ff5a1f', letterSpacing: '1.5px', marginBottom: '6px' }}>
              NO TELEMETRY DATA AVAILABLE
            </div>
            <div style={{ color: '#00d2ff', fontSize: '0.82rem', fontFamily: 'var(--rta-font-mono, monospace)', fontWeight: 700, marginBottom: '12px' }}>
              // {activeVehicle?.name?.toUpperCase()} &bull; {activeTrack?.name?.toUpperCase()}
            </div>
            <p style={{ color: '#9ca3af', fontSize: '0.88rem', lineHeight: 1.6, maxWidth: '560px', margin: '0 auto' }}>
              No telemetry sessions currently exist for this vehicle at {activeTrack?.name}. Telemetry is strictly isolated by vehicle, track, and session. Upload a CSV log or generate demonstration telemetry below to begin analysis.
            </p>
          </div>

          {/* Session Type Selector */}
          <div style={{ maxWidth: '380px', margin: '0 auto 20px auto' }}>
            <div style={{ padding: '12px', borderRadius: '6px', background: 'rgba(255, 90, 31, 0.06)', border: '1px solid rgba(255, 90, 31, 0.3)' }}>
              <label style={{ display: 'block', fontSize: '0.72rem', fontWeight: 800, letterSpacing: '1px', color: '#ff5a1f', marginBottom: '6px' }}>
                🏁 SESSION TYPE
              </label>
              <select
                value={noDataSessionType}
                onChange={(e) => setNoDataSessionType(e.target.value)}
                className="rta-input"
                style={{ width: '100%', background: '#12121c', color: '#fff', border: '1px solid #ff5a1f', fontWeight: 700, fontSize: '0.85rem' }}
              >
                <option value="PRACTICE 1">PRACTICE 1</option>
                <option value="PRACTICE 2">PRACTICE 2</option>
                <option value="PRACTICE 3">PRACTICE 3</option>
                <option value="QUALIFYING">QUALIFYING</option>
                <option value="SPRINT QUALIFYING">SPRINT QUALIFYING</option>
                <option value="SPRINT RACE">SPRINT RACE</option>
                <option value="RACE">RACE</option>
              </select>
            </div>
          </div>

          {/* Action Cards */}
          <div className="no-data-cards-grid">
            {/* Card 1: CSV Upload */}
            <div className="no-data-card" style={{ borderColor: 'rgba(0, 229, 255, 0.25)' }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                  <span style={{ color: '#00e5ff', fontSize: '1.2rem' }}>📁</span>
                  <h5 style={{ color: '#fff', margin: 0, fontWeight: 700, letterSpacing: '1px' }}>IMPORT TELEMETRY CSV</h5>
                </div>
                <p style={{ color: '#9ca3af', fontSize: '0.8rem', lineHeight: 1.5, margin: '0 0 16px 0' }}>
                  Upload raw session telemetry CSV log from your sim or telemetry logger (up to 250 MB).
                </p>
              </div>
              <button
                type="button"
                onClick={() => setIsCsvModalOpen(true)}
                className="rta-btn-primary"
                style={{ width: '100%', padding: '10px', background: 'transparent', border: '1px solid #00e5ff', color: '#00e5ff', fontWeight: 700 }}
              >
                ⬆ UPLOAD & PARSE CSV
              </button>
            </div>

            {/* Card 2: Demo Generation */}
            <div className="no-data-card" style={{ borderColor: 'rgba(255, 90, 31, 0.25)' }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                  <span style={{ color: '#ff5a1f', fontSize: '1.2rem' }}>⚡</span>
                  <h5 style={{ color: '#fff', margin: 0, fontWeight: 700, letterSpacing: '1px' }}>GENERATE DEMO TELEMETRY</h5>
                </div>
                <p style={{ color: '#9ca3af', fontSize: '0.8rem', lineHeight: 1.5, margin: '0 0 16px 0' }}>
                  Generate a realistic 3-lap synthetic telemetry session for testing and analysis workspace verification.
                </p>
              </div>
              <button
                type="button"
                disabled={generatingDemo}
                onClick={handleGenerateDemoTelemetry}
                className="rta-btn-primary"
                style={{ width: '100%', padding: '10px', background: '#ff5a1f', border: 'none', color: '#fff', fontWeight: 700 }}
              >
                {generatingDemo ? 'GENERATING SYNTHETIC DATA...' : '⚡ GENERATE DEMO SESSION'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MAIN DASHBOARD (When data exists) */}
      {!loading && currentSessionData && sessions.length > 0 && (
        <>
          <div className="dashboard-grid">
            {/* ========================================================
               1. LEFT SIDEBAR COLUMN
               ======================================================== */}
            <div className="col-sidebar-left">
              {/* Vehicle Card */}
              <div className="dash-card vehicle-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                  <span className="card-section-title" style={{ color: '#ff5a1f', margin: 0, border: 'none' }}>
                    ACTIVE MACHINE
                  </span>
                  <select
                    value={activeVehicle?.id || activeVehicle?.vehicleId || ''}
                    onChange={(e) => handleSwitchVehicle(e.target.value)}
                    className="vehicle-quick-select"
                  >
                    {vehicles.map((v) => (
                      <option key={v.id || v.vehicleId} value={v.id || v.vehicleId}>
                        {v.name} ({v.vehicleType === 1 || v.vehicleType === 'Bike' ? 'Bike' : 'Car'})
                      </option>
                    ))}
                  </select>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div className="vehicle-thumb-container">
                    <img
                      src={activeVehicle?.imagePath || (isBike ? '/images/placeholder_bike.png' : '/images/placeholder_car.png')}
                      alt={activeVehicle?.name}
                      className="vehicle-thumb"
                      onError={(e) => {
                        e.target.onerror = null;
                        e.target.src = isBike ? '/images/placeholder_bike.png' : '/images/placeholder_car.png';
                      }}
                    />
                  </div>
                  <div>
                    <h3 className="vehicle-name">{activeVehicle?.name?.toUpperCase()}</h3>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '4px' }}>
                      <span className="vehicle-class-badge">{activeVehicle?.class?.toUpperCase()}</span>
                      <span className="rta-badge rta-badge-orange" style={{ fontSize: '0.62rem', padding: '1px 5px' }}>
                        {isBike ? 'BIKE' : 'CAR'}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Session Info */}
              <div className="dash-card session-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                  <span className="card-section-title" style={{ color: '#ff5a1f', margin: 0, border: 'none' }}>
                    SESSION INFO
                  </span>
                  <select
                    value={activeSessionId || ''}
                    onChange={(e) => handleSwitchSession(e.target.value)}
                    className="vehicle-quick-select"
                    style={{ maxWidth: '140px', fontSize: '0.72rem' }}
                    title="Switch Session"
                  >
                    {sessions.map((s) => (
                      <option key={s.sessionId} value={s.sessionId}>
                        {s.sessionType || s.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="info-rows">
                  <div className="info-row">
                    <span className="info-label">Track</span>
                    <select
                      value={activeTrack?.id || activeTrack?.trackId || ''}
                      onChange={(e) => handleSwitchTrack(e.target.value)}
                      className="vehicle-quick-select"
                      style={{ border: 'none', background: 'transparent', textAlign: 'right', padding: 0 }}
                    >
                      {tracks.map((t) => (
                        <option key={t.id || t.trackId} value={t.id || t.trackId} style={{ background: '#12121c' }}>
                          {t.name?.toUpperCase()}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="info-row">
                    <span className="info-label">Session</span>
                    <span className="info-val text-orange" style={{ fontWeight: 700 }}>
                      {currentSessionData?.sessionType || currentSessionData?.sessionName || 'PRACTICE 1'}
                    </span>
                  </div>
                  <div className="info-row">
                    <span className="info-label">Date</span>
                    <span className="info-val">
                      {currentSessionData?.date
                        ? new Date(currentSessionData.date).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
                        : '24 May 2024'}
                    </span>
                  </div>
                  <div className="info-row">
                    <span className="info-label">Driver</span>
                    <span className="info-val">A. Driver</span>
                  </div>
                  <div className="info-row">
                    <span className="info-label">Best Lap</span>
                    <span className="info-val text-purple" style={{ fontWeight: 700 }}>
                      {bestLap ? bestLap.lapTime : 'N/A'}
                    </span>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => setIsCsvModalOpen(true)}
                  className="rta-btn-secondary"
                  style={{ width: '100%', marginTop: '10px', fontSize: '0.74rem', padding: '6px', color: '#00e5ff', borderColor: 'rgba(0, 229, 255, 0.4)' }}
                >
                  📁 IMPORT TELEMETRY CSV
                </button>
              </div>

              {/* Lap List */}
              <div className="dash-card lap-list-card" style={{ flexGrow: 1 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                  <span className="card-section-title" style={{ margin: 0, border: 'none' }}>LAP LIST</span>
                  <span style={{ fontSize: '0.72rem', color: '#6b7280' }}>{allLaps.length} LAPS</span>
                </div>
                <div className="lap-scroll-container">
                  {allLaps.map((l) => {
                    const isCurrent = l.lapNumber === activeLapNumber;
                    const isFastest = bestLap && l.lapId === bestLap.lapId;
                    return (
                      <div
                        key={l.lapId || l.lapNumber}
                        className={`lap-item ${isCurrent ? 'active' : ''} ${isFastest ? 'fastest' : ''}`}
                        onClick={() => handleSelectLap(l.lapNumber)}
                        title={`Inspect Lap ${l.lapNumber} on Graph`}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <span style={{ fontWeight: 700 }}>Lap {l.lapNumber}</span>
                          {isFastest && <span className="fastest-indicator" title="Fastest Lap">★</span>}
                        </div>
                        <span className={`font-monospace ${isFastest ? 'text-purple' : 'text-white'}`}>
                          {l.lapTime}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Track Map */}
              <div className="dash-card track-map-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                  <span className="card-section-title" style={{ margin: 0, border: 'none' }}>TRACK MAP</span>
                  <span style={{ fontSize: '0.75rem', color: '#ff5a1f', fontWeight: 700 }}>
                    {activeTrack?.length ? `${activeTrack.length.toFixed(1)} KM` : '5.0 KM'}
                  </span>
                </div>
                <div className="circuit-visual-wrapper">
                  <TrackMapSvg trackName={activeTrack?.name || ''} />
                </div>
              </div>
            </div>

            {/* ========================================================
               2. CENTER COLUMN: MAIN TELEMETRY GRAPH
               ======================================================== */}
            <div className="col-main-center">
              <TelemetryRollingCanvas
                telemetryData={currentSessionData?.data || []}
                selectedLapNumber={activeLapNumber}
                onLapChange={(newLap) => setActiveLapNumber(newLap)}
                onTelemetryTick={handleTelemetryTick}
              />
            </div>

            {/* ========================================================
               3. RIGHT SIDEBAR COLUMN
               ======================================================== */}
            <div className="col-sidebar-right">
              {/* Tyre Status */}
              <div className="dash-card tyre-card">
                <div className="card-section-title">
                  {isBike ? 'TYRE STATUS // BIKE' : 'TYRE STATUS'}
                </div>
                {isBike ? (
                  <div className="tyre-visual-container-bike">
                    {/* Front */}
                    <div className="tyre-node text-start">
                      <div className="tyre-pos" style={{ color: '#00d2ff' }}>FRONT</div>
                      <div className="tyre-temp">{tyreStatus.front.temp}°C</div>
                      <div className="tyre-press">{tyreStatus.front.press} bar</div>
                      <div className="tyre-grip">{tyreStatus.front.wear}% COND</div>
                    </div>

                    {/* Bike chassis silhouette */}
                    <div className="bike-chassis-silhouette">
                      <div className="bike-wheel-block bike-wheel-front" />
                      <div className="bike-body-center">
                        <div className="bike-bars" />
                        <div className="bike-core" />
                      </div>
                      <div className="bike-wheel-block bike-wheel-rear" />
                    </div>

                    {/* Rear */}
                    <div className="tyre-node text-end">
                      <div className="tyre-pos" style={{ color: '#ff5a1f' }}>REAR</div>
                      <div className="tyre-temp">{tyreStatus.rear.temp}°C</div>
                      <div className="tyre-press">{tyreStatus.rear.press} bar</div>
                      <div className="tyre-grip">{tyreStatus.rear.wear}% COND</div>
                    </div>
                  </div>
                ) : (
                  <div className="tyre-visual-container">
                    {/* Left Corners (FL, RL) */}
                    <div className="tyre-column">
                      <div className="tyre-node">
                        <div className="tyre-pos">FL</div>
                        <div className="tyre-temp">{tyreStatus.fl.temp}°C</div>
                        <div className="tyre-press">{tyreStatus.fl.press} bar</div>
                        <div className="tyre-grip">{tyreStatus.fl.wear}%</div>
                      </div>
                      <div className="tyre-node" style={{ marginTop: '12px' }}>
                        <div className="tyre-pos">RL</div>
                        <div className="tyre-temp">{tyreStatus.rl.temp}°C</div>
                        <div className="tyre-press">{tyreStatus.rl.press} bar</div>
                        <div className="tyre-grip">{tyreStatus.rl.wear}%</div>
                      </div>
                    </div>

                    {/* Chassis silhouette */}
                    <div className="chassis-silhouette">
                      <div className="tyre-block tyre-block-fl" />
                      <div className="tyre-block tyre-block-fr" />
                      <div className="car-chassis-body" />
                      <div className="tyre-block tyre-block-rl" />
                      <div className="tyre-block tyre-block-rr" />
                    </div>

                    {/* Right Corners (FR, RR) */}
                    <div className="tyre-column" style={{ textAlign: 'right' }}>
                      <div className="tyre-node">
                        <div className="tyre-pos">FR</div>
                        <div className="tyre-temp">{tyreStatus.fr.temp}°C</div>
                        <div className="tyre-press">{tyreStatus.fr.press} bar</div>
                        <div className="tyre-grip">{tyreStatus.fr.wear}%</div>
                      </div>
                      <div className="tyre-node" style={{ marginTop: '12px' }}>
                        <div className="tyre-pos">RR</div>
                        <div className="tyre-temp">{tyreStatus.rr.temp}°C</div>
                        <div className="tyre-press">{tyreStatus.rr.press} bar</div>
                        <div className="tyre-grip">{tyreStatus.rr.wear}%</div>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Engine Status */}
              <div className="dash-card engine-card">
                <div className="card-section-title">ENGINE STATUS</div>
                <div className="engine-rows">
                  <div className="eng-row">
                    <div className="eng-labels"><span>Engine Temp</span><span>92 °C</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: '76%', background: '#ff5a1f' }} /></div>
                  </div>
                  <div className="eng-row">
                    <div className="eng-labels"><span>Oil Temp</span><span>98 °C</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: '82%', background: '#ff5a1f' }} /></div>
                  </div>
                  <div className="eng-row">
                    <div className="eng-labels"><span>Oil Pressure</span><span>5.6 bar</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: '70%', background: '#00d4ff' }} /></div>
                  </div>
                  <div className="eng-row">
                    <div className="eng-labels"><span>Water Temp</span><span>88 °C</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: '72%', background: '#ff5a1f' }} /></div>
                  </div>
                  <div className="eng-row">
                    <div className="eng-labels"><span>Fuel Level</span><span>42.3 L</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: '65%', background: '#00ff41' }} /></div>
                  </div>
                  <div className="eng-row">
                    <div className="eng-labels"><span>ERS Deployment</span><span>{isBike ? 'N/A' : '76%'}</span></div>
                    <div className="eng-bar"><div className="eng-bar-fill" style={{ width: isBike ? '0%' : '76%', background: '#a200ff' }} /></div>
                  </div>
                </div>
              </div>

              {/* G-Force Friction Circle */}
              <div className="dash-card gforce-card">
                <div className="card-section-title">G-FORCE</div>
                <div className="gforce-content">
                  <div className="gforce-radar">
                    <div className="radar-circle circle-1" />
                    <div className="radar-circle circle-2" />
                    <div className="radar-circle circle-3" />
                    <div className="radar-cross-h" />
                    <div className="radar-cross-v" />
                    <div ref={gforceDotRef} className="radar-dot" style={{ top: '50%', left: '50%' }} />
                  </div>
                  <div className="gforce-readouts">
                    <div className="g-stat"><span>Lat G</span><strong ref={gforceLatRef}>0.00 G</strong></div>
                    <div className="g-stat"><span>Long G</span><strong ref={gforceLongRef}>0.00 G</strong></div>
                    <div className="g-stat"><span>Vert G</span><strong ref={gforceVertRef}>0.22 G</strong></div>
                  </div>
                </div>
              </div>

              {/* Weather Status */}
              <div className="dash-card weather-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                  <span className="card-section-title" style={{ margin: 0, border: 'none' }}>WEATHER</span>
                  <span className="rta-badge rta-badge-orange" style={{ fontSize: '0.62rem', padding: '1px 5px' }}>
                    {currentSessionData?.condition?.weather || 'DRY'}
                  </span>
                </div>
                <div className="weather-table">
                  <div className="weather-row">
                    <span className="w-label">Track</span>
                    <span className="w-val">{(currentSessionData?.condition?.trackTemperature || 32.1).toFixed(1)} °C</span>
                  </div>
                  <div className="weather-row">
                    <span className="w-label">Air</span>
                    <span className="w-val">{(currentSessionData?.condition?.airTemperature || 24.8).toFixed(1)} °C</span>
                  </div>
                  <div className="weather-row">
                    <span className="w-label">Humidity</span>
                    <span className="w-val">45 %</span>
                  </div>
                  <div className="weather-row">
                    <span className="w-label">Wind</span>
                    <span className="w-val">{(currentSessionData?.condition?.windSpeed || 12).toFixed(0)} km/h</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* ========================================================
             4. BOTTOM DETAILED ENGINEERING PANELS
             ======================================================== */}
          <div className="bottom-analysis-grid">
            {/* Panel 1: Lap Analysis Table */}
            <div className="dash-card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <span className="card-section-title" style={{ margin: 0, border: 'none' }}>LAP ANALYSIS</span>
                <span style={{ fontSize: '0.72rem', color: '#6b7280' }}>FULL SESSION</span>
              </div>
              <div className="lap-table-wrap">
                <table className="lap-table">
                  <thead>
                    <tr>
                      <th>LAP</th>
                      <th>TIME</th>
                      <th>S1</th>
                      <th>S2</th>
                      <th>S3</th>
                      <th>DELTA</th>
                    </tr>
                  </thead>
                  <tbody>
                    {allLaps.map((l) => {
                      const isCurrent = l.lapNumber === activeLapNumber;
                      const isFastest = bestLap && l.lapId === bestLap.lapId;
                      const deltaSec = bestLap ? l.lapTimeSec - bestLap.lapTimeSec : 0;
                      return (
                        <tr
                          key={l.lapId || l.lapNumber}
                          className={`lap-table-row ${isCurrent ? 'table-active' : ''}`}
                          onClick={() => handleSelectLap(l.lapNumber)}
                        >
                          <td style={{ fontWeight: 700 }}>Lap {l.lapNumber}</td>
                          <td style={{ fontFamily: 'var(--rta-font-mono, monospace)' }}>{l.lapTime}</td>
                          <td style={{ fontFamily: 'var(--rta-font-mono, monospace)', color: '#9ca3af' }}>{l.sector1Time}</td>
                          <td style={{ fontFamily: 'var(--rta-font-mono, monospace)', color: '#9ca3af' }}>{l.sector2Time}</td>
                          <td style={{ fontFamily: 'var(--rta-font-mono, monospace)', color: '#9ca3af' }}>{l.sector3Time}</td>
                          <td style={{ fontFamily: 'var(--rta-font-mono, monospace)', fontWeight: 700 }} className={isFastest ? 'text-purple' : deltaSec <= 0 ? 'text-green' : 'text-red'}>
                            {isFastest ? 'BEST' : deltaSec > 0 ? `+${deltaSec.toFixed(3)}` : deltaSec.toFixed(3)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Panel 2: Braking & Throttle Metrics */}
            <div className="dash-card braking-throttle-card">
              <div className="card-section-title">BRAKING & THROTTLE</div>
              <div className="metrics-row-list">
                <div className="metric-data-row">
                  <span className="metric-label">BRAKING PEAK</span>
                  <span className="metric-value text-red">{Math.round(brakeMetric.brakingPercentage || 85)}%</span>
                </div>
                <div className="metric-data-row">
                  <span className="metric-label">APEX MIN SPEED</span>
                  <span className="metric-value text-white">{Math.round(brakeMetric.minimumSpeed || 60)} KM/H</span>
                </div>
                <div className="metric-data-row">
                  <span className="metric-label">THROTTLE LATENCY</span>
                  <span className="metric-value text-green">{Math.round((throttleMetric.throttleResponseTime || 0.25) * 1000)} ms</span>
                </div>
                <div className="metric-data-row">
                  <span className="metric-label">CORNER EXIT SPEED</span>
                  <span className="metric-value text-cyan">{Math.round(throttleMetric.exitSpeed || 253)} KM/H</span>
                </div>
              </div>
            </div>

            {/* Panel 3: Suspension Telemetry */}
            <div className="dash-card">
              <div className="card-section-title">SUSPENSION TELEMETRY</div>
              <div className="suspension-container">
                {isBike ? (
                  <>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>MAX LEAN ANGLE</span>
                      <span className="text-orange" style={{ fontWeight: 700, fontFamily: 'monospace' }}>54°</span>
                    </div>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>FRONT FORK TRAVEL</span>
                      <span className="text-white" style={{ fontWeight: 700, fontFamily: 'monospace' }}>46 mm</span>
                    </div>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>REAR SHOCK TRAVEL</span>
                      <span className="text-cyan" style={{ fontWeight: 700, fontFamily: 'monospace' }}>52 mm</span>
                    </div>
                  </>
                ) : (
                  <>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>FL / FR TRAVEL</span>
                      <span className="text-white" style={{ fontWeight: 700, fontFamily: 'monospace' }}>
                        <span ref={suspFLRef}>45</span> mm / <span ref={suspFRRef}>46</span> mm
                      </span>
                    </div>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>RL / RR TRAVEL</span>
                      <span className="text-cyan" style={{ fontWeight: 700, fontFamily: 'monospace' }}>
                        <span ref={suspRLRef}>52</span> mm / <span ref={suspRRRef}>52</span> mm
                      </span>
                    </div>
                    <div className="susp-data-row">
                      <span style={{ color: '#9ca3af' }}>PITCH & ROLL STABILITY</span>
                      <span className="text-green" style={{ fontWeight: 700, fontFamily: 'monospace' }}>OPTIMAL</span>
                    </div>
                  </>
                )}
              </div>
            </div>

            {/* Panel 4: Analysis & Recommendations */}
            <div className="dash-card">
              <div className="card-section-title">ANALYSIS & RECOMMENDATIONS</div>
              <div className="rec-scroll-container">
                {analysisResults.length > 0 ? (
                  <ul className="rec-list">
                    {analysisResults.map((ar, idx) => (
                      <li key={ar.analysisResultId || idx} className="rec-item">
                        <span className="rec-type">[{ar.recommendationType}]</span>
                        <span style={{ color: '#e5e7eb' }}>{ar.recommendation}</span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <div style={{ color: '#9ca3af', fontSize: '0.8rem', padding: '6px' }}>
                    <span style={{ color: '#00ff41', marginRight: '6px' }}>✓</span>
                    Consistent telemetry profile. Minimal loss detected on sector transitions.
                  </div>
                )}
              </div>
            </div>
          </div>
        </>
      )}

      {/* CSV Import Modal */}
      <CsvImportModal
        isOpen={isCsvModalOpen}
        onClose={() => setIsCsvModalOpen(false)}
        vehicleId={activeVehicle?.id || activeVehicle?.vehicleId}
        trackId={activeTrack?.id || activeTrack?.trackId}
        onImportSuccess={handleCsvSuccess}
      />
    </div>
  );
}
