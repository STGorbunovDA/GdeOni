import { useState } from 'react';
import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Modal,
  Pagination,
  Select,
  Stack,
  Table,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { ChevronLeft, Trash2 } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  TitleLabel,
} from '../../components/ui';
import {
  adminUsersApi,
  type AdminUserTrackedItem,
} from '../../api/endpoints/adminUsersApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateOnly, formatDateTime } from '../../utils/formatDate';
import { relationshipDisplay } from '../../utils/relationshipDisplay';

/**
 * F17.12. Управление отслеживаниями конкретного юзера. Из
 * AdminUserDetailsPage → «Отслеживания» открывается эта таблица.
 *
 * Действия:
 *  - «Удалить» на строке — снять одно отслеживание (без confirm,
 *    админ обычно точно знает что делает + список тут же обновится);
 *  - «Удалить все отслеживания» кнопкой сверху — с confirm,
 *    возвращает removedCount → snack-bar.
 *
 * Web-расширение «чекбоксы для выборочного batch-удаления» из плана
 * оставлено в backlog: одиночное удаление быстрое и достаточное для
 * MVP; batch есть только "всё разом".
 */
const PAGE_SIZE_OPTIONS = ['20', '50', '100'];

const STATUS_LABELS: Record<string, string> = {
  Active: 'Активно',
  Muted: 'Без уведомлений',
  Archived: 'В архиве',
};

const STATUS_COLORS: Record<string, string> = {
  Active: 'green',
  Muted: 'gray',
  Archived: 'orange',
};

export function AdminUserTrackedPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [confirmRemoveAll, setConfirmRemoveAll] = useState(false);

  const query = useQuery({
    queryKey: ['admin-user-tracked', id, page, pageSize],
    queryFn: () => adminUsersApi.listTracked(id!, page, pageSize),
    enabled: !!id,
    placeholderData: (prev) => prev,
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['admin-user-tracked', id] });
    // Детали юзера показывают TrackingCount — тоже освежаем.
    queryClient.invalidateQueries({ queryKey: ['admin-user-details', id] });
    queryClient.invalidateQueries({ queryKey: ['admin-users'] });
  }

  const removeOneMutation = useMutation({
    mutationFn: (deceasedId: string) =>
      adminUsersApi.removeTracking(id!, deceasedId),
    onSuccess: () => invalidate(),
  });

  const removeAllMutation = useMutation({
    mutationFn: () => adminUsersApi.removeAllTracking(id!),
    onSuccess: (data) => {
      invalidate();
      setConfirmRemoveAll(false);
      notifications.show({
        color: 'green',
        title: 'Отслеживания удалены',
        message: `Снято ${data.removedCount} шт.`,
      });
    },
  });

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / pageSize))
    : 1;

  if (!id) {
    return (
      <Stack gap="lg">
        <Alert color="red" variant="light">
          Некорректный идентификатор пользователя.
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between" wrap="wrap">
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate(`/admin/users/${id}`)}
        >
          Назад
        </GhostButton>
        {query.data && query.data.totalCount > 0 && (
          <Button
            color="red"
            variant="light"
            leftSection={<Trash2 size={16} />}
            onClick={() => setConfirmRemoveAll(true)}
          >
            Удалить все отслеживания
          </Button>
        )}
      </Group>

      <Stack gap="xs">
        <TitleLabel>Отслеживания пользователя</TitleLabel>
        <CaptionLabel>
          Список карточек умерших, за которыми следит выбранный юзер.
          Удаление отслеживания НЕ трогает саму карточку — только связь
          «юзер ↔ карточка».
        </CaptionLabel>
      </Stack>

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {removeOneMutation.isError && (
        <Alert
          color="red"
          variant="light"
          withCloseButton
          onClose={() => removeOneMutation.reset()}
        >
          {formatError(removeOneMutation.error)}
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
                    <Table.Th>ФИО</Table.Th>
                    <Table.Th>Род.</Table.Th>
                    <Table.Th>Смерть</Table.Th>
                    <Table.Th>Отношение</Table.Th>
                    <Table.Th>Статус</Table.Th>
                    <Table.Th>Подписался</Table.Th>
                    <Table.Th w={60}></Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {query.data.items.map((item) => (
                    <TrackingRow
                      key={item.deceasedId}
                      item={item}
                      onOpenCard={() =>
                        navigate(`/admin/deceased/${item.deceasedId}`)
                      }
                      onRemove={() =>
                        removeOneMutation.mutate(item.deceasedId)
                      }
                      removing={
                        removeOneMutation.isPending &&
                        removeOneMutation.variables === item.deceasedId
                      }
                    />
                  ))}
                  {query.data.items.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={7}>
                        <BodyLabel>
                          Пользователь никого не отслеживает.
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

      <Modal
        opened={confirmRemoveAll}
        onClose={() =>
          !removeAllMutation.isPending && setConfirmRemoveAll(false)
        }
        title="Удалить все отслеживания?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            У юзера пропадёт весь список отслеживаемых карточек. Сами
            карточки при этом останутся — удалится только связь
            «юзер ↔ карточка».
          </BodyLabel>
          {removeAllMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(removeAllMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmRemoveAll(false)}
              disabled={removeAllMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              onClick={() => removeAllMutation.mutate()}
              loading={removeAllMutation.isPending}
            >
              Удалить все
            </Button>
          </Group>
        </Stack>
      </Modal>

    </Stack>
  );
}

function TrackingRow({
  item,
  onOpenCard,
  onRemove,
  removing,
}: {
  item: AdminUserTrackedItem;
  onOpenCard: () => void;
  onRemove: () => void;
  removing: boolean;
}) {
  return (
    <Table.Tr>
      <Table.Td>
        <GhostButton size="compact-xs" onClick={onOpenCard}>
          {item.fullName}
        </GhostButton>
      </Table.Td>
      <Table.Td>
        {item.birthDate ? formatDateOnly(item.birthDate) : '—'}
      </Table.Td>
      <Table.Td>{formatDateOnly(item.deathDate)}</Table.Td>
      <Table.Td>{relationshipDisplay(item.relationshipType)}</Table.Td>
      <Table.Td>
        <Badge
          color={STATUS_COLORS[item.status] ?? 'gray'}
          variant="light"
        >
          {STATUS_LABELS[item.status] ?? item.status}
        </Badge>
      </Table.Td>
      <Table.Td>{formatDateTime(item.trackedAtUtc)}</Table.Td>
      <Table.Td>
        <ActionIcon
          variant="subtle"
          color="red"
          loading={removing}
          onClick={onRemove}
          aria-label="Удалить отслеживание"
          title="Удалить отслеживание"
        >
          <Trash2 size={16} />
        </ActionIcon>
      </Table.Td>
    </Table.Tr>
  );
}
