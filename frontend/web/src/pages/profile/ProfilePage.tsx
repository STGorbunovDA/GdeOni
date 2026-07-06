import { Alert, Badge, Button, Group, Loader, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import {
  CreditCard,
  Download,
  KeyRound,
  LogOut,
  MessageSquare,
  MessagesSquare,
  RefreshCw,
  Smartphone,
} from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';
import { useSubscription } from '../../hooks/useSubscription';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { CURRENT_APP_VERSION, useAppVersion } from '../../hooks/useAppVersion';
import { formatDateTime } from '../../utils/formatDate';
import { displaySubscriptionPlan } from '../../utils/subscriptionPlanDisplay';

/**
 * F27. Дублируем fallback из DownloadPage, чтобы кнопка «Скачать APK»
 * работала даже без ответа GET /api/app/version. Env-переменная
 * бейкается на build-time, значит один источник — единая правда,
 * задавать не надо.
 */
const APK_FALLBACK_URL: string =
  import.meta.env.VITE_APK_FALLBACK_URL ?? 'https://gdeoni.ru/apk/latest.apk';

/**
 * F16. Профиль пользователя — UserName / FullName / Email.
 * Role не показываем (зеркало решения mobile E18 от 2026-05-13: юзеру
 * это служебная информация, ничего не даёт).
 *
 * Subscription/admin/support блоки сюда сознательно не кладём — это
 * F17/F22+ и не входит в скоуп F16.
 */
export function ProfilePage() {
  const navigate = useNavigate();
  const clear = useAuthStore((s) => s.clear);
  const isAdmin = useIsAdmin();
  const subscription = useSubscription();
  const features = useAppFeatures();
  const appVersion = useAppVersion();
  const apkUrl = appVersion.data?.downloadUrl ?? APK_FALLBACK_URL;
  const apkVersion = appVersion.data?.latestVersion;

  const query = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  async function handleLogout() {
    await authApi.logout();
    clear();
    navigate('/login', { replace: true });
  }

  return (
    <Stack gap="lg">
      <TitleLabel>Профиль</TitleLabel>

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.data && (
        <CloudCard>
          <Stack gap="md">
            <Field label="Имя пользователя" value={query.data.userName} />
            <Field
              label="Полное имя"
              value={query.data.fullName ?? 'Не указано'}
            />
            <Field label="Email" value={query.data.email} />

            <Group>
              <GhostButton
                leftSection={<RefreshCw size={16} />}
                onClick={() => query.refetch()}
                loading={query.isFetching}
              >
                Обновить
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {/* F22. Блок подписки. Скрываем для админов (subscription-эндпоинт
          для них может вернуть 404) и когда фичефлаг выключен. */}
      {!isAdmin && features.data?.subscriptionEnabled && (
        <CloudCard>
          <Stack gap="md">
            <Group justify="space-between" align="flex-start">
              <SubTitleLabel>Подписка</SubTitleLabel>
              {subscription.data && (
                <SubscriptionStatusBadge status={subscription.data.status} />
              )}
            </Group>
            <SubscriptionSummary
              loading={subscription.isLoading}
              data={subscription.data}
            />
            <Group>
              <PrimaryButton
                leftSection={<CreditCard size={16} />}
                onClick={() => navigate('/subscription')}
              >
                Управлять подпиской
              </PrimaryButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {/* F17.14. Ссылки в поддержку. Отдельная карточка для симметрии
          с mobile ProfilePage. */}
      <CloudCard>
        <Stack gap="md">
          <BodyLabel>Поддержка</BodyLabel>
          <Group>
            <PrimaryButton
              leftSection={<MessageSquare size={16} />}
              onClick={() => navigate('/support/new')}
            >
              Обращение в службу поддержки
            </PrimaryButton>
            <GhostButton
              leftSection={<MessagesSquare size={16} />}
              onClick={() => navigate('/support/mine')}
            >
              Мои обращения
            </GhostButton>
          </Group>
        </Stack>
      </CloudCard>

      {/* F27. Блок «Мобильное приложение» — прямая кнопка Скачать APK
          + ссылка на /download c инструкцией по установке. Симметрично
          с mobile ProfilePage, где есть «Открыть веб-версию». */}
      <CloudCard>
        <Stack gap="md">
          <Group gap={8}>
            <Smartphone size={20} />
            <BodyLabel>Мобильное приложение</BodyLabel>
          </Group>
          <CaptionLabel>
            Установите Android-приложение, чтобы получать напоминания о
            годовщинах даже без открытой вкладки.
          </CaptionLabel>
          <Group>
            <Button
              component="a"
              href={apkUrl}
              leftSection={<Download size={16} />}
              loading={appVersion.isLoading}
              radius={24}
              fw={700}
              size="md"
            >
              Скачать APK
            </Button>
            <Button
              component={Link}
              to="/download"
              variant="default"
              radius={24}
              fw={700}
              size="md"
            >
              Инструкция по установке
            </Button>
          </Group>
          {apkVersion && (
            <CaptionLabel>Версия {apkVersion}</CaptionLabel>
          )}
        </Stack>
      </CloudCard>

      {/* Общие действия по аккаунту — заворачиваем в CloudCard, чтобы
          визуально не выпадали из ряда других блоков профиля. */}
      <CloudCard>
        <Stack gap="md">
          <BodyLabel>Аккаунт</BodyLabel>
          <Group>
            <PrimaryButton
              leftSection={<KeyRound size={16} />}
              onClick={() => navigate('/change-password')}
            >
              Сменить пароль
            </PrimaryButton>
            <GhostButton
              leftSection={<LogOut size={16} />}
              onClick={handleLogout}
            >
              Выйти
            </GhostButton>
          </Group>
        </Stack>
      </CloudCard>

      {/* F22. Версия — для поддержки: юзер сможет назвать, на какой
          сборке словил баг. Зеркало mobile ProfileViewModel (E22.1). */}
      <CaptionLabel>Версия: {CURRENT_APP_VERSION}</CaptionLabel>
    </Stack>
  );
}

function SubscriptionSummary(props: {
  loading: boolean;
  data: ReturnType<typeof useSubscription>['data'];
}) {
  if (props.loading) {
    return <CaptionLabel>Загружаем…</CaptionLabel>;
  }
  const data = props.data;
  if (!data) {
    return (
      <CaptionLabel>Оформите Monthly, чтобы начать пользоваться.</CaptionLabel>
    );
  }
  if (data.hasComplimentaryAccess) {
    return (
      <CaptionLabel>
        Бесплатный доступ от администратора
        {data.complimentaryAccessUntilUtc
          ? ` до ${formatDateTime(data.complimentaryAccessUntilUtc)}`
          : ' (бессрочно)'}
        .
      </CaptionLabel>
    );
  }
  if (data.status === 'Trial' && data.expiresAtUtc) {
    return (
      <CaptionLabel>
        Пробный период до {formatDateTime(data.expiresAtUtc)} — осталось{' '}
        {data.daysUntilExpiry} {pluralDays(data.daysUntilExpiry)}.
      </CaptionLabel>
    );
  }
  if (data.status === 'Active' && data.expiresAtUtc) {
    return (
      <CaptionLabel>
        {data.plan ? `Тариф ${displaySubscriptionPlan(data.plan)}. ` : ''}Следующее списание{' '}
        {formatDateTime(data.expiresAtUtc)}.
      </CaptionLabel>
    );
  }
  if (data.status === 'Cancelled' && data.expiresAtUtc) {
    return (
      <CaptionLabel>
        Отменена, доступ до {formatDateTime(data.expiresAtUtc)}.
      </CaptionLabel>
    );
  }
  if (data.status === 'PendingPayment') {
    return (
      <CaptionLabel>
        Ждём подтверждение оплаты от YooKassa (обычно 5–15 секунд).
        Если оплата прервалась — откройте «Управлять подпиской» и
        нажмите «Продолжить оплату».
      </CaptionLabel>
    );
  }
  if (data.status === 'Expired') {
    return <CaptionLabel>Подписка истекла. Оформите заново.</CaptionLabel>;
  }
  return null;
}

function SubscriptionStatusBadge({ status }: { status: string }) {
  const map: Record<string, { color: string; label: string }> = {
    None: { color: 'gray', label: 'Нет' },
    Trial: { color: 'yellow', label: 'Пробный' },
    PendingPayment: { color: 'blue', label: 'Ожидание' },
    Active: { color: 'green', label: 'Активна' },
    Cancelled: { color: 'orange', label: 'Отменена' },
    Expired: { color: 'red', label: 'Истекла' },
  };
  const v = map[status] ?? { color: 'gray', label: status };
  return (
    <Badge color={v.color} variant="light">
      {v.label}
    </Badge>
  );
}

function pluralDays(n: number): string {
  const mod10 = n % 10;
  const mod100 = n % 100;
  if (mod10 === 1 && mod100 !== 11) return 'день';
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'дня';
  return 'дней';
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <CaptionLabel>{label}</CaptionLabel>
      <BodyLabel>{value}</BodyLabel>
    </Stack>
  );
}
