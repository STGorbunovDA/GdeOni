import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore, useIsAuthenticated } from '../auth/authStore';
import { BootstrapSplash } from '../auth/BootstrapSplash';

/**
 * F1 / F4. Защита приватных маршрутов.
 *  - Если идёт startup-refresh (SessionBootstrap) → показываем splash,
 *    НЕ редиректим. Иначе юзер с валидным persisted refresh-токеном
 *    видел бы вспышку /login при каждом обновлении страницы.
 *  - Если bootstrap закончился и токена нет → редирект на /login,
 *    сохраняем исходный URL в state.from (LoginPage использует его
 *    после успешного входа).
 *
 * Зеркало AppShell.OnAppearing.HasSessionAsync на mobile.
 */
export function ProtectedRoute() {
  const isAuthenticated = useIsAuthenticated();
  const isBootstrapping = useAuthStore((s) => s.isBootstrapping);
  const location = useLocation();

  if (isBootstrapping) {
    return <BootstrapSplash />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  return <Outlet />;
}
