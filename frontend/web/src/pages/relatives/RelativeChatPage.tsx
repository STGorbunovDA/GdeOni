import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Loader,
  Stack,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { ChevronLeft, Send } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  relativesApi,
  type RelativeConversationDetail,
  type RelativeMessage,
} from '../../api/endpoints/relativesApi';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';

/**
 * Функция «Родственники» (Фаза 3). Turn-based переписка: пишем по одному
 * сообщению по очереди. Пока собеседник не ответил — поле ввода скрыто, а
 * своё последнее сообщение можно изменить/удалить. Обновления подтягиваем
 * поллингом раз в 15 с и при возврате на вкладку.
 */
export function RelativeChatPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [draft, setDraft] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState('');

  const query = useQuery({
    queryKey: ['relative-conversation', id],
    queryFn: () => relativesApi.getConversation(id!),
    enabled: !!id,
    refetchInterval: 15_000,
    refetchOnWindowFocus: true,
  });

  const apply = (d: RelativeConversationDetail) =>
    queryClient.setQueryData(['relative-conversation', id], d);

  const notifyError = (e: unknown) =>
    notifications.show({
      title: 'Ошибка',
      message: formatError(e),
      color: 'red',
    });

  const sendMutation = useMutation({
    mutationFn: (text: string) => relativesApi.sendMessage(id!, text),
    onSuccess: (d) => {
      apply(d);
      setDraft('');
    },
    onError: notifyError,
  });

  const editMutation = useMutation({
    mutationFn: (vars: { messageId: string; text: string }) =>
      relativesApi.editMessage(id!, vars.messageId, vars.text),
    onSuccess: (d) => {
      apply(d);
      setEditingId(null);
    },
    onError: notifyError,
  });

  const deleteMutation = useMutation({
    mutationFn: (messageId: string) => relativesApi.deleteMessage(id!, messageId),
    onSuccess: apply,
    onError: notifyError,
  });

  const data = query.data;

  return (
    <Stack gap="md" style={{ maxWidth: 640, margin: '0 auto' }}>
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/relatives')}
        >
          К родственникам
        </GhostButton>
      </Group>

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {data && (
        <>
          <Stack gap={2}>
            <SubTitleLabel>{data.otherUserName}</SubTitleLabel>
            <CaptionLabel>
              {data.otherRelationship
                ? `${relationshipDisplay(data.otherRelationship)} · `
                : ''}
              {data.deceasedFullName}
            </CaptionLabel>
          </Stack>

          <Stack gap="sm">
            {data.messages.length === 0 && (
              <CaptionLabel>
                Сообщений пока нет. Напишите первым.
              </CaptionLabel>
            )}
            {data.messages.map((m) => (
              <MessageBubble
                key={m.id}
                message={m}
                isEditing={editingId === m.id}
                editDraft={editDraft}
                onEditDraft={setEditDraft}
                onStartEdit={() => {
                  setEditingId(m.id);
                  setEditDraft(m.text);
                }}
                onCancelEdit={() => setEditingId(null)}
                onSaveEdit={() =>
                  editMutation.mutate({ messageId: m.id, text: editDraft })
                }
                onDelete={() => deleteMutation.mutate(m.id)}
                busy={editMutation.isPending || deleteMutation.isPending}
              />
            ))}
          </Stack>

          {data.canSend ? (
            <Group align="flex-end" gap="sm" wrap="nowrap">
              <Textarea
                value={draft}
                onChange={(e) => setDraft(e.currentTarget.value)}
                placeholder="Сообщение…"
                autosize
                minRows={1}
                maxRows={5}
                style={{ flex: 1 }}
              />
              <PrimaryButton
                leftSection={<Send size={16} />}
                onClick={() => sendMutation.mutate(draft)}
                loading={sendMutation.isPending}
                disabled={draft.trim().length === 0}
              >
                Отправить
              </PrimaryButton>
            </Group>
          ) : (
            <Alert color="blue" variant="light">
              Вы отправили сообщение — ждём ответа от {data.otherUserName}.
              Написать снова можно будет, когда он ответит.
            </Alert>
          )}
        </>
      )}
    </Stack>
  );
}

function MessageBubble({
  message,
  isEditing,
  editDraft,
  onEditDraft,
  onStartEdit,
  onCancelEdit,
  onSaveEdit,
  onDelete,
  busy,
}: {
  message: RelativeMessage;
  isEditing: boolean;
  editDraft: string;
  onEditDraft: (v: string) => void;
  onStartEdit: () => void;
  onCancelEdit: () => void;
  onSaveEdit: () => void;
  onDelete: () => void;
  busy: boolean;
}) {
  const mine = message.isMine;
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: mine ? 'flex-end' : 'flex-start',
      }}
    >
      <div
        style={{
          maxWidth: '82%',
          borderRadius: 14,
          padding: '8px 12px',
          background: mine ? cloudColors.azure : cloudColors.sky,
          color: mine ? '#ffffff' : cloudColors.inkBlue,
        }}
      >
        {isEditing ? (
          <Stack gap={6}>
            <Textarea
              value={editDraft}
              onChange={(e) => onEditDraft(e.currentTarget.value)}
              autosize
              minRows={1}
              maxRows={5}
            />
            <Group gap="xs" justify="flex-end">
              <Button size="xs" variant="subtle" onClick={onCancelEdit}>
                Отмена
              </Button>
              <Button
                size="xs"
                onClick={onSaveEdit}
                loading={busy}
                disabled={editDraft.trim().length === 0}
              >
                Сохранить
              </Button>
            </Group>
          </Stack>
        ) : (
          <Stack gap={2}>
            <BodyLabel
              style={{
                whiteSpace: 'pre-wrap',
                color: mine ? '#ffffff' : undefined,
              }}
            >
              {message.text}
            </BodyLabel>
            <div
              style={{
                fontSize: 11,
                opacity: 0.85,
                color: mine ? '#eaf3fb' : cloudColors.azureDeep,
              }}
            >
              {formatDateTime(message.createdAtUtc)}
              {message.editedAtUtc ? ' · изменено' : ''}
              {mine ? (message.isRead ? ' · прочитано' : ' · отправлено') : ''}
            </div>
            {message.canEditDelete && (
              <Group gap="sm">
                <button
                  type="button"
                  onClick={onStartEdit}
                  disabled={busy}
                  style={miniLinkStyle(mine)}
                >
                  Изменить
                </button>
                <button
                  type="button"
                  onClick={onDelete}
                  disabled={busy}
                  style={miniLinkStyle(mine)}
                >
                  Удалить
                </button>
              </Group>
            )}
          </Stack>
        )}
      </div>
    </div>
  );
}

function miniLinkStyle(mine: boolean): React.CSSProperties {
  return {
    background: 'transparent',
    border: 'none',
    padding: 0,
    cursor: 'pointer',
    fontSize: 12,
    textDecoration: 'underline',
    color: mine ? '#eaf3fb' : cloudColors.azureDeep,
  };
}
