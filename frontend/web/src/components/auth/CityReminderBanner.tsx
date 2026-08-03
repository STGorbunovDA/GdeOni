import { Alert, Button, Group, Text } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate } from 'react-router-dom';
import { MapPin } from 'lucide-react';
import { usersApi } from '../../api/endpoints/authApi';
import { cloudColors } from '../../design/theme';

/**
 * Баннер «Укажите город» — по образцу EmailConfirmationBanner. Показывается,
 * пока у пользователя не заполнен город (у аккаунтов, зарегистрированных до
 * введения поля, он пустой; новые тоже стартуют без города). Кнопка ведёт в
 * профиль, где город можно указать.
 *
 * Живёт в AppLayout и переиспользует ['me'] (тот же ключ, что и
 * EmailConfirmationBanner / OutdatedLegalModal) — лишнего запроса нет. На
 * самой странице профиля не показываемся, чтобы не дублировать поле рядом.
 */
export function CityReminderBanner() {
  const navigate = useNavigate();
  const location = useLocation();
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  const me = meQuery.data;
  // Показываем только когда точно знаем, что город не заполнен.
  if (!me) return null;
  const hasCity = typeof me.city === 'string' && me.city.trim().length > 0;
  if (hasCity) return null;
  // На странице профиля не мозолим глаза — там и так есть поле города.
  if (location.pathname === '/profile') return null;

  return (
    <Alert
      variant="light"
      color="blue"
      icon={<MapPin size={20} />}
      mb="md"
      title="Укажите город"
      styles={{ title: { color: cloudColors.inkBlue } }}
    >
      <Group justify="space-between" align="center" wrap="wrap" gap="sm">
        <Text size="sm" c={cloudColors.text}>
          У вас не указан город. Зайдите в профиль, чтобы указать его.
        </Text>
        <Button
          variant="light"
          color="blue"
          size="xs"
          radius="xl"
          onClick={() => navigate('/profile')}
        >
          Перейти в профиль
        </Button>
      </Group>
    </Alert>
  );
}
