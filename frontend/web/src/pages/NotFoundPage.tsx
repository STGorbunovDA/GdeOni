import { Container, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import {
  BodyLabel,
  CloudCard,
  TitleLabel,
} from '../components/ui';

export function NotFoundPage() {
  return (
    <Container size="xs" pt={64}>
      <CloudCard>
        <Stack gap="sm" align="center">
          <TitleLabel>404</TitleLabel>
          <BodyLabel>Страница не найдена.</BodyLabel>
          <Link to="/tracked">На главную</Link>
        </Stack>
      </CloudCard>
    </Container>
  );
}
