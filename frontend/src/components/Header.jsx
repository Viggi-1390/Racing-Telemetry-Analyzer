import React from 'react';
import { NavLink } from 'react-router-dom';

export default function Header() {
  return (
    <header>
      <nav className="navbar-slim">
        <NavLink to="/" className="navbar-brand">
          <svg width="22" height="22" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M6 6H18C21.866 6 25 9.13401 25 13C25 16.866 21.866 20 18 20H13L22 28H15L7.5 20.5V28H6V6Z" fill="#ff1f38" />
          </svg>
          <span>
            RACING TELEMETRY <span className="accent-text">ANALYZER</span>
          </span>
        </NavLink>

        <div className="nav-links-cluster">
          <NavLink
            to="/"
            end
            className={({ isActive }) => `nav-link-btn ${isActive ? 'active' : ''}`}
          >
            GARAGE
          </NavLink>
          <NavLink
            to="/telemetry"
            className={({ isActive }) => `nav-link-btn ${isActive ? 'active' : ''}`}
          >
            TELEMETRY
          </NavLink>
          <NavLink
            to="/tracks"
            className={({ isActive }) => `nav-link-btn ${isActive ? 'active' : ''}`}
          >
            TRACKS
          </NavLink>
        </div>

        <div className="header-telemetry-badge">
          <span>
            <span className="status-dot-live"></span>
            SYSTEM ONLINE
          </span>
          <span style={{ color: 'rgba(255, 255, 255, 0.15)' }}>|</span>
          <span style={{ color: '#00d2ff' }}>RTA PRO v2.0</span>
        </div>
      </nav>
    </header>
  );
}

