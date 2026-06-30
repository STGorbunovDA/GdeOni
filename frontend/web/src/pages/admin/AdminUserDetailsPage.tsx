import { useEffect, useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Select,
  Stack,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, Save } from 'lucide-react';
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
import { formatDateTime } from '../../utils/formatDate';

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

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate('/admin/users')} />
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
        <BackButton onClick={() => navigate('/admin/users')} />
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

  return (
    <Stack gap="lg">
      <Group>
        <BackButton onClick={() => navigate('/admin/users')} />
      </Group>

      <Stack gap="xs">
        <TitleLabel>{user.userName}</TitleLabel>
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
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Подписка</SubTitleLabel>
          <Field label="Статус" value={user.subscriptionStatus} />
          {user.subscriptionPlan && (
            <Field label="Тариф" value={user.subscriptionPlan} />
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
        </Stack>
      </CloudCard>

      {user.isBlocked && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <Stack gap="md">
            <SubTitleLabel>Блокировка</SubTitleLabel>
            {user.blockedAtUtc && (
              <Field
                label="Заблокирован"
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
        </CloudCard>
      )}

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



