import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5298'

  // Proxy API calls to the backend so the browser sees same-origin requests
  // (refresh cookie and CORS stop being a problem). Applied to both dev and preview.
  const proxy = {
    '/api': {
      target: apiTarget,
      changeOrigin: true,
      secure: false,
    },
  }

  return {
    plugins: [react()],
    server: { proxy },
    preview: { proxy },
  }
})
