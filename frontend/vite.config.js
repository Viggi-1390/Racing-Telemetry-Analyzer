import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5177',
        changeOrigin: true,
        secure: false,
      },
      '/images': {
        target: 'http://localhost:5177',
        changeOrigin: true,
        secure: false,
      },
      '/Telemetry': {
        target: 'http://localhost:5177',
        changeOrigin: true,
        secure: false,
      },
      '/Track': {
        target: 'http://localhost:5177',
        changeOrigin: true,
        secure: false,
      },
      '/Home': {
        target: 'http://localhost:5177',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});

