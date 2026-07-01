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
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, Diff } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  TitleLabel,
} from '../../components/ui';
import {
  adminEditsApi,
  type EditItem,
  type EditKind,
} from '../../api/endpoints/adminEditsApi';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';
import { DiffModal } from './AdminEditsPage';

/**
 * F17.9. Per-card история правок карточки умершего. Открывается из
 * AdminDeceasedViewPage → «История правок» и живёт под AdminLayout,
 * чтобы sidebar админки остался виден.
 *
 * Без фильтров (карточка одна, фильтровать нечего кроме pagination).
 * Модаль diff — общий компонент с /admin/edits, экспортирован оттуда.
 */
const PAGE_SIZE_OPTIONS = ['20', '50', '100'];

const KIND_LABELS: Record<EditKind, string> = {
  MainInfo: 'Основное',
  Metadata: 'Дополнительно',
  BurialLocation: 'Место захоронения',
  Reassignment: 'Переуступка',
};

const KIND_COLORS: Record<EditKind, string> = {
  MainInfo: 'azure',
  Metadata: 'gray',
  BurialLocation: 'teal',
  Reassignment: 'orange',
};

export function AdminDeceasedEditsPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [viewingDiff, setViewingDiff] = useState<EditItem | null>(null);

  // Тянем ФИО отдельно из /deceased-records/{id} — endpoint истории
  // возвращает записи без ФИО (в per-card карточка одна). Показываем
  // в заголовке для контекста.
  const deceasedQuery = useQuery({
    queryKey: ['admin-deceased-details', id],
    queryFn: () => deceasedApi.getById(id!),
    enabled: !!id,
  });

  const editsQuery = useQuery({
    queryKey: ['admin-deceased-edits', id, page, pageSize],
    queryFn: () => adminEditsApi.listByDeceased(id!, page, pageSize),
    enabled: !!id,
    placeholderData: (prev) => prev,
  });

  const totalPages = editsQuery.data
    ? Math.max(1, Math.ceil(editsQuery.data.totalCount / pageSize))
    : 1;

  if (!id) {
    return (
      <Stack gap="lg">
        <Alert color="red" variant="light">
          Некорректный идентификатор карточки.
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate(`/admin/deceased/${id}`)}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>История правок</TitleLabel>
        <CaptionLabel>
          {deceasedQuery.data
            ? `Карточка «${deceasedQuery.data.fullName}»`
            : 'Загружаем карточку…'}
        </CaptionLabel>
      </Stack>

      {editsQuery.isError && (
        <Alert color="red" variant="light">
          {formatError(editsQuery.error)}
        </Alert>
      )}

      {editsQuery.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {editsQuery.data && (
        <>
          <CloudCard p={0}>
            <Table.ScrollContainer minWidth={800}>
              <Table verticalSpacing="sm" highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Дата</Table.Th>
                    <Table.Th>Редактор</Table.Th>
                    <Table.Th>Тип</Table.Th>
                    <Table.Th>Diff</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {editsQuery.data.items.map((item) => (
                    <Table.Tr key={item.id}>
                      <Table.Td style={{ whiteSpace: 'nowrap' }}>
                        {formatDateTime(item.editedAtUtc)}
                      </Table.Td>
                      <Table.Td>
                        {item.editedByDisplayName ??
                          item.editedByEmail ??
                          '(удалён)'}
                      </Table.Td>
                      <Table.Td>
                        <Badge color={KIND_COLORS[item.kind]} variant="light">
                          {KIND_LABELS[item.kind]}
                        </Badge>
                      </Table.Td>
                      <Table.Td>
                        <GhostButton
                          size="compact-xs"
                          leftSection={<Diff size={14} />}
                          onClick={() => setViewingDiff(item)}
                        >
                          Показать
                        </GhostButton>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                  {editsQuery.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={4}>
                        <BodyLabel>
                          Правок по этой карточке ещё не было.
                        </BodyLabel>
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
                Всего: {editsQuery.data.totalCount}
                {editsQuery.isFetching && ' · обновляем…'}
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

      <DiffModal edit={viewingDiff} onClose={() => setViewingDiff(null)} />
    </Stack>
  );
}
