import { useState } from 'react';
import { Alert, Badge, Group, Loader, Modal, Stack } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { CalendarClock, CreditCard, ExternalLink, Gift, RefreshCw, XCircle } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { subscriptionApi } from '../../api/endpoints/subscriptionApi';
import { useSubscription } from '../../hooks/useSubscription';
import { useSubscriptionPrice } from '../../hooks/useSubscriptionPrice';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';
import { displaySubscriptionPlan } from '../../utils/subscriptionPlanDisplay';

/**
 * F22 / D16. Управление подпиской. Роут whitelisted в
 * RequireSubscription — юзер без активной подписки должен уметь
 * сюда попасть и оформить.
 *
 * Кнопка "Оформить" ведёт на POST create-payment → редирект на
 * checkoutUrl YooKassa. После оплаты YooKassa вернёт юзера на
 * /payment/return (см. PaymentReturnPage).
 */
export function SubscriptionPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data, isLoading, isError } = useSubscription();
  // F39. Цена — с бэка, не текстом в разметке (см. useSubscriptionPrice).
  const { priceLabel } = useSubscriptionPrice();
  const [busy, setBusy] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);

  /**
   * Правильный refetch после мутации: сначала гасим in-flight-запросы
   * от 3-секундного поллинга (иначе stale-ответ, уже в полёте до
   * мутации, перезапишет свежее состояние), потом форсим свежий getMy.
   */
  async function invalidateSubscription() {
    await queryClient.cancelQueries({ queryKey: ['subscription', 'me'] });
    await queryClient.invalidateQueries({ queryKey: ['subscription', 'me'] });
  }

  async function handleRefresh() {
    setRefreshing(true);
    try {
      await invalidateSubscription();
    } finally {
      setRefreshing(false);
    }
  }

  async function handleCheckout() {
    setBusy(true);
    try {
      const resp = await subscriptionApi.createPayment('Monthly');
      window.location.href = resp.checkoutUrl;
    } catch (e) {
      notifications.show({
        title: 'Не удалось создать платёж',
        message: formatError(e),
        color: 'red',
      });
      setBusy(false);
    }
  }

  async function handleCancel() {
    setBusy(true);
    try {
      await subscriptionApi.cancel();
      setCancelOpen(false);
      notifications.show({
        title: 'Подписка отменена',
        message: 'Доступ сохранится до конца оплаченного периода.',
      });
      await invalidateSubscription();
    } catch (e) {
      notifications.show({
        title: 'Не удалось отменить',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setBusy(false);
    }
  }

  async function handleCancelPending() {
    setBusy(true);
    try {
      await subscriptionApi.cancelPending();
      notifications.show({
        title: 'Оплата отменена',
        message:
          'Незавершённый платёж закрыт. Вы можете оформить подписку заново, когда будете готовы.',
      });
      await invalidateSubscription();
    } catch (e) {
      notifications.show({
        title: 'Не удалось отменить оплату',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Stack gap="lg">
      <TitleLabel>Подписка</TitleLabel>

      {isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {isError && (
        <Alert color="red" variant="light">
          Не удалось загрузить данные подписки. Попробуйте позже.
        </Alert>
      )}

      {data === null && (
        <CloudCard>
          <BodyLabel>
            У вас пока нет подписки. Это может быть, если ваш аккаунт создан
            без пробного периода — оформите Monthly, чтобы получить доступ.
          </BodyLabel>
          <Group mt="md">
            <PrimaryButton
              leftSection={<CreditCard size={16} />}
              onClick={handleCheckout}
              loading={busy}
            >
              {priceLabel ? `Оформить Monthly — ${priceLabel}` : 'Оформить Monthly'}
            </PrimaryButton>
          </Group>
        </CloudCard>
      )}

      {data && (
        <>
          <CloudCard>
            <Stack gap="sm">
              <Group justify="space-between" align="flex-start">
                <SubTitleLabel>Текущий статус</SubTitleLabel>
                <StatusBadge status={data.status} />
              </Group>
              <StatusDescription {...data} />
            </Stack>
          </CloudCard>

          {data.hasComplimentaryAccess && (
            <CloudCard>
              <Stack gap="xs">
                <Group gap={8}>
                  <Gift size={18} color={cloudColors.azureDeep} />
                  <SubTitleLabel>Бесплатный доступ</SubTitleLabel>
                </Group>
                <BodyLabel>
                  Администратор выдал вам бесплатный доступ
                  {data.complimentaryAccessUntilUtc
                    ? ` до ${formatDateTime(data.complimentaryAccessUntilUtc)}`
                    : ' бессрочно'}
                  .
                </BodyLabel>
                {data.complimentaryAccessNote && (
                  <CaptionLabel>
                    Причина: {data.complimentaryAccessNote}
                  </CaptionLabel>
                )}
              </Stack>
            </CloudCard>
          )}

          <CloudCard>
            <Stack gap="md">
              <SubTitleLabel>Действия</SubTitleLabel>
              <Group>
                {needsPayment(data.status) ? (
                  <PrimaryButton
                    leftSection={<CreditCard size={16} />}
                    onClick={handleCheckout}
                    loading={busy}
                  >
                    {data.status === 'Trial'
                      ? withPrice('Оплатить сейчас', priceLabel)
                      : data.status === 'Cancelled'
                        ? withPrice('Возобновить', priceLabel)
                        : data.status === 'PendingPayment'
                          ? 'Продолжить оплату'
                          : withPrice('Оформить Monthly', priceLabel)}
                  </PrimaryButton>
                ) : null}

                {data.status === 'Active' && (
                  <GhostButton
                    leftSection={<XCircle size={16} />}
                    onClick={() => setCancelOpen(true)}
                    disabled={busy}
                  >
                    Отменить подписку
                  </GhostButton>
                )}

                {data.status === 'PendingPayment' && (
                  <>
                    <GhostButton
                      leftSection={<RefreshCw size={16} />}
                      onClick={handleRefresh}
                      loading={refreshing}
                    >
                      Обновить статус
                    </GhostButton>
                    <GhostButton
                      leftSection={<XCircle size={16} />}
                      onClick={handleCancelPending}
                      disabled={busy}
                    >
                      Отменить оплату
                    </GhostButton>
                  </>
                )}

                <GhostButton onClick={() => navigate('/profile')}>
                  Назад в профиль
                </GhostButton>
              </Group>
            </Stack>
          </CloudCard>
        </>
      )}

      <Modal
        opened={cancelOpen}
        onClose={() => setCancelOpen(false)}
        title="Отменить подписку?"
        centered
      >
        <Stack gap="md">
          <BodyLabel>
            Подписка перестанет продлеваться автоматически. Доступ сохранится
            до конца оплаченного периода
            {data?.expiresAtUtc
              ? ` (${formatDateTime(data.expiresAtUtc)})`
              : ''}
            . После этого приложение попросит оформить заново.
          </BodyLabel>
          <Group justify="flex-end">
            <GhostButton onClick={() => setCancelOpen(false)} disabled={busy}>
              Не отменять
            </GhostButton>
            <PrimaryButton
              color="red"
              onClick={handleCancel}
              loading={busy}
            >
              Да, отменить
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

function needsPayment(status: string): boolean {
  return (
    status === 'None'
    || status === 'Trial'
    || status === 'Cancelled'
    || status === 'Expired'
    // PendingPayment: юзер мог нажать «Назад» на странице YooKassa —
    // висит зомби-Pending до автоотмены (~10 мин). Кнопка "Продолжить
    // оплату" через CreatePayment либо переиспользует свежий
    // CheckoutUrl (D23 GetActivePendingForUser), либо создаст новый.
    || status === 'PendingPayment'
  );
}

function StatusBadge({ status }: { status: string }) {
  const map: Record<string, { color: string; label: string }> = {
    None: { color: 'gray', label: 'Нет подписки' },
    Trial: { color: 'yellow', label: 'Пробный период' },
    PendingPayment: { color: 'blue', label: 'Ожидание оплаты' },
    Active: { color: 'green', label: 'Активна' },
    Cancelled: { color: 'orange', label: 'Отменена' },
    Expired: { color: 'red', label: 'Истекла' },
  };
  const v = map[status] ?? { color: 'gray', label: status };
  return (
    <Badge color={v.color} variant="light" size="lg">
      {v.label}
    </Badge>
  );
}

function StatusDescription(props: {
  status: string;
  expiresAtUtc: string | null;
  cancelledAtUtc: string | null;
  daysUntilExpiry: number;
  isOnTrial: boolean;
  plan: string | null;
}) {
  const { status, expiresAtUtc, cancelledAtUtc, daysUntilExpiry, isOnTrial, plan } = props;

  if (status === 'Trial' && isOnTrial && expiresAtUtc) {
    return (
      <Group gap={8}>
        <CalendarClock size={16} color={cloudColors.azureDeep} />
        <CaptionLabel>
          Пробный период до {formatDateTime(expiresAtUtc)} (осталось{' '}
          {daysUntilExpiry} {pluralDays(daysUntilExpiry)}).
        </CaptionLabel>
      </Group>
    );
  }
  if (status === 'Active' && expiresAtUtc) {
    return (
      <Group gap={8}>
        <CalendarClock size={16} color={cloudColors.azureDeep} />
        <CaptionLabel>
          {plan ? `Тариф ${displaySubscriptionPlan(plan)}. ` : ''}Следующее списание{' '}
          {formatDateTime(expiresAtUtc)} (через {daysUntilExpiry}{' '}
          {pluralDays(daysUntilExpiry)}).
        </CaptionLabel>
      </Group>
    );
  }
  if (status === 'Cancelled' && expiresAtUtc) {
    return (
      <Group gap={8}>
        <CalendarClock size={16} color={cloudColors.azureDeep} />
        <CaptionLabel>
          Отменена {cancelledAtUtc ? formatDateTime(cancelledAtUtc) : ''}. Доступ
          сохраняется до {formatDateTime(expiresAtUtc)}.
        </CaptionLabel>
      </Group>
    );
  }
  if (status === 'PendingPayment') {
    return (
      <Group gap={8}>
        <ExternalLink size={16} color={cloudColors.azureDeep} />
        <CaptionLabel>
          Ждём подтверждение оплаты от YooKassa (обычно 5–15 секунд).
          Если оплата не была завершена — нажмите «Продолжить оплату»
          ниже, чтобы вернуться на страницу платежа.
        </CaptionLabel>
      </Group>
    );
  }
  if (status === 'Expired') {
    return (
      <CaptionLabel>
        Подписка истекла. Оформите заново, чтобы вернуть доступ.
      </CaptionLabel>
    );
  }
  return <CaptionLabel>Оформите Monthly, чтобы начать пользоваться.</CaptionLabel>;
}

function pluralDays(n: number): string {
  const mod10 = n % 10;
  const mod100 = n % 100;
  if (mod10 === 1 && mod100 !== 11) return 'день';
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'дня';
  return 'дней';
}

/**
 * F39. Подпись кнопки с ценой. Пока цена не приехала с бэка — показываем
 * кнопку без суммы: соврать про тариф хуже, чем промолчать.
 */
function withPrice(label: string, priceLabel: string): string {
  return priceLabel ? `${label} — ${priceLabel}` : label;
}
