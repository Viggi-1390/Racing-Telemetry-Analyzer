import React, { useEffect, useRef } from 'react';

export default function TrackContextMenu({ visible, x, y, track, onClose, onEdit, onDelete }) {
  const menuRef = useRef(null);

  useEffect(() => {
    function handleClickOutside(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        onClose();
      }
    }
    if (visible) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('scroll', onClose, true);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('scroll', onClose, true);
    };
  }, [visible, onClose]);

  if (!visible || !track) return null;

  // Ensure menu stays within screen viewport
  const style = {
    top: Math.min(y, window.innerHeight - 150),
    left: Math.min(x, window.innerWidth - 230),
  };

  return (
    <div ref={menuRef} className="track-context-menu" style={style}>
      <div className="context-menu-header">
        <span className="context-track-name" title={track.name}>
          {track.name}
        </span>
        <span className="context-pill">// ACTIONS</span>
      </div>
      <div className="context-menu-items">
        <button
          type="button"
          className="context-menu-item"
          onClick={() => {
            onClose();
            onEdit(track);
          }}
        >
          <span className="ctx-icon">✏️</span>
          <span>Edit Track Details</span>
        </button>
        <div className="context-menu-divider" />
        <button
          type="button"
          className="context-menu-item item-danger"
          onClick={() => {
            onClose();
            onDelete(track);
          }}
        >
          <span className="ctx-icon">🗑️</span>
          <span>Delete Track</span>
        </button>
      </div>
    </div>
  );
}

