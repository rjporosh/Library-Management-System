import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  build: {
    // The admin SPA ships as one bundle behind nginx (gzip); ~1 MB raw is expected.
    chunkSizeWarningLimit: 1200,
  },
  server: {
    port: 5173,
  },
})
