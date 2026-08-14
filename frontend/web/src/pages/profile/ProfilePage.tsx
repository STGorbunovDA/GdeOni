import { useEffect, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Divider,
  Group,
  Loader,
  Modal,
  NumberInput,
  Stack,
  Switch,
  TextInput,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { Link, useNavigate } from 'react-router-dom';
import {
  Bell,
  CreditCard,
  Gift,
  KeyRound,
  LogOut,
  MailCheck,
  MailWarning,
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
import { cloudColors } from '../../design/theme';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { adminUsersApi } from '../../api/endpoints/adminUsersApi';
import { useAuthStore, useIsAdmin, useIsSuperAdmin } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';
import { useSubscription } from '../../hooks/useSubscription';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { formatDateTime } from '../../utils/formatDate';
import { displaySubscriptionPlan } from '../../utils/subscriptionPlanDisplay';
import { InstallPwaButton } from '../../components/pwa/InstallPwaButton';
import {
  disablePush,
  enablePush,
  fetchPushStatus,
  getPushPermission,
  isPushSupported,
} from '../../pwa/push';

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
  // Массовое проставление логинов — операция владельца сервиса.
  const isSuperAdmin = useIsSuperAdmin();
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

  // Смена собственного логина. Уникальность проверяет бэк: занятый логин
  // вернёт 409 user.login.already.exists, и мы покажем его текст в модалке.
  const [loginModalOpen, setLoginModalOpen] = useState(false);
  const [loginInput, setLoginInput] = useState('');
  const [loginError, setLoginError] = useState<string | null>(null);

  const loginMutation = useMutation({
    mutationFn: (login: string) => usersApi.changeLogin(login.trim()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] });
      setLoginModalOpen(false);
      setLoginError(null);
      notifications.show({
        title: 'Логин изменён',
        message: 'Теперь можно входить с новым логином.',
        color: 'green',
      });
    },
    onError: (e) => setLoginError(formatError(e)),
  });

  // Полное имя — то, как человека видят остальные. Не уникально, пустая
  // строка очищает (тогда другим показывается логин).
  const [fullNameModalOpen, setFullNameModalOpen] = useState(false);
  const [fullNameInput, setFullNameInput] = useState('');
  const [fullNameError, setFullNameError] = useState<string | null>(null);

  const fullNameMutation = useMutation({
    mutationFn: (fullName: string) =>
      usersApi.changeFullName(fullName.trim().length > 0 ? fullName.trim() : null),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] });
      setFullNameModalOpen(false);
      setFullNameError(null);
      notifications.show({
        title: 'Имя сохранено',
        message: '',
        color: 'green',
      });
    },
    onError: (e) => setFullNameError(formatError(e)),
  });

  // Повторная отправка письма подтверждения. Дублирует кнопку в баннере, но
  // баннер можно пролистать, а профиль — постоянное место, где видно статус
  // адреса. Ходит через анонимный resend с email текущего юзера.
  const resendConfirmationMutation = useMutation({
    mutationFn: (email: string) => authApi.resendConfirmation(email),
    onSuccess: (_data, email) =>
      notifications.show({
        title: 'Письмо отправлено',
        message: `Проверьте почту ${email} и перейдите по ссылке.`,
        color: 'blue',
      }),
    onError: (e) =>
      notifications.show({
        title: 'Не удалось отправить',
        message: formatError(e),
        color: 'red',
      }),
  });

  // Push-уведомления. Состояние держим на сервере (есть ли подписка), а не в
  // localStorage: человек мог включить их на другом устройстве.
  const pushStatus = useQuery({
    queryKey: ['push-status'],
    queryFn: fetchPushStatus,
    enabled: isPushSupported(),
  });

  // Разрешение отозвано в настройках браузера — переключатель бесполезен,
  // объясняем это текстом вместо молчаливого «не работает».
  const pushBlocked = getPushPermission() === 'denied';

  const pushMutation = useMutation({
    mutationFn: async (enable: boolean) => {
      if (enable) {
        await enablePush(features.data?.pushPublicKey ?? '');
      } else {
        await disablePush();
      }
      return enable;
    },
    onSuccess: (enabled) => {
      queryClient.invalidateQueries({ queryKey: ['push-status'] });
      notifications.show({
        color: 'green',
        title: enabled ? 'Уведомления включены' : 'Уведомления выключены',
        message: enabled
          ? 'Пришлём памятные даты и ответы поддержки.'
          : '',
      });
    },
    onError: (e) =>
      notifications.show({
        color: 'red',
        title: 'Не получилось',
        message: formatError(e),
      }),
  });

  // Массовая выдача бесплатного доступа всем — «подушка» перед возвратом
  // платного режима. Живёт в профиле владельца, а не в админской «Информации»:
  // та страница — справка без действий.
  const [grantAllOpen, setGrantAllOpen] = useState(false);
  const [grantDays, setGrantDays] = useState<number | string>(30);
  const grantDaysNum = Math.max(1, Math.min(3650, Number(grantDays) || 30));

  const grantAllMutation = useMutation({
    mutationFn: () => adminUsersApi.grantComplimentaryToAll(grantDaysNum),
    onSuccess: (res) => {
      setGrantAllOpen(false);
      notifications.show({
        color: 'green',
        title: 'Готово',
        message: `Бесплатный доступ выдан ${res.affectedCount} пользователям — до ${new Date(
          res.untilUtc,
        ).toLocaleDateString('ru-RU')}.`,
      });
    },
    onError: (e) =>
      notifications.show({
        color: 'red',
        title: 'Не получилось',
        message: formatError(e),
      }),
  });

  // Разовая операция для владельца сервиса: проставить логин тем, у кого его
  // нет. Идемпотентна — повторный запуск вернёт 0.
  const assignLoginsMutation = useMutation({
    mutationFn: () => usersApi.assignMissingLogins(),
    onSuccess: (res) =>
      notifications.show({
        title: 'Готово',
        message:
          res.assignedCount > 0
            ? `Логин проставлен: ${res.assignedCount} чел.`
            : 'Все пользователи уже имеют логин.',
        color: 'green',
      }),
    onError: (e) =>
      notifications.show({
        title: 'Не удалось',
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
            {/* «Имя пользователя» (UserName) убрано: оно не уникально и
                пользователю ничего не даёт. Логин — уникальный идентификатор
                входа, Полное имя — то, как человека видят остальные. Оба
                редактируются здесь же. */}
            <Group justify="space-between" align="flex-end" wrap="nowrap">
              <Field
                label="Логин (для входа)"
                value={query.data.login}
                hint="Войти можно по email или по логину"
              />
              <GhostButton
                size="xs"
                onClick={() => {
                  setLoginInput(query.data.login);
                  setLoginError(null);
                  setLoginModalOpen(true);
                }}
              >
                Изменить
              </GhostButton>
            </Group>

            <Group justify="space-between" align="flex-end" wrap="nowrap">
              <Field
                label="Полное имя"
                value={query.data.fullName ?? 'Не указано'}
                hint="Так вас видят другие; если не указано — показывается логин"
              />
              <GhostButton
                size="xs"
                onClick={() => {
                  setFullNameInput(query.data.fullName ?? '');
                  setFullNameError(null);
                  setFullNameModalOpen(true);
                }}
              >
                Изменить
              </GhostButton>
            </Group>

            {/* Статус адреса видно прямо в профиле: баннер сверху можно
                пролистать, а сюда человек приходит осознанно. */}
            <Group justify="space-between" align="flex-end" wrap="nowrap">
              <Field
                label="Email"
                value={query.data.email}
                hint={
                  query.data.isEmailConfirmed
                    ? 'Адрес подтверждён'
                    : 'Адрес не подтверждён'
                }
              />
              {query.data.isEmailConfirmed ? (
                <MailCheck size={20} color={cloudColors.azureDeep} />
              ) : (
                <GhostButton
                  size="xs"
                  leftSection={<MailWarning size={14} />}
                  loading={resendConfirmationMutation.isPending}
                  onClick={() =>
                    resendConfirmationMutation.mutate(query.data.email)
                  }
                >
                  Подтвердить
                </GhostButton>
              )}
            </Group>

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

            {/* Разовая операция владельца сервиса: у аккаунтов, заведённых до
                появления логина, он мог остаться пустым. Кнопка проставляет
                его из email; идемпотентна — повторный клик вернёт 0. */}
            {isSuperAdmin && (
              <>
                <Divider my="xs" />
                <Stack gap={6}>
                  <CaptionLabel>
                    Обслуживание: проставить логин пользователям, у которых его
                    нет (берётся часть email до «@», при совпадении — полный
                    адрес).
                  </CaptionLabel>
                  <Group>
                    <GhostButton
                      onClick={() => assignLoginsMutation.mutate()}
                      loading={assignLoginsMutation.isPending}
                    >
                      Проставить логины всем без логина
                    </GhostButton>
                  </Group>
                </Stack>

                <Stack gap={6} mt="sm">
                  <CaptionLabel>
                    Выдать бесплатный доступ ВСЕМ пользователям на указанный
                    срок. Только продлевает — у кого доступ дольше, не трогает.
                    Удобно перед возвратом платного режима, чтобы никто резко не
                    упёрся в оплату.
                  </CaptionLabel>
                  <Group gap="sm" align="flex-end" wrap="wrap">
                    <NumberInput
                      label="На сколько дней"
                      value={grantDays}
                      onChange={setGrantDays}
                      min={1}
                      max={3650}
                      allowDecimal={false}
                      w={160}
                    />
                    <GhostButton
                      leftSection={<Gift size={16} />}
                      onClick={() => setGrantAllOpen(true)}
                    >
                      Выдать всем бесплатный доступ
                    </GhostButton>
                  </Group>
                </Stack>
              </>
            )}
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

      {/* Push-уведомления. Карточку показываем только если браузер их умеет
          И на сервере заданы VAPID-ключи — иначе переключатель обманывал бы. */}
      {isPushSupported() && !!features.data?.pushPublicKey && (
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Bell size={20} />
              <BodyLabel>Уведомления на телефон</BodyLabel>
            </Group>
            <Switch
              color="azure"
              checked={pushStatus.data === true}
              disabled={pushStatus.isLoading || pushMutation.isPending}
              onChange={(e) => pushMutation.mutate(e.currentTarget.checked)}
              label="Присылать уведомления в браузер: памятные даты, ответы поддержки, сообщения от родственников"
            />
            {pushBlocked ? (
              <CaptionLabel c={cloudColors.errorRed}>
                Уведомления запрещены в настройках браузера. Откройте замочек
                в адресной строке и разрешите их для этого сайта.
              </CaptionLabel>
            ) : (
              <CaptionLabel>
                Работают, даже когда сайт закрыт. Чтобы приходили на телефон,
                установите приложение на главный экран (кнопка ниже).
              </CaptionLabel>
            )}
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

      {/* Смена логина. Уникальность проверяет сервер — занятый логин
          вернётся ошибкой прямо в модалку, и она не закроется. */}
      <Modal
        opened={loginModalOpen}
        onClose={() => setLoginModalOpen(false)}
        title="Изменить логин"
        centered
      >
        <Stack>
          <TextInput
            label="Логин"
            placeholder="ivan_petrov"
            value={loginInput}
            onChange={(e) => {
              setLoginInput(e.currentTarget.value);
              setLoginError(null);
            }}
            error={loginError}
            autoComplete="username"
          />
          <CaptionLabel>
            Латинские буквы, цифры и знаки . _ - + @ (можно указать полный
            email). Минимум 3 символа. Логин должен быть свободен.
          </CaptionLabel>
          <Group justify="flex-end">
            <GhostButton onClick={() => setLoginModalOpen(false)}>
              Отмена
            </GhostButton>
            <PrimaryButton
              onClick={() => loginMutation.mutate(loginInput)}
              loading={loginMutation.isPending}
              disabled={loginInput.trim().length === 0}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>

      {/* Полное имя. Уникальность не нужна — тёзки допустимы, поэтому
          сохраняем без проверок; пустое поле очищает имя. */}
      <Modal
        opened={fullNameModalOpen}
        onClose={() => setFullNameModalOpen(false)}
        title="Изменить полное имя"
        centered
      >
        <Stack>
          <TextInput
            label="Полное имя"
            placeholder="Иван Петров"
            value={fullNameInput}
            onChange={(e) => {
              setFullNameInput(e.currentTarget.value);
              setFullNameError(null);
            }}
            error={fullNameError}
            autoComplete="name"
          />
          <CaptionLabel>
            Так вас будут видеть другие пользователи. Оставьте поле пустым,
            чтобы очистить — тогда будет показываться логин.
          </CaptionLabel>
          <Group justify="flex-end">
            <GhostButton onClick={() => setFullNameModalOpen(false)}>
              Отмена
            </GhostButton>
            <PrimaryButton
              onClick={() => fullNameMutation.mutate(fullNameInput)}
              loading={fullNameMutation.isPending}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>

      {/* Подтверждение массовой выдачи: действие затрагивает всех разом. */}
      <Modal
        opened={grantAllOpen}
        onClose={() => setGrantAllOpen(false)}
        title="Выдать всем бесплатный доступ?"
        centered
      >
        <Stack>
          <BodyLabel>
            Всем пользователям будет выдан бесплатный доступ на {grantDaysNum}{' '}
            дн. Затронет тех, у кого сейчас нет доступа на более поздний срок.
          </BodyLabel>
          <Group justify="flex-end" gap="sm">
            <GhostButton onClick={() => setGrantAllOpen(false)}>
              Отмена
            </GhostButton>
            <PrimaryButton
              loading={grantAllMutation.isPending}
              onClick={() => grantAllMutation.mutate()}
            >
              Выдать всем
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>
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
