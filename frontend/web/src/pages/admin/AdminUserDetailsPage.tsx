import { useEffect, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Modal,
  Radio,
  Select,
  Stack,
  Textarea,
  TextInput,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { Ban, ChevronLeft, Gift, RefreshCw, Save, XCircle } from 'lucide-react';
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
import {
  adminUsersApi,
  type AdminUserDetails,
  type AssignableUserRole,
} from '../../api/endpoints/adminUsersApi';
import { useAuthStore } from '../../auth/authStore';
import { formatError } from '../../auth/errorMessages';
import {
  formatDateTime,
  toDateInputValue,
  parseDateInputValue,
} from '../../utils/formatDate';
import { displaySubscriptionPlan } from '../../utils/subscriptionPlanDisplay';

/**
 * F17.7. Детали пользователя в админке + смена роли.
 *
 * Все поля бэк-DTO отображаем как есть — комплимент / блокировка /
 * подписка нужны админу, чтобы решить, что сделать с юзером. Сами
 * управляющие действия (блокировка — F17.10, удаление — F17.11,
 * выдача complimentary — F17.6) приедут отдельными подпунктами.
 *
 * Смена роли:
 *  - Admin может выдать RegularUser / Manager (но не Admin/SuperAdmin);
 *  - SuperAdmin — ещё и Admin;
 *  - SuperAdmin'у нельзя поменять роль (бэк защищает; UI показывает
 *    карточку «без права смены», чтобы не делать заведомо проигрышный
 *    запрос).
 *
 * После смены роли бэк ротирует SecurityStamp юзера → его текущий
 * access-токен умрёт через TTL ~30s, ему придётся войти заново.
 */
const ROLE_LABELS: Record<string, string> = {
  RegularUser: 'Пользователь',
  Manager: 'Менеджер',
  Admin: 'Админ',
  SuperAdmin: 'Супер-админ',
};

export function AdminUserDetailsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();
  const currentUserRole = useAuthStore((s) => s.user?.role);
  const currentUserId = useAuthStore((s) => s.user?.id);

  const query = useQuery({
    queryKey: ['admin-user-details', id],
    queryFn: () => adminUsersApi.getById(id!),
    enabled: !!id,
  });

  const [selectedRole, setSelectedRole] = useState<AssignableUserRole | null>(
    null,
  );

  // Pre-fill сразу после первой успешной загрузки и каждый раз, когда
  // приходят свежие данные с бэка — admin может перейти к другому юзеру
  // и Select должен показать его актуальную роль, а не предыдущего.
  useEffect(() => {
    if (query.data && query.data.role !== 'SuperAdmin') {
      setSelectedRole(query.data.role as AssignableUserRole);
    }
  }, [query.data]);

  const changeRoleMutation = useMutation({
    mutationFn: (newRole: AssignableUserRole) =>
      adminUsersApi.changeRole(id!, newRole),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-user-details', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    },
  });

  // F17.6. Управление подпиской и комплиментом. Все 4 мутации после
  // успеха инвалидируют один и тот же details-query, чтобы UI сразу
  // отобразил новый статус. tracked-list инвалидировать не нужно —
  // подписка на нём не отображается.
  function invalidateUserData() {
    queryClient.invalidateQueries({ queryKey: ['admin-user-details', id] });
    queryClient.invalidateQueries({ queryKey: ['admin-users'] });
  }

  const grantMutation = useMutation({
    mutationFn: (req: { untilUtc: string | null; note: string | null }) =>
      adminUsersApi.grantComplimentaryAccess(id!, req),
    onSuccess: () => {
      invalidateUserData();
      setGrantModalOpen(false);
    },
  });

  const revokeComplimentaryMutation = useMutation({
    mutationFn: () => adminUsersApi.revokeComplimentaryAccess(id!),
    onSuccess: () => {
      invalidateUserData();
      setConfirmRevokeComp(false);
    },
  });

  const restartTrialMutation = useMutation({
    mutationFn: () => adminUsersApi.restartTrial(id!),
    onSuccess: () => {
      invalidateUserData();
      setConfirmRestartTrial(false);
    },
  });

  const revokeSubscriptionMutation = useMutation({
    mutationFn: () => adminUsersApi.revokeSubscription(id!),
    onSuccess: () => {
      invalidateUserData();
      setConfirmRevokeSub(false);
    },
  });

  // F17.10. Блокировка/разблокировка. Backend ротирует SecurityStamp,
  // юзер вылетит из своих сессий при следующем запросе. UI после
  // успеха закрывает confirm-модаль и через invalidate обновляет
  // isBlocked/BlockedAtUtc/BlockedByEmail/BlockedReason в details.
  const blockMutation = useMutation({
    mutationFn: (reason: string | null) => adminUsersApi.block(id!, reason),
    onSuccess: () => {
      invalidateUserData();
      setConfirmBlock(false);
    },
  });

  const unblockMutation = useMutation({
    mutationFn: () => adminUsersApi.unblock(id!),
    onSuccess: () => {
      invalidateUserData();
    },
  });

  // F17.11. Удаление юзера навсегда. Только SuperAdmin (backend это
  // перепроверит). Бэк каскадно переуступает контент текущему
  // SuperAdmin'у и создаёт audit-запись Reassignment на каждой карточке.
  // После успеха уходим в /admin/users с snack-bar'ом.
  const removeMutation = useMutation({
    mutationFn: () => adminUsersApi.remove(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      notifications.show({
        color: 'green',
        title: 'Пользователь удалён',
        message: `${query.data?.email ?? 'Аккаунт'} удалён, контент переуступлен.`,
      });
      navigate('/admin/users', { replace: true });
    },
  });

  // F17.6. Состояние модалей. Grant — форма (бессрочно / до даты + reason),
  // остальные три — простые confirm'ы.
  const [grantModalOpen, setGrantModalOpen] = useState(false);
  const [grantMode, setGrantMode] = useState<'forever' | 'until'>('forever');
  const [grantUntil, setGrantUntil] = useState<Date | null>(null);
  const [grantNote, setGrantNote] = useState('');
  const [confirmRevokeComp, setConfirmRevokeComp] = useState(false);
  const [confirmRestartTrial, setConfirmRestartTrial] = useState(false);
  const [confirmRevokeSub, setConfirmRevokeSub] = useState(false);
  // F17.10. Confirm-модаль блокировки + причина (≤500).
  const [confirmBlock, setConfirmBlock] = useState(false);
  const [blockReason, setBlockReason] = useState('');
  // F17.11. Delete-модаль с email-подтверждением. Юзер должен ввести
  // ровно email удаляемого — защита от случайного клика (web-only,
  // на mobile был просто DisplayAlert).
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deleteEmailInput, setDeleteEmailInput] = useState('');

  function openGrantModal() {
    setGrantMode('forever');
    setGrantUntil(null);
    setGrantNote('');
    setGrantModalOpen(true);
  }

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate(-1)} />
        <Alert color="red" variant="light">
          Некорректный идентификатор пользователя.
        </Alert>
      </Stack>
    );
  }

  if (query.isLoading) {
    return (
      <Stack align="center" py="xl">
        <Loader color="azure" />
      </Stack>
    );
  }

  if (query.isError || !query.data) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate(-1)} />
        <Alert color="red" variant="light">
          {query.error ? formatError(query.error) : 'Пользователь не найден.'}
        </Alert>
      </Stack>
    );
  }

  const user = query.data;
  const canChangeRole =
    user.role !== 'SuperAdmin' &&
    (currentUserRole === 'SuperAdmin' || currentUserRole === 'Admin');

  // F17.6 guards. Управление подпиской/комплиментом доступно когда:
  //  - target не сам админ (себе нельзя — backend отдаёт 403);
  //  - target не Manager (он автоматически bypass подписки, см. D16.5);
  //  - target не SuperAdmin (SuperAdmin не должен попадать в админ-листинг
  //    — includeSuperAdmins=false на бэке, но на всякий случай);
  //  - если current=Admin: target не должен быть Admin (Admin не управляет
  //    Admin'ом — backend проверит, UI прячет ради UX);
  const isSelf = currentUserId === user.id;
  const canManageAccess =
    !isSelf &&
    user.role !== 'Manager' &&
    user.role !== 'SuperAdmin' &&
    !(currentUserRole === 'Admin' && user.role === 'Admin');

  // F17.10 guard. Блок «Опасная зона» доступен когда:
  //  - target не сам админ (себе нельзя — 403 user.block.self.forbidden);
  //  - target не SuperAdmin (403 user.block.super_admin.forbidden);
  //  - если current=Admin: target не Admin (403 user.block.peer_admin.
  //    forbidden). Manager блокировать МОЖНО — в отличие от F17.6, где
  //    ему и так подписка не нужна.
  const canManageBlock =
    !isSelf &&
    user.role !== 'SuperAdmin' &&
    !(currentUserRole === 'Admin' && user.role === 'Admin');

  // F17.11 guard. Удаление юзера навсегда:
  //  - только SuperAdmin;
  //  - target не сам SuperAdmin;
  //  - target не другой SuperAdmin (backend вернёт 403 super_admin.forbidden).
  const canDeleteUser =
    currentUserRole === 'SuperAdmin' &&
    !isSelf &&
    user.role !== 'SuperAdmin';

  return (
    <Stack gap="lg">
      <Group>
        <BackButton onClick={() => navigate(-1)} />
      </Group>

      <Stack gap="xs">
        {/* В заголовке логин — уникальный идентификатор, по которому человек
            входит. UserName («Kipun») не уникален и пользователю больше не
            показывается, поэтому опознавать по нему учётку нельзя. */}
        <TitleLabel>{user.login}</TitleLabel>
        <Group gap="xs">
          <CaptionLabel>{user.email}</CaptionLabel>
          {user.isBlocked && (
            <Badge color="red" variant="filled">
              🚫 Заблокирован
            </Badge>
          )}
        </Group>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Профиль</SubTitleLabel>
          <Field label="Логин (для входа)" value={user.login} />
          <Field label="Полное имя" value={user.fullName ?? 'Не указано'} />
          <Field label="Текущая роль" value={ROLE_LABELS[user.role] ?? user.role} />
          <Field
            label="Зарегистрирован"
            value={formatDateTime(user.registeredAtUtc)}
          />
          <Field
            label="Последний вход"
            value={
              user.lastLoginAtUtc ? formatDateTime(user.lastLoginAtUtc) : '—'
            }
          />
          <Field label="Отслеживает умерших" value={String(user.trackingCount)} />
          <Group>
            <GhostButton
              size="compact-sm"
              onClick={() => navigate(`/admin/users/${user.id}/tracked`)}
            >
              Открыть список отслеживаний
            </GhostButton>
          </Group>
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Доступ и подписка</SubTitleLabel>
          <Field label="Статус" value={user.subscriptionStatus} />
          {user.subscriptionPlan && (
            <Field label="Тариф" value={displaySubscriptionPlan(user.subscriptionPlan)} />
          )}
          {user.subscriptionExpiresAtUtc && (
            <Field
              label="Действует до"
              value={formatDateTime(user.subscriptionExpiresAtUtc)}
            />
          )}
          {user.hasComplimentaryAccess && (
            <>
              <Field
                label="Бесплатный доступ"
                value={
                  user.complimentaryAccessUntilUtc
                    ? `до ${formatDateTime(user.complimentaryAccessUntilUtc)}`
                    : 'Бессрочно'
                }
              />
              {user.complimentaryAccessNote && (
                <Field
                  label="Причина"
                  value={user.complimentaryAccessNote}
                />
              )}
            </>
          )}

          {/* F17.6. Действия. Скрываем целиком если current не может
              управлять (см. canManageAccess guard). */}
          {canManageAccess ? (
            <Group gap="sm" wrap="wrap">
              {!user.hasComplimentaryAccess ? (
                <Button
                  variant="light"
                  color="green"
                  leftSection={<Gift size={16} />}
                  onClick={openGrantModal}
                >
                  Выдать бесплатный доступ
                </Button>
              ) : (
                <Button
                  variant="light"
                  color="orange"
                  leftSection={<XCircle size={16} />}
                  onClick={() => setConfirmRevokeComp(true)}
                >
                  Отозвать бесплатный доступ
                </Button>
              )}
              <Button
                variant="light"
                color="azure"
                leftSection={<RefreshCw size={16} />}
                onClick={() => setConfirmRestartTrial(true)}
              >
                Восстановить Trial (30 дней)
              </Button>
              <Button
                variant="light"
                color="red"
                leftSection={<Ban size={16} />}
                onClick={() => setConfirmRevokeSub(true)}
              >
                Снять подписку
              </Button>
            </Group>
          ) : (
            <CaptionLabel>
              {isSelf
                ? 'Себе подписку и комплимент менять нельзя.'
                : user.role === 'Manager'
                  ? 'Для менеджера это не нужно — у них автоматический bypass подписки.'
                  : 'У вас нет прав на управление подпиской этого пользователя.'}
            </CaptionLabel>
          )}
        </Stack>
      </CloudCard>


      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Смена роли</SubTitleLabel>
          {canChangeRole ? (
            <RoleChangeForm
              user={user}
              currentUserRole={currentUserRole ?? null}
              selectedRole={selectedRole}
              onChange={setSelectedRole}
              onSubmit={(newRole) => changeRoleMutation.mutate(newRole)}
              submitting={changeRoleMutation.isPending}
              error={
                changeRoleMutation.isError
                  ? formatError(changeRoleMutation.error)
                  : null
              }
              success={
                changeRoleMutation.isSuccess && !changeRoleMutation.isPending
              }
            />
          ) : (
            <BodyLabel c={cloudColors.azureDeep}>
              {user.role === 'SuperAdmin'
                ? 'Роль супер-админа сменить нельзя.'
                : 'У вас нет прав на смену роли этого пользователя.'}
            </BodyLabel>
          )}
        </Stack>
      </CloudCard>

      {/* F17.10. Опасная зона: блокировка навсегда. Backend ротирует
          SecurityStamp → мгновенный logout юзера. Guard'ы зеркалят бэк
          (self / SuperAdmin / peer-admin для current=Admin). */}
      {canManageBlock && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <Stack gap="md">
            <SubTitleLabel c={cloudColors.errorRed}>
              Опасная зона
            </SubTitleLabel>

            {user.isBlocked ? (
              <>
                <Stack gap="xs">
                  <BodyLabel>Пользователь заблокирован навсегда.</BodyLabel>
                  {user.blockedAtUtc && (
                    <Field
                      label="Дата блокировки"
                      value={formatDateTime(user.blockedAtUtc)}
                    />
                  )}
                  {user.blockedByUserEmail && (
                    <Field label="Кем" value={user.blockedByUserEmail} />
                  )}
                  {user.blockedReason && (
                    <Field label="Причина" value={user.blockedReason} />
                  )}
                </Stack>
                {unblockMutation.isError && (
                  <Alert color="red" variant="light">
                    {formatError(unblockMutation.error)}
                  </Alert>
                )}
                <Group justify="flex-end">
                  <PrimaryButton
                    onClick={() => unblockMutation.mutate()}
                    loading={unblockMutation.isPending}
                  >
                    Разблокировать
                  </PrimaryButton>
                </Group>
              </>
            ) : (
              <>
                <BodyLabel>
                  После блокировки юзер моментально потеряет доступ (текущие
                  сессии инвалидируются) и не сможет войти повторно. Действие
                  обратимо через «Разблокировать».
                </BodyLabel>
                <Textarea
                  label="Причина (необязательно)"
                  placeholder="Например: спам, нарушение правил, жалобы"
                  value={blockReason}
                  onChange={(e) => setBlockReason(e.currentTarget.value)}
                  autosize
                  minRows={2}
                  maxRows={5}
                  maxLength={500}
                />
                <Group justify="flex-end">
                  <Button
                    color="red"
                    onClick={() => setConfirmBlock(true)}
                  >
                    Заблокировать пользователя навсегда
                  </Button>
                </Group>
              </>
            )}
          </Stack>
        </CloudCard>
      )}

      {/* F17.11. Удаление юзера навсегда. Отдельная секция в «Опасной
          зоне» — видна только SuperAdmin'у, backend продублирует
          проверку. */}
      {canDeleteUser && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <Stack gap="md">
            <SubTitleLabel c={cloudColors.errorRed}>
              Удаление аккаунта
            </SubTitleLabel>
            <BodyLabel>
              Юзер будет удалён навсегда. Содержимое каскадно
              переуступается текущему супер-админу, для каждой карточки
              создаётся audit-запись «Переуступка».
            </BodyLabel>
            <Stack gap={2}>
              <CaptionLabel c={cloudColors.errorRed}>Удалятся</CaptionLabel>
              <BodyLabel>
                Аккаунт, refresh-токены, права.
              </BodyLabel>
            </Stack>
            <Stack gap={2}>
              <CaptionLabel c={cloudColors.azureDeep}>Переуступятся</CaptionLabel>
              <BodyLabel>
                Карточки умерших (CreatedByUserId), медиа
                (UploadedByUserId), отслеживания (TrackedDeceased).
              </BodyLabel>
            </Stack>
            <Stack gap={2}>
              <CaptionLabel>Останутся как есть</CaptionLabel>
              <BodyLabel>
                Платежи, воспоминания (AuthorUserId → null), история
                правок.
              </BodyLabel>
            </Stack>
            <Group justify="flex-end">
              <Button
                color="red"
                onClick={() => {
                  setDeleteEmailInput('');
                  setConfirmDelete(true);
                }}
              >
                Удалить пользователя навсегда
              </Button>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {/* F17.10. Confirm block. Отдельная модаль перед необратимой
          (в UX-смысле) операцией — юзеров дороже случайно вырубить,
          чем ошибиться с complimentary. */}
      <Modal
        opened={confirmBlock}
        onClose={() => !blockMutation.isPending && setConfirmBlock(false)}
        title="Заблокировать пользователя?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Пользователь <b>{user.email}</b> моментально потеряет доступ.
            Все его сессии инвалидируются, повторный вход не пройдёт.
          </BodyLabel>
          {blockMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(blockMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmBlock(false)}
              disabled={blockMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              onClick={() =>
                blockMutation.mutate(blockReason.trim() || null)
              }
              loading={blockMutation.isPending}
            >
              Заблокировать
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* F17.11. Confirm delete. Требует ввести email юзера для
          подтверждения — защита от случайного клика, mobile такого
          не требует. */}
      <Modal
        opened={confirmDelete}
        onClose={() => !removeMutation.isPending && setConfirmDelete(false)}
        title="Удалить пользователя навсегда?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Действие необратимо. Юзер <b>{user.email}</b> и его сессии
            уйдут навсегда; карточки, медиа и отслеживания перейдут к
            вам как к супер-админу.
          </BodyLabel>
          <TextInput
            label="Для подтверждения введите email юзера"
            placeholder={user.email}
            value={deleteEmailInput}
            onChange={(e) => setDeleteEmailInput(e.currentTarget.value)}
            autoFocus
          />
          {removeMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(removeMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmDelete(false)}
              disabled={removeMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              disabled={deleteEmailInput.trim() !== user.email}
              loading={removeMutation.isPending}
              onClick={() => removeMutation.mutate()}
            >
              Удалить навсегда
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* F17.6. Grant complimentary access. Бессрочно / до даты + reason. */}
      <Modal
        opened={grantModalOpen}
        onClose={() => !grantMutation.isPending && setGrantModalOpen(false)}
        title="Выдать бесплатный доступ"
        centered
        size="md"
      >
        <Stack gap="md">
          <Radio.Group
            value={grantMode}
            onChange={(v) => setGrantMode(v as 'forever' | 'until')}
            label="Срок"
          >
            <Stack gap="xs" mt="xs">
              <Radio value="forever" label="Бессрочно" />
              <Radio value="until" label="До конкретной даты" />
            </Stack>
          </Radio.Group>
          {grantMode === 'until' && (
            <TextInput
              type="date"
              label="Действует до"
              min={toDateInputValue(new Date())}
              value={toDateInputValue(grantUntil)}
              onChange={(e) =>
                setGrantUntil(parseDateInputValue(e.currentTarget.value))
              }
            />
          )}
          <Textarea
            label="Причина"
            placeholder="Например: друг основателя, support ticket 42"
            value={grantNote}
            onChange={(e) => setGrantNote(e.currentTarget.value)}
            autosize
            minRows={2}
            maxRows={5}
            maxLength={500}
          />
          {grantMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(grantMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setGrantModalOpen(false)}
              disabled={grantMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="green"
              loading={grantMutation.isPending}
              disabled={grantMode === 'until' && !grantUntil}
              onClick={() =>
                grantMutation.mutate({
                  untilUtc:
                    grantMode === 'until' && grantUntil
                      ? grantUntil.toISOString()
                      : null,
                  note: grantNote.trim() || null,
                })
              }
            >
              Выдать
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* F17.6. Confirm revoke complimentary. */}
      <Modal
        opened={confirmRevokeComp}
        onClose={() =>
          !revokeComplimentaryMutation.isPending && setConfirmRevokeComp(false)
        }
        title="Отозвать бесплатный доступ?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Юзер потеряет бесплатный доступ. Если у него нет активной
            подписки — сервис закроется при следующем запросе.
          </BodyLabel>
          {revokeComplimentaryMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(revokeComplimentaryMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmRevokeComp(false)}
              disabled={revokeComplimentaryMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="orange"
              loading={revokeComplimentaryMutation.isPending}
              onClick={() => revokeComplimentaryMutation.mutate()}
            >
              Отозвать
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* F17.6. Confirm restart trial. */}
      <Modal
        opened={confirmRestartTrial}
        onClose={() =>
          !restartTrialMutation.isPending && setConfirmRestartTrial(false)
        }
        title="Восстановить Trial?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Подписка перейдёт в Trial с новым сроком 30 дней (значение
            из SubscriptionOptions.TrialDurationDays). Текущий статус
            не имеет значения — переключим из любого.
          </BodyLabel>
          {restartTrialMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(restartTrialMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmRestartTrial(false)}
              disabled={restartTrialMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="azure"
              loading={restartTrialMutation.isPending}
              onClick={() => restartTrialMutation.mutate()}
            >
              Восстановить Trial
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* F17.6. Confirm revoke subscription. */}
      <Modal
        opened={confirmRevokeSub}
        onClose={() =>
          !revokeSubscriptionMutation.isPending && setConfirmRevokeSub(false)
        }
        title="Снять подписку?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Подписка моментально переведётся в Expired. Юзер потеряет
            доступ при следующем запросе. Если у него выдан бесплатный
            доступ (complimentary), он продолжит работать.
          </BodyLabel>
          {revokeSubscriptionMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(revokeSubscriptionMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmRevokeSub(false)}
              disabled={revokeSubscriptionMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              loading={revokeSubscriptionMutation.isPending}
              onClick={() => revokeSubscriptionMutation.mutate()}
            >
              Снять подписку
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

function RoleChangeForm({
  user,
  currentUserRole,
  selectedRole,
  onChange,
  onSubmit,
  submitting,
  error,
  success,
}: {
  user: AdminUserDetails;
  currentUserRole: string | null;
  selectedRole: AssignableUserRole | null;
  onChange: (v: AssignableUserRole | null) => void;
  onSubmit: (v: AssignableUserRole) => void;
  submitting: boolean;
  error: string | null;
  success: boolean;
}) {
  // Admin может назначать RegularUser/Manager. SuperAdmin — ещё и Admin.
  // Backend всё равно перепроверит (см. ChangeRoleUseCase) — это просто
  // защита UX от заведомо проигрышного запроса.
  const options: { value: AssignableUserRole; label: string }[] = [
    { value: 'RegularUser', label: ROLE_LABELS.RegularUser },
    { value: 'Manager', label: ROLE_LABELS.Manager },
  ];
  if (currentUserRole === 'SuperAdmin') {
    options.push({ value: 'Admin', label: ROLE_LABELS.Admin });
  }

  const changed = selectedRole !== null && selectedRole !== user.role;

  return (
    <Stack gap="md">
      <Select
        label="Новая роль"
        data={options}
        value={selectedRole}
        onChange={(v) => onChange((v as AssignableUserRole | null) ?? null)}
        allowDeselect={false}
      />
      <CaptionLabel>
        После смены пользователю придётся войти заново — текущий токен
        перестанет действовать через ~30 секунд.
      </CaptionLabel>

      {error && (
        <Alert color="red" variant="light">
          {error}
        </Alert>
      )}
      {success && (
        <Alert color="green" variant="light">
          Роль обновлена.
        </Alert>
      )}

      <Group justify="flex-end">
        <PrimaryButton
          leftSection={<Save size={16} />}
          disabled={!changed}
          loading={submitting}
          onClick={() => selectedRole && onSubmit(selectedRole)}
        >
          Сохранить
        </PrimaryButton>
      </Group>
    </Stack>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <CaptionLabel>{label}</CaptionLabel>
      <BodyLabel>{value}</BodyLabel>
    </Stack>
  );
}

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <GhostButton leftSection={<ChevronLeft size={16} />} onClick={onClick}>
      Назад
    </GhostButton>
  );
}



