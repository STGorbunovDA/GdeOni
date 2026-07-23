import {
  Anchor,
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
import { authApi, usersApi } from '../../api/endpoints/authApi';
import {
  type RegisterFormValues,
  registerSchema,
} from '../../auth/schemas';
import { formatError } from '../../auth/errorMessages';
import { parseDateInputValue, toDateInputValue } from '../../utils/formatDate';

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
      userName: '',
      // birthDate: undefined в defaults, чтобы поле выглядело пустым;
      // Zod required-refine отработает при submit.
      birthDate: undefined as unknown as Date,
      privacyPolicyAccepted: false as unknown as true,
      termsAccepted: false as unknown as true,
    },
  });

  async function onSubmit(values: RegisterFormValues) {
    setSubmitError(null);
    try {
      await usersApi.register({
        email: values.email,
        password: values.password,
        userName: values.userName?.trim() || undefined,
        // ISO date «yyyy-MM-dd» без учёта таймзоны — DateOnly на бэке.
        birthDate: toDateInputValue(values.birthDate),
      });

      // Регистрация без токенов — сразу login с теми же creds.
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
            <TextInput
              label="Имя пользователя (необязательно)"
              placeholder="Иван"
              autoComplete="nickname"
              error={errors.userName?.message}
              {...register('userName')}
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
                <TextInput
                  type="date"
                  label="Дата рождения"
                  // min/max — браузерный guard: календарь не даст выбрать
                  // будущее и заведомо нереальный год. Зод проверяет то же
                  // самое на сабмите (ввод с клавиатуры min/max не режет).
                  min="1900-01-01"
                  max={toDateInputValue(new Date())}
                  value={field.value ? toDateInputValue(field.value) : ''}
                  onChange={(e) => {
                    const v = e.currentTarget.value;
                    field.onChange(v ? parseDateInputValue(v) : undefined);
                  }}
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
      </CloudCard>
    </Container>
  );
}

// Локальные копии форматтера/парсера дат удалены: они дублировали
// utils/formatDate и содержали баг round-trip (год 0–99 → 1900+год),
// из-за которого набранный 1987 превращался в 1901 и не правился.
// Теперь страница использует общие toDateInputValue/parseDateInputValue.
