import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import GaragePage from './pages/GaragePage';
import TracksPage from './pages/TracksPage';
import TelemetryPage from './pages/TelemetryPage';
import NotFoundPage from './pages/NotFoundPage';

export default function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<GaragePage />} />
          <Route path="/garage" element={<Navigate to="/" replace />} />
          <Route path="/tracks" element={<TracksPage />} />
          <Route path="/Track" element={<TracksPage />} />
          <Route path="/telemetry" element={<TelemetryPage />} />
          <Route path="/Telemetry" element={<TelemetryPage />} />
          <Route path="/Telemetry/Index" element={<TelemetryPage />} />
          <Route path="/Telemetry/Loading" element={<TelemetryPage />} />
          <Route path="/telemetry/loading" element={<TelemetryPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}

