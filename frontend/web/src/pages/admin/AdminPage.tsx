import { Container, List, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';

export function AdminPage() {
  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <Stack gap="xs">
          <TitleLabel>Админка</TitleLabel>
          <CaptionLabel>
            Полные разделы появятся в F17. Сейчас только заглушки для
            проверки маршрутов.
          </CaptionLabel>
        </Stack>

        <CloudCard>
          <Stack gap="sm">
            <BodyLabel>Разделы:</BodyLabel>
            <List spacing="xs">
              <List.Item><Link to="/admin/all">Все карточки</Link> (F17.1)</List.Item>
              <List.Item><Link to="/admin/users">Пользователи</Link> (F17.7)</List.Item>
              <List.Item><Link to="/tracked">← Назад в обычную часть</Link></List.Item>
            </List>
          </Stack>
        </CloudCard>
      </Stack>
    </Container>
  );
}
