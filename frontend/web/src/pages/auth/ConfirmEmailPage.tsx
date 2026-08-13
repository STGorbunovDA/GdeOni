import { Anchor, Container, Group, Loader, Stack } from '@mantine/core';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Cloud, MailCheck, MailX } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
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
import { formatError } from '../../auth/errorMessages';

type ConfirmStatus = 'no_token' | 'confirming' | 'success' | 'error';

/**
 * Последний успешно использованный токен. Нужен из-за одноразовости ссылки:
 * телефон выгружает свёрнутую вкладку из памяти, при возврате страница
 * грузится заново с тем же ?token= — повторный запрос уходил на сервер и
 * получал «ссылка недействительна», хотя email уже подтверждён. Помним факт
 * успеха и не дёргаем API второй раз.
 *
 * localStorage, а не sessionStorage: восстановление вкладки может завести
 * новую сессию, и тогда мы снова показали бы ложную ошибку.
 */
const CONFIRMED_TOKEN_KEY = 'gdeoni-confirmed-email-token';

function readConfirmedToken(): string | null {
  try {
    return localStorage.getItem(CONFIRMED_TOKEN_KEY);
  } catch {
    // Приватный режим без localStorage — просто теряем защиту от повтора.
    return null;
  }
}

function rememberConfirmedToken(token: string): void {
  try {
    localStorage.setItem(CONFIRMED_TOKEN_KEY, token);
  } catch {
    /* см. readConfirmedToken */
  }
}

/**
 * D45. Подтверждение email по ссылке из письма. Токен в query:
 * /confirm-email?token=...
 *
 * Публичная страница (юзер под гейтом ещё не вошёл). После успеха
 * инвалидируем ['me'] — если человек параллельно залогинен в другой
 * вкладке (кейс «старого» юзера с баннером), баннер там пропадёт.
 */
export function ConfirmEmailPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  // Этот токен уже отработал (страницу перезагрузили/вернули из фона) —
  // показываем успех сразу, повторный запрос ушёл бы в ошибку.
  const alreadyConfirmed = token.length > 0 && readConfirmedToken() === token;

  const [status, setStatus] = useState<ConfirmStatus>(() => {
    if (alreadyConfirmed) return 'success';
    return token ? 'confirming' : 'no_token';
  });
  const [errorText, setErrorText] = useState<string | null>(null);

  // Guard от двойного вызова в React.StrictMode (dev дважды монтирует).
  const startedRef = useRef(false);

  useEffect(() => {
    if (!token || alreadyConfirmed || startedRef.current) return;
    startedRef.current = true;

    (async () => {
      try {
        await authApi.confirmEmail(token);
        rememberConfirmedToken(token);
        setStatus('success');
        await queryClient.invalidateQueries({ queryKey: ['me'] });
      } catch (e) {
        setErrorText(formatError(e));
        setStatus('error');
      }
    })();
  }, [token, alreadyConfirmed, queryClient]);

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
        <CaptionLabel>Подтверждение email.</CaptionLabel>
      </Stack>

      <CloudCard>
        {status === 'no_token' ? (
          <Stack gap="md" align="center">
            <BodyLabel ta="center">
              Ссылка неполная — в ней не хватает кода подтверждения.
            </BodyLabel>
            <CaptionLabel ta="center">
              Откройте ссылку из письма целиком. Не пришло письмо — войдите и
              нажмите «Отправить письмо повторно».
            </CaptionLabel>
            <Anchor component={Link} to="/login" c={cloudColors.azureDeep}>
              Перейти ко входу
            </Anchor>
          </Stack>
        ) : status === 'confirming' ? (
          <Stack gap="md" align="center">
            <Loader color={cloudColors.azureDeep} />
            <BodyLabel ta="center">Подтверждаем адрес…</BodyLabel>
          </Stack>
        ) : status === 'success' ? (
          <Stack gap="md" align="center">
            <MailCheck size={40} color={cloudColors.azureDeep} />
            <BodyLabel ta="center">Email подтверждён.</BodyLabel>
            <CaptionLabel ta="center">
              Теперь вы можете войти в приложение.
            </CaptionLabel>
            <PrimaryButton onClick={() => navigate('/login', { replace: true })}>
              Войти
            </PrimaryButton>
          </Stack>
        ) : (
          <Stack gap="md" align="center">
            <MailX size={40} color={cloudColors.errorRed} />
            <BodyLabel ta="center">Не удалось подтвердить email.</BodyLabel>
            {errorText && (
              <CaptionLabel ta="center">{errorText}</CaptionLabel>
            )}
            <CaptionLabel ta="center">
              Ссылка одноразовая и с ограниченным сроком. Войдите и нажмите
              «Отправить письмо повторно», чтобы получить новую.
            </CaptionLabel>
            <Anchor component={Link} to="/login" c={cloudColors.azureDeep}>
              Перейти ко входу
            </Anchor>
          </Stack>
        )}
      </CloudCard>
    </Container>
  );
}
