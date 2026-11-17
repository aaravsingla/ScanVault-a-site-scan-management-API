import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In development the React app runs on :5173 and forwards API calls to the .NET API on :5297.
// In production `npm run build` writes the app into the API's wwwroot, so one Azure App Service serves both.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5297',
      '/health': 'http://localhost:5297',
      '/swagger': 'http://localhost:5297'
    }
  },
  build: {
    outDir: '../src/ScanVault.Api/wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 1500
  }
})
