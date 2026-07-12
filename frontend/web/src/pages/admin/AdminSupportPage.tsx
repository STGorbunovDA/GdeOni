import { useState } from 'react';
import {
  Alert,
  Badge,
  Chip,
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
import { RefreshCcw, Search } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import {
  supportApi,
  type SupportTicket,
  type TicketKind,
  type TicketSeverity,
  type TicketSource,
  type TicketStatus,
} from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import {
  formatDateTime,
  toDateInputValue,
  parseDateInputValue,
} from '../../utils/formatDate';
import {
  KIND_LABELS,
  KIND_OPTIONS,
  SEVERITY_COLORS,
  SEVERITY_LABELS,
  SOURCE_COLORS,
  SOURCE_LABELS,
  SOURCE_OPTIONS,
  STATUS_COLORS,
  STATUS_LABELS,
} from '../support/supportLabels';

/**
 * F17.14. Админ-листинг обращений. Фильтры зеркалят backend:
 *  - Статусы (multi-chip): Open / InProgress / Resolved;
 *  - Severity (multi-chip): Normal / Urgent;
 *  - Kind (Select single);
 *  - Source (Select single: Manual/Auto);
 *  - Search (title/description/email);
 *  - CreatedFromUtc/CreatedToUtc.
 *
 * Backend отдаёт items уже отсортированные по CreatedAtUtc DESC.
 */
const PAGE_SIZE_OPTIONS = ['20', '50', '100'];

export function AdminSupportPage() {
  const navigate = useNavigate();
  const [statuses, setStatuses] = useState<TicketStatus[]>([]);
  const [severities, setSeverities] = useState<TicketSeverity[]>([]);
  const [kind, setKind] = useState<TicketKind | null>(null);
  const [source, setSource] = useState<TicketSource | null>(null);
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [createdFrom, setCreatedFrom] = useState<Date | null>(null);
  const [createdTo, setCreatedTo] = useState<Date | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const query = useQuery({
    queryKey: [
      'admin-support-tickets',
      {
        statuses,
        severities,
        kind,
        source,
        search: debouncedSearch || undefined,
        from: createdFrom?.toISOString(),
        to: createdTo?.toISOString(),
        page,
        pageSize,
      },
    ],
    queryFn: () =>
      supportApi.adminList({
        statuses: statuses.length > 0 ? statuses : undefined,
        severities: severities.length > 0 ? severities : undefined,
        kind: kind ?? undefined,
        source: source ?? undefined,
        search: debouncedSearch.trim() || undefined,
        createdFromUtc: createdFrom?.toISOString(),
        createdToUtc: createdTo?.toISOString(),
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
        <TitleLabel>Обращения</TitleLabel>
        <CaptionLabel>
          Обращения пользователей и автоматические инциденты. Сортировка
          — самые свежие сверху.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Stack gap="xs">
            <CaptionLabel>Статус</CaptionLabel>
            <Chip.Group
              multiple
              value={statuses}
              onChange={(v) => resetToFirstPage(setStatuses)(v as TicketStatus[])}
            >
              <Group gap="xs">
                <Chip value="Open">{STATUS_LABELS.Open}</Chip>
                <Chip value="InProgress">{STATUS_LABELS.InProgress}</Chip>
                <Chip value="Resolved">{STATUS_LABELS.Resolved}</Chip>
              </Group>
            </Chip.Group>
          </Stack>

          <Stack gap="xs">
            <CaptionLabel>Критичность</CaptionLabel>
            <Chip.Group
              multiple
              value={severities}
              onChange={(v) =>
                resetToFirstPage(setSeverities)(v as TicketSeverity[])
              }
            >
              <Group gap="xs">
                <Chip value="Normal">{SEVERITY_LABELS.Normal}</Chip>
                <Chip color="red" value="Urgent">
                  {SEVERITY_LABELS.Urgent}
                </Chip>
              </Group>
            </Chip.Group>
          </Stack>

          <Group grow align="flex-end">
            <TextInput
              label="Поиск"
              placeholder="Заголовок, текст, email"
              leftSection={<Search size={16} />}
              value={search}
              onChange={(e) =>
                resetToFirstPage(setSearch)(e.currentTarget.value)
              }
            />
            <Select
              label="Тип"
              placeholder="Все"
              clearable
              data={KIND_OPTIONS}
              value={kind}
              onChange={(v) =>
                resetToFirstPage(setKind)((v as TicketKind | null) ?? null)
              }
            />
            <Select
              label="Источник"
              placeholder="Все"
              clearable
              data={SOURCE_OPTIONS}
              value={source}
              onChange={(v) =>
                resetToFirstPage(setSource)((v as TicketSource | null) ?? null)
              }
            />
          </Group>
          <Group grow align="flex-end">
            <TextInput
              type="date"
              label="Создан с"
              value={toDateInputValue(createdFrom)}
              onChange={(e) =>
                resetToFirstPage(setCreatedFrom)(
                  parseDateInputValue(e.currentTarget.value),
                )
              }
            />
            <TextInput
              type="date"
              label="по"
              value={toDateInputValue(createdTo)}
              onChange={(e) =>
                resetToFirstPage(setCreatedTo)(
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
            <Table.ScrollContainer minWidth={1200}>
              <Table verticalSpacing="sm" highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Создан</Table.Th>
                    <Table.Th>Источник</Table.Th>
                    <Table.Th>Тип</Table.Th>
                    <Table.Th>Критичность</Table.Th>
                    <Table.Th>Статус</Table.Th>
                    <Table.Th>Заголовок</Table.Th>
                    <Table.Th>Email</Table.Th>
                    <Table.Th>↻</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <TicketRow
                      key={item.id}
                      item={item}
                      onOpen={() =>
                        navigate(`/admin/support-tickets/${item.id}`)
                      }
                      onOpenUser={(userId) =>
                        navigate(`/admin/users/${userId}`)
                      }
                    />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={8}>
                        <BodyLabel>Обращений по фильтрам не найдено.</BodyLabel>
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

function TicketRow({
  item,
  onOpen,
  onOpenUser,
}: {
  item: SupportTicket;
  onOpen: () => void;
  onOpenUser: (userId: string) => void;
}) {
  return (
    <Table.Tr
      onClick={onOpen}
      style={{
        cursor: 'pointer',
        // Urgent-подсветку снимаем на Resolved — нет смысла привлекать
        // внимание к уже закрытому вопросу.
        background:
          item.severity === 'Urgent' && item.status !== 'Resolved'
            ? '#FFEBEB'
            : undefined,
      }}
    >
      <Table.Td style={{ whiteSpace: 'nowrap' }}>
        {formatDateTime(item.createdAtUtc)}
      </Table.Td>
      <Table.Td>
        <Badge color={SOURCE_COLORS[item.source]} variant="light">
          {SOURCE_LABELS[item.source]}
        </Badge>
      </Table.Td>
      <Table.Td>{KIND_LABELS[item.kind]}</Table.Td>
      <Table.Td>
        <Badge color={SEVERITY_COLORS[item.severity]} variant="light">
          {SEVERITY_LABELS[item.severity]}
        </Badge>
      </Table.Td>
      <Table.Td>
        <Badge color={STATUS_COLORS[item.status]} variant="light">
          {STATUS_LABELS[item.status]}
        </Badge>
      </Table.Td>
      <Table.Td style={{ maxWidth: 300 }}>
        <BodyLabel style={{
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
        }}>
          {item.title}
        </BodyLabel>
      </Table.Td>
      <Table.Td onClick={(e) => e.stopPropagation()}>
        {item.userId && item.userEmail ? (
          <a
            href="#"
            onClick={(e) => {
              e.preventDefault();
              onOpenUser(item.userId!);
            }}
          >
            {item.userEmail}
          </a>
        ) : (
          '—'
        )}
      </Table.Td>
      <Table.Td>
        {item.reopenedCount > 0 && (
          <Group gap={4}>
            <RefreshCcw size={14} color="red" />
            <BodyLabel>{item.reopenedCount}</BodyLabel>
          </Group>
        )}
      </Table.Td>
    </Table.Tr>
  );
}
