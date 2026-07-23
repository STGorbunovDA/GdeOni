import { Navigate, Outlet } from 'react-router-dom';
import { useIsSuperAdmin } from '../auth/authStore';

/**
 * D44. UI-защита роутов, доступных ТОЛЬКО владельцу сервиса
 * (роль SuperAdmin), без обычных админов.
 *
 * Сейчас это раздел обращений: в переписке идут платёжные реквизиты,
 * договорённости о переводах и решение о выдаче бесплатного доступа —
 * зона владельца, а не любого администратора.
 *
 * Как и AdminRoute, это только клиентский слой: настоящая защита —
 * [Authorize(Roles = "SuperAdmin")] на AdminSupportTicketsController
 * плюс IsSuperAdmin() внутри use case'ов (defense in depth).
 */
export function SuperAdminRoute() {
  const isSuperAdmin = useIsSuperAdmin();
  if (!isSuperAdmin) {
    return <Navigate to="/admin" replace />;
  }
  return <Outlet />;
}
