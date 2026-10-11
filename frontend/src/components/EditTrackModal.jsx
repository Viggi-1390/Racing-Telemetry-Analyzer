import React, { useState, useEffect } from 'react';
import { updateTrack } from '../api/client';

export default function EditTrackModal({ isOpen, track, onClose, onTrackUpdated }) {
  const [name, setName] = useState('');
  const [country, setCountry] = useState('');
  const [length, setLength] = useState('');
  const [turns, setTurns] = useState('');
  const [imageFile, setImageFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState('/images/tracks/default-track.jpg');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (track) {
      setName(track.name || '');
      setCountry(track.country || '');
      setLength(track.length !== undefined ? track.length : '');
      setTurns(track.numberOfTurns !== undefined ? track.numberOfTurns : '');
      setPreviewUrl(track.imagePath || '/images/tracks/default-track.jpg');
      setImageFile(null);
      setError(null);
    }
  }, [track]);

  if (!isOpen || !track) return null;

  function handleImageChange(e) {
    const file = e.target.files[0];
    if (file) {
      setImageFile(file);
      setPreviewUrl(URL.createObjectURL(file));
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);

    const lengthNum = parseFloat(length);
    const turnsNum = parseInt(turns, 10);

    if (!name.trim()) {
      setError('Circuit Name is required.');
      return;
    }
    if (!country.trim()) {
      setError('Country / Location is required.');
      return;
    }
    if (isNaN(lengthNum) || lengthNum <= 0 || lengthNum > 100) {
      setError('Circuit length must be a positive number between 0.1 and 100 km.');
      return;
    }
    if (isNaN(turnsNum) || turnsNum <= 0 || turnsNum > 200) {
      setError('Number of turns must be an integer between 1 and 200.');
      return;
    }

    try {
      setSaving(true);
      const formData = new FormData();
      formData.append('TrackId', track.id);
      formData.append('Name', name.trim().toUpperCase());
      formData.append('Country', country.trim());
      formData.append('Length', lengthNum);
      formData.append('NumberOfTurns', turnsNum);
      if (imageFile) {
        formData.append('ImageFile', imageFile);
      }

      const res = await updateTrack(track.id, formData);
      if (res && res.success) {
        onTrackUpdated(res.track);
        onClose();
      } else {
        setError(res.message || 'Failed to update circuit specifications.');
      }
    } catch (err) {
      setError(err.message || 'Server error while saving track modifications.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-motorsport" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h5 className="modal-title">
            <span className="text-red">✏️</span> EDIT CIRCUIT SPECIFICATIONS
          </h5>
          <button type="button" className="modal-btn-close" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && <div className="modal-alert-error">{error}</div>}

            {/* SECTION 1: IDENTITY */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 01 CIRCUIT IDENTITY</div>
              <div className="form-grid-2">
                <div className="form-field">
                  <label className="micro-label">CIRCUIT NAME</label>
                  <input
                    type="text"
                    className="form-control-dark"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g. MONZA"
                    required
                  />
                </div>
                <div className="form-field">
                  <label className="micro-label">COUNTRY / LOCATION</label>
                  <input
                    type="text"
                    className="form-control-dark"
                    value={country}
                    onChange={(e) => setCountry(e.target.value)}
                    placeholder="e.g. Italy"
                    required
                  />
                </div>
              </div>
            </div>

            {/* SECTION 2: METRICS */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 02 CIRCUIT METRICS</div>
              <div className="form-grid-2">
                <div className="form-field">
                  <label className="micro-label">CIRCUIT LENGTH (KM)</label>
                  <input
                    type="number"
                    step="0.001"
                    min="0.1"
                    max="100"
                    className="form-control-dark"
                    value={length}
                    onChange={(e) => setLength(e.target.value)}
                    placeholder="5.793"
                    required
                  />
                </div>
                <div className="form-field">
                  <label className="micro-label">NUMBER OF TURNS</label>
                  <input
                    type="number"
                    min="1"
                    max="200"
                    className="form-control-dark"
                    value={turns}
                    onChange={(e) => setTurns(e.target.value)}
                    placeholder="11"
                    required
                  />
                </div>
              </div>
            </div>

            {/* SECTION 3: SCHEMATIC */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 03 CIRCUIT MAP SCHEMATIC</div>
              <div className="image-upload-row">
                <div className="edit-img-preview-box">
                  <img
                    src={previewUrl}
                    alt="Circuit Preview"
                    onError={(e) => {
                      e.currentTarget.onerror = null;
                      e.currentTarget.src = '/images/tracks/default-track.jpg';
                    }}
                  />
                </div>
                <div style={{ flex: 1 }}>
                  <label className="micro-label">UPDATE CIRCUIT SCHEMATIC</label>
                  <input
                    type="file"
                    accept="image/*"
                    className="form-control-dark"
                    onChange={handleImageChange}
                  />
                  <small style={{ color: '#7a8294', fontSize: '0.72rem', display: 'block', marginTop: '4px' }}>
                    Leave empty to preserve existing schematic. Supports JPG, PNG, WEBP, SVG.
                  </small>
                </div>
              </div>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-motorsport-secondary" onClick={onClose} disabled={saving}>
              CANCEL
            </button>
            <button type="submit" className="btn-motorsport-primary" disabled={saving}>
              {saving ? 'SAVING...' : 'SAVE CHANGES'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

