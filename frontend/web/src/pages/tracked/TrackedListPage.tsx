import { Group, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { Plus } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { useAuthStore } from '../../auth/authStore';

/**
 * F2.1 / F4 / F6. Главная защищённая страница.
 * Кнопка "Добавить умершего" ведёт на /search (поиск перед
 * добавлением — анти-дубликат), не сразу на форму создания (F8).
 * Реальный список отслеживаемых появится в F9.
 */
export function TrackedListPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start">
        <Stack gap="xs">
          <TitleLabel>Отслеживаемые</TitleLabel>
          <CaptionLabel>
            {user
              ? `Привет, ${user.userName || user.email} (${user.role}).`
              : 'Заглушка для F9.'}
          </CaptionLabel>
        </Stack>
        <PrimaryButton
          onClick={() => navigate('/search')}
          leftSection={<Plus size={16} />}
        >
          Добавить умершего
        </PrimaryButton>
      </Group>

      <CloudCard>
        <Stack gap="sm">
          <BodyLabel>
            Реальный список отслеживаемых появится в F9. Сейчас навигация
            живёт в sidebar — клик по пунктам должен подсвечивать активный.
          </BodyLabel>
          <CaptionLabel>
            Кнопка "Добавить умершего" сверху ведёт на поиск (F6) —
            прежде чем создать новую карточку, мы проверяем, не завёл
            ли её кто-то раньше.
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}
