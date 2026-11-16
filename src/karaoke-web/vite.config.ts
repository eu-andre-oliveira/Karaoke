import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Em desenvolvimento o Vite faz proxy da API e do SignalR, então o navegador
// enxerga tudo na mesma origem (o cookie de login funciona sem CORS).
const apiTarget = process.env.VITE_API_TARGET ?? 'http://localhost:5009'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true, // permite abrir no celular pela rede local
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/hubs': { target: apiTarget, changeOrigin: true, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    globals: false,
    css: false,
  },
})
