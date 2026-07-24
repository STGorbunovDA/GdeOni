/// <reference types="vitest" />
import { execSync } from 'node:child_process';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * F22. Версия веб-сборки для футера (профиль / админ-sidebar). Считается
 * АВТОМАТИЧЕСКИ на этапе build и подставляется в код как глобальная
 * константа `__APP_VERSION__` (см. hooks/useAppVersion.ts). Раньше версия
 * бралась из статичной `VITE_APP_VERSION` в .env.production и «застывала»
 * (показывала 1.0.0 после каждого деплоя).
 *
 * Приоритет: явный `VITE_APP_VERSION` из окружения (релизный тег в CI) →
 * иначе короткий git-SHA + дата сборки (меняется каждым деплоем, удобно
 * поддержке) → иначе 'dev' (git недоступен / локальная сборка).
 */
function resolveAppVersion(): string {
  const explicit = process.env.VITE_APP_VERSION?.trim();
  if (explicit && explicit !== 'dev') return explicit;

  try {
    const sha = execSync('git rev-parse --short HEAD', {
      stdio: ['ignore', 'pipe', 'ignore'],
    })
      .toString()
      .trim();
    const date = new Date().toISOString().slice(0, 10);
    return sha ? `${sha} · ${date}` : `build ${date}`;
  } catch {
    return 'dev';
  }
}

/**
 * F1 / F19. Vite + Vitest конфигурация.
 *
 * strictPort: чтобы не открыть случайно 5174 при занятом 5173 —
 * сразу видно конфликт и решаем явно.
 *
 * F19 тесты: environment=jsdom нужен компонент-тестам React Testing
 * Library. globals=true — не тащить `import { describe, it, expect }`
 * в каждый файл, поведение как в Jest. setupFiles навешивает
 * @testing-library/jest-dom матчеры и MantineProvider-обёртку.
 */
export default defineConfig({
  plugins: [react()],
  // F22. Версия сборки → глобальная константа __APP_VERSION__.
  define: {
    __APP_VERSION__: JSON.stringify(resolveAppVersion()),
  },
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
