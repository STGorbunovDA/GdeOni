import { Alert, Group, Loader, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { KeyRound, LogOut, RefreshCw } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';

/**
 * F16. Профиль пользователя — UserName / FullName / Email.
 * Role не показываем (зеркало решения mobile E18 от 2026-05-13: юзеру
 * это служебная информация, ничего не даёт).
 *
 * Subscription/admin/support блоки сюда сознательно не кладём — это
 * F17/F22+ и не входит в скоуп F16.
 */
export function ProfilePage() {
  const navigate = useNavigate();
  const clear = useAuthStore((s) => s.clear);

  const query = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  async function handleLogout() {
    await authApi.logout();
    clear();
    navigate('/login', { replace: true });
  }

  return (
    <Stack gap="lg">
      <TitleLabel>Профиль</TitleLabel>

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.data && (
        <CloudCard>
          <Stack gap="md">
            <Field label="Имя пользователя" value={query.data.userName} />
            <Field
              label="Полное имя"
              value={query.data.fullName ?? 'Не указано'}
            />
            <Field label="Email" value={query.data.email} />

            <Group>
              <GhostButton
                leftSection={<RefreshCw size={16} />}
                onClick={() => query.refetch()}
                loading={query.isFetching}
              >
                Обновить
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      <Group>
        <PrimaryButton
          leftSection={<KeyRound size={16} />}
          onClick={() => navigate('/change-password')}
        >
          Сменить пароль
        </PrimaryButton>
        <GhostButton leftSection={<LogOut size={16} />} onClick={handleLogout}>
          Выйти
        </GhostButton>
      </Group>
    </Stack>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <CaptionLabel>{label}</CaptionLabel>
      <BodyLabel>{value}</BodyLabel>
    </Stack>
  );
}
