import { useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Modal,
  Pagination,
  Select,
  Stack,
  Table,
  TextInput,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Diff } from 'lucide-react';
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
  type EditWithCard,
} from '../../api/endpoints/adminEditsApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';
import {
  formatDateTime,
  toDateInputValue,
  parseDateInputValue,
} from '../../utils/formatDate';

/**
 * F17.9 / D24. Глобальная лента правок карточек умерших. Сортировка
 * на бэке: EditedAtUtc desc, самые свежие сверху.
 *
 * Kind → русские лейблы:
 *  MainInfo → «Основное», Metadata → «Дополнительно»,
 *  BurialLocation → «Место захоронения»,
 *  Reassignment → «Переуступка» (появляется при удалении автора карточки
 *   — в ChangesJson.PreviousAuthor лежит email прежнего владельца).
 *
 * Фильтры deceasedId / editorUserId принимают Guid'ы вручную —
 * автокомплит по карточкам и юзерам оставлен в backlog (плановое
 * улучшение); в первую очередь нужен именно raw-фильтр для дежа-вю
 * с mobile.
 *
 * "Показать diff" открывает модаль с side-by-side "Было / Стало" по
 * полям ChangesJson. Web-расширение над mobile (там был просто JSON).
 * "Откатить правку" — backlog per F17.9 плану.
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

export function AdminEditsPage() {
  const navigate = useNavigate();
  const [deceasedId, setDeceasedId] = useState('');
  const [editorUserId, setEditorUserId] = useState('');
  const [editedFrom, setEditedFrom] = useState<Date | null>(null);
  const [editedTo, setEditedTo] = useState<Date | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [viewingDiff, setViewingDiff] = useState<
    EditWithCard | EditItem | null
  >(null);

  const query = useQuery({
    queryKey: [
      'admin-edits',
      {
        deceasedId: deceasedId || undefined,
        editorUserId: editorUserId || undefined,
        from: editedFrom?.toISOString(),
        to: editedTo?.toISOString(),
        page,
        pageSize,
      },
    ],
    queryFn: () =>
      adminEditsApi.listAll({
        deceasedId: deceasedId.trim() || undefined,
        editorUserId: editorUserId.trim() || undefined,
        editedFromUtc: editedFrom?.toISOString(),
        editedToUtc: editedTo?.toISOString(),
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
        <TitleLabel>История правок</TitleLabel>
        <CaptionLabel>
          Лента всех изменений карточек умерших по всей системе.
          Сортировка — самые свежие сверху.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Group grow align="flex-end">
            <TextInput
              label="ID карточки"
              placeholder="GUID"
              value={deceasedId}
              onChange={(e) =>
                resetToFirstPage(setDeceasedId)(e.currentTarget.value)
              }
            />
            <TextInput
              label="ID редактора"
              placeholder="GUID пользователя"
              value={editorUserId}
              onChange={(e) =>
                resetToFirstPage(setEditorUserId)(e.currentTarget.value)
              }
            />
          </Group>
          <Group grow align="flex-end">
            <TextInput
              type="date"
              label="Правка с"
              value={toDateInputValue(editedFrom)}
              onChange={(e) =>
                resetToFirstPage(setEditedFrom)(
                  parseDateInputValue(e.currentTarget.value),
                )
              }
            />
            <TextInput
              type="date"
              label="по"
              value={toDateInputValue(editedTo)}
              onChange={(e) =>
                resetToFirstPage(setEditedTo)(
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
            <Table.ScrollContainer minWidth={1100}>
              <Table verticalSpacing="sm" highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Дата</Table.Th>
                    <Table.Th>Карточка</Table.Th>
                    <Table.Th>Редактор</Table.Th>
                    <Table.Th>Тип</Table.Th>
                    <Table.Th>Diff</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <EditRow
                      key={item.id}
                      item={item}
                      onOpenCard={() =>
                        navigate(`/admin/deceased/${item.deceasedId}`)
                      }
                      onOpenDiff={() => setViewingDiff(item)}
                    />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={5}>
                        <BodyLabel>Правок по фильтрам не найдено.</BodyLabel>
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

      <DiffModal edit={viewingDiff} onClose={() => setViewingDiff(null)} />
    </Stack>
  );
}

function EditRow({
  item,
  onOpenCard,
  onOpenDiff,
}: {
  item: EditWithCard;
  onOpenCard: () => void;
  onOpenDiff: () => void;
}) {
  const editor =
    item.editedByDisplayName ?? item.editedByEmail ?? '(удалён)';
  return (
    <Table.Tr>
      <Table.Td style={{ whiteSpace: 'nowrap' }}>
        {formatDateTime(item.editedAtUtc)}
      </Table.Td>
      <Table.Td>
        <GhostButton size="compact-xs" onClick={onOpenCard}>
          {item.deceasedFullName}
        </GhostButton>
      </Table.Td>
      <Table.Td>{editor}</Table.Td>
      <Table.Td>
        <Badge color={KIND_COLORS[item.kind]} variant="light">
          {KIND_LABELS[item.kind]}
        </Badge>
      </Table.Td>
      <Table.Td>
        <GhostButton
          size="compact-xs"
          leftSection={<Diff size={14} />}
          onClick={onOpenDiff}
        >
          Показать
        </GhostButton>
      </Table.Td>
    </Table.Tr>
  );
}

/**
 * Side-by-side diff по ChangesJson формата
 * { "FieldName": { "old": "...", "new": "..." } }. Bad-JSON (не должен
 * встречаться, но на всякий случай) отображается как «сырое» тело.
 */
