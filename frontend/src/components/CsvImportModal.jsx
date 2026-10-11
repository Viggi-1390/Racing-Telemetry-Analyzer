import React, { useState } from 'react';
import { uploadCsvTelemetry } from '../api/client';

export default function CsvImportModal({ isOpen, onClose, vehicleId, trackId, onImportSuccess }) {
  const [file, setFile] = useState(null);
  const [sessionType, setSessionType] = useState('PRACTICE 1');
  const [uploading, setUploading] = useState(false);
  const [progressState, setProgressState] = useState({ percent: 0, stage: '', message: '' });
  const [errorMessage, setErrorMessage] = useState(null);

  if (!isOpen) return null;

  function handleFileChange(e) {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
      setErrorMessage(null);
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (!file) {
      setErrorMessage('Please select a .csv telemetry file to upload.');
      return;
    }

    try {
      setUploading(true);
      setErrorMessage(null);
      setProgressState({ percent: 0, stage: 'uploading', message: 'UPLOADING CSV FILE...' });

      const result = await uploadCsvTelemetry({
        file,
        vehicleId,
        trackId: trackId || 1,
        sessionType,
        onProgress: (prog) => {
          if (prog.stage === 'uploading') {
            setProgressState({
              percent: Math.min(99, prog.percent),
              stage: 'uploading',
              message: `UPLOADING FILE... (${prog.percent}%)`,
            });
          } else if (prog.stage === 'parsing') {
            const detail = prog.detail;
            const rowsMsg = detail?.processedRows
              ? `${detail.processedRows.toLocaleString()} rows streamed`
              : `${prog.percent}%`;
            setProgressState({
              percent: prog.percent,
              stage: 'parsing',
              message: `STREAMING ROWS INTO DATABASE... (${rowsMsg})`,
            });
          }
        },
      });

      if (result && result.success) {
        setProgressState({ percent: 100, stage: 'complete', message: 'IMPORT COMPLETED!' });
        setTimeout(() => {
          setUploading(false);
          setFile(null);
          onClose();
          if (onImportSuccess) {
            onImportSuccess(result.sessionId);
          }
        }, 500);
      } else {
        throw new Error(result?.message || 'Import failed.');
      }
    } catch (err) {
      setUploading(false);
      setErrorMessage(err.message || 'Failed to import CSV telemetry.');
    }
  }

  function handleOverlayClick(e) {
    if (e.target === e.currentTarget && !uploading) {
      onClose();
    }
  }

  return (
    <div className="rta-modal-overlay" onClick={handleOverlayClick}>
      <div className="rta-modal-dialog">
        <div className="rta-modal-header">
          <h5 className="rta-modal-title">
            <span style={{ color: '#00e5ff' }}>📁</span> IMPORT TELEMETRY CSV
          </h5>
          <button
            type="button"
            className="rta-btn-close"
            onClick={onClose}
            disabled={uploading}
            aria-label="Close"
          >
            ✕
          </button>
        </div>

        <p style={{ color: 'var(--rta-text-secondary, #9ca3af)', fontSize: '0.82rem', margin: '0 0 16px 0', lineHeight: 1.5 }}>
          Upload a raw session telemetry CSV log (up to 250 MB). All laps and high-frequency 60 FPS telemetry points will be streamed directly into the database.
        </p>

        {errorMessage && (
          <div style={{
            background: 'rgba(255, 23, 68, 0.15)',
            border: '1px solid #ff1744',
            borderRadius: '6px',
            padding: '10px 14px',
            color: '#ff1744',
            fontSize: '0.82rem',
            marginBottom: '16px'
          }}>
            <strong>Error:</strong> {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '0.72rem', fontWeight: 800, letterSpacing: '1px', color: '#ff5a1f', marginBottom: '6px' }}>
              🏁 SESSION TYPE
            </label>
            <select
              value={sessionType}
              onChange={(e) => setSessionType(e.target.value)}
              disabled={uploading}
              className="rta-input"
              style={{
                width: '100%',
                background: '#15151e',
                border: '1px solid #2a2a3c',
                color: '#fff',
                padding: '8px 10px',
                borderRadius: '4px',
                fontSize: '0.85rem',
                fontFamily: 'var(--rta-font-mono, monospace)',
                fontWeight: 700
              }}
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

          <div>
            <label style={{ display: 'block', fontSize: '0.72rem', fontWeight: 800, letterSpacing: '1px', color: '#9ca3af', marginBottom: '6px' }}>
              SELECT CSV DATASET
            </label>
            <input
              type="file"
              accept=".csv"
              onChange={handleFileChange}
              disabled={uploading}
              style={{
                width: '100%',
                background: '#15151e',
                border: '1px solid #2a2a3c',
                color: '#fff',
                padding: '6px 10px',
                borderRadius: '4px',
                fontSize: '0.8rem'
              }}
              required
            />
          </div>

          {uploading && (
            <div className="rta-progress-bar-wrap">
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', fontFamily: 'var(--rta-font-mono, monospace)', fontWeight: 700 }}>
                <span style={{ color: '#00e5ff' }}>{progressState.message}</span>
                <span style={{ color: '#9ca3af' }}>{progressState.percent}%</span>
              </div>
              <div className="rta-progress-bar-track">
                <div
                  className="rta-progress-bar-fill"
                  style={{ width: `${progressState.percent}%` }}
                />
              </div>
            </div>
          )}

          <div style={{ display: 'flex', gap: '10px', marginTop: '6px' }}>
            <button
              type="submit"
              disabled={uploading || !file}
              className="rta-btn-primary"
              style={{
                flex: 1,
                padding: '10px 16px',
                background: uploading ? '#2a2a3c' : 'linear-gradient(135deg, #00d2ff 0%, #0077b6 100%)',
                color: '#fff',
                fontWeight: 800,
                letterSpacing: '1px',
                fontSize: '0.82rem',
                border: 'none',
                borderRadius: '4px',
                cursor: uploading ? 'not-allowed' : 'pointer'
              }}
            >
              {uploading ? 'STREAMING DATASET...' : '⬆ UPLOAD & PARSE CSV'}
            </button>
            <button
              type="button"
              onClick={onClose}
              disabled={uploading}
              className="rta-btn-secondary"
              style={{
                padding: '10px 16px',
                background: '#15151e',
                border: '1px solid #2a2a3c',
                color: '#9ca3af',
                fontWeight: 700,
                fontSize: '0.82rem',
                borderRadius: '4px',
                cursor: uploading ? 'not-allowed' : 'pointer'
              }}
            >
              CANCEL
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

