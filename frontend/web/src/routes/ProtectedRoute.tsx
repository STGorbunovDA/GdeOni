import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useIsAuthenticated } from '../auth/authStore';

/**
 * F1. Защита приватных маршрутов. Если access-токена в Zustand нет —
 * редирект на /login с сохранением исходного URL в state (после логина
 * вернёмся туда же). Зеркало AppShell.OnAppearing на mobile.
 *
 * В F4 сюда добавится проверка expiry токена + редирект на refresh.
 * В F22 — paywall guard для истёкшей подписки.
 */
export function ProtectedRoute() {
  const isAuthenticated = useIsAuthenticated();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  return <Outlet />;
}
