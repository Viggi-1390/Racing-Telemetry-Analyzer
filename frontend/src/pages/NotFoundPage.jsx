import React from 'react';
import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div style={{ textAlign: 'center', padding: '100px 24px' }}>
      <div className="rta-tag-micro" style={{ marginBottom: '12px' }}>// 404 OFF-TRACK</div>
      <h1 style={{ fontSize: '2.5rem', fontWeight: 900, letterSpacing: '2px', color: '#ffffff', marginBottom: '8px' }}>
        CIRCUIT <span style={{ color: '#ff1f38' }}>NOT FOUND</span>
      </h1>
      <p style={{ color: 'var(--rta-text-secondary)', marginBottom: '24px' }}>
        The requested pit-lane console route does not exist.
      </p>
      <Link to="/" className="rta-btn-primary">
        RETURN TO GARAGE
      </Link>
    </div>
  );
}

