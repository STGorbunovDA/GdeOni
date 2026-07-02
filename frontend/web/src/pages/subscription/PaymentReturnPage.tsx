import { useEffect, useRef, useState } from 'react';
import { Alert, Container, Loader, Stack } from '@mantine/core';
import { CheckCircle2, Cloud } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { subscriptionApi } from '../../api/endpoints/subscriptionApi';

/**
 * F22. YooKassa после оплаты редиректит юзера на этот роут (ReturnUrl
 * на бэке настраивается в SubscriptionOptions). Здесь мы поллим
 * GET /api/users/me/subscription раз в 3 секунды до тех пор, пока
 * webhook YooKassa не переведёт подписку в Active — тогда редиректим
 * на /tracked.
 *
 * Таймаут 60 секунд: если webhook задерживается, показываем
 * жёлтое предупреждение и предлагаем перейти в профиль вручную.
 * Обычно webhook приходит за 5–15 секунд.
 */
const POLL_INTERVAL_MS = 3000;
const POLL_TIMEOUT_MS = 60_000;

type PollState = 'polling' | 'success' | 'timeout' | 'error';

export function PaymentReturnPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [state, setState] = useState<PollState>('polling');
  const cancelledRef = useRef(false);

  useEffect(() => {
    cancelledRef.current = false;
    const startedAt = Date.now();

    async function tick(): Promise<void> {
      if (cancelledRef.current) return;
      try {
        // Pull-fallback вместо webhook: бэк дёргает YooKassa за
        // реальным статусом. Обязательно в dev (webhook не долетает
        // до localhost), полезно и в проде — safety-net.
        try {
          await subscriptionApi.sync();
        } catch {
          // Sync упал — читаем текущий статус getMy как есть.
        }
        const sub = await subscriptionApi.getMy();
        if (cancelledRef.current) return;
        if (sub.status === 'Active' || sub.isActiveNow) {
          // Инвалидируем кеш, чтобы блоки на других страницах
          // сразу увидели актуальный статус.
          queryClient.invalidateQueries({ queryKey: ['subscription', 'me'] });
          setState('success');
          // Даём юзеру полсекунды увидеть галочку — потом на /tracked.
          window.setTimeout(() => navigate('/tracked', { replace: true }), 800);
          return;
        }
      } catch {
        // Игнорируем — попробуем ещё раз.
      }

      if (Date.now() - startedAt >= POLL_TIMEOUT_MS) {
        setState('timeout');
        return;
      }
      window.setTimeout(tick, POLL_INTERVAL_MS);
    }

    tick();
    return () => {
      cancelledRef.current = true;
    };
  }, [navigate, queryClient]);

  return (
    <Container size="xs" pt={64} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Cloud size={48} color={cloudColors.azureDeep} />
        <TitleLabel>Оплата подписки</TitleLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md" align="center">
          {state === 'polling' && (
            <>
              <Loader color="azure" />
              <BodyLabel>Подтверждаем оплату…</BodyLabel>
              <CaptionLabel>
                Обычно это занимает несколько секунд. Не закрывайте страницу.
              </CaptionLabel>
            </>
          )}

          {state === 'success' && (
            <>
              <CheckCircle2 size={48} color="#2E9E52" />
              <BodyLabel>Оплата подтверждена. Возвращаемся в приложение…</BodyLabel>
            </>
          )}

          {state === 'timeout' && (
            <>
              <Alert color="yellow" variant="light" w="100%">
                Мы пока не получили подтверждение от YooKassa. Оплата может
                прийти в течение нескольких минут. Проверьте статус в профиле
                — если подписка не активировалась, напишите в поддержку.
              </Alert>
              <PrimaryButton onClick={() => navigate('/profile')}>
                В профиль
              </PrimaryButton>
              <GhostButton onClick={() => navigate('/subscription')}>
                К подписке
              </GhostButton>
            </>
          )}

          {state === 'error' && (
            <>
              <Alert color="red" variant="light" w="100%">
                Что-то пошло не так. Попробуйте открыть профиль и проверить
                статус подписки.
              </Alert>
              <GhostButton onClick={() => navigate('/profile')}>
                В профиль
              </GhostButton>
            </>
          )}
        </Stack>
      </CloudCard>
    </Container>
  );
}
