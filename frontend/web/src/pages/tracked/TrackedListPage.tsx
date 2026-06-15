import { Stack } from '@mantine/core';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import { useAuthStore } from '../../auth/authStore';

/**
 * F2.1 / F4. Главная защищённая страница. Здесь должен быть список
 * tracked deceased — придёт в F9. Сейчас просто маркер "роут работает"
 * и приветствие текущего юзера (после F4 login flow).
 */
export function TrackedListPage() {
  const user = useAuthStore((s) => s.user);

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Отслеживаемые</TitleLabel>
        <CaptionLabel>
          {user
            ? `Привет, ${user.userName || user.email} (${user.role}).`
            : 'Заглушка для F9.'}
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="sm">
          <BodyLabel>
            Реальный список отслеживаемых появится в F9. Сейчас навигация
            живёт в sidebar — клик по пунктам должен подсвечивать активный.
          </BodyLabel>
          <CaptionLabel>
            Пункт "Админка" виден только если у текущего юзера роль
            Admin или SuperAdmin.
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}
