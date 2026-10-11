import React from 'react';

export default function TrackMapSvg({ trackName = '' }) {
  const name = (trackName || '').toLowerCase();

  if (name.includes('interlagos') || name.includes('pace')) {
    return (
      <svg className="circuit-svg" viewBox="0 0 200 130" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M 175 105 L 80 105 C 55 105, 30 95, 32 75 C 34 58, 55 58, 68 70 L 82 85" stroke="#00d4ff" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 82 85 C 95 98, 120 92, 140 70 C 160 50, 180 42, 168 22 C 156 8, 125 12, 105 28 L 75 52" stroke="#ff5a1f" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 75 52 C 58 64, 42 52, 52 32 C 64 12, 125 12, 155 32 C 180 48, 190 85, 175 105" stroke="#00ff41" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <text x="36" y="82" fill="#00d4ff" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S1</text>
        <text x="145" y="58" fill="#ff5a1f" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S2</text>
        <text x="82" y="38" fill="#00ff41" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S3</text>
        <circle cx="175" cy="105" r="4" fill="#00ff41" stroke="#fff" strokeWidth="1"/>
      </svg>
    );
  }

  if (name.includes('le mans') || name.includes('sarthe')) {
    return (
      <svg className="circuit-svg" viewBox="0 0 200 130" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M 45 110 L 30 85 C 28 65, 45 50, 60 52 L 180 52" stroke="#00d4ff" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 180 52 L 180 115 L 140 115 L 115 100" stroke="#ff5a1f" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 115 100 C 95 90, 75 120, 45 110" stroke="#00ff41" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <text x="35" y="65" fill="#00d4ff" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S1</text>
        <text x="160" y="85" fill="#ff5a1f" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S2</text>
        <text x="80" y="115" fill="#00ff41" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S3</text>
        <circle cx="45" cy="110" r="4" fill="#00ff41" stroke="#fff" strokeWidth="1"/>
      </svg>
    );
  }

  if (name.includes('spa') || name.includes('francorchamps')) {
    return (
      <svg className="circuit-svg" viewBox="0 0 200 130" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M 30 95 L 30 110 L 45 110 C 60 90, 80 85, 110 50 L 165 25" stroke="#00d4ff" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 165 25 C 185 30, 185 60, 160 75 C 135 90, 110 70, 95 85 L 120 110" stroke="#ff5a1f" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <path d="M 120 110 C 145 115, 175 120, 150 95 L 80 95 L 30 95" stroke="#00ff41" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
        <text x="55" y="75" fill="#00d4ff" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S1</text>
        <text x="145" y="65" fill="#ff5a1f" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S2</text>
        <text x="95" y="105" fill="#00ff41" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S3</text>
        <circle cx="30" cy="95" r="4" fill="#00ff41" stroke="#fff" strokeWidth="1"/>
      </svg>
    );
  }

  // Dynamic generic circuit
  return (
    <svg className="circuit-svg" viewBox="0 0 200 130" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path d="M 35 110 C 15 90, 15 40, 45 25 C 75 10, 115 15, 135 25" stroke="#00d4ff" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
      <path d="M 135 25 C 155 35, 185 55, 175 80 C 165 105, 150 115, 130 110" stroke="#ff5a1f" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
      <path d="M 130 110 C 110 105, 80 120, 60 115 C 45 110, 40 115, 35 110" stroke="#00ff41" strokeWidth="3.5" strokeLinecap="round" fill="none"/>
      <text x="35" y="45" fill="#00d4ff" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S1</text>
      <text x="160" y="55" fill="#ff5a1f" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S2</text>
      <text x="85" y="118" fill="#00ff41" fontSize="7.5" fontFamily="monospace" fontWeight="bold">S3</text>
      <circle cx="35" cy="110" r="4" fill="#00ff41" stroke="#fff" strokeWidth="1"/>
    </svg>
  );
}

