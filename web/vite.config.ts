import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  // The site is served at subri.web.app/subri2gether
  base: '/subri2gether/',
  // Build into dist/subri2gether so Firebase serves it under that path.
  build: { outDir: 'dist/subri2gether', emptyOutDir: true },
  plugins: [react(), tailwindcss()],
})
