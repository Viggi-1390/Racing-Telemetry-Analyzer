import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { getTracks, getVehicle } from '../api/client';
import AddTrackModal from '../components/AddTrackModal';
import EditTrackModal from '../components/EditTrackModal';
import DeleteTrackModal from '../components/DeleteTrackModal';
import TrackContextMenu from '../components/TrackContextMenu';
import '../styles/tracks.css';

function getCountryFlag(country) {
  if (!country) return '🏁';
  const c = country.trim().toUpperCase();
  switch (c) {
    case 'ITALY': return '🇮🇹';
    case 'UNITED KINGDOM':
    case 'UK': return '🇬🇧';
    case 'FRANCE': return '🇫🇷';
    case 'BELGIUM': return '🇧🇪';
    case 'JAPAN': return '🇯🇵';
    case 'GERMANY': return '🇩🇪';
    case 'BRAZIL': return '🇧🇷';
    case 'USA':
    case 'UNITED STATES':
    case 'AMERICA': return '🇺🇸';
    case 'AUSTRALIA': return '🇦🇺';
    case 'SPAIN': return '🇪🇸';
    case 'NETHERLANDS': return '🇳🇱';
    case 'AUSTRIA': return '🇦🇹';
    case 'CANADA': return '🇨🇦';
    default: return '🏁';
  }
}

