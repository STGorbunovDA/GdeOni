import { useState } from 'react';
import {
  Alert,
  Badge,
  Checkbox,
  Group,
  Loader,
  Pagination,
  Select,
  Stack,
  Table,
  TextInput,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
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
import {
  adminDeceasedApi,
  type AdminDeceasedListItem,
} from '../../api/endpoints/adminDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateOnly, formatDateTime } from '../../utils/formatDate';

/**
 * F17.1. Все карточки умерших таблицей — админская сводка.
 *
 * На бэке GET /api/deceased-records после D15 открыт всем
 * authenticated юзерам, поэтому используем стандартный list-endpoint
 * с расширенными фильтрами (IsVerified, CreatedFrom/CreatedTo,
 * Country, City). Action'ы на строке (Verify/Unverify — F17.3,
 * Удалить — F17.2) приедут отдельными подпунктами; пока «Открыть»
 * ведёт на admin-view карточки (/admin/deceased/:id), всё остальное —
 * backlog F17.1.
 *
 * Pagination + page-size selector 20/50/100. Поиск дебоунсится
 * на 300мс через @mantine/hooks.
 *
 * Web-расширения (сортировка колонок, CSV-экспорт) пока не
 * добавлены — backlog как и в F17.7.
 */
const PAGE_SIZE_OPTIONS = ['20', '50', '100'];

export function AdminDeceasedPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [country, setCountry] = useState('');
  const [debouncedCountry] = useDebouncedValue(country, 300);
  const [city, setCity] = useState('');
  const [debouncedCity] = useDebouncedValue(city, 300);
  const [unverifiedOnly, setUnverifiedOnly] = useState(false);
  const [createdFrom, setCreatedFrom] = useState<Date | null>(null);
  const [createdTo, setCreatedTo] = useState<Date | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const query = useQuery({
    queryKey: [
      'admin-deceased',
      {
        search: debouncedSearch || undefined,
        country: debouncedCountry || undefined,
        city: debouncedCity || undefined,
        isVerified: unverifiedOnly ? false : undefined,
        createdFrom: createdFrom?.toISOString(),
        createdTo: createdTo?.toISOString(),
        page,
        pageSize,
      },
    ],
    queryFn: () =>
      adminDeceasedApi.list({
        search: debouncedSearch.trim() || undefined,
        country: debouncedCountry.trim() || undefined,
        city: debouncedCity.trim() || undefined,
        isVerified: unverifiedOnly ? false : undefined,
        createdFrom: createdFrom?.toISOString(),
        createdTo: createdTo?.toISOString(),
        page,
        pageSize,
      }),
    placeholderData: (prev) => prev,
  });

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / pageSize))
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
        <TitleLabel>Карточки умерших</TitleLabel>
        <CaptionLabel>
          Все карточки в системе. Используется для модерации и аудита.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Group grow align="flex-end">
            <TextInput
              label="Поиск по имени"
              placeholder="ФИО или часть"
              leftSection={<Search size={16} />}
              value={search}
              onChange={(e) =>
                resetToFirstPage(setSearch)(e.currentTarget.value)
              }
            />
            <TextInput
              label="Страна"
              placeholder="Россия"
              value={country}
              onChange={(e) =>
                resetToFirstPage(setCountry)(e.currentTarget.value)
              }
            />
            <TextInput
              label="Город"
              placeholder="Москва"
              value={city}
              onChange={(e) =>
                resetToFirstPage(setCity)(e.currentTarget.value)
              }
            />
          </Group>
          <Group grow align="flex-end">
            <DateInput
              label="Создана с"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={createdFrom}
              onChange={(v) =>
                resetToFirstPage(setCreatedFrom)(
                  v ? new Date(v as unknown as string) : null,
                )
              }
            />
            <DateInput
              label="по"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={createdTo}
              onChange={(v) =>
                resetToFirstPage(setCreatedTo)(
                  v ? new Date(v as unknown as string) : null,
                )
              }
            />
            <Checkbox
              label="Только неверифицированные"
              checked={unverifiedOnly}
              onChange={(e) =>
                resetToFirstPage(setUnverifiedOnly)(e.currentTarget.checked)
              }
              color="azure"
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
            <Table.ScrollContainer minWidth={1100}>
              <Table verticalSpacing="sm" highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>ФИО</Table.Th>
                    <Table.Th>Род.</Table.Th>
                    <Table.Th>Смерть</Table.Th>
                    <Table.Th>Страна</Table.Th>
                    <Table.Th>Город</Table.Th>
                    <Table.Th>Verified</Table.Th>
                    <Table.Th>Создана</Table.Th>
                    <Table.Th>Автор</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <DeceasedRow
                      key={item.id}
                      item={item}
                      onClick={() => navigate(`/admin/deceased/${item.id}`)}
                    />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={8}>
                        <BodyLabel>Ничего не найдено по фильтрам.</BodyLabel>
                      </Table.Td>
                    </Table.Tr>
                  )}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
          </CloudCard>

          <Group justify="space-between" wrap="wrap">
            <Group gap="md">
              <CaptionLabel>
                Всего: {query.data.totalCount}
                {query.isFetching && ' · обновляем…'}
              </CaptionLabel>
              <Select
                size="xs"
                data={PAGE_SIZE_OPTIONS}
                value={String(pageSize)}
                onChange={(v) => {
                  setPageSize(Number(v ?? 20));
                  setPage(1);
                }}
                allowDeselect={false}
                w={80}
              />
              <CaptionLabel>на стр.</CaptionLabel>
            </Group>
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

function DeceasedRow({
  item,
  onClick,
}: {
  item: AdminDeceasedListItem;
  onClick: () => void;
}) {
  return (
    <Table.Tr onClick={onClick} style={{ cursor: 'pointer' }}>
      <Table.Td>{item.fullName}</Table.Td>
      <Table.Td>{item.birthDate ? formatDateOnly(item.birthDate) : '—'}</Table.Td>
      <Table.Td>{formatDateOnly(item.deathDate)}</Table.Td>
      <Table.Td>{item.country ?? '—'}</Table.Td>
      <Table.Td>{item.city ?? '—'}</Table.Td>
      <Table.Td>
        {item.isVerified ? (
          <Badge color="green" variant="light">
            Verified
          </Badge>
        ) : (
          <Badge color="gray" variant="light">
            —
          </Badge>
        )}
      </Table.Td>
      <Table.Td>{formatDateTime(item.createdAtUtc)}</Table.Td>
      <Table.Td>{item.createdByUserName ?? '—'}</Table.Td>
    </Table.Tr>
  );
}
