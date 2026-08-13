import { useEffect, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Stack,
  Switch,
  TextInput,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { Link, useNavigate } from 'react-router-dom';
import {
  CreditCard,
  KeyRound,
  LogOut,
  MapPin,
  MessageSquare,
  MessagesSquare,
  RefreshCw,
  Smartphone,
  UsersRound,
} from 'lucide-react';
import type { CurrentUserResponse } from '../../api/types';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';
import { useSubscription } from '../../hooks/useSubscription';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { formatDateTime } from '../../utils/formatDate';
import { displaySubscriptionPlan } from '../../utils/subscriptionPlanDisplay';
import { InstallPwaButton } from '../../components/pwa/InstallPwaButton';

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

  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  // Город: контролируемое поле, синхронизируем с сервером при загрузке/
  // сохранении. Зависимость по значению city — фоновый refetch с тем же
  // городом не перетрёт то, что человек печатает.
  const [cityInput, setCityInput] = useState('');
  useEffect(() => {
    setCityInput(query.data?.city ?? '');
  }, [query.data?.city]);

  const cityMutation = useMutation({
    mutationFn: (city: string) =>
      usersApi.updateCity(city.trim().length > 0 ? city.trim() : null),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] });
      notifications.show({
        title: 'Город сохранён',
        message: '',
        color: 'green',
      });
    },
    onError: (e) =>
      notifications.show({
        title: 'Не удалось сохранить',
        message: formatError(e),
        color: 'red',
      }),
  });

  // Функция «Родственники»: переключатель согласия. Оптимистично меняем
  // ['me'], при ошибке откатываем. SecurityStamp на бэке не трогается —
  // перелогин не нужен.
  const consentMutation = useMutation({
    mutationFn: (allow: boolean) =>
      usersApi.setRelativeConnectionsConsent(allow),
    onMutate: async (allow) => {
      await queryClient.cancelQueries({ queryKey: ['me'] });
      const prev = queryClient.getQueryData<CurrentUserResponse>(['me']);
      if (prev) {
        queryClient.setQueryData<CurrentUserResponse>(['me'], {
          ...prev,
          allowRelativeConnections: allow,
        });
      }
      return { prev };
    },
    onError: (e, _allow, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(['me'], ctx.prev);
      notifications.show({
        title: 'Не удалось сохранить',
        message: formatError(e),
        color: 'red',
      });
    },
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
            {/* Логин показываем, чтобы человек знал, чем ещё может войти:
                при регистрации он не вводится, а генерируется из email. */}
            <Field
              label="Логин (для входа)"
              value={query.data.login}
              hint="Войти можно по email или по логину"
            />

            <Group>
              <GhostButton
                leftSection={<RefreshCw size={16} />}
                onClick={() => query.refetch()}
                loading={query.isFetching}
              >
                Обновить
              </GhostButton>
              <PrimaryButton
                leftSection={<KeyRound size={16} />}
                onClick={() => navigate('/change-password')}
              >
                Сменить пароль
              </PrimaryButton>
              {/* Красный outline — юзер должен подумать дважды перед
                  logout, особенно с мобилки, где кнопка близко к
                  «Сменить пароль». */}
              <Button
                leftSection={<LogOut size={16} />}
                onClick={handleLogout}
                variant="outline"
                color="red"
                radius={24}
                size="md"
                fw={700}
              >
                Выйти
              </Button>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {/* Город: указывается здесь; пока пусто — в приложении висит баннер
          «укажите город» (аналог баннера неподтверждённого email). */}
      {query.data && (
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <MapPin size={20} />
              <BodyLabel>Город</BodyLabel>
            </Group>
            <CaptionLabel>
              Укажите свой город. Пока он не указан, в приложении показывается
              напоминание.
            </CaptionLabel>
            <Group align="flex-end" gap="sm" wrap="nowrap">
              <TextInput
                style={{ flex: 1 }}
                placeholder="Например, Москва"
                value={cityInput}
                maxLength={200}
                onChange={(e) => setCityInput(e.currentTarget.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') cityMutation.mutate(cityInput);
                }}
              />
              <PrimaryButton
                onClick={() => cityMutation.mutate(cityInput)}
                loading={cityMutation.isPending}
                disabled={cityInput.trim() === (query.data.city ?? '')}
              >
                Сохранить
              </PrimaryButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {/* Функция «Родственники»: согласие быть видимым и получать сообщения. */}
      {query.data && (
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <UsersRound size={20} />
              <BodyLabel>Родственники</BodyLabel>
            </Group>
            <Switch
              color="azure"
              checked={query.data.allowRelativeConnections}
              onChange={(e) =>
                consentMutation.mutate(e.currentTarget.checked)
              }
              label="Показывать меня как родственника другим, кто отслеживает те же карточки, и разрешить им писать мне (внутри приложения, без раскрытия почты)"
            />
            <CaptionLabel>
              Если выключить — вы не появитесь в чужих списках родственников
              и вам нельзя будет написать.
            </CaptionLabel>
          </Stack>
        </CloudCard>
      )}

      {/* F22. Блок подписки. Скрываем для админов (subscription-эндпоинт
          для них может вернуть 404) и когда фичефлаг выключен. */}
      {!isAdmin && features.data?.subscriptionEnabled && (
        <CloudCard>
          <Stack gap="md">
            <Group justify="space-between" align="flex-start">
              <BodyLabel>Подписка</BodyLabel>
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

      {/* F27 / PWA. «Скачать на смартфон» = установка сайта как приложения
          (PWA) — работает и на Android, и на iPhone. Внутри кнопки, если
          нативной установки нет, показывается инструкция + запасной APK. */}
      <CloudCard>
        <Stack gap="md">
          <Group gap={8}>
            <Smartphone size={20} />
            <BodyLabel>Мобильное приложение</BodyLabel>
          </Group>
          <CaptionLabel>
            Установите сайт на главный экран — работает на Android и iPhone,
            открывается как приложение. Все функции те же, что в браузере.
          </CaptionLabel>
          <Group>
            <InstallPwaButton />
            <Button
              variant="subtle"
              radius={24}
              component={Link}
              to="/download"
            >
              Как установить
            </Button>
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

function Field({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  /** Пояснение под значением — например, чем можно войти. */
  hint?: string;
}) {
  return (
    <Stack gap={2}>
      <CaptionLabel>{label}</CaptionLabel>
      <BodyLabel>{value}</BodyLabel>
      {hint && <CaptionLabel>{hint}</CaptionLabel>}
    </Stack>
  );
}
