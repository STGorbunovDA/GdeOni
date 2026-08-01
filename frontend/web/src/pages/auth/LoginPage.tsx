import {
  Anchor,
  Button,
  Container,
  Group,
  PasswordInput,
  Stack,
  TextInput,
} from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Cloud, MailWarning } from 'lucide-react';
import { useState } from 'react';
import { notifications } from '@mantine/notifications';
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
import { formatError, getErrorCode } from '../../auth/errorMessages';
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
  // D45. Гейт: вход нового юзера закрыт до подтверждения email. Ловим код
  // user.email.not_confirmed и показываем панель «подтвердите почту» с
  // повторной отправкой — email берём из введённого в форме.
  const [needsConfirmEmail, setNeedsConfirmEmail] = useState<string | null>(null);
  const [resendBusy, setResendBusy] = useState(false);

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
      if (getErrorCode(e) === 'user.email.not_confirmed') {
        setNeedsConfirmEmail(values.email);
        return;
      }
      setSubmitError(formatError(e));
    }
  }

  async function handleResend() {
    if (!needsConfirmEmail) return;
    setResendBusy(true);
    try {
      await authApi.resendConfirmation(needsConfirmEmail);
      notifications.show({
        title: 'Письмо отправлено',
        message: `Проверьте почту ${needsConfirmEmail} и перейдите по ссылке.`,
        color: 'blue',
      });
    } catch (e) {
      notifications.show({
        title: 'Не удалось отправить',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setResendBusy(false);
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
        {needsConfirmEmail ? (
          <Stack gap="md" align="center">
            <MailWarning size={40} color={cloudColors.azureDeep} />
            <BodyLabel ta="center">Подтвердите email, чтобы войти.</BodyLabel>
            <CaptionLabel ta="center">
              Мы отправили ссылку на {needsConfirmEmail}. Перейдите по ней,
              затем войдите снова. Проверьте папку «Спам».
            </CaptionLabel>
            <Button
              variant="subtle"
              size="sm"
              fw={600}
              loading={resendBusy}
              onClick={handleResend}
            >
              Отправить письмо повторно
            </Button>
            <Anchor
              component="button"
              type="button"
              c={cloudColors.azureDeep}
              onClick={() => setNeedsConfirmEmail(null)}
            >
              Вернуться ко входу
            </Anchor>
          </Stack>
        ) : (
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
        )}
      </CloudCard>
    </Container>
  );
}
