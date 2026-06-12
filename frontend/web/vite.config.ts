import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// F1. Минимальная конфигурация Vite. Theme и CSS-модули — в F2.
// strictPort: чтобы не открыть случайно 5174 при занятом 5173 —
// сразу видно конфликт и решаем явно.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    host: 'localhost',
  },
});
