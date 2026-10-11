import React from 'react';
import Header from './Header';

export default function Layout({ children }) {
  return (
    <div className="rta-app-root">
      <Header />
      <div className="app-main-viewport">
        {children}
      </div>
    </div>
  );
}

