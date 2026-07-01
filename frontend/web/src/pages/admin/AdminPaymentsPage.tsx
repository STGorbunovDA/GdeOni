import { useState } from 'react';
import {
  Alert,
  Anchor,
  Badge,
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
import { ExternalLink, Search } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import {
  adminPaymentsApi,
  type PaymentRecord,
  type PaymentStatus,
} from '../../api/endpoints/adminPaymentsApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';

/**
 * F17.8 / D23. Аудит платежей подписок. Read-only таблица — платежи
 * приходят через YooKassa webhook, менять их с админки нельзя, поэтому
 * action'ов на строках нет.
 *
 * ExternalPaymentId кликабелен и открывает страницу платежа в
 * YooKassa-админке (поиск по этому id).
 *
 * Фильтры зеркалят backend endpoint: emailSearch (частичный), status
 * (single Select — бэк не принимает multi), диапазон CreatedFrom/To.
 * Плановые "multi-select status" и CSV-экспорт — backlog.
 */
const PAGE_SIZE_OPTIONS = ['20', '50', '100'];

const STATUS_LABELS: Record<PaymentStatus, string> = {
  Pending: 'Ждёт оплаты',
  Succeeded: 'Оплачен',
  Cancelled: 'Отменён',
  Failed: 'Ошибка',
};

const STATUS_COLORS: Record<PaymentStatus, string> = {
  Pending: 'yellow',
  Succeeded: 'green',
  Cancelled: 'gray',
  Failed: 'red',
};

const STATUS_OPTIONS: { value: PaymentStatus; label: string }[] = [
  { value: 'Pending', label: STATUS_LABELS.Pending },
  { value: 'Succeeded', label: STATUS_LABELS.Succeeded },
  { value: 'Cancelled', label: STATUS_LABELS.Cancelled },
  { value: 'Failed', label: STATUS_LABELS.Failed },
];

export function AdminPaymentsPage() {
  const [emailSearch, setEmailSearch] = useState('');
  const [debouncedEmail] = useDebouncedValue(emailSearch, 300);
  const [status, setStatus] = useState<PaymentStatus | null>(null);
  const [createdFrom, setCreatedFrom] = useState<Date | null>(null);
  const [createdTo, setCreatedTo] = useState<Date | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const query = useQuery({
    queryKey: [
      'admin-payments',
      {
        emailSearch: debouncedEmail || undefined,
        status: status ?? undefined,
        from: createdFrom?.toISOString(),
        to: createdTo?.toISOString(),
        page,
        pageSize,
      },
    ],
    queryFn: () =>
      adminPaymentsApi.list({
        emailSearch: debouncedEmail.trim() || undefined,
        status: status ?? undefined,
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
        <TitleLabel>Платежи</TitleLabel>
        <CaptionLabel>
          История всех платежей подписок. Статусы обновляются
          автоматически из YooKassa через webhook.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Group grow align="flex-end">
            <TextInput
              label="Email юзера"
              placeholder="частичный поиск"
              leftSection={<Search size={16} />}
              value={emailSearch}
              onChange={(e) =>
                resetToFirstPage(setEmailSearch)(e.currentTarget.value)
              }
            />
            <Select
              label="Статус"
              placeholder="Все"
              clearable
              data={STATUS_OPTIONS}
              value={status}
              onChange={(v) =>
                resetToFirstPage(setStatus)((v as PaymentStatus | null) ?? null)
              }
            />
          </Group>
          <Group grow align="flex-end">
            <DateInput
              label="Создан с"
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
                    <Table.Th>Email</Table.Th>
                    <Table.Th>YooKassa ID</Table.Th>
                    <Table.Th>Тариф</Table.Th>
                    <Table.Th>Сумма</Table.Th>
                    <Table.Th>Статус</Table.Th>
                    <Table.Th>Создан</Table.Th>
                    <Table.Th>Обновлён</Table.Th>
                    <Table.Th>Период</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <PaymentRow key={item.id} item={item} />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={8}>
                        <BodyLabel>Платежей по фильтрам не найдено.</BodyLabel>
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

function PaymentRow({ item }: { item: PaymentRecord }) {
  // YooKassa дашборд принимает поиск по ID → открываем поиск с
  // автозаполнением. Если у них поменяется URL — админ увидит
  // сам ID (monospace) и сможет скопировать вручную.
  const yookassaUrl = `https://yookassa.ru/my/payments?search=${encodeURIComponent(item.externalPaymentId)}`;

  return (
    <Table.Tr>
      <Table.Td>{item.userEmail ?? '—'}</Table.Td>
      <Table.Td>
        <Anchor
          href={yookassaUrl}
          target="_blank"
          rel="noopener noreferrer"
          size="sm"
        >
          <Group gap={4} wrap="nowrap">
            <span style={{ fontFamily: 'monospace' }}>
              {shortId(item.externalPaymentId)}
            </span>
            <ExternalLink size={12} />
          </Group>
        </Anchor>
      </Table.Td>
      <Table.Td>{item.plan}</Table.Td>
      <Table.Td style={{ whiteSpace: 'nowrap' }}>
        {formatMoney(item.amountRub)}
      </Table.Td>
      <Table.Td>
        <Badge color={STATUS_COLORS[item.status]} variant="light">
          {STATUS_LABELS[item.status]}
        </Badge>
      </Table.Td>
      <Table.Td>{formatDateTime(item.createdAtUtc)}</Table.Td>
      <Table.Td>
        {item.updatedAtUtc ? formatDateTime(item.updatedAtUtc) : '—'}
      </Table.Td>
      <Table.Td>
        {item.periodStartUtc && item.periodEndUtc
          ? `${formatDateOnly(item.periodStartUtc)} — ${formatDateOnly(item.periodEndUtc)}`
          : '—'}
      </Table.Td>
    </Table.Tr>
  );
}

function shortId(id: string): string {
  if (id.length <= 16) return id;
  return `${id.slice(0, 8)}…${id.slice(-6)}`;
}

function formatMoney(amount: number): string {
  return `${amount.toLocaleString('ru-RU', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} ₽`;
}

function formatDateOnly(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('ru-RU');
}
