import React, { useState } from 'react';
import { deleteVehicle } from '../api/client';

export default function DeleteVehicleModal({ isOpen, vehicle, onClose, onVehicleDeleted }) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState(null);

  if (!isOpen || !vehicle) return null;

  async function handleDelete() {
    try {
      setDeleting(true);
      setError(null);

      const res = await deleteVehicle(vehicle.id);
      if (res && res.success) {
        onVehicleDeleted(vehicle.id);
        onClose();
      } else {
        setError(res?.message || 'Failed to delete vehicle.');
      }
    } catch (err) {
      setError(err.message || 'Error deleting vehicle.');
    } finally {
      setDeleting(false);
    }
  }

  return (
    <div className="rta-modal-backdrop" onClick={onClose}>
      <div className="modal-motorsport border-danger" style={{ maxWidth: '480px' }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header" style={{ borderColor: 'rgba(255, 31, 56, 0.3)' }}>
          <h5 className="modal-title" style={{ color: '#ff3b50' }}>
            <span>⚠️</span> DELETE VEHICLE?
          </h5>
          <button type="button" className="btn-close-custom" onClick={onClose} aria-label="Close">
            &times;
          </button>
        </div>

        <div className="modal-body" style={{ textAlign: 'center', padding: '24px 20px' }}>
          {error && (
            <div style={{ background: 'rgba(255, 31, 56, 0.15)', border: '1px solid #ff1f38', color: '#ff3b50', padding: '10px 14px', borderRadius: '4px', marginBottom: '16px', fontSize: '0.85rem' }}>
              {error}
            </div>
          )}

          <p style={{ margin: '0 0 8px 0', color: 'var(--rta-text-secondary, #828a9c)', fontSize: '0.9rem' }}>
            Are you sure you want to delete:
          </p>
          <h3 style={{ margin: '0 0 16px 0', color: '#ffffff', fontWeight: 900, letterSpacing: '1px', textTransform: 'uppercase' }}>
            "{vehicle.name}"
          </h3>

          <div style={{ background: 'rgba(234, 179, 8, 0.12)', border: '1px solid rgba(234, 179, 8, 0.35)', color: '#facc15', padding: '10px 14px', borderRadius: '6px', fontSize: '0.82rem', lineHeight: 1.5, textAlign: 'left' }}>
            <strong>Warning:</strong> This action cannot be undone. All telemetry sessions and analysis recorded for this vehicle will also be removed.
          </div>
        </div>

        <div className="modal-footer" style={{ justifyContent: 'center', gap: '14px', borderColor: 'rgba(255, 255, 255, 0.08)' }}>
          <button type="button" className="btn-outline-cancel" onClick={onClose} disabled={deleting}>
            CANCEL
          </button>
          <button type="button" className="btn-delete-confirm" onClick={handleDelete} disabled={deleting}>
            {deleting ? 'DELETING...' : 'DELETE VEHICLE'}
          </button>
        </div>
      </div>
    </div>
  );
}

