# GdeOni Web

Web-фронт GdeOni на React 18 + Vite 5 + TypeScript.

## Что внутри F1

Каркас приложения: роутинг, ProtectedRoute, заглушки страниц. Реальные
страницы и API-интеграция — в следующих F-блоках (см.
`backend/docs/PlanFull.txt`).

## Запуск

```bash
cd frontend/web
npm install        # один раз
npm run dev        # http://localhost:5173
```

Backend должен быть запущен отдельно:

```bash
cd backend
dotnet run --project src/GdeOni.API/GdeOni.API.csproj --urls "http://0.0.0.0:5000"
```

## Структура src

```
src/
  api/        # axios + endpoints (F3)
  auth/       # Zustand store с токенами
  components/ # переиспользуемые блоки
  design/     # Mantine theme (F2)
  hooks/      # useGeolocation, useCurrentUser
  pages/      # страницы (роутинг-компоненты)
  routes/     # AppRouter, ProtectedRoute
  utils/      # парсеры, форматтеры
```

## Стек

- React 18.3
- Vite 5.4
- TypeScript 5.6 (strict)
- React Router 6.26
- TanStack Query 5
- Zustand 4 (с persist в localStorage)
- Mantine 7 (theme — в F2)
- Axios 1.7 (interceptors — в F3)
- React Hook Form + Zod (формы — в F4)
- lucide-react (иконки)
