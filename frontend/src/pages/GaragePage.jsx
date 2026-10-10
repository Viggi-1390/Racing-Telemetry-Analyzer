import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getVehicles } from '../api/client';
import AddVehicleModal from '../components/AddVehicleModal';
import EditVehicleModal from '../components/EditVehicleModal';
import DeleteVehicleModal from '../components/DeleteVehicleModal';
import ContextMenu from '../components/ContextMenu';
import '../styles/garage.css';

export default function GaragePage() {
  const [searchParams] = useSearchParams();
  const urlVehicleId = searchParams.get('vehicleId');

  const [vehicles, setVehicles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [notification, setNotification] = useState(null);

  const [selectedVehicleId, setSelectedVehicleId] = useState(null);
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [heroFading, setHeroFading] = useState(false);

  // Modals state: { type: 'add' | 'edit' | 'delete' | null, vehicle: null }
  const [modalState, setModalState] = useState({ type: null, vehicle: null });

  // Context Menu state: { visible: false, x: 0, y: 0, vehicle: null }
  const [contextMenu, setContextMenu] = useState({ visible: false, x: 0, y: 0, vehicle: null });

  const carouselTrackRef = useRef(null);

  // Notification auto-dismiss
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 3800);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  // Load initial fleet from backend
  useEffect(() => {
    let isMounted = true;
    async function loadFleet() {
      try {
        setLoading(true);
        setError(null);
        const data = await getVehicles();
        if (isMounted) {
          setVehicles(data);

          if (data.length > 0) {
            // Check if URL specified a vehicleId
            const requestedId = urlVehicleId ? parseInt(urlVehicleId, 10) : null;
            const target = data.find((v) => v.id === requestedId) || data[0];
            setSelectedVehicleId(target.id);

            // If requested vehicle is bike or production, adjust initial category filter
            if (target.type === 'bike') {
              setCategoryFilter('bike');
            } else if (target.category === 'production') {
              setCategoryFilter('production');
            }
          }
        }
      } catch (err) {
        if (isMounted) {
          setError(err.message || 'Failed to acquire vehicle fleet from backend.');
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    loadFleet();
    return () => { isMounted = false; };
  }, [urlVehicleId]);

  // Filter vehicles by category
  const filteredVehicles = useMemo(() => {
    if (categoryFilter === 'all') return vehicles;

    return vehicles.filter((v) => {
      const vType = (v.type || '').toLowerCase();
      const vCat = (v.category || '').toLowerCase();
      const vClass = (v.vClass || v.class || '').toUpperCase();
      const vName = (v.name || '').toUpperCase();

      if (categoryFilter === 'gt3') {
        return vType === 'car' && (vClass.includes('GT3') || vName.includes('R8') || vName.includes('GT3'));
      }
      if (categoryFilter === 'gtp') {
        return (
          vType === 'car' &&
          (vClass.includes('GTP') ||
            vClass.includes('LMDH') ||
            vClass.includes('HYPERCAR') ||
            vName.includes('CADILLAC') ||
            vName.includes('963') ||
            vName.includes('499P'))
        );
      }
      if (categoryFilter === 'f1') {
        return vType === 'car' && (vClass.includes('FORMULA') || vClass.includes('F1') || vName.includes('F1'));
      }
      if (categoryFilter === 'bike') {
        return vType === 'bike';
      }
      if (categoryFilter === 'production') {
        return vCat === 'production';
      }
      return true;
    });
  }, [vehicles, categoryFilter]);

  // Selected vehicle object
  const selectedVehicle = useMemo(() => {
    if (!vehicles.length) return null;
    return vehicles.find((v) => v.id === selectedVehicleId) || filteredVehicles[0] || vehicles[0];
  }, [vehicles, selectedVehicleId, filteredVehicles]);

  // Handle vehicle card selection with smooth transition
  function handleSelectVehicle(vehicle) {
    if (selectedVehicleId === vehicle.id) return;

    setHeroFading(true);
    setTimeout(() => {
      setSelectedVehicleId(vehicle.id);
      setHeroFading(false);
    }, 140);
  }

  // Category filter switch handler
  function handleCategoryFilter(filterKey) {
    setCategoryFilter(filterKey);

    // Auto-select first matching vehicle if current selection is filtered out
    const stillMatches = filteredVehicles.some((v) => v.id === selectedVehicleId);
    if (!stillMatches && filteredVehicles.length > 0) {
      handleSelectVehicle(filteredVehicles[0]);
    }
  }

  // Carousel scroll buttons
  function handleCarouselScroll(direction) {
    if (carouselTrackRef.current) {
      carouselTrackRef.current.scrollBy({
        left: direction === 'left' ? -580 : 580,
        behavior: 'smooth',
      });
    }
  }

  // Context Menu opener
  function handleCardContextMenu(e, vehicle) {
    e.preventDefault();
    e.stopPropagation();
    setContextMenu({
      visible: true,
      x: e.clientX,
      y: e.clientY,
      vehicle,
    });
  }

  // Callbacks for modal operations
  function handleVehicleCreated(newVehicle) {
    setVehicles((prev) => [newVehicle, ...prev]);
    setSelectedVehicleId(newVehicle.id);
    setNotification({ type: 'success', text: `✓ Successfully registered ${newVehicle.name} to garage fleet.` });
  }

  function handleVehicleUpdated(updatedVehicle) {
    setVehicles((prev) =>
      prev.map((v) => (v.id === updatedVehicle.id ? updatedVehicle : v))
    );
    setNotification({ type: 'success', text: `✓ Updated specifications for ${updatedVehicle.name}.` });
  }

  function handleVehicleDeleted(deletedId) {
    setVehicles((prev) => prev.filter((v) => v.id !== deletedId));

    // Choose next vehicle to select
    const remaining = vehicles.filter((v) => v.id !== deletedId);
    if (remaining.length > 0) {
      const nextSelected = remaining.find((v) => filteredVehicles.some((fv) => fv.id === v.id)) || remaining[0];
      setSelectedVehicleId(nextSelected.id);
    } else {
      setSelectedVehicleId(null);
    }

    setNotification({ type: 'success', text: '✓ Vehicle deleted from garage.' });
  }

  // Dynamic calculations for selected vehicle
  const tyreTemps = useMemo(() => {
    if (!selectedVehicle) return 'N/A';
    const power = selectedVehicle.power || 600;
    const speed = selectedVehicle.topSpeed || 300;
    const isBike = selectedVehicle.type === 'bike';

    if (isBike) {
      const f = Math.round(28 + 46 + (power / 250) * 8 + (speed / 300) * 5);
      const r = Math.round(28 + 52 + (power / 200) * 10 + (speed / 300) * 6);
      return `FRONT ${f}° / REAR ${r}°`;
    } else {
      const pScale = (power / 800) * 10;
      const sScale = (speed / 200) * 5;
      const fl = Math.round(28 + 49 + pScale + sScale);
      const fr = Math.round(28 + 51 + pScale + sScale);
      const rl = Math.round(28 + 47 + pScale * 0.9 + sScale);
      const rr = Math.round(28 + 49 + pScale * 0.9 + sScale);
      return `FL ${fl}° / FR ${fr}° / RL ${rl}° / RR ${rr}°`;
    }
  }, [selectedVehicle]);

  const powerToWeight = useMemo(() => {
    if (!selectedVehicle || !selectedVehicle.weight || selectedVehicle.weight <= 0) return 'N/A';
    return (selectedVehicle.power / selectedVehicle.weight).toFixed(2) + ' HP/KG';
  }, [selectedVehicle]);

  const extraSpec = useMemo(() => {
    if (!selectedVehicle) return { label: 'DRIVETRAIN', val: 'N/A' };
    const isBike = selectedVehicle.type === 'bike';

    if (isBike) {
      const engine = selectedVehicle.engine || '';
      const val = engine.includes('998') ? '998 cc' : (engine.includes('999') ? '999 cc' : '1000 cc');
      return { label: 'DISPLACEMENT', val };
    } else if (selectedVehicle.category === 'race') {
      const val = selectedVehicle.name?.includes('FERRARI F1') ? 'RWD Hybrid' : 'AWD Hybrid';
      return { label: 'DRIVETRAIN', val };
    } else {
      const name = selectedVehicle.name || '';
      const val = name.includes('BMW M4') || name.includes('PORSCHE 911') ? 'RWD' : 'AWD';
      return { label: 'DRIVETRAIN', val };
    }
  }, [selectedVehicle]);

  return (
    <>
      {/* CINEMATIC MOTORSPORT BACKGROUND & VIGNETTE */}
      <div className="cinematic-garage-bg" />
      <div className="cinematic-garage-overlay" />

      {/* NOTIFICATION TOAST */}
      {notification && (
        <div className={`garage-notification-banner ${notification.type}`}>
          {notification.text}
        </div>
      )}

      <div className="garage-workstation-container">
        {/* UPPER GRID: HERO VEHICLE + SPECIFICATIONS */}
        <div className="workstation-upper-grid">
          {/* PANEL 1: HERO DISPLAY */}
          <div className="dash-panel panel-hero-vehicle">
            <div className="panel-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span className="panel-tag">// 01</span>
                <span className="panel-title">SELECTED MACHINE</span>
              </div>
              {selectedVehicle && (
                <div className="hero-vehicle-badge">
                  {selectedVehicle.name} {selectedVehicle.vClass} — LIVE VIEW
                </div>
              )}
            </div>

            <div className="hero-image-stage">
              <div className="stage-crosshair top-left">+</div>
              <div className="stage-crosshair top-right">+</div>
              <div className="stage-crosshair bottom-left">+</div>
              <div className="stage-crosshair bottom-right">+</div>

              <div className="hero-ambient-underglow" />

              {loading ? (
                <div style={{ textAlign: 'center', padding: '40px', zIndex: 5 }}>
                  <div className="rta-tag-micro" style={{ marginBottom: '8px' }}>INITIALIZING PIT WORKSTATION...</div>
                  <div style={{ fontFamily: 'var(--rta-font-mono, monospace)', fontSize: '0.9rem', color: 'var(--rta-text-secondary, #828a9c)' }}>
                    Loading vehicle specifications from backend...
                  </div>
                </div>
              ) : selectedVehicle ? (
                <>
                  <img
                    src={selectedVehicle.imagePath || (selectedVehicle.type === 'bike' ? '/images/bikes/default-bike.jpg' : '/images/cars/default-car.jpg')}
                    alt={selectedVehicle.name}
                    className={`hero-vehicle-img ${heroFading ? 'fade-out' : ''}`}
                    onError={(e) => {
                      e.target.onerror = null;
                      e.target.src = selectedVehicle.type === 'bike' ? '/images/placeholder_bike.png' : '/images/placeholder_car.png';
                    }}
                  />
                  <img
                    src={selectedVehicle.imagePath || (selectedVehicle.type === 'bike' ? '/images/bikes/default-bike.jpg' : '/images/cars/default-car.jpg')}
                    alt="Reflection"
                    className={`hero-vehicle-reflect-img ${heroFading ? 'fade-out' : ''}`}
                    onError={(e) => {
                      e.target.style.display = 'none';
                    }}
                  />
                </>
              ) : (
                <div className="hero-fallback">
                  <div className="fallback-icon">🏎️</div>
                  <div className="fallback-title">NO VEHICLES IN GARAGE</div>
                  <div className="fallback-sub">
                    The garage fleet is currently empty. Click "+ ADD VEHICLE" below to register a race car or bike.
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* PANEL 2: SPECIFICATIONS */}
          <div className="dash-panel panel-vehicle-stats">
            <div className="panel-header">
              <span className="panel-tag">// 02</span>
              <span className="panel-title">SPECIFICATIONS</span>
            </div>

            {selectedVehicle ? (
              <>
                <div className="vehicle-identity-header">
                  <h1 className="stats-name" title={selectedVehicle.name}>
                    {selectedVehicle.name}
                  </h1>
                  <div className="stats-classification">
                    <span className="badge-class">{selectedVehicle.vClass || 'RACE'}</span>
                    <span className="badge-manufacturer">{selectedVehicle.manufacturer?.toUpperCase()}</span>
                  </div>
                </div>

                <div className="spec-table-list">
                  <div className="spec-row">
                    <span className="spec-title">POWER OUTPUT</span>
                    <span className="spec-num text-red">{selectedVehicle.power} HP</span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">CHASSIS WEIGHT</span>
                    <span className="spec-num">{selectedVehicle.weight} KG</span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">MAX VELOCITY</span>
                    <span className="spec-num text-cyan">{selectedVehicle.topSpeed} KM/H</span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">POWERTRAIN</span>
                    <span className="spec-num" style={{ maxWidth: '280px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={selectedVehicle.engine}>
                      {selectedVehicle.engine || 'Race Engine'}
                    </span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">PWR-TO-WEIGHT</span>
                    <span className="spec-num">{powerToWeight}</span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">{extraSpec.label}</span>
                    <span className="spec-num">{extraSpec.val}</span>
                  </div>
                  <div className="spec-row">
                    <span className="spec-title">TYRE TEMPERATURE</span>
                    <span className="spec-num spec-tyre-temp">{tyreTemps}</span>
                  </div>
                </div>

                <div className="analyze-btn-container">
                  <a
                    href={`/Track?vehicleId=${selectedVehicle.id}`}
                    className="btn-analyze-primary"
                    id="btnAnalyzeVehicle"
                    title={`Analyze ${selectedVehicle.name} on track`}
                  >
                    <span style={{ fontWeight: 900, letterSpacing: '2px' }}>ANALYZE VEHICLE</span>
                    <span className="btn-icon">➔</span>
                  </a>
                </div>
              </>
            ) : (
              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', textAlign: 'center', padding: '20px' }}>
                <p style={{ color: 'var(--rta-text-secondary, #828a9c)', fontSize: '0.85rem', marginBottom: '16px' }}>
                  No vehicle currently selected.
                </p>
                <button
                  type="button"
                  className="btn-red-primary"
                  onClick={() => setModalState({ type: 'add', vehicle: null })}
                >
                  + REGISTER VEHICLE
                </button>
              </div>
            )}
          </div>
        </div>

        {/* CENTERED ADD VEHICLE STRIP */}
        <div className="add-vehicle-center-strip">
          <button
            type="button"
            className="btn-add-vehicle-center"
            onClick={() => setModalState({ type: 'add', vehicle: null })}
          >
            <span className="plus-sign">+</span>
            <span>ADD VEHICLE</span>
          </button>
        </div>

        {/* LOWER SECTION: CATEGORY SIDEBAR + VEHICLE CAROUSEL */}
        <div className="workstation-carousel-container">
          {/* CATEGORY FILTER SIDEBAR */}
          <div className="vehicle-filter-sidebar">
            <div className="sidebar-micro-tag">// CATEGORIES</div>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'all' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('all')}
            >
              <span className="filter-dot" />
              <span>ALL MACHINES</span>
            </button>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'gt3' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('gt3')}
            >
              <span className="filter-dot" />
              <span>GT3 RACING</span>
            </button>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'gtp' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('gtp')}
            >
              <span className="filter-dot" />
              <span>GTP / LMDh</span>
            </button>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'f1' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('f1')}
            >
              <span className="filter-dot" />
              <span>FORMULA 1</span>
            </button>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'bike' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('bike')}
            >
              <span className="filter-dot" />
              <span>MOTORCYCLE</span>
            </button>
            <button
              type="button"
              className={`sidebar-filter-btn ${categoryFilter === 'production' ? 'active' : ''}`}
              onClick={() => handleCategoryFilter('production')}
            >
              <span className="filter-dot" />
              <span>PRODUCTION</span>
            </button>
          </div>

          {/* PREVIOUS ARROW */}
          <button
            type="button"
            className="carousel-arrow-btn prev-btn"
            onClick={() => handleCarouselScroll('left')}
            title="Previous Vehicles"
          >
            <span>‹</span>
          </button>

          {/* HORIZONTAL CAROUSEL VIEWPORT TRACK */}
          <div className="carousel-viewport-track" ref={carouselTrackRef} id="vehicleCarouselTrack">
            {error ? (
              <div style={{ color: '#ff3b50', padding: '20px', fontSize: '0.85rem' }}>
                Error loading garage: {error}
              </div>
            ) : filteredVehicles.length > 0 ? (
              filteredVehicles.map((v) => {
                const isActive = selectedVehicle?.id === v.id;
                return (
                  <div
                    key={v.id}
                    className={`vehicle-card ${isActive ? 'active' : ''}`}
                    onClick={() => handleSelectVehicle(v)}
                    onContextMenu={(e) => handleCardContextMenu(e, v)}
                    title={`${v.name} (Right-click for options)`}
                  >
                    {isActive && (
                      <div className="card-active-indicator">
                        <span className="card-indicator-dot" />
                        <span className="card-indicator-text">ACTIVE</span>
                      </div>
                    )}

                    <div className="card-image-box">
                      <img
                        src={v.imagePath || (v.type === 'bike' ? '/images/placeholder_bike.png' : '/images/placeholder_car.png')}
                        alt={v.name}
                        className="card-thumb"
                        onError={(e) => {
                          e.target.onerror = null;
                          e.target.src = v.type === 'bike' ? '/images/placeholder_bike.png' : '/images/placeholder_car.png';
                        }}
                      />
                      <div className="card-gradient-overlay" />
                      <div className="card-top-tags">
                        <span className="badge-mini-class">{v.vClass || 'RACE'}</span>
                        <span className="badge-mini-spec">{v.power} HP</span>
                      </div>
                    </div>

                    <div className="card-info-box">
                      <div className="card-vehicle-title" title={v.name}>
                        {v.name}
                      </div>
                      <div className="card-vehicle-sub">
                        <span className="card-mfg-badge">{v.manufacturer}</span>
                        <span className="card-speed-badge">{v.topSpeed} KM/H</span>
                      </div>
                    </div>
                  </div>
                );
              })
            ) : (
              <div style={{ color: 'var(--rta-text-secondary, #828a9c)', padding: '20px', fontFamily: 'var(--rta-font-mono, monospace)', fontSize: '0.85rem' }}>
                No machines match the selected filter category ({categoryFilter.toUpperCase()}).
              </div>
            )}
          </div>

          {/* NEXT ARROW */}
          <button
            type="button"
            className="carousel-arrow-btn next-btn"
            onClick={() => handleCarouselScroll('right')}
            title="Next Vehicles"
          >
            <span>›</span>
          </button>
        </div>
      </div>

      {/* RIGHT-CLICK CONTEXT MENU */}
      <ContextMenu
        menuState={contextMenu}
        onClose={() => setContextMenu({ visible: false, x: 0, y: 0, vehicle: null })}
        onEdit={(v) => setModalState({ type: 'edit', vehicle: v })}
        onDelete={(v) => setModalState({ type: 'delete', vehicle: v })}
      />

      {/* MODALS */}
      <AddVehicleModal
        isOpen={modalState.type === 'add'}
        onClose={() => setModalState({ type: null, vehicle: null })}
        onVehicleCreated={handleVehicleCreated}
      />

      <EditVehicleModal
        isOpen={modalState.type === 'edit'}
        vehicle={modalState.vehicle}
        onClose={() => setModalState({ type: null, vehicle: null })}
        onVehicleUpdated={handleVehicleUpdated}
      />

      <DeleteVehicleModal
        isOpen={modalState.type === 'delete'}
        vehicle={modalState.vehicle}
        onClose={() => setModalState({ type: null, vehicle: null })}
        onVehicleDeleted={handleVehicleDeleted}
      />
    </>
  );
}
