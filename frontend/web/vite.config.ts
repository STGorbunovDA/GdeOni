import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * F1 / F22. Минимальная конфигурация Vite. Theme и CSS-модули — в F2.
 * strictPort: чтобы не открыть случайно 5174 при занятом 5173 —
 * сразу видно конфликт и решаем явно.
 *
 * F22 версионность: `VITE_APP_VERSION` подставляется на этапе build
 * через .env.production или переменную окружения CI (например,
 * `VITE_APP_VERSION=$(git rev-parse --short HEAD)`). fallback 'dev'
 * означает "локальная сборка" — версия в футере покажет 'dev'.
 *
 * Раньше мы читали git SHA прямо здесь через execSync, но это
 * требовало @types/node ради одной строки. CI-скрипт проще.
 */
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    host: 'localhost',
  },
});
