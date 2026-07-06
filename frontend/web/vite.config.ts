/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * F1 / F22 / F19. Vite + Vitest конфигурация.
 *
 * strictPort: чтобы не открыть случайно 5174 при занятом 5173 —
 * сразу видно конфликт и решаем явно.
 *
 * F22 версионность: `VITE_APP_VERSION` подставляется на этапе build
 * через .env.production или переменную окружения CI (например,
 * `VITE_APP_VERSION=$(git rev-parse --short HEAD)`). fallback 'dev'
 * означает "локальная сборка" — версия в футере покажет 'dev'.
 *
 * F19 тесты: environment=jsdom нужен компонент-тестам React Testing
 * Library. globals=true — не тащить `import { describe, it, expect }`
 * в каждый файл, поведение как в Jest. setupFiles навешивает
 * @testing-library/jest-dom матчеры и MantineProvider-обёртку.
 */
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    host: 'localhost',
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    exclude: ['node_modules', 'dist'],
  },
});
