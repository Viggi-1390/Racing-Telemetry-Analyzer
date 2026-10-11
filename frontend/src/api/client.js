/**
 * Centralized API Client for Racing Telemetry Analyzer
 * Communicates with ASP.NET Core .NET 8 backend via Vite development proxy (/api)
 */

async function request(endpoint, options = {}) {
  const url = endpoint.startsWith('http') ? endpoint : endpoint;
  
  const headers = {
    'Accept': 'application/json',
    ...(options.headers || {}),
  };

  // If body is NOT FormData, default to JSON content type
  if (options.body && !(options.body instanceof FormData) && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let errorMessage = `API error: HTTP ${response.status} ${response.statusText}`;
    try {
      const errorJson = await response.json();
      if (errorJson.message) {
        errorMessage = errorJson.message;
      }
    } catch (_) {
      // Non-JSON response error body
    }
    const error = new Error(errorMessage);
    error.status = response.status;
    throw error;
  }

  // Handle empty responses
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return await response.json();
  }
  return await response.text();
}

// ============================================================================
// VEHICLE ENDPOINTS
// ============================================================================

export async function getVehicles() {
  const res = await request('/api/vehicles');
  return res.vehicles || [];
}

export async function getVehicle(id) {
  const res = await request(`/api/vehicles/${id}`);
  return res.vehicle || res;
}

export async function createVehicle(payload) {
  const isFormData = payload instanceof FormData;
  return await request('/api/vehicles', {
    method: 'POST',
    body: isFormData ? payload : JSON.stringify(payload),
  });
}

export async function updateVehicle(id, payload) {
  const isFormData = payload instanceof FormData;
  return await request(`/api/vehicles/${id}`, {
    method: 'POST',
    body: isFormData ? payload : JSON.stringify(payload),
  });
}

export async function deleteVehicle(id) {
  return await request(`/api/vehicles/${id}`, {
    method: 'DELETE',
  });
}

// ============================================================================
// TRACK ENDPOINTS
// ============================================================================

export async function getTracks() {
  const res = await request('/api/tracks');
  return res.tracks || [];
}

export async function getTrack(id) {
  const res = await request(`/api/tracks/${id}`);
  return res.track || res;
}

export async function createTrack(payload) {
  const isFormData = payload instanceof FormData;
  return await request('/api/tracks', {
    method: 'POST',
    body: isFormData ? payload : JSON.stringify(payload),
  });
}

export async function updateTrack(id, payload) {
  const isFormData = payload instanceof FormData;
  return await request(`/api/tracks/${id}`, {
    method: 'POST',
    body: isFormData ? payload : JSON.stringify(payload),
  });
}

export async function deleteTrack(id) {
  return await request(`/api/tracks/${id}`, {
    method: 'DELETE',
  });
}

// ============================================================================
// SESSION & TELEMETRY ENDPOINTS
// ============================================================================

export async function getSessions(vehicleId = null, trackId = null) {
  const params = new URLSearchParams();
  if (vehicleId) params.append('vehicleId', vehicleId);
  if (trackId) params.append('trackId', trackId);

  const query = params.toString() ? `?${params.toString()}` : '';
  const res = await request(`/api/sessions${query}`);
  return res.sessions || [];
}

export async function getSessionTelemetry(sessionId, vehicleId = null, trackId = null) {
  const params = new URLSearchParams();
  if (vehicleId) params.append('vehicleId', vehicleId);
  if (trackId) params.append('trackId', trackId);

  const query = params.toString() ? `?${params.toString()}` : '';
  return await request(`/api/telemetry/session/${sessionId}${query}`);
}

export async function getLatestTelemetry(vehicleId = null, trackId = null) {
  const params = new URLSearchParams();
  if (vehicleId) params.append('vehicleId', vehicleId);
  if (trackId) params.append('trackId', trackId);

  const query = params.toString() ? `?${params.toString()}` : '';
  return await request(`/api/telemetry/latest${query}`);
}

export async function generateSyntheticTelemetry(vehicleId, trackId, sessionType = 'PRACTICE 1') {
  return await request('/api/telemetry/generate', {
    method: 'POST',
    body: JSON.stringify({ vehicleId, trackId, sessionType }),
  });
}

export async function getImportProgress(token) {
  return await request(`/api/telemetry/import-progress?token=${encodeURIComponent(token)}`);
}

export function uploadCsvTelemetry({ file, vehicleId, trackId, sessionType = 'PRACTICE 1', onProgress }) {
  return new Promise((resolve, reject) => {
    const uploadToken = 'tok_' + Math.random().toString(36).substring(2, 10) + '_' + Date.now();
    const formData = new FormData();
    formData.append('csvFile', file);
    formData.append('vehicleId', vehicleId);
    formData.append('trackId', trackId);
    formData.append('sessionType', sessionType);
    formData.append('uploadToken', uploadToken);

    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/telemetry/import');

    if (xhr.upload && onProgress) {
      xhr.upload.onprogress = (e) => {
        if (e.lengthComputable) {
          const percent = Math.round((e.loaded / e.total) * 100);
          onProgress({ stage: 'uploading', percent, loaded: e.loaded, total: e.total, token: uploadToken });
        }
      };
    }

    // Server-side parsing progress poller
    let pollInterval = null;
    if (onProgress) {
      pollInterval = setInterval(async () => {
        try {
          const prog = await getImportProgress(uploadToken);
          if (prog && prog.percentage !== undefined) {
            onProgress({ stage: 'parsing', percent: prog.percentage, detail: prog, token: uploadToken });
          }
        } catch (_) {
          // ignore transient poll errors
        }
      }, 250);
    }

    xhr.onload = () => {
      if (pollInterval) clearInterval(pollInterval);
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          const res = JSON.parse(xhr.responseText);
          resolve(res);
        } catch (err) {
          resolve({ success: true });
        }
      } else {
        let msg = `HTTP ${xhr.status}: ${xhr.statusText}`;
        try {
          const errRes = JSON.parse(xhr.responseText);
          if (errRes.message) msg = errRes.message;
        } catch (_) {}
        reject(new Error(msg));
      }
    };

    xhr.onerror = () => {
      if (pollInterval) clearInterval(pollInterval);
      reject(new Error('Network error during CSV upload.'));
    };

    xhr.send(formData);
  });
}

export default {
  getVehicles,
  getVehicle,
  createVehicle,
  updateVehicle,
  deleteVehicle,
  getTracks,
  getTrack,
  createTrack,
  updateTrack,
  deleteTrack,
  getSessions,
  getSessionTelemetry,
  getLatestTelemetry,
  generateSyntheticTelemetry,
  getImportProgress,
  uploadCsvTelemetry,
};