export function DiffModal({
  edit,
  onClose,
}: {
  edit: EditWithCard | EditItem | null;
  onClose: () => void;
}) {
  const opened = edit !== null;
  const parsed = edit ? tryParseChanges(edit.changesJson) : null;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Правка карточки"
      centered
      size="xl"
    >
      {edit && (
        <Stack gap="md">
          <Group gap="md">
            <CaptionLabel>
              {formatDateTime(edit.editedAtUtc)} ·{' '}
              <Badge color={KIND_COLORS[edit.kind]} variant="light">
                {KIND_LABELS[edit.kind]}
              </Badge>
            </CaptionLabel>
          </Group>

          {parsed ? (
            <Table striped withTableBorder>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Поле</Table.Th>
                  <Table.Th>Было</Table.Th>
                  <Table.Th>Стало</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {parsed.map(({ field, oldValue, newValue }) => (
                  <Table.Tr key={field}>
                    <Table.Td style={{ fontWeight: 500 }}>
                      {friendlyFieldName(field)}
                    </Table.Td>
                    <Table.Td>{formatValue(oldValue)}</Table.Td>
                    <Table.Td>{formatValue(newValue)}</Table.Td>
                  </Table.Tr>
                ))}
                {parsed.length === 0 && (
                  <Table.Tr>
                    <Table.Td colSpan={3}>
                      <BodyLabel>Изменений в JSON нет.</BodyLabel>
                    </Table.Td>
                  </Table.Tr>
                )}
              </Table.Tbody>
            </Table>
          ) : (
            <pre
              style={{
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                background: cloudColors.sunken,
                color: cloudColors.text,
                padding: 12,
                borderRadius: 8,
                fontSize: 12,
              }}
            >
              {edit.changesJson}
            </pre>
          )}
        </Stack>
      )}
    </Modal>
  );
}

type Change = { field: string; oldValue: unknown; newValue: unknown };

function tryParseChanges(json: string): Change[] | null {
  try {
    const raw = JSON.parse(json) as Record<string, unknown>;
    if (typeof raw !== 'object' || raw === null) return null;
    return Object.entries(raw).map(([field, val]) => {
      // Ожидаемый shape: { old, new }. Если бэк отдал строку/число
      // напрямую — покажем в "Стало" как есть, "Было" оставим пустым.
      if (
        typeof val === 'object' &&
        val !== null &&
        ('old' in val || 'new' in val)
      ) {
        const obj = val as { old?: unknown; new?: unknown };
        return { field, oldValue: obj.old ?? null, newValue: obj.new ?? null };
      }
      return { field, oldValue: null, newValue: val };
    });
  } catch {
    return null;
  }
}

function friendlyFieldName(field: string): string {
  // Небольшой словарик под самые частые. Остальное показываем как есть —
  // раскрывать все возможные поля Deceased слишком дорого, лучше пусть
  // админ видит raw field name из C# DTO.
  const MAP: Record<string, string> = {
    FirstName: 'Имя',
    LastName: 'Фамилия',
    MiddleName: 'Отчество',
    BirthDate: 'Дата рождения',
    DeathDate: 'Дата смерти',
    ShortDescription: 'Кратко',
    Biography: 'Биография',
    Country: 'Страна',
    Region: 'Регион',
    City: 'Город',
    CemeteryName: 'Кладбище',
    PlotNumber: 'Участок',
    GraveNumber: 'Могила',
    Latitude: 'Широта',
    Longitude: 'Долгота',
    AccuracyMeters: 'Точность (м)',
    Epitaph: 'Эпитафия',
    Religion: 'Религия',
    Source: 'Источник',
    IsMilitaryService: 'Военная служба',
    AdditionalInfo: 'Дополнительно',
    PreviousAuthor: 'Прежний автор',
  };
  return MAP[field] ?? field;
}

function formatValue(v: unknown): string {
  if (v === null || v === undefined || v === '') return '—';
  if (typeof v === 'boolean') return v ? 'да' : 'нет';
  if (typeof v === 'object') return JSON.stringify(v);
  return String(v);
}
