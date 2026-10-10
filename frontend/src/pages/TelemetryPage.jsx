import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getSessions, getLatestTelemetry, generateSyntheticTelemetry } from '../api/client';

export default function TelemetryPage() {
  const [sessions, setSessions] = useState([]);
  const [latestData, setLatestData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState(null);
  const [genMessage, setGenMessage] = useState(null);

  async function loadData() {
    try {
      setLoading(true);
      setError(null);
      const sessionList = await getSessions();
      setSessions(sessionList);

      try {
        const latest = await getLatestTelemetry();
        if (latest && latest.success) {
          setLatestData(latest);
        }
      } catch (_) {
        // No active session is normal if no data has been imported yet
        setLatestData(null);
      }
    } catch (err) {
      setError(err.message || 'Failed to acquire telemetry data from backend.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  async function handleTriggerSyntheticGen() {
    try {
      setGenerating(true);
      setGenMessage(null);
      // Trigger demo synthetic generation for Vehicle 1 (Cadillac) on Track 1 (Monza)
      const res = await generateSyntheticTelemetry(1, 1, 'PRACTICE 1');
      if (res && res.success) {
        setGenMessage(`✓ Successfully generated 3 laps demo telemetry (Session #${res.sessionId}).`);
        await loadData();
      }
    } catch (err) {
      setError(err.message || 'Failed to generate synthetic telemetry.');
    } finally {
      setGenerating(false);
    }
  }

  return (
    <div style={{ padding: '24px 32px', maxWidth: '1600px', margin: '0 auto', width: '100%' }}>
      {/* Top Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '24px' }}>
        <div>
          <div className="rta-tag-micro">// MOTORSPORT TELEMETRY WORKSTATION</div>
          <h1 style={{ margin: '4px 0 6px 0', fontSize: '1.8rem', fontWeight: 900, letterSpacing: '2px', color: '#ffffff' }}>
            TELEMETRY <span style={{ color: '#00d2ff' }}>WORKSTATION</span>
          </h1>
          <p style={{ margin: 0, color: 'var(--rta-text-secondary)', fontSize: '0.85rem' }}>
            STEP 2 FOUNDATION CONNECTED TO /api/telemetry/session AND /api/telemetry/generate
          </p>
        </div>

        <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
          <button
            onClick={handleTriggerSyntheticGen}
            disabled={generating}
            className="rta-btn-primary"
            style={{ background: 'linear-gradient(135deg, #00d2ff 0%, #0077b6 100%)', boxShadow: '0 4px 14px rgba(0, 210, 255, 0.35)' }}
          >
            {generating ? 'GENERATING SYNTHETIC DATA...' : '⚡ TEST DEMO DATA GENERATION'}
          </button>
          <Link to="/" className="rta-btn-secondary">
            ◀ RETURN TO GARAGE
          </Link>
        </div>
      </div>

      {/* Messages */}
      {genMessage && (
        <div style={{ background: 'rgba(0, 230, 118, 0.15)', border: '1px solid #00e676', borderRadius: '6px', padding: '12px 18px', color: '#00e676', marginBottom: '20px' }}>
          {genMessage}
        </div>
      )}

      {error && (
        <div style={{ background: 'rgba(255, 23, 68, 0.15)', border: '1px solid #ff1744', borderRadius: '6px', padding: '14px 20px', color: '#ff1744', marginBottom: '20px' }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Grid of Telemetry Connectivity Status */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '20px', marginBottom: '24px' }}>
        {/* Card 1: Session Status */}
        <div className="rta-card">
          <div className="rta-panel-header">
            <span className="rta-tag-micro">// SESSIONS DETECTED</span>
            <span className="rta-badge rta-badge-orange">{sessions.length} SESSIONS</span>
          </div>
          {loading ? (
            <div style={{ color: 'var(--rta-text-secondary)', fontSize: '0.85rem' }}>Acquiring session list...</div>
          ) : sessions.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {sessions.slice(0, 4).map((s) => (
                <div key={s.sessionId} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 12px', background: 'rgba(0,0,0,0.3)', borderRadius: '4px', border: '1px solid rgba(255,255,255,0.05)' }}>
                  <div>
                    <strong style={{ color: '#ffffff', fontSize: '0.85rem' }}>#{s.sessionId} {s.name}</strong>
                    <div style={{ color: 'var(--rta-text-muted)', fontSize: '0.75rem' }}>{s.vehicleName} @ {s.trackName}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div style={{ color: '#00d2ff', fontFamily: 'var(--rta-font-mono)', fontSize: '0.85rem' }}>{s.bestLapTime || 'N/A'}</div>
                    <div style={{ color: 'var(--rta-text-muted)', fontSize: '0.7rem' }}>{s.totalLaps} LAPS</div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ color: 'var(--rta-text-secondary)', fontSize: '0.85rem' }}>
              No telemetry sessions currently recorded. Click "TEST DEMO DATA GENERATION" to run user-triggered demo generation.
            </div>
          )}
        </div>

        {/* Card 2: Latest Session Telemetry Feed */}
        <div className="rta-card">
          <div className="rta-panel-header">
            <span className="rta-tag-micro">// LATEST TELEMETRY BUNDLE</span>
            <span className="rta-badge rta-badge-cyan">
              {latestData ? `${latestData.totalPoints?.toLocaleString()} PTS` : 'AWAITING DATA'}
            </span>
          </div>
          {latestData ? (
            <div>
              <div style={{ marginBottom: '12px', fontSize: '0.9rem' }}>
                Active Machine: <strong style={{ color: '#ffffff' }}>{latestData.vehicle?.name}</strong> on <strong style={{ color: '#00d2ff' }}>{latestData.track?.name}</strong>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '8px', background: 'rgba(0,0,0,0.3)', padding: '10px', borderRadius: '4px' }}>
                <div>
                  <div style={{ fontSize: '0.7rem', color: 'var(--rta-text-muted)' }}>LAPS</div>
                  <div style={{ fontFamily: 'var(--rta-font-mono)', color: '#ffffff', fontWeight: 800 }}>{latestData.totalLaps}</div>
                </div>
                <div>
                  <div style={{ fontSize: '0.7rem', color: 'var(--rta-text-muted)' }}>TOTAL POINTS</div>
                  <div style={{ fontFamily: 'var(--rta-font-mono)', color: '#00d2ff', fontWeight: 800 }}>{latestData.totalPoints}</div>
                </div>
                <div>
                  <div style={{ fontSize: '0.7rem', color: 'var(--rta-text-muted)' }}>API STATUS</div>
                  <div style={{ fontFamily: 'var(--rta-font-mono)', color: '#00e676', fontWeight: 800 }}>READY</div>
                </div>
              </div>
            </div>
          ) : (
            <div style={{ color: 'var(--rta-text-secondary)', fontSize: '0.85rem' }}>
              No telemetry loaded. The backend strictly isolates data by Vehicle & Track.
            </div>
          )}
        </div>
      </div>

      {/* Architecture Plan for Step 6 Canvas Integration */}
      <div className="rta-card" style={{ borderLeft: '3px solid #ff1f38' }}>
        <div className="rta-panel-header">
          <span className="rta-tag-micro">// CANVAS INTEGRATION ARCHITECTURE (PRESERVED FOR STEP 6)</span>
          <span className="rta-badge rta-badge-red">CANVAS 60 FPS ENGINE</span>
        </div>
        <p style={{ color: 'var(--rta-text-secondary)', fontSize: '0.85rem', lineHeight: 1.6, margin: 0 }}>
          The high-performance vanilla HTML5 Canvas engine in <code style={{ color: '#00d2ff' }}>wwwroot/js/rollingTelemetry.js</code> and <code style={{ color: '#00d2ff' }}>Views/Telemetry/Index.cshtml</code> will be encapsulated inside a React component (<code style={{ color: '#ffffff' }}>&lt;TelemetryCanvasGraph /&gt;</code>) during Step 6 using <code style={{ color: '#ffffff' }}>useRef</code> and <code style={{ color: '#ffffff' }}>useEffect</code>.
          This ensures zero performance regression when rendering 65,000+ points at 60 FPS, maintaining binary-search culling, synchronized scrubber, and oscilloscope crosshair tooltips without overhead.
        </p>
      </div>
    </div>
  );
}

