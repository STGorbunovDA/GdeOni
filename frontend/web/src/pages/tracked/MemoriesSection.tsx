import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Modal,
  Stack,
  Textarea,
} from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  memoriesApi,
  MEMORY_TEXT_MAX_LENGTH,
} from '../../api/endpoints/memoriesApi';
import { formatError } from '../../auth/errorMessages';
import { useAuthStore } from '../../auth/authStore';
import type { DeceasedMemory } from '../../api/endpoints/deceasedApi';

/**
 * F12. Блок "Воспоминания" внутри F11. Зеркало mobile
 * MemoryEditorViewModel + соответствующего блока DeceasedDetailsPage.xaml.
 *
 *  - Список карточек, отсортированных бэком по CreatedAtUtc
 *  - Кнопка "+ Добавить" → modal Create
 *  - У своих memory есть Edit/Delete (по authorUserId === currentUserId)
 *  - После любой mutation инвалидируем details-query (memories живут
 *    внутри DeceasedDetailsResponse — отдельного GET-list нет)
 */
export function MemoriesSection({
  deceasedId,
  memories,
}: {
  deceasedId: string;
  memories: DeceasedMemory[];
}) {
  const queryClient = useQueryClient();
  const currentUserId = useAuthStore((s) => s.user?.id);
  const [editing, setEditing] = useState<DeceasedMemory | null>(null);
  const [creating, setCreating] = useState(false);
  const [pendingDelete, setPendingDelete] = useState<DeceasedMemory | null>(null);

  const deleteMutation = useMutation({
    mutationFn: (memoryId: string) => memoriesApi.remove(deceasedId, memoryId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', deceasedId] });
      setPendingDelete(null);
    },
  });

  return (
    <CloudCard>
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <SubTitleLabel>Воспоминания</SubTitleLabel>
          <GhostButton
            leftSection={<Plus size={16} />}
            onClick={() => setCreating(true)}
          >
            Добавить
          </GhostButton>
        </Group>

        {deleteMutation.isError && (
          <Alert color="red" variant="light">
            {formatError(deleteMutation.error)}
          </Alert>
        )}

        {memories.length === 0 ? (
          <CaptionLabel>
            Пока нет воспоминаний. Нажмите «Добавить», чтобы поделиться
            первым.
          </CaptionLabel>
        ) : (
          memories.map((m) => (
            <MemoryItem
              key={m.id}
              memory={m}
              canEdit={!!currentUserId && m.authorUserId === currentUserId}
              onEdit={() => setEditing(m)}
              onDelete={() => setPendingDelete(m)}
            />
          ))
        )}

        <MemoryEditorModal
          opened={creating}
          mode="create"
          deceasedId={deceasedId}
          onClose={() => setCreating(false)}
        />
        <MemoryEditorModal
          opened={editing !== null}
          mode="edit"
          deceasedId={deceasedId}
          initial={editing}
          onClose={() => setEditing(null)}
        />

        <Modal
          opened={pendingDelete !== null}
          onClose={() => !deleteMutation.isPending && setPendingDelete(null)}
          title="Удалить воспоминание?"
          centered
          size="md"
        >
          <Stack gap="md">
            <BodyLabel>
              Удалённое воспоминание восстановить нельзя. Подтвердите,
              если уверены.
            </BodyLabel>
            <Group justify="flex-end" gap="sm">
              <Button
                variant="default"
                onClick={() => setPendingDelete(null)}
                disabled={deleteMutation.isPending}
              >
                Отмена
              </Button>
              <Button
                color="red"
                onClick={() => pendingDelete && deleteMutation.mutate(pendingDelete.id)}
                loading={deleteMutation.isPending}
              >
                Удалить
              </Button>
            </Group>
          </Stack>
        </Modal>
      </Stack>
    </CloudCard>
  );
}

