import { defineConfig, loadEnv } from 'vite'
import process from 'node:process'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = env.VITE_API_BASE_URL?.trim()
  return {
    plugins: [react()],
    server: {
      port: 3000,
      proxy: target ? { '/api': { target, changeOrigin: true } } : undefined,
    },
  }
})
