import React, { useEffect, useRef } from 'react';

export default function ContextMenu({ menuState, onClose, onEdit, onDelete }) {
  const menuRef = useRef(null);

  useEffect(() => {
    function handleClickOutside(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        onClose();
      }
    }

    function handleKeyDown(e) {
      if (e.key === 'Escape') {
        onClose();
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleKeyDown);
    window.addEventListener('resize', onClose);
    window.addEventListener('scroll', onClose, true);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('resize', onClose);
      window.removeEventListener('scroll', onClose, true);
    };
  }, [onClose]);

  if (!menuState || !menuState.visible || !menuState.vehicle) return null;

  const { x, y, vehicle } = menuState;

  // Window bounds safety check
  const menuWidth = 220;
  const menuHeight = 110;
  let posX = x + 4;
  let posY = y + 4;

  if (typeof window !== 'undefined') {
    if (posX + menuWidth > window.innerWidth - 10) {
      posX = Math.max(10, x - menuWidth - 4);
    }
    if (posY + menuHeight > window.innerHeight - 10) {
      posY = Math.max(10, y - menuHeight - 4);
    }
  }

  return (
    <div
      ref={menuRef}
      className="vehicle-context-menu"
      style={{ left: `${posX}px`, top: `${posY}px` }}
      onClick={(e) => e.stopPropagation()}
    >
      <div className="context-menu-header">
        <span className="context-vehicle-name" title={vehicle.name}>
          {vehicle.name}
        </span>
        <span className="context-pill">// ACTIONS</span>
      </div>

      <div className="context-menu-items">
        <button
          type="button"
          className="context-menu-item"
          onClick={() => {
            onClose();
            onEdit(vehicle);
          }}
        >
          <span className="ctx-icon">✏️</span>
          <span>Edit Vehicle Stats</span>
        </button>

        <div className="context-menu-divider"></div>

        <button
          type="button"
          className="context-menu-item item-danger"
          onClick={() => {
            onClose();
            onDelete(vehicle);
          }}
        >
          <span className="ctx-icon">🗑️</span>
          <span>Delete Vehicle</span>
        </button>
      </div>
    </div>
  );
}

