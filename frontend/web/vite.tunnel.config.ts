// ВРЕМЕННЫЙ конфиг для показа веба наружу через туннель (ngrok / cloudflared).
// НЕ для продакшена. Удалить после теста (см. teardown ниже).
//
// Ничего не меняет в основном vite.config.ts и НЕ влияет на `npm run build`
// и на то, что уедет на сервер (dev-сервер и proxy в сборку не попадают).
//
// Провайдер туннеля конфиг не различает — оба отдают наружу один и тот же
// localhost:5173:
//   ngrok       — постоянный адрес (статический домен), ссылка не меняется;
//   cloudflared — quick tunnel, адрес случайный при каждом запуске.
//
// Запуск (из frontend/web, бэкенд должен слушать localhost:5000) — одной командой:
//   .\tunnel.ps1                  # cloudflared (по умолчанию)
//   .\tunnel.ps1 -Provider ngrok  # домен из $env:NGROK_DOMAIN
// либо вручную, в двух терминалах:
//   npx vite --mode tunnel --config vite.tunnel.config.ts
//   cloudflared tunnel --url http://localhost:5173
//   # или: ngrok http 5173 --domain=<твой>.ngrok-free.app
//
// Teardown: Ctrl+C оба процесса, удалить этот файл, tunnel.ps1 и .env.tunnel.local.
import { mergeConfig } from 'vite';
import baseConfig from './vite.config';

export default mergeConfig(baseConfig, {
  server: {
    // Разрешаем произвольный публичный host (*.trycloudflare.com,
    // *.ngrok-free.app), иначе Vite 5.4+ отвечает
    // "Blocked request. This host is not allowed.".
    allowedHosts: true,
    // Один origin для SPA и API: /api/* проксируется на локальный бэкенд,
    // поэтому CORS не нужен и appsettings трогать не надо.
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
      // Фото умерших/могил лежат в MinIO (:9000). Через https-туннель
      // прямой http://localhost:9000 не отдать (mixed content), поэтому
      // web строит same-origin путь /<bucket>/<key>, а мы проксируем его
      // на MinIO. Только публичные фото-бакеты.
      '/deceased-photos': { target: 'http://localhost:9000', changeOrigin: true },
      '/grave-photos': { target: 'http://localhost:9000', changeOrigin: true },
    },
    // Через https-туннель HMR-вебсокет всё равно не подключится —
    // отключаем, чтобы не сыпало ошибками в консоль. Для теста live-reload
    // не нужен.
    hmr: false,
  },
});