export default function TracksPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const urlVehicleId = searchParams.get('vehicleId');
  const parsedVehicleId = urlVehicleId ? parseInt(urlVehicleId, 10) : null;

  // Track and vehicle states
  const [tracks, setTracks] = useState([]);
  const [assignedVehicle, setAssignedVehicle] = useState(null);
  const [vehicleLoading, setVehicleLoading] = useState(true);
  const [vehicleError, setVehicleError] = useState(null);

  const [tracksLoading, setTracksLoading] = useState(true);
  const [tracksError, setTracksError] = useState(null);

  const [selectedTrackId, setSelectedTrackId] = useState(null);
  const [notification, setNotification] = useState(null);

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingTrack, setEditingTrack] = useState(null);
  const [deletingTrack, setDeletingTrack] = useState(null);

  // Context menu state
  const [contextMenu, setContextMenu] = useState({ visible: false, x: 0, y: 0, track: null });

  // Auto-dismiss notification
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 3800);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  // 1. Fetch assigned vehicle
  const loadAssignedVehicle = useCallback(async () => {
    if (!parsedVehicleId || isNaN(parsedVehicleId) || parsedVehicleId <= 0) {
      setVehicleLoading(false);
      setVehicleError('No vehicle assigned. Please select a machine from the Garage first.');
      setAssignedVehicle(null);
      return;
    }

    try {
      setVehicleLoading(true);
      setVehicleError(null);
      const vehicle = await getVehicle(parsedVehicleId);
      if (vehicle && vehicle.id) {
        setAssignedVehicle(vehicle);
      } else {
        setVehicleError(`Vehicle ID ${parsedVehicleId} was not found or has been removed.`);
        setAssignedVehicle(null);
      }
    } catch (err) {
      setVehicleError(err.message || `Failed to verify vehicle ID ${parsedVehicleId}.`);
      setAssignedVehicle(null);
    } finally {
      setVehicleLoading(false);
    }
  }, [parsedVehicleId]);

  useEffect(() => {
    loadAssignedVehicle();
  }, [loadAssignedVehicle]);

  // 2. Fetch tracks
  const loadTracks = useCallback(async () => {
    try {
      setTracksLoading(true);
      setTracksError(null);
      const data = await getTracks();
      setTracks(data || []);
    } catch (err) {
      setTracksError(err.message || 'Failed to acquire circuits from backend API.');
    } finally {
      setTracksLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTracks();
  }, [loadTracks]);

  // Find currently selected track
  const selectedTrack = useMemo(() => {
    return tracks.find((t) => t.id === selectedTrackId) || null;
  }, [tracks, selectedTrackId]);

  // Context menu handlers
  function handleCardContextMenu(e, track) {
    e.preventDefault();
    setContextMenu({
      visible: true,
      x: e.clientX,
      y: e.clientY,
      track,
    });
  }

  function handleKebabClick(e, track) {
    e.stopPropagation();
    const rect = e.currentTarget.getBoundingClientRect();
    setContextMenu({
      visible: true,
      x: rect.left,
      y: rect.bottom + 4,
      track,
    });
  }

  // Handlers for Add, Edit, Delete
  function handleTrackCreated(newTrack) {
    setTracks((prev) => [...prev, newTrack]);
    setSelectedTrackId(newTrack.id);
    setNotification(`✓ Registered new circuit: ${newTrack.name}`);
  }

  function handleTrackUpdated(updatedTrack) {
    setTracks((prev) =>
      prev.map((t) => (t.id === updatedTrack.id ? updatedTrack : t))
    );
    setNotification(`✓ Updated specifications for: ${updatedTrack.name}`);
  }

  function handleTrackDeleted(deletedId) {
    setTracks((prev) => prev.filter((t) => t.id !== deletedId));
    if (selectedTrackId === deletedId) {
      setSelectedTrackId(null);
    }
    setNotification('✓ Circuit specification removed from database.');
  }

  // Proceed / Start Analysis
  function handleProceedToTelemetry() {
    if (!assignedVehicle || !selectedTrackId) return;
    // Navigate to existing telemetry loading / analysis workstation
    const telemetryUrl = `/Telemetry/Loading?vehicleId=${assignedVehicle.id}&trackId=${selectedTrackId}`;
    window.location.href = telemetryUrl;
  }

  // Fallback image error handler
  function handleImgError(e) {
    e.currentTarget.style.display = 'none';
    const fallback = e.currentTarget.nextElementSibling;
    if (fallback) {
      fallback.style.display = 'flex';
    }
  }

  return (
    <div className="track-workstation-container">
      {/* Background Ambience */}
      <div className="cinematic-track-bg" />
      <div className="cinematic-track-overlay" />

      {/* TOP HEADER / CONTROL STRIP */}
      <div className="track-header-bar">
        <div className="track-title-block">
          <div className="track-micro-tag">// CIRCUIT WORKSTATION</div>
          <h1 className="track-page-title">
            TRACK <span className="text-red">SELECTION</span>
          </h1>
          <p className="track-page-desc">
            SELECT TARGET CIRCUIT FOR TELEMETRIC DATA ANALYSIS & PERFORMANCE ACQUISITION
          </p>
        </div>

        <div className="track-header-actions">
          {/* ASSIGNED MACHINE CONTEXT */}
          {assignedVehicle && (
            <div className="assigned-vehicle-badge">
              <span className="micro-indicator-dot" />
              {assignedVehicle.imagePath && (
                <img
                  src={assignedVehicle.imagePath}
                  alt={assignedVehicle.name}
                  className="assigned-vehicle-thumb"
                  onError={(e) => { e.currentTarget.style.display = 'none'; }}
                />
              )}
              <span className="badge-lbl">ASSIGNED MACHINE:</span>
              <span className="badge-val">{assignedVehicle.name}</span>
              <span className="badge-tag">
                {assignedVehicle.vClass || assignedVehicle.class || (assignedVehicle.type === 'car' ? 'RACE' : 'BIKE')}
              </span>
              <Link
                to={`/?vehicleId=${assignedVehicle.id}`}
                className="btn-change-vehicle"
                title="Return to Garage to change vehicle"
              >
                Change
              </Link>
            </div>
          )}

          <button
            type="button"
            className="btn-add-track-nav"
            onClick={() => setIsAddModalOpen(true)}
          >
            <span className="plus-sign">+</span>
            <span>ADD TRACK</span>
          </button>
        </div>
      </div>

      {/* TOAST NOTIFICATION */}
      {notification && (
        <div
          style={{
            position: 'absolute',
            top: '80px',
            right: '32px',
            zIndex: 100,
            background: 'rgba(9, 14, 22, 0.95)',
            border: '1px solid #00e676',
            boxShadow: '0 8px 24px rgba(0, 0, 0, 0.6), 0 0 16px rgba(0, 230, 118, 0.3)',
            borderRadius: '6px',
            padding: '10px 18px',
            color: '#00e676',
            fontFamily: 'var(--rta-font-mono, "JetBrains Mono", monospace)',
            fontSize: '0.82rem',
            letterSpacing: '1px',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          {notification}
        </div>
      )}

      {/* MAIN VIEWPORT */}
      <div className="track-grid-viewport">
        {/* CASE 1: RECOVERY STATE — INVALID OR MISSING VEHICLE */}
        {!vehicleLoading && vehicleError && (
          <div className="track-state-box">
            <div className="track-state-icon">⚠️</div>
            <h2 className="track-state-title">VEHICLE ASSIGNMENT REQUIRED</h2>
            <p className="track-state-desc">
              {vehicleError}
              <br />
              Telemetry acquisition requires an active machine from your garage before selecting a circuit.
            </p>
            <div className="track-state-actions">
              <Link to="/" className="btn-motorsport-primary">
                ◀ RETURN TO GARAGE
              </Link>
            </div>
          </div>
        )}

        {/* CASE 2: LOADING CIRCUITS */}
        {tracksLoading && !vehicleError && (
          <div className="track-state-box">
            <div className="track-micro-tag" style={{ marginBottom: '12px' }}>
              // ACQUIRING CIRCUIT REGISTRY
            </div>
            <h2 className="track-state-title">LOADING CIRCUITS...</h2>
            <p className="track-state-desc">
              Querying /api/tracks from SQL Server LocalDB...
            </p>
          </div>
        )}

        {/* CASE 3: TRACKS ERROR WITH RETRY */}
        {!tracksLoading && tracksError && !vehicleError && (
          <div className="track-state-box">
            <div className="track-state-icon">⚡</div>
            <h2 className="track-state-title" style={{ color: '#ff1f38' }}>
              CONNECTION ERROR
            </h2>
            <p className="track-state-desc">{tracksError}</p>
            <div className="track-state-actions">
              <button
                type="button"
                className="btn-motorsport-primary"
                onClick={loadTracks}
              >
                ↻ RETRY ACQUISITION
              </button>
              <Link to="/" className="btn-motorsport-secondary">
                RETURN TO GARAGE
              </Link>
            </div>
          </div>
        )}

        {/* CASE 4: EMPTY TRACKS STATE */}
        {!tracksLoading && !tracksError && tracks.length === 0 && !vehicleError && (
          <div className="track-state-box">
            <div className="track-state-icon">🏁</div>
            <h2 className="track-state-title">NO CIRCUITS REGISTERED</h2>
            <p className="track-state-desc">
              Your circuit registry is currently empty. You can register a custom circuit specification or use explicit seeding.
            </p>
            <div className="track-state-actions">
              <button
                type="button"
                className="btn-motorsport-primary"
                onClick={() => setIsAddModalOpen(true)}
              >
                + REGISTER NEW CIRCUIT
              </button>
            </div>
          </div>
        )}

        {/* CASE 5: ACTIVE TRACKS GRID */}
        {!tracksLoading && !tracksError && tracks.length > 0 && !vehicleError && (
          <div className="track-grid" id="trackGrid">
            {tracks.map((track) => {
              const isSelected = selectedTrackId === track.id;
              const flag = getCountryFlag(track.country);

              return (
                <div
                  key={track.id}
                  id={`track-${track.id}`}
                  className={`track-card ${isSelected ? 'active' : ''}`}
                  onClick={() => setSelectedTrackId(track.id)}
                  onContextMenu={(e) => handleCardContextMenu(e, track)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      setSelectedTrackId(track.id);
                    }
                  }}
                >
                  {/* CARD HEADER */}
                  <div className="track-card-header">
                    <div className="active-indicator-pill">
                      <span className="active-dot-pulse" />
                      <span>SELECTED</span>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <div className="track-flag-country">
                        <span className="track-flag-icon">{flag}</span>
                        <span>{track.country?.toUpperCase()}</span>
                      </div>
                      <button
                        type="button"
                        className="card-action-kebab"
                        title="Actions"
                        onClick={(e) => handleKebabClick(e, track)}
                      >
                        ⋮
                      </button>
                    </div>
                  </div>

                  {/* CIRCUIT NAME & INDEX */}
                  <div className="track-card-title-wrap">
                    <h2 className="track-card-name" title={track.name}>
                      {track.name}
                    </h2>
                    <span className="track-card-index">
                      // TRK-{String(track.id).padStart(2, '0')}
                    </span>
                  </div>

                  {/* CIRCUIT SCHEMATIC VIEWPORT */}
                  <div className="track-map-viewport">
                    <div className="hud-reticle hud-tl">+</div>
                    <div className="hud-reticle hud-tr">+</div>
                    <div className="hud-reticle hud-bl">+</div>
                    <div className="hud-reticle hud-br">+</div>

                    <img
                      src={track.imagePath}
                      alt={`${track.name} Schematic`}
                      className="track-circuit-img"
                      onError={handleImgError}
                    />

                    {/* FALLBACK SVG SCHEMATIC */}
                    <div className="track-fallback-schematic" style={{ display: 'none' }}>
                      <svg viewBox="0 0 100 50" className="fallback-circuit-svg">
                        <path
                          d="M 15,25 C 25,5 45,5 50,25 C 55,45 75,45 85,25 C 75,5 55,5 50,25 C 45,45 25,45 15,25 Z"
                          fill="none"
                          stroke="#ff1f38"
                          strokeWidth="1.6"
                          strokeDasharray="3 3"
                        />
                      </svg>
                      <span className="fallback-circuit-text">CIRCUIT SCHEMATIC STANDBY</span>
                    </div>

                    <div className="circuit-ambient-glow" />
                  </div>

                  {/* CIRCUIT SPECIFICATIONS */}
                  <div className="track-card-specs">
                    <div className="spec-col">
                      <span className="spec-label">LENGTH</span>
                      <span className="spec-value text-cyan">
                        {track.length ? Number(track.length).toFixed(3) : '0.000'} KM
                      </span>
                    </div>
                    <div className="spec-divider" />
                    <div className="spec-col">
                      <span className="spec-label">TURNS</span>
                      <span className="spec-value text-white">
                        {track.numberOfTurns}
                      </span>
                    </div>
                    <div className="spec-divider" />
                    <div className="spec-col">
                      <span className="spec-label">DIRECTION</span>
                      <span className="spec-value text-muted">CLOCKWISE</span>
                    </div>
                  </div>

                  {/* BOTTOM ACCENT STRIP */}
                  <div className="card-bottom-accent" />
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* BOTTOM ACTION BAR */}
      <div className="track-bottom-bar">
        <div className="bottom-selected-summary">
          <span className={selectedTrack ? 'status-dot-green' : 'status-dot-amber'} />
          <span className="summary-prefix">TARGET CIRCUIT:</span>
          <span className="summary-track-name" id="barTrackName">
            {selectedTrack ? selectedTrack.name : 'SELECT CIRCUIT'}
          </span>
          <span className="summary-track-meta" id="barTrackMeta">
            {selectedTrack
              ? `// ${selectedTrack.country.toUpperCase()} — ${Number(selectedTrack.length).toFixed(3)} KM, ${selectedTrack.numberOfTurns} TURNS`
              : '// AWAITING OPERATOR INPUT'}
          </span>
        </div>

        <div className="bottom-action-container">
          <button
            type="button"
            id="btnContinueTelemetry"
            className={`btn-proceed-telemetry ${!assignedVehicle || !selectedTrackId ? 'disabled' : ''}`}
            disabled={!assignedVehicle || !selectedTrackId}
            onClick={handleProceedToTelemetry}
            title={
              !assignedVehicle
                ? 'Assign a vehicle first'
                : !selectedTrackId
                ? 'Select a track to proceed'
                : `Proceed to Telemetry with ${assignedVehicle.name} at ${selectedTrack?.name}`
            }
          >
            <span className="btn-caption">PROCEED TO TELEMETRY</span>
            <span className="btn-arrow">➔</span>
          </button>
        </div>
      </div>

      {/* CONTEXT MENU */}
      <TrackContextMenu
        visible={contextMenu.visible}
        x={contextMenu.x}
        y={contextMenu.y}
        track={contextMenu.track}
        onClose={() => setContextMenu({ visible: false, x: 0, y: 0, track: null })}
        onEdit={(track) => setEditingTrack(track)}
        onDelete={(track) => setDeletingTrack(track)}
      />

      {/* ADD TRACK MODAL */}
      <AddTrackModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onTrackCreated={handleTrackCreated}
      />

      {/* EDIT TRACK MODAL */}
      <EditTrackModal
        isOpen={Boolean(editingTrack)}
        track={editingTrack}
        onClose={() => setEditingTrack(null)}
        onTrackUpdated={handleTrackUpdated}
      />

      {/* DELETE TRACK MODAL */}
      <DeleteTrackModal
        isOpen={Boolean(deletingTrack)}
        track={deletingTrack}
        onClose={() => setDeletingTrack(null)}
        onTrackDeleted={handleTrackDeleted}
      />
    </div>
  );
}
