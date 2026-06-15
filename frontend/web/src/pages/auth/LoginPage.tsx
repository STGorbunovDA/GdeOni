import { Container, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../auth/authStore';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';

/**
 * F2. Заглушка логина с применёнными Cloud-стилями. Реальная форма —
 * в F4 (React Hook Form + Zod + POST /api/auth/login).
 */
export function LoginPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);

  function handleFakeLogin() {
    setTokens('fake-access-token-f1', 'fake-refresh-token-f1');
    navigate('/tracked', { replace: true });
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <CloudCard>
        <Stack gap="md">
          <TitleLabel>Вход</TitleLabel>
          <BodyLabel>
            Это временный экран (F2). Настоящая форма с email + паролем
            появится в F4. Сейчас просто кнопка, чтобы проверить
            ProtectedRoute и стили.
          </BodyLabel>
          <PrimaryButton onClick={handleFakeLogin}>
            Войти как тест-юзер
          </PrimaryButton>
          <CaptionLabel>
            Реальный логин — в F4. До тех пор — fake-токены в localStorage.
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Container>
  );
}
