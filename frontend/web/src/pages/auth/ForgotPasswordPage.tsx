import { Anchor, Container, Group, Stack, TextInput } from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { Cloud, MailCheck } from 'lucide-react';
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
  type ForgotPasswordFormValues,
  forgotPasswordSchema,
} from '../../auth/schemas';
import { formatError } from '../../auth/errorMessages';

/**
 * D43. «Забыли пароль» — ввод email, после которого на почту уходит
 * ссылка для установки нового пароля.
 *
 * ВАЖНО ПРО ТЕКСТ ПОСЛЕ ОТПРАВКИ. Бэк намеренно отвечает успехом даже
 * для незарегистрированного адреса, чтобы по ответу нельзя было
 * перебором выяснить, кто есть в сервисе. Значит и здесь нельзя писать
 * «письмо отправлено» — это подтвердило бы существование аккаунта.
 * Формулировка обязана быть условной: «если адрес зарегистрирован...».
 */
export function ForgotPasswordPage() {
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  async function onSubmit(values: ForgotPasswordFormValues) {
    setSubmitError(null);
    try {
      await authApi.forgotPassword(values.email);
      setSent(true);
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
        <CaptionLabel>Восстановление доступа к аккаунту.</CaptionLabel>
      </Stack>

      <CloudCard>
        {sent ? (
          <Stack gap="md" align="center">
            <MailCheck size={40} color={cloudColors.azureDeep} />
            <BodyLabel ta="center">
              Если аккаунт с адресом <b>{getValues('email')}</b> существует, мы
              отправили на него письмо со ссылкой для смены пароля.
            </BodyLabel>
            <CaptionLabel ta="center">
              Ссылка действует ограниченное время. Не пришло письмо — проверьте
              папку «Спам».
            </CaptionLabel>
            <Anchor component={Link} to="/login" c={cloudColors.azureDeep}>
              Вернуться ко входу
            </Anchor>
          </Stack>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)}>
            <Stack gap="md">
              <BodyLabel>
                Укажите email, на который зарегистрирован аккаунт. Мы отправим
                ссылку для установки нового пароля.
              </BodyLabel>

              <TextInput
                label="Email"
                placeholder="you@example.com"
                type="email"
                autoComplete="email"
                error={errors.email?.message}
                {...register('email')}
              />

              {submitError && (
                <BodyLabel c={cloudColors.errorRed}>{submitError}</BodyLabel>
              )}

              <PrimaryButton type="submit" loading={isSubmitting}>
                Отправить ссылку
              </PrimaryButton>

              <CaptionLabel>
                Вспомнили пароль?{' '}
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
