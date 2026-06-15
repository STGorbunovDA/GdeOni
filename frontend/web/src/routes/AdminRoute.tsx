import { Navigate, Outlet } from 'react-router-dom';
import { useIsAdmin } from '../auth/authStore';

/**
 * F4. UI-защита админ-роутов. Если у юзера роль НЕ Admin/SuperAdmin —
 * редирект на /tracked. Это слой защиты на клиенте; реальная
 * защита API всё равно живёт на бэке ([Authorize(Roles=...)] +
 * AuthorizationPolicies) — он отдаст 403 на любой админ-эндпоинт
 * от не-админа, даже если фронт пропустил.
 *
 * UI-проверка нужна, чтобы юзер не видел "пустых" страниц-заглушек
 * админки, на которых при попытке запроса всё равно получил бы 403.
 */
export function AdminRoute() {
  const isAdmin = useIsAdmin();
  if (!isAdmin) {
    return <Navigate to="/tracked" replace />;
  }
  return <Outlet />;
}
