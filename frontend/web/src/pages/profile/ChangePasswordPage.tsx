import { useState } from 'react';
import { Alert, Group, PasswordInput, Stack } from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import {
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';
import {
  type ChangePasswordFormValues,
  changePasswordSchema,
} from '../../auth/schemas';

/**
 * F16. Смена пароля. После 200 OK бэк ротирует SecurityStamp — текущий
 * access-токен умрёт через TTL ~30s (см. F4 OnTokenValidated). Чтобы не
 * показывать полминуты протухший UI и не словить 401 — сразу делаем
 * force-logout и редирект на /login. Зеркалит mobile E18.
 */
export function ChangePasswordPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  async function onSubmit(values: ChangePasswordFormValues) {
    setSubmitError(null);
    if (!user) {
      setSubmitError('Не удалось определить пользователя. Перезайдите.');
      return;
    }
    try {
      await usersApi.changePassword(user.id, {
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      setSuccess(true);
      // SecurityStamp ротирован — токены умрут через TTL. Чистим
      // store сразу и кидаем на /login, чтобы юзер увидел понятный
      // экран входа, а не случайный 401 на следующем переходе.
      await authApi.logout();
      clear();
      navigate('/login', { replace: true });
    } catch (e) {
      setSubmitError(formatError(e));
    }
  }

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/profile')}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>Сменить пароль</TitleLabel>
        <CaptionLabel>
          После смены нужно будет войти заново — текущая сессия
          завершится автоматически.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <form onSubmit={handleSubmit(onSubmit)}>
          <Stack gap="md">
            <PasswordInput
              label="Текущий пароль"
              autoComplete="current-password"
              error={errors.currentPassword?.message}
              {...register('currentPassword')}
            />
            <PasswordInput
              label="Новый пароль"
              autoComplete="new-password"
              error={errors.newPassword?.message}
              {...register('newPassword')}
            />
            <PasswordInput
              label="Повторите новый пароль"
              autoComplete="new-password"
              error={errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />

            {submitError && (
              <Alert color="red" variant="light">
                {submitError}
              </Alert>
            )}

            {success && (
              <Alert color="green" variant="light">
                Пароль изменён. Перенаправляем на вход…
              </Alert>
            )}

            <Group justify="flex-end">
              <PrimaryButton type="submit" loading={isSubmitting}>
                Сохранить
              </PrimaryButton>
            </Group>
          </Stack>
        </form>
      </CloudCard>
    </Stack>
  );
}
