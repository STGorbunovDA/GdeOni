# GdeOni Web

Web-фронт GdeOni на React 18 + Vite 5 + TypeScript (strict).

## Быстрый старт (dev)

```bash
cd frontend/web
npm install                 # один раз
cp .env.example .env.development
npm run dev                 # http://localhost:5173
```

Backend должен быть запущен отдельно:

```bash
cd backend
dotnet run --project src/GdeOni.API/GdeOni.API.csproj --urls "http://0.0.0.0:5000"
```

По умолчанию `.env.development` смотрит на `http://localhost:5000` —
поменяй `VITE_API_BASE_URL`, если бэк слушает на другом хосте/порту.

## Production build

```bash
cp .env.example .env.production
# отредактируй VITE_API_BASE_URL — публичный URL API-домена
# (или оставь пустым если фронт и бэк на одном origin через nginx-прокси)
npm run build               # → dist/ со статикой
```

`npm run build` = `tsc -b && vite build`, т.е. сначала type-check,
потом сборка. Ошибки TypeScript валят build. `dist/` содержит
самодостаточные ассеты — можно копировать на любой статик-хостинг.

## Деплой (nginx-way — рекомендуемый)

1. Собери на CI/локально `npm run build`, залей `dist/` на сервер
   в `/var/www/gdeoni/dist/`.
2. Возьми [nginx.conf.example](./nginx.conf.example) как шаблон,
   подставь свой `server_name`, пути к SSL-сертификатам, upstream
   backend'а. Скопируй в `/etc/nginx/sites-available/gdeoni.conf`
   и активируй через symlink в `sites-enabled/`.
3. Одинаковый origin для фронта и `/api` через reverse-proxy: CORS
   не нужен, cookie/bearer auth работает без хаков.
4. HTTPS обязателен — без него `navigator.geolocation` (F5) и SW не
   работают в prod-браузерах. Let's Encrypt / Certbot — стандартный
   путь.
5. SPA-роутинг: неизвестные пути отдают `index.html`, React Router
   разбирает клиентски (see `try_files $uri $uri/ /index.html`).

Альтернатива без nginx — раздача через ASP.NET static-file middleware.
Проще для первого деплоя, но CORS придётся включать на бэке
(`AllowSpecificOrigin` в `Program.cs`), плюс кеш-стратегия менее гибкая.

## Env vars

| Var                  | Что                                                                                        |
| -------------------- | ------------------------------------------------------------------------------------------ |
| `VITE_API_BASE_URL`  | Публичный URL backend'а. Пустое значение = same-origin (через nginx-прокси).               |

Все VITE\_\*-переменные бейкаются в бандл на этапе `npm run build` —
на runtime поменять нельзя. Для смены нужно пересобрать.

## Структура src

```
src/
  api/         # axios + endpoints (F3), types
  auth/        # Zustand store с токенами, schemas
  components/  # переиспользуемые блоки (Cloud-стиль)
  design/      # Mantine theme (F2)
  hooks/       # useGeolocation, useAppFeatures
  pages/       # страницы (роутинг-компоненты)
    admin/     # F17.* админ-панель
    profile/   # F16
    support/   # F17.14 обращения
    tracked/   # F9-F15 карточки умерших
    search/    # F6-F8 поиск
    route/     # F14 маршрут
    auth/      # F4 login/register
  routes/      # AppRouter, ProtectedRoute, AdminRoute
  utils/       # парсеры, форматтеры (formatDate, routing, mediaUrl)
```

## Стек

- React 18.3
- Vite 5.4
- TypeScript 5.6 (strict)
- React Router 6.26
- TanStack Query 5
- Zustand 4 (с persist в localStorage)
- Mantine 7 + `@mantine/dates`, `@mantine/hooks`, `@mantine/notifications`
- Axios 1.7 (interceptors: auth + refresh)
- React Hook Form + Zod (валидация форм)
- lucide-react (иконки)

## Что где смотреть

- План работ: [`backend/docs/PlanFull.txt`](../../backend/docs/PlanFull.txt) — F-блоки от F1 до F27.
- Backend-контракты: [`backend/src/GdeOni.API/Controllers/`](../../backend/src/GdeOni.API/Controllers/).
- Общие правила проекта: [`CLAUDE.md`](../../CLAUDE.md).
