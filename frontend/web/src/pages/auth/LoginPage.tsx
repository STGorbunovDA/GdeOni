import {
  Anchor,
  Container,
  Group,
  PasswordInput,
  Stack,
  TextInput,
} from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Cloud } from 'lucide-react';
import { useState } from 'react';
import { useAuthStore } from '../../auth/authStore';
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
import { type LoginFormValues, loginSchema } from '../../auth/schemas';
import { formatError } from '../../auth/errorMessages';
import { InAppBrowserNotice } from '../../components/InAppBrowserNotice';
import { InstallPwaButton } from '../../components/pwa/InstallPwaButton';

/**
 * F4. Форма логина: email + password, валидация Zod, сабмит через
 * React Hook Form. При успехе → setSession + редирект на исходный
 * URL (если ProtectedRoute сохранил его в location.state.from)
 * или /tracked.
 */
type LocationStateFrom = { from?: string };

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const setSession = useAuthStore((s) => s.setSession);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  async function onSubmit(values: LoginFormValues) {
    setSubmitError(null);
    try {
      const resp = await authApi.login(values.email, values.password);
      setSession(resp.accessToken, resp.refreshToken, {
        id: resp.id,
        email: resp.email,
        userName: resp.userName,
        fullName: resp.fullName,
        role: resp.role,
      });
      const state = location.state as LocationStateFrom | null;
      const target = state?.from && state.from !== '/login' ? state.from : '/tracked';
      navigate(target, { replace: true });
    } catch (e) {
      setSubmitError(formatError(e));
    }
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Stack gap={6} align="center">
          <Cloud size={48} color={cloudColors.azureDeep} />
          {/* F37. Переключатель темы рядом с названием — до логина
              сайдбара нет, а сменить тему надо где-то. */}
          <Group gap={6} align="center" wrap="nowrap">
            <TitleLabel>ГдеОни</TitleLabel>
            <ThemeToggle size="md" />
          </Group>
        </Stack>
        <CaptionLabel>
          Войдите, чтобы продолжить.
        </CaptionLabel>
      </Stack>

      <InAppBrowserNotice />

      <CloudCard>
        <form onSubmit={handleSubmit(onSubmit)}>
          <Stack gap="md">
            <TextInput
              label="Email"
              placeholder="you@example.com"
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              {...register('email')}
            />
            <PasswordInput
              label="Пароль"
              placeholder="Ваш пароль"
              autoComplete="current-password"
              error={errors.password?.message}
              {...register('password')}
            />

            {submitError && (
              <BodyLabel c={cloudColors.errorRed}>{submitError}</BodyLabel>
            )}

            <PrimaryButton type="submit" loading={isSubmitting}>
              Войти
            </PrimaryButton>

            {/* D43. Ссылка восстановления — сразу под кнопкой входа:
                именно здесь человек понимает, что пароль не подходит. */}
            <CaptionLabel>
              <Anchor
                component={Link}
                to="/forgot-password"
                c={cloudColors.azureDeep}
              >
                Забыли пароль?
              </Anchor>
            </CaptionLabel>

            <CaptionLabel>
              Нет аккаунта?{' '}
              <Anchor component={Link} to="/register" c={cloudColors.azureDeep}>
                Зарегистрируйтесь
              </Anchor>
            </CaptionLabel>

            {/* PWA. «Скачать на смартфон» = установить сайт как приложение
                (Android/iPhone). Компактно, чтобы не отвлекать от логина. */}
            <Group justify="center">
              <InstallPwaButton
                label="Скачать на смартфон"
                variant="subtle"
                size="sm"
                fw={600}
              />
            </Group>
          </Stack>
        </form>
      </CloudCard>
    </Container>
  );
}
