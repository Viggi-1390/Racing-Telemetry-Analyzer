import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getTracks } from '../api/client';

export default function TracksPage() {
  const [tracks, setTracks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let isMounted = true;
    async function loadCircuits() {
      try {
        setLoading(true);
        const data = await getTracks();
        if (isMounted) {
          setTracks(data);
          setError(null);
        }
      } catch (err) {
        if (isMounted) {
          setError(err.message || 'Failed to connect to backend.');
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    loadCircuits();
    return () => { isMounted = false; };
  }, []);

  return (
    <div style={{ padding: '24px 32px', maxWidth: '1600px', margin: '0 auto', width: '100%' }}>
      {/* Top Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '24px' }}>
        <div>
          <div className="rta-tag-micro">// CIRCUIT WORKSTATION</div>
          <h1 style={{ margin: '4px 0 6px 0', fontSize: '1.8rem', fontWeight: 900, letterSpacing: '2px', color: '#ffffff' }}>
            CIRCUIT <span style={{ color: '#ff1f38' }}>REGISTRY</span>
          </h1>
          <p style={{ margin: 0, color: 'var(--rta-text-secondary)', fontSize: '0.85rem' }}>
            CIRCUIT REGISTRY CONNECTED TO GET /api/tracks
          </p>
        </div>

        <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
          <Link to="/" className="rta-btn-secondary">
            ◀ BACK TO GARAGE
          </Link>
          <Link to="/telemetry" className="rta-btn-primary">
            PROCEED TO TELEMETRY ➔
          </Link>
        </div>
      </div>

      {/* Connectivity Alert / Error */}
      {error && (
        <div style={{ background: 'rgba(255, 23, 68, 0.15)', border: '1px solid #ff1744', borderRadius: '6px', padding: '14px 20px', color: '#ff1744', marginBottom: '24px' }}>
          <strong>Connection Error:</strong> {error}
        </div>
      )}

      {/* Loading State */}
      {loading && (
        <div style={{ textAlign: 'center', padding: '60px 0', color: 'var(--rta-text-secondary)' }}>
          <div className="rta-tag-micro" style={{ marginBottom: '8px' }}>ACQUIRING CIRCUITS...</div>
          <div style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.9rem' }}>Querying /api/tracks from SQL Server LocalDB</div>
        </div>
      )}

      {/* Track Grid */}
      {!loading && !error && (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
            <span style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.8rem', color: 'var(--rta-text-muted)' }}>
              REGISTERED CIRCUITS: <strong style={{ color: '#ffffff' }}>{tracks.length}</strong>
            </span>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '16px' }}>
            {tracks.map((t) => (
              <div key={t.id} className="rta-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                    <span className="rta-badge rta-badge-cyan" style={{ fontSize: '0.7rem' }}>
                      {t.country?.toUpperCase()}
                    </span>
                    <span style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.75rem', color: 'var(--rta-text-muted)' }}>
                      TRK-0{t.id}
                    </span>
                  </div>

                  <h3 style={{ margin: '4px 0 12px 0', fontSize: '1.2rem', fontWeight: 800, color: '#ffffff', letterSpacing: '1px' }}>
                    {t.name}
                  </h3>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', background: 'rgba(0,0,0,0.3)', padding: '10px', borderRadius: '4px', border: '1px solid rgba(255,255,255,0.05)' }}>
                  <div>
                    <div style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.65rem', color: 'var(--rta-text-muted)' }}>CIRCUIT LENGTH</div>
                    <div style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.85rem', fontWeight: 800, color: '#00d2ff' }}>
                      {t.length ? Number(t.length).toFixed(3) : '0.000'} KM
                    </div>
                  </div>
                  <div>
                    <div style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.65rem', color: 'var(--rta-text-muted)' }}>TURNS</div>
                    <div style={{ fontFamily: 'var(--rta-font-mono)', fontSize: '0.85rem', fontWeight: 800, color: '#e2e5eb' }}>
                      {t.numberOfTurns} CORNERS
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

