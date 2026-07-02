import { Container, Stack } from '@mantine/core';
import { Cloud, LogOut } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { authApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';

/**
 * F22 / E22.6. Глобальный paywall — юзер попадает сюда:
 *  - через axios interceptor при 403 subscription.required
 *    (подписка истекла между табами);
 *  - через RequireSubscription в AppRouter при переходе на любой
 *    роут вне whitelist без активной подписки.
 *
 * Из paywall возможен только:
 *  - переход на /subscription (оформить);
 *  - logout.
 *
 * Зеркало SubscriptionRequiredPage на mobile.
 */
export function SubscriptionRequiredPage() {
  const navigate = useNavigate();
  const clear = useAuthStore((s) => s.clear);

  async function handleLogout() {
    await authApi.logout();
    clear();
    navigate('/login', { replace: true });
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Cloud size={48} color={cloudColors.azureDeep} />
        <TitleLabel>Нужна подписка</TitleLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <BodyLabel>
            Чтобы пользоваться приложением, оформите подписку — 49 ₽/мес.
            Доступ ко всем функциям без ограничений.
          </BodyLabel>
          <CaptionLabel>
            После оплаты вы автоматически вернётесь в приложение и сможете
            продолжить.
          </CaptionLabel>

          <PrimaryButton
            fullWidth
            onClick={() => navigate('/subscription')}
          >
            Оформить подписку
          </PrimaryButton>

          <GhostButton
            fullWidth
            leftSection={<LogOut size={16} />}
            onClick={handleLogout}
          >
            Выйти из аккаунта
          </GhostButton>
        </Stack>
      </CloudCard>
    </Container>
  );
}
