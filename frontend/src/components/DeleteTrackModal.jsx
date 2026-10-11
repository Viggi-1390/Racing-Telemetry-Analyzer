import React, { useState } from 'react';
import { deleteTrack } from '../api/client';

export default function DeleteTrackModal({ isOpen, track, onClose, onTrackDeleted }) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState(null);

  if (!isOpen || !track) return null;

  async function handleConfirmDelete() {
    try {
      setDeleting(true);
      setError(null);
      const res = await deleteTrack(track.id);
      if (res && res.success) {
        onTrackDeleted(track.id);
        onClose();
      } else {
        setError(res.message || 'Failed to delete circuit.');
      }
    } catch (err) {
      setError(err.message || 'Server error while removing circuit specification.');
    } finally {
      setDeleting(false);
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-motorsport border-danger" style={{ maxWidth: '480px' }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header" style={{ borderColor: 'rgba(255, 31, 56, 0.3)' }}>
          <h5 className="modal-title" style={{ color: '#ff1f38' }}>
            <span>⚠️</span> DELETE CIRCUIT SPECIFICATION?
          </h5>
          <button type="button" className="modal-btn-close" onClick={onClose}>✕</button>
        </div>

        <div className="modal-body" style={{ textAlign: 'center' }}>
          {error && <div className="modal-alert-error">{error}</div>}

          <p style={{ color: '#7a8294', fontSize: '0.88rem', marginBottom: '8px' }}>
            Are you sure you want to permanently remove:
          </p>
          <h3 style={{ color: '#ffffff', fontWeight: 900, fontSize: '1.3rem', letterSpacing: '1.5px', marginBottom: '16px', textTransform: 'uppercase' }}>
            {track.name}
          </h3>

          <div style={{ background: 'rgba(255, 31, 56, 0.15)', border: '1px solid rgba(255, 31, 56, 0.3)', borderRadius: '6px', padding: '10px 14px', color: '#ff8595', fontSize: '0.78rem', lineHeight: 1.5 }}>
            This action cannot be undone. Associated telemetry sessions registered for this circuit will be isolated or deleted.
          </div>
        </div>

        <div className="modal-footer" style={{ justifyContent: 'center', gap: '16px' }}>
          <button type="button" className="btn-motorsport-secondary" onClick={onClose} disabled={deleting}>
            CANCEL
          </button>
          <button type="button" className="btn-motorsport-danger" onClick={handleConfirmDelete} disabled={deleting}>
            {deleting ? 'DELETING...' : 'DELETE CIRCUIT'}
          </button>
        </div>
      </div>
    </div>
  );
}

