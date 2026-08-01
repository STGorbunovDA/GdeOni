import { useState } from 'react';
import { Alert, Button, Group, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { MailWarning } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { usersApi, authApi } from '../../api/endpoints/authApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';

/**
 * D45. Баннер «Подтвердите email» для «старых» пользователей.
 *
 * Новых до подтверждения не пускает гейт входа, поэтому внутри приложения
 * неподтверждённым оказывается только тот, кто зарегистрировался ДО фичи
 * (EmailConfirmationRequired=false — доступ есть, но адрес не подтверждён).
 *
 * Живёт в AppLayout, поэтому переиспользует ['me'] (тот же ключ, что и
 * OutdatedLegalModal) — лишнего запроса нет. Кнопка шлёт письмо повторно
 * через анонимный resend, подставляя email текущего юзера.
 */
export function EmailConfirmationBanner() {
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);

  const me = meQuery.data;
  // Показываем только когда точно знаем, что адрес не подтверждён.
  if (!me || me.isEmailConfirmed !== false) return null;

  async function handleResend() {
    if (!me) return;
    setBusy(true);
    try {
      await authApi.resendConfirmation(me.email);
      setSent(true);
      notifications.show({
        title: 'Письмо отправлено',
        message: `Проверьте почту ${me.email} и перейдите по ссылке.`,
        color: 'blue',
      });
    } catch (e) {
      notifications.show({
        title: 'Не удалось отправить',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Alert
      variant="light"
      color="yellow"
      icon={<MailWarning size={20} />}
      mb="md"
      title="Подтвердите email"
      styles={{ title: { color: cloudColors.inkBlue } }}
    >
      <Group justify="space-between" align="center" wrap="wrap" gap="sm">
        <Text size="sm" c={cloudColors.text}>
          Мы отправили ссылку подтверждения на {me.email}. Перейдите по ней,
          чтобы подтвердить адрес.
        </Text>
        <Button
          variant="light"
          color="yellow"
          size="xs"
          radius="xl"
          loading={busy}
          onClick={handleResend}
        >
          {sent ? 'Отправить ещё раз' : 'Отправить письмо повторно'}
        </Button>
      </Group>
    </Alert>
  );
}