function MemoryItem({
  memory,
  canEdit,
  onEdit,
  onDelete,
}: {
  memory: DeceasedMemory;
  canEdit: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const createdAt = new Date(memory.createdAtUtc);
  const dateText = `${String(createdAt.getDate()).padStart(2, '0')}.${String(createdAt.getMonth() + 1).padStart(2, '0')}.${createdAt.getFullYear()}`;
  const wasEdited = !!memory.updatedAtUtc;

  return (
    <div
      style={{
        padding: '12px 14px',
        borderRadius: 12,
        background: '#FAFCFE',
        border: `1px solid ${cloudColors.cloudBorder}`,
      }}
    >
      <Stack gap="xs">
        <CaptionLabel c={cloudColors.azureDeep}>
          {memory.authorName ?? 'Аноним'}
        </CaptionLabel>
        <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>{memory.text}</BodyLabel>
        <Group justify="space-between" align="center">
          <CaptionLabel>
            {dateText}
            {wasEdited && ' · отредактировано'}
          </CaptionLabel>
          {canEdit && (
            <Group gap="xs">
              <Button
                variant="subtle"
                color="azure"
                size="xs"
                leftSection={<Pencil size={14} />}
                onClick={onEdit}
              >
                Изменить
              </Button>
              <Button
                variant="subtle"
                color="red"
                size="xs"
                leftSection={<Trash2 size={14} />}
                onClick={onDelete}
              >
                Удалить
              </Button>
            </Group>
          )}
        </Group>
      </Stack>
    </div>
  );
}

/**
 * Модалка создания/редактирования. Один компонент по props — режим
 * различается только source/destination и текстом кнопки.
 *
 * Trim перед save — backend всё равно его делает, но клиентский trim
 * даёт корректный валидационный disable для строки "   ".
 */
function MemoryEditorModal({
  opened,
  mode,
  deceasedId,
  initial,
  onClose,
}: {
  opened: boolean;
  mode: 'create' | 'edit';
  deceasedId: string;
  initial?: DeceasedMemory | null;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [text, setText] = useState(initial?.text ?? '');
  // Когда opened меняется на true, перезагружаем текст из initial.
  // useEffect не нужен — modal каждый раз монтируется заново.
  const [didInit, setDidInit] = useState(false);
  if (opened && !didInit) {
    setText(initial?.text ?? '');
    setDidInit(true);
  }
  if (!opened && didInit) {
    setDidInit(false);
  }

  const mutation = useMutation({
    mutationFn: async () => {
      if (mode === 'create') {
        await memoriesApi.add(deceasedId, text.trim());
      } else if (initial) {
        await memoriesApi.update(deceasedId, initial.id, text.trim());
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', deceasedId] });
      onClose();
    },
  });

  const trimmedLength = text.trim().length;
  const isTooLong = text.length > MEMORY_TEXT_MAX_LENGTH;
  const canSave = trimmedLength > 0 && !isTooLong && !mutation.isPending;

  return (
    <Modal
      opened={opened}
      onClose={() => !mutation.isPending && onClose()}
      title={mode === 'create' ? 'Новое воспоминание' : 'Редактировать воспоминание'}
      centered
      size="lg"
    >
      <Stack gap="md">
        <Textarea
          placeholder="Расскажите что-то, что помните о нём…"
          value={text}
          onChange={(e) => setText(e.currentTarget.value)}
          autosize
          minRows={4}
          maxRows={12}
          error={isTooLong ? `Превышено на ${text.length - MEMORY_TEXT_MAX_LENGTH} символов` : undefined}
        />
        <Group justify="space-between" align="center">
          <CaptionLabel c={isTooLong ? cloudColors.errorRed : undefined}>
            {text.length} / {MEMORY_TEXT_MAX_LENGTH}
          </CaptionLabel>
          <Group gap="sm">
            <Button
              variant="default"
              onClick={onClose}
              disabled={mutation.isPending}
            >
              Отмена
            </Button>
            <PrimaryButton
              onClick={() => mutation.mutate()}
              disabled={!canSave}
              loading={mutation.isPending}
            >
              {mode === 'create' ? 'Опубликовать' : 'Сохранить'}
            </PrimaryButton>
          </Group>
        </Group>
        {mutation.isError && (
          <Alert color="red" variant="light">
            {formatError(mutation.error)}
          </Alert>
        )}
      </Stack>
    </Modal>
  );
}
