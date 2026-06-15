import { Container, Group, List, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import { LogOut } from 'lucide-react';
import { useAuthStore } from '../../auth/authStore';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';

export function TrackedListPage() {
  const clear = useAuthStore((s) => s.clear);

  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <Stack gap="xs">
          <TitleLabel>Отслеживаемые</TitleLabel>
          <CaptionLabel>
            Заглушка для F9. Сейчас здесь проверяем, что Cloud-стиль
            применяется и что ProtectedRoute пускает залогиненного юзера.
          </CaptionLabel>
        </Stack>

        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>Навигация (F1 / F2 заглушки)</SubTitleLabel>
            <BodyLabel>
              Все ссылки ведут на стабовые страницы — там видно, что
              ссылка/роутинг работает.
            </BodyLabel>
            <List spacing="xs">
              <List.Item><Link to="/route">Маршрут</Link> (F14)</List.Item>
              <List.Item><Link to="/search">Поиск</Link> (F6)</List.Item>
              <List.Item><Link to="/at-grave">Добавить умершего</Link> (F8)</List.Item>
              <List.Item><Link to="/profile">Профиль</Link> (F16)</List.Item>
              <List.Item><Link to="/admin">Админка</Link> (F17)</List.Item>
              <List.Item>
                <Link to="/tracked/00000000-0000-0000-0000-000000000000">
                  Карточка (тест-id)
                </Link>{' '}
                (F11)
              </List.Item>
              <List.Item>
                <Link to="/style-demo">Демо дизайн-системы</Link> (F2, публичная)
              </List.Item>
            </List>
          </Stack>
        </CloudCard>

        <Group>
          <GhostButton
            onClick={clear}
            leftSection={<LogOut size={16} />}
            color="red"
            c="red"
            style={{ borderColor: '#C0392B' }}
          >
            Выйти (clear токенов)
          </GhostButton>
        </Group>
      </Stack>
    </Container>
  );
}
