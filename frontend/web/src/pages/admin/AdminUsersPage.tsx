import { useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Pagination,
  Select,
  Stack,
  Table,
  TextInput,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Search } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  adminUsersApi,
  type AdminUserListItem,
  type AdminUserRole,
} from '../../api/endpoints/adminUsersApi';
import { formatError } from '../../auth/errorMessages';
import {
  formatDateTime,
  toDateInputValue,
  parseDateInputValue,
} from '../../utils/formatDate';

/**
 * F17.7. Список пользователей в админке.
 *
 * Server-side пагинация и фильтры — никакого client-side, чтобы при
 * нескольких тысячах юзеров не тащить всё разом. Поиск дебоунсится
 * на 300мс через @mantine/hooks, чтобы каждое нажатие клавиши не
 * улетало в /api/users.
 *
 * Зеркало mobile AdminUsersPage: фильтр по роли, диапазон по дате
 * регистрации, подсветка заблокированных красным. Web-расширения
 * (CSV-экспорт, сортировка по колонкам) пока не добавлены —
 * остаются в backlog F17.7.
 */
const PAGE_SIZE = 20;

const ROLE_LABELS: Record<AdminUserRole, string> = {
  RegularUser: 'Пользователь',
  Manager: 'Менеджер',
  Admin: 'Админ',
  SuperAdmin: 'Супер-админ',
};

// SuperAdmin намеренно НЕ в фильтре: на бэке includeSuperAdmins=false,
// в списке их в принципе нет, поэтому такой выбор давал бы пустой результат.
const ROLE_FILTER_OPTIONS: { value: AdminUserRole; label: string }[] = [
  { value: 'RegularUser', label: ROLE_LABELS.RegularUser },
  { value: 'Manager', label: ROLE_LABELS.Manager },
  { value: 'Admin', label: ROLE_LABELS.Admin },
];

export function AdminUsersPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [role, setRole] = useState<AdminUserRole | null>(null);
  const [registeredFrom, setRegisteredFrom] = useState<Date | null>(null);
  const [registeredTo, setRegisteredTo] = useState<Date | null>(null);
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: [
      'admin-users',
      {
        search: debouncedSearch || undefined,
        role: role ?? undefined,
        from: registeredFrom?.toISOString(),
        to: registeredTo?.toISOString(),
        page,
      },
    ],
    queryFn: () =>
      adminUsersApi.list({
        search: debouncedSearch.trim() || undefined,
        role: role ?? undefined,
        registeredFromUtc: registeredFrom?.toISOString(),
        registeredToUtc: registeredTo?.toISOString(),
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (prev) => prev,
  });

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / PAGE_SIZE))
    : 1;

  function resetToFirstPage<T>(setter: (v: T) => void): (v: T) => void {
    return (v) => {
      setter(v);
      setPage(1);
    };
  }

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Пользователи</TitleLabel>
        <CaptionLabel>
          Список зарегистрированных пользователей. Сам админ и
          супер-админы в списке скрыты.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Group grow align="flex-end">
            <TextInput
              label="Поиск"
              placeholder="Email, логин, имя, ФИО"
              leftSection={<Search size={16} />}
              value={search}
              onChange={(e) =>
                resetToFirstPage(setSearch)(e.currentTarget.value)
              }
            />
            <Select
              label="Роль"
              placeholder="Все"
              clearable
              data={ROLE_FILTER_OPTIONS}
              value={role}
              onChange={(v) =>
                resetToFirstPage(setRole)((v as AdminUserRole | null) ?? null)
              }
            />
          </Group>
          <Group grow align="flex-end">
            <TextInput
              type="date"
              label="Зарегистрирован с"
              value={toDateInputValue(registeredFrom)}
              onChange={(e) =>
                resetToFirstPage(setRegisteredFrom)(
                  parseDateInputValue(e.currentTarget.value),
                )
              }
            />
            <TextInput
              type="date"
              label="по"
              value={toDateInputValue(registeredTo)}
              onChange={(e) =>
                resetToFirstPage(setRegisteredTo)(
                  parseDateInputValue(e.currentTarget.value),
                )
              }
            />
          </Group>
        </Stack>
      </CloudCard>

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.data && (
        <>
          <CloudCard p={0}>
            <Table.ScrollContainer minWidth={900}>
              <Table verticalSpacing="sm" highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Email</Table.Th>
                    {/* Логин — уникален, им можно войти наравне с email.
                        «Имя» рядом — отображаемое, тёзки допустимы. */}
                    <Table.Th>Логин</Table.Th>
                    <Table.Th>Имя</Table.Th>
                    <Table.Th>Полное имя</Table.Th>
                    <Table.Th>Роль</Table.Th>
                    <Table.Th>Регистрация</Table.Th>
                    <Table.Th>Последний вход</Table.Th>
                    <Table.Th>Отслеживаний</Table.Th>
                    <Table.Th>Статус</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <UserRow
                      key={item.id}
                      item={item}
                      onClick={() => navigate(`/admin/users/${item.id}`)}
                    />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={9}>
                        <BodyLabel>Никто не найден по фильтрам.</BodyLabel>
                      </Table.Td>
                    </Table.Tr>
                  )}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
          </CloudCard>

          <Group justify="space-between">
            <CaptionLabel>
              Всего: {query.data.totalCount}
              {query.isFetching && ' · обновляем…'}
            </CaptionLabel>
            <Pagination
              total={totalPages}
              value={page}
              onChange={setPage}
              color="azure"
              size="sm"
            />
          </Group>
        </>
      )}
    </Stack>
  );
}

function UserRow({
  item,
  onClick,
}: {
  item: AdminUserListItem;
  onClick: () => void;
}) {
  return (
    <Table.Tr
      onClick={onClick}
      style={{
        cursor: 'pointer',
        // F17.10: блокированных подсвечиваем красным. background через
        // inline-style работает корректнее, чем класс — Mantine добавляет
        // hover-фон поверх, но базовый цвет блока сохраняется.
        background: item.isBlocked ? cloudColors.dangerSurface : undefined,
      }}
    >
      <Table.Td>{item.email}</Table.Td>
      <Table.Td>{item.login}</Table.Td>
      <Table.Td>{item.userName}</Table.Td>
      <Table.Td>{item.fullName ?? '—'}</Table.Td>
      <Table.Td>
        <Badge variant="light" color={roleBadgeColor(item.role)}>
          {ROLE_LABELS[item.role]}
        </Badge>
      </Table.Td>
      <Table.Td>{formatDateTime(item.registeredAtUtc)}</Table.Td>
      <Table.Td>
        {item.lastLoginAtUtc ? formatDateTime(item.lastLoginAtUtc) : '—'}
      </Table.Td>
      <Table.Td>{item.trackingCount}</Table.Td>
      <Table.Td>
        {item.isBlocked ? (
          <Badge color="red" variant="filled">
            🚫 Заблокирован
          </Badge>
        ) : (
          <BodyLabel c={cloudColors.azureDeep}>Активен</BodyLabel>
        )}
      </Table.Td>
    </Table.Tr>
  );
}

function roleBadgeColor(role: AdminUserRole): string {
  switch (role) {
    case 'SuperAdmin':
      return 'grape';
    case 'Admin':
      return 'azure';
    case 'Manager':
      return 'teal';
    case 'RegularUser':
    default:
      return 'gray';
  }
}

