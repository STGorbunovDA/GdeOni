import { Navigate, Outlet } from 'react-router-dom';
import { Stack } from '@mantine/core';
import { useAppFeatures } from '../hooks/useAppFeatures';
import { useSubscription } from '../hooks/useSubscription';
import { useIsAdmin } from '../auth/authStore';
import { BootstrapSplash } from '../auth/BootstrapSplash';

/**
 * F22 / E22.6. Клиентский paywall-gate. Зеркало серверного
 * `ActiveSubscriptionAuthorizationHandler` (D16.5) и мобильного
 * `PaywallEvaluator`.
 *
 * Логика (та же, что на бэке и mobile):
 *  - features.subscriptionEnabled=false → пропускаем (open-beta режим);
 *  - admin/superadmin роль → пропускаем (staff не платит);
 *  - subscription.isActiveNow=true (включая Trial и complimentary D22)
 *    → пропускаем;
 *  - иначе → редирект на /subscription-required.
 *
 * Пока features/subscription грузятся — показываем splash. Без этого
 * при первом заходе юзер увидит вспышку paywall'а на долю секунды.
 *
 * Whitelist сюда НЕ входит — он реализован в AppRouter через порядок
 * маршрутов: /profile, /subscription, /subscription-required,
 * /payment/return, /support/* и /change-password маунтятся ВНЕ этой
 * обёртки.
 */
export function RequireSubscription() {
  const features = useAppFeatures();
  const subscription = useSubscription();
  const isAdmin = useIsAdmin();

  // Ждём загрузки features и subscription. Ошибка features (500 например) —
  // не блокируем UI: логичнее пропустить, чем показать paywall из-за
  // технического сбоя эндпоинта features.
  if (features.isLoading || subscription.isLoading) {
    return (
      <Stack align="center" py="xl">
        <BootstrapSplash />
      </Stack>
    );
  }

  const subscriptionEnabled = features.data?.subscriptionEnabled ?? false;
  if (!subscriptionEnabled) {
    return <Outlet />;
  }

  if (isAdmin) {
    return <Outlet />;
  }

  const isActive = subscription.data?.isActiveNow ?? false;
  if (!isActive) {
    return <Navigate to="/subscription-required" replace />;
  }

  return <Outlet />;
}
