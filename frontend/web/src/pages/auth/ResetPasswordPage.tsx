import {
  Anchor,
  Container,
  Group,
  PasswordInput,
  Stack,
} from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Cloud, ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  ThemeToggle,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { authApi } from '../../api/endpoints/authApi';
import {
  type ResetPasswordFormValues,
  resetPasswordSchema,
} from '../../auth/schemas';
import { formatError } from '../../auth/errorMessages';

/**
 * D43. Установка нового пароля по ссылке из письма.
 * Токен приходит в query: /reset-password?token=...
 *
 * Текущий пароль не спрашиваем — человек его и не помнит; подтверждением
 * личности служит сам токен. После успеха бэк закрывает все активные
 * сессии, поэтому ведём на страницу входа, а не внутрь приложения.
 */
export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [submitError, setSubmitError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  async function onSubmit(values: ResetPasswordFormValues) {
    setSubmitError(null);
    try {
      await authApi.resetPassword(token, values.newPassword);
      setDone(true);
    } catch (e) {
      setSubmitError(formatError(e));
    }
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Stack gap={6} align="center">
          <Cloud size={48} color={cloudColors.azureDeep} />
          <Group gap={6} align="center" wrap="nowrap">
            <TitleLabel>ГдеОни</TitleLabel>
            <ThemeToggle size="md" />
          </Group>
        </Stack>
        <CaptionLabel>Новый пароль.</CaptionLabel>
      </Stack>

      <CloudCard>
        {/* Ссылку открыли без токена — например, скопировали руками
            не целиком. Форму не показываем вовсе: сабмит всё равно
            отобьётся бэком, а так причина понятна сразу. */}
        {!token ? (
          <Stack gap="md" align="center">
            <BodyLabel ta="center">
              Ссылка неполная — в ней не хватает кода восстановления.
            </BodyLabel>
            <CaptionLabel ta="center">
              Откройте ссылку из письма целиком или запросите восстановление
              заново.
            </CaptionLabel>
            <Anchor
              component={Link}
              to="/forgot-password"
              c={cloudColors.azureDeep}
            >
              Запросить ссылку заново
            </Anchor>
          </Stack>
        ) : done ? (
          <Stack gap="md" align="center">
            <ShieldCheck size={40} color={cloudColors.azureDeep} />
            <BodyLabel ta="center">Пароль изменён.</BodyLabel>
            <CaptionLabel ta="center">
              Для безопасности мы завершили сессии на других устройствах —
              войдите заново с новым паролем.
            </CaptionLabel>
            <PrimaryButton onClick={() => navigate('/login', { replace: true })}>
              Войти
            </PrimaryButton>
          </Stack>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)}>
            <Stack gap="md">
              <BodyLabel>Придумайте новый пароль для входа.</BodyLabel>

              <PasswordInput
                label="Новый пароль"
                placeholder="Минимум 8 символов"
                autoComplete="new-password"
                error={errors.newPassword?.message}
                {...register('newPassword')}
              />
              <PasswordInput
                label="Повторите пароль"
                placeholder="Ещё раз"
                autoComplete="new-password"
                error={errors.confirmPassword?.message}
                {...register('confirmPassword')}
              />

              {submitError && (
                <BodyLabel c={cloudColors.errorRed}>{submitError}</BodyLabel>
              )}

              <PrimaryButton type="submit" loading={isSubmitting}>
                Сохранить пароль
              </PrimaryButton>

              <CaptionLabel>
                Ссылка не работает?{' '}
                <Anchor
                  component={Link}
                  to="/forgot-password"
                  c={cloudColors.azureDeep}
                >
                  Запросить заново
                </Anchor>
              </CaptionLabel>
            </Stack>
          </form>
        )}
      </CloudCard>
    </Container>
  );
}
