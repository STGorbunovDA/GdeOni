import {
  Anchor,
  Button,
  Checkbox,
  Container,
  Group,
  PasswordInput,
  Stack,
  TextInput,
} from '@mantine/core';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { Cloud, MailCheck } from 'lucide-react';
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
import { authApi, usersApi } from '../../api/endpoints/authApi';
import {
  type RegisterFormValues,
  registerSchema,
} from '../../auth/schemas';
import { DateMaskInput } from '../../components/DateMaskInput';
import { formatError } from '../../auth/errorMessages';
import { toDateInputValue } from '../../utils/formatDate';
import { InAppBrowserNotice } from '../../components/InAppBrowserNotice';

/**
 * F4. Регистрация: email + password + confirm + (опционально) имя.
 * Обязательны два чекбокса согласия (D19, ФЗ-152). После успешной
 * регистрации (POST /api/users) сразу делаем login (бэк токены при
 * регистрации не возвращает) → редирект на /tracked.
 */
export function RegisterPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);
  const [submitError, setSubmitError] = useState<string | null>(null);
  // D45. После регистрации нового юзера вход закрыт до подтверждения email
  // (гейт). Тогда вместо авто-логина показываем экран «проверьте почту» и
  // помним адрес — чтобы кнопка «отправить повторно» знала, куда слать.
  const [sentEmail, setSentEmail] = useState<string | null>(null);
  const [resendBusy, setResendBusy] = useState(false);

  async function handleResend() {
    if (!sentEmail) return;
    setResendBusy(true);
    try {
      await authApi.resendConfirmation(sentEmail);
      notifications.show({
        title: 'Письмо отправлено',
        message: `Проверьте почту ${sentEmail} и перейдите по ссылке.`,
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

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      fullName: '',
      // birthDate: undefined в defaults, чтобы поле выглядело пустым;
      // Zod required-refine отработает при submit.
      birthDate: undefined as unknown as Date,
      privacyPolicyAccepted: false as unknown as true,
      termsAccepted: false as unknown as true,
      // Функция «Родственники»: по умолчанию включено.
      allowRelativeConnections: true,
    },
  });

  async function onSubmit(values: RegisterFormValues) {
    setSubmitError(null);
    try {
      const reg = await usersApi.register({
        email: values.email,
        password: values.password,
        fullName: values.fullName?.trim() || undefined,
        // ISO date «yyyy-MM-dd» без учёта таймзоны — DateOnly на бэке.
        birthDate: toDateInputValue(values.birthDate),
        allowRelativeConnections: values.allowRelativeConnections,
      });

      // D45. Новому юзеру вход закрыт до подтверждения email — показываем
      // экран «проверьте почту», логиниться нет смысла (гейт отобьёт).
      if (reg.requiresEmailConfirmation) {
        setSentEmail(values.email);
        return;
      }

      // Канал подтверждения не настроен (dev без SMTP) — гейта нет, логинимся
      // сразу с теми же creds, как раньше.
      const resp = await authApi.login(values.email, values.password);
      setSession(resp.accessToken, resp.refreshToken, {
        id: resp.id,
        email: resp.email,
        userName: resp.userName,
        fullName: resp.fullName,
        role: resp.role,
      });
      navigate('/tracked', { replace: true });
    } catch (e) {
      setSubmitError(formatError(e));
    }
  }

  return (
    <Container size="xs" pt={64} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Stack gap={6} align="center">
          <Cloud size={48} color={cloudColors.azureDeep} />
          {/* F37. Переключатель темы рядом с названием — как на входе. */}
          <Group gap={6} align="center" wrap="nowrap">
            <TitleLabel>ГдеОни</TitleLabel>
            <ThemeToggle size="md" />
          </Group>
        </Stack>
        <CaptionLabel>Создайте аккаунт, чтобы начать.</CaptionLabel>
      </Stack>

      <InAppBrowserNotice />

      <CloudCard>
        {sentEmail ? (
          <Stack gap="md" align="center">
            <MailCheck size={40} color={cloudColors.azureDeep} />
            <BodyLabel ta="center">Почти готово — подтвердите email.</BodyLabel>
            <CaptionLabel ta="center">
              Мы отправили ссылку на {sentEmail}. Перейдите по ней, чтобы
              подтвердить адрес и войти в приложение. Проверьте папку «Спам».
            </CaptionLabel>
            <PrimaryButton onClick={() => navigate('/login', { replace: true })}>
              Перейти ко входу
            </PrimaryButton>
            <Button
              variant="subtle"
              size="sm"
              fw={600}
              loading={resendBusy}
              onClick={handleResend}
            >
              Отправить письмо повторно
            </Button>
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
            {/* Спрашиваем полное имя, а не «имя пользователя»: именно его
                увидят другие. Логин генерируется из email автоматически и
                меняется потом в профиле. */}
            <TextInput
              label="Ваше имя (необязательно)"
              placeholder="Иван Петров"
              autoComplete="name"
              error={errors.fullName?.message}
              {...register('fullName')}
            />
            <PasswordInput
              label="Пароль"
              placeholder="Минимум 8 символов"
              autoComplete="new-password"
              error={errors.password?.message}
              {...register('password')}
            />
            <PasswordInput
              label="Повторите пароль"
              placeholder="Ещё раз"
              autoComplete="new-password"
              error={errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />

            {/* D19. Дата рождения — сервисом могут пользоваться лица
                от 14 лет (Условия использования, п. 3.4). */}
            <Controller
              control={control}
              name="birthDate"
              render={({ field }) => (
                <DateMaskInput
                  label="Дата рождения"
                  // Маска: набираешь цифры — точки в ДД.ММ.ГГГГ ставятся сами
                  // (плюс календарь по кнопке). minDate/maxDate — guard: не
                  // выбрать будущее и заведомо нереальный год. Zod проверяет
                  // то же на сабмите.
                  placeholder="дд.мм.гггг"
                  minDate={new Date(1900, 0, 1)}
                  maxDate={new Date()}
                  value={field.value ?? null}
                  onChange={(d) => field.onChange(d ?? undefined)}
                  error={errors.birthDate?.message}
                />
              )}
            />

            <Controller
              control={control}
              name="privacyPolicyAccepted"
              render={({ field }) => (
                <Checkbox
                  label={
                    <span>
                      Принимаю{' '}
                      <Anchor
                        component={Link}
                        to="/legal/privacy"
                        target="_blank"
                        c={cloudColors.azureDeep}
                      >
                        Политику конфиденциальности
                      </Anchor>
                    </span>
                  }
                  checked={field.value === true}
                  onChange={(e) =>
                    field.onChange(e.currentTarget.checked as unknown as true)
                  }
                  error={errors.privacyPolicyAccepted?.message}
                />
              )}
            />
            <Controller
              control={control}
              name="termsAccepted"
              render={({ field }) => (
                <Checkbox
                  label={
                    <span>
                      Принимаю{' '}
                      <Anchor
                        component={Link}
                        to="/legal/terms"
                        target="_blank"
                        c={cloudColors.azureDeep}
                      >
                        Условия использования
                      </Anchor>
                    </span>
                  }
                  checked={field.value === true}
                  onChange={(e) =>
                    field.onChange(e.currentTarget.checked as unknown as true)
                  }
                  error={errors.termsAccepted?.message}
                />
              )}
            />

            {/* Функция «Родственники»: согласие по умолчанию включено. */}
            <Controller
              control={control}
              name="allowRelativeConnections"
              render={({ field }) => (
                <Checkbox
                  label="Разрешить другим людям, отслеживающим ту же карточку, видеть меня как родственника и писать мне (внутри приложения, без раскрытия почты)"
                  checked={field.value === true}
                  onChange={(e) => field.onChange(e.currentTarget.checked)}
                />
              )}
            />

            {submitError && (
              <BodyLabel c={cloudColors.errorRed}>{submitError}</BodyLabel>
            )}

            <PrimaryButton type="submit" loading={isSubmitting}>
              Зарегистрироваться
            </PrimaryButton>

            <CaptionLabel>
              Уже есть аккаунт?{' '}
              <Anchor component={Link} to="/login" c={cloudColors.azureDeep}>
                Войти
              </Anchor>
            </CaptionLabel>
          </Stack>
          </form>
        )}
      </CloudCard>
    </Container>
  );
}

// Локальные копии форматтера/парсера дат удалены: они дублировали
// utils/formatDate и содержали баг round-trip (год 0–99 → 1900+год),
// из-за которого набранный 1987 превращался в 1901 и не правился.
// Теперь страница использует общие toDateInputValue/parseDateInputValue.
