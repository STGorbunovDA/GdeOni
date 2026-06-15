import { Container, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../auth/authStore';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';

/**
 * F2. Заглушка логина с Cloud-стилями.
 * F2.1: две кнопки — "войти как юзер" и "войти как админ" — чтобы
 * можно было проверить, что пункт "Админка" в sidebar появляется
 * только у админа. В F4 это станет реальной формой login + role
 * из JWT-claim.
 */
export function LoginPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);

  function loginAs(role: 'User' | 'Admin') {
    setSession('fake-access-token-f2', 'fake-refresh-token-f2', role);
    navigate('/tracked', { replace: true });
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <CloudCard>
        <Stack gap="md">
          <TitleLabel>Вход</TitleLabel>
          <BodyLabel>
            Это временный экран (F2.1). Настоящая форма с email + паролем
            появится в F4. Сейчас две кнопки для проверки роли в sidebar.
          </BodyLabel>
          <PrimaryButton onClick={() => loginAs('User')}>
            Войти как юзер
          </PrimaryButton>
          <GhostButton onClick={() => loginAs('Admin')}>
            Войти как админ
          </GhostButton>
          <CaptionLabel>
            Реальный логин — в F4. До тех пор — fake-токены и role
            в localStorage (ключ "gdeoni-auth").
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Container>
  );
}
