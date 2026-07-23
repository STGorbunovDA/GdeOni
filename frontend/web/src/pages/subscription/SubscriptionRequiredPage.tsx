import { Container, Stack } from '@mantine/core';
import { Cloud, LogOut, MessageCircle } from 'lucide-react';
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
import { useSubscriptionPrice } from '../../hooks/useSubscriptionPrice';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F22 / E22.6. Глобальный paywall — юзер попадает сюда:
 *  - через axios interceptor при 403 subscription.required
 *    (подписка истекла между табами);
 *  - через RequireSubscription в AppRouter при переходе на любой
 *    роут вне whitelist без активной подписки.
 *
 * D44. Пока онлайн-оплата не подключена (paymentsAvailable=false),
 * кнопка «Оформить подписку» отключена, а основным действием
 * становится «Написать в поддержку»: оплату проводим переводом,
 * договариваемся в переписке, доступ админ выдаёт вручную.
 * Без этого кнопка вела на заглушку с несуществующим checkout-URL.
 *
 * Зеркало SubscriptionRequiredPage на mobile.
 */
export function SubscriptionRequiredPage() {
  const navigate = useNavigate();
  const clear = useAuthStore((s) => s.clear);
  // F39. Цена — с бэка (см. useSubscriptionPrice), не текстом в разметке.
  const { priceLabel } = useSubscriptionPrice();
  const features = useAppFeatures();

  // Пока флаги грузятся, считаем оплату недоступной: показать рабочую
  // кнопку и тут же её погасить хуже, чем наоборот.
  const paymentsAvailable = features.data?.paymentsAvailable ?? false;

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
          {paymentsAvailable ? (
            <>
              <BodyLabel>
                Чтобы пользоваться приложением, оформите подписку
                {priceLabel ? ` — ${priceLabel}` : ''}. Доступ ко всем функциям
                без ограничений.
              </BodyLabel>
              <CaptionLabel>
                После оплаты вы автоматически вернётесь в приложение и сможете
                продолжить.
              </CaptionLabel>

              <PrimaryButton fullWidth onClick={() => navigate('/subscription')}>
                Оформить подписку
              </PrimaryButton>
            </>
          ) : (
            <>
              <BodyLabel>
                Пробный период закончился. Чтобы продолжить пользоваться
                приложением, нужна подписка
                {priceLabel ? ` — ${priceLabel}` : ''}.
              </BodyLabel>
              <CaptionLabel>
                Онлайн-оплата пока не подключена. Напишите нам — подскажем, как
                оплатить, и откроем доступ.
              </CaptionLabel>

              <PrimaryButton
                fullWidth
                leftSection={<MessageCircle size={16} />}
                onClick={() => navigate('/support/new?kind=Payment')}
              >
                Написать в поддержку
              </PrimaryButton>

              <GhostButton fullWidth onClick={() => navigate('/support/mine')}>
                Мои обращения
              </GhostButton>
            </>
          )}

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
