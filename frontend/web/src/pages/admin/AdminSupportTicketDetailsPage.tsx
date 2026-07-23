import { useState } from 'react';
import {
  Alert,
  Anchor,
  Badge,
  Button,
  Group,
  Loader,
  Modal,
  Select,
  Stack,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, ExternalLink, RefreshCcw, Send } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import {
  supportApi,
  type TicketSeverity,
  type TicketStatus,
} from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';
import { formatDateTime } from '../../utils/formatDate';
import {
  KIND_LABELS,
  SEVERITY_COLORS,
  SEVERITY_LABELS,
  SEVERITY_OPTIONS,
  SOURCE_COLORS,
  SOURCE_LABELS,
  STATUS_COLORS,
  STATUS_LABELS,
  STATUS_OPTIONS,
} from '../support/supportLabels';
import { MessagesChat } from '../support/MessagesChat';
import { AttachmentsSection } from '../support/AttachmentsSection';
import { extractDeceasedRefId } from '../support/deceasedRef';
import { AdminAttachmentActions } from './AdminAttachmentActions';

/**
 * F17.14 / D25.1 / D25.2. Админская карточка тикета. Показывает:
 *  - шапку с email юзера (ссылка на /admin/users/{id}), тип, source,
 *    даты, бейджи acceptedByUser / reopenedCount (D25.1);
 *  - переписку в chat-bubble стиле (D25.2);
 *  - модаль смены статуса (Resolved требует resolutionNote);
 *  - модаль смены severity (blocked when Status=Resolved).
 */
export function AdminSupportTicketDetailsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();
  const [statusModal, setStatusModal] = useState(false);
  const [newStatus, setNewStatus] = useState<TicketStatus>('InProgress');
  const [resolutionNote, setResolutionNote] = useState('');
  const [severityModal, setSeverityModal] = useState(false);
  const [newSeverity, setNewSeverity] = useState<TicketSeverity>('Normal');
  // D40. Принудительное закрытие: причина обязательна.
  const [forceCloseModal, setForceCloseModal] = useState(false);
  const [closeNote, setCloseNote] = useState('');
  const [messageText, setMessageText] = useState('');

  const query = useQuery({
    queryKey: ['admin-support-ticket', id],
    queryFn: () => supportApi.adminGetById(id!),
    enabled: !!id,
  });

  const statusMutation = useMutation({
    mutationFn: () =>
      supportApi.adminUpdateStatus(
        id!,
        newStatus,
        newStatus === 'Resolved' ? resolutionNote.trim() || null : null,
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-support-tickets'] });
      setStatusModal(false);
    },
  });

  const severityMutation = useMutation({
    mutationFn: () => supportApi.adminUpdateSeverity(id!, newSeverity),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-support-tickets'] });
      setSeverityModal(false);
    },
  });

  const forceCloseMutation = useMutation({
    mutationFn: () => supportApi.adminForceClose(id!, closeNote.trim()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-support-tickets'] });
      setForceCloseModal(false);
    },
  });

  // D44. Ответ без смены статуса. Раньше, чтобы написать юзеру, админу
  // приходилось помечать обращение решённым — статус врал.
  const messageMutation = useMutation({
    mutationFn: () => supportApi.adminAddMessage(id!, messageText.trim()),
    onSuccess: () => {
      setMessageText('');
      queryClient.invalidateQueries({ queryKey: ['admin-support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-support-tickets'] });
    },
  });

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate('/admin/support-tickets')} />
        <Alert color="red" variant="light">
          Некорректный идентификатор обращения.
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
        <BackButton onClick={() => navigate('/admin/support-tickets')} />
        <Alert color="red" variant="light">
          {query.error ? formatError(query.error) : 'Обращение не найдено.'}
        </Alert>
      </Stack>
    );
  }

  const t = query.data;
  const messages = t.messages ?? [];
  const deceasedRefId = extractDeceasedRefId(t.description);

  return (
    <Stack gap="lg">
      <Group justify="space-between" wrap="wrap">
        <BackButton onClick={() => navigate('/admin/support-tickets')} />
      </Group>

      <Stack gap="xs">
        <TitleLabel>{t.title}</TitleLabel>
        <Group gap="xs" wrap="wrap">
          <Badge color={SOURCE_COLORS[t.source]} variant="light">
            {SOURCE_LABELS[t.source]}
          </Badge>
          <Badge color="gray" variant="light">
            {KIND_LABELS[t.kind]}
          </Badge>
          <Badge color={SEVERITY_COLORS[t.severity]} variant="light">
            {SEVERITY_LABELS[t.severity]}
          </Badge>
          <Badge color={STATUS_COLORS[t.status]} variant="light">
            {STATUS_LABELS[t.status]}
          </Badge>
          {t.acceptedByUser && (
            <Badge color="green" variant="filled">
              ✓ Юзер закрепил решение
            </Badge>
          )}
          {t.reopenedCount > 0 && (
            <Badge color="red" variant="filled">
              <Group gap={4} wrap="nowrap">
                <RefreshCcw size={12} />
                Переоткрыто {t.reopenedCount}
              </Group>
            </Badge>
          )}
        </Group>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Автор</SubTitleLabel>
          {t.userId && t.userEmail ? (
            // Ссылка, а не GhostButton: клик ведёт на профиль, а Anchor
            // берёт цвет из темы (var(--cloud-azure)) и остаётся читаемым
            // на тёмном фоне. Длинный email переносим по словам.
            <Anchor
              onClick={() => navigate(`/admin/users/${t.userId}`)}
              style={{ cursor: 'pointer', wordBreak: 'break-all' }}
            >
              {t.userEmail}
            </Anchor>
          ) : (
            <BodyLabel>Авто-тикет от системы</BodyLabel>
          )}
          <Field label="Создан" value={formatDateTime(t.createdAtUtc)} />
          {t.updatedAtUtc && (
            <Field label="Обновлён" value={formatDateTime(t.updatedAtUtc)} />
          )}
          {t.resolvedAtUtc && (
            <Field label="Решён" value={formatDateTime(t.resolvedAtUtc)} />
          )}
          {t.acceptedByUserAtUtc && (
            <Field
              label="Юзер закрепил решение"
              value={formatDateTime(t.acceptedByUserAtUtc)}
            />
          )}
        </Stack>
      </CloudCard>

      {deceasedRefId && (
        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>Связанная карточка умершего</SubTitleLabel>
            <BodyLabel>
              Юзер написал обращение с карточки умершего. Можно открыть
              её админ-просмотром в один клик.
            </BodyLabel>
            <Group>
              <GhostButton
                leftSection={<ExternalLink size={16} />}
                onClick={() => navigate(`/admin/deceased/${deceasedRefId}`)}
              >
                Открыть карточку
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Описание</SubTitleLabel>
          <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
            {t.description}
          </BodyLabel>
          {t.details && (
            <>
              <SubTitleLabel>Детали (auto)</SubTitleLabel>
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
                {t.details}
              </pre>
            </>
          )}
        </Stack>
      </CloudCard>

      {t.attachments && t.attachments.length > 0 && (
        <CloudCard>
          <AttachmentsSection
            ticketId={t.id}
            attachments={t.attachments}
            renderExtraActions={(att) => (
              <AdminAttachmentActions
                ticketId={t.id}
                attachment={att}
                deceasedRefId={deceasedRefId}
              />
            )}
          />
        </CloudCard>
      )}

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Переписка</SubTitleLabel>
          <MessagesChat messages={messages} viewerIsAdmin />

          {/* D44. Ответ без смены статуса — доступен на любой стадии,
              включая закрытое обращение (можно вернуться с реквизитами). */}
          <Stack gap="xs">
            <Textarea
              label="Ответ пользователю"
              placeholder="Напишите сообщение…"
              autosize
              minRows={2}
              maxRows={8}
              value={messageText}
              onChange={(e) => setMessageText(e.currentTarget.value)}
              disabled={messageMutation.isPending}
            />
            <Group justify="flex-end">
              <PrimaryButton
                leftSection={<Send size={16} />}
                onClick={() => messageMutation.mutate()}
                loading={messageMutation.isPending}
                disabled={!messageText.trim()}
              >
                Отправить
              </PrimaryButton>
            </Group>
            {messageMutation.isError && (
              <Alert color="red" variant="light">
                {formatError(messageMutation.error)}
              </Alert>
            )}
          </Stack>
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Действия</SubTitleLabel>
          <Group>
            <Button
              variant="light"
              onClick={() => {
                setNewStatus(t.status === 'Open' ? 'InProgress' : 'Open');
                setResolutionNote('');
                setStatusModal(true);
              }}
              disabled={t.status === 'Resolved'}
            >
              Сменить статус
            </Button>
            <Button
              variant="light"
              color="red"
              onClick={() => {
                setNewSeverity(t.severity);
                setSeverityModal(true);
              }}
              disabled={t.status === 'Resolved'}
            >
              Сменить критичность
            </Button>
            {/* D40. Отдельная кнопка, а не пункт в «сменить статус»:
                закрытие терминально и требует причины. */}
            <Button
              variant="filled"
              color="gray"
              onClick={() => {
                setCloseNote('');
                setForceCloseModal(true);
              }}
              disabled={t.status === 'Closed'}
            >
              Закрыть принудительно
            </Button>
          </Group>
          {t.status === 'Resolved' && (
            <CaptionLabel>
              Обращение решено. Статус и критичность больше не меняются;
              юзер может закрепить решение или переоткрыть спор. Если он
              просто забыл — закройте обращение принудительно.
            </CaptionLabel>
          )}
          {t.status === 'Closed' && (
            <CaptionLabel>
              Обращение закрыто принудительно — пользователь переоткрыть его
              не может. Если закрыли по ошибке, верните его в работу кнопкой
              «Сменить статус».
            </CaptionLabel>
          )}
        </Stack>
      </CloudCard>

      <Modal
        opened={statusModal}
        onClose={() => !statusMutation.isPending && setStatusModal(false)}
        title="Сменить статус"
        centered
        size="md"
      >
        <Stack gap="md">
          <Select
            label="Новый статус"
            data={STATUS_OPTIONS}
            value={newStatus}
            onChange={(v) => setNewStatus((v as TicketStatus) ?? 'Open')}
            allowDeselect={false}
          />
          {newStatus === 'Resolved' && (
            <Textarea
              label="Резолюция"
              placeholder="Опишите, что было сделано / решение"
              value={resolutionNote}
              onChange={(e) => setResolutionNote(e.currentTarget.value)}
              autosize
              minRows={3}
              maxRows={8}
              maxLength={4000}
              required
            />
          )}
          {statusMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(statusMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setStatusModal(false)}
              disabled={statusMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="azure"
              onClick={() => statusMutation.mutate()}
              loading={statusMutation.isPending}
              disabled={
                newStatus === 'Resolved' && !resolutionNote.trim()
              }
            >
              Сохранить
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* D40. Принудительное закрытие. Причина обязательна: юзер увидит её
          в переписке — иначе обращение просто молча исчезнет из его списка. */}
      <Modal
        opened={forceCloseModal}
        onClose={() => !forceCloseMutation.isPending && setForceCloseModal(false)}
        title="Закрыть принудительно"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Обращение будет закрыто без подтверждения пользователя. Это
            конечное состояние: переоткрыть его он уже не сможет.
          </BodyLabel>
          <Textarea
            label="Причина закрытия"
            placeholder="Например: пользователь не ответил, вопрос решён по телефону"
            value={closeNote}
            onChange={(e) => setCloseNote(e.currentTarget.value)}
            autosize
            minRows={3}
            maxRows={8}
            maxLength={4000}
            required
          />
          <CaptionLabel>
            Причина уйдёт пользователю в переписку сообщением от админа.
          </CaptionLabel>
          {forceCloseMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(forceCloseMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setForceCloseModal(false)}
              disabled={forceCloseMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="gray"
              onClick={() => forceCloseMutation.mutate()}
              loading={forceCloseMutation.isPending}
              disabled={!closeNote.trim()}
            >
              Закрыть принудительно
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={severityModal}
        onClose={() => !severityMutation.isPending && setSeverityModal(false)}
        title="Сменить критичность"
        centered
        size="md"
      >
        <Stack gap="md">
          <Select
            label="Новая критичность"
            data={SEVERITY_OPTIONS}
            value={newSeverity}
            onChange={(v) => setNewSeverity((v as TicketSeverity) ?? 'Normal')}
            allowDeselect={false}
          />
          {severityMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(severityMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setSeverityModal(false)}
              disabled={severityMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              onClick={() => severityMutation.mutate()}
              loading={severityMutation.isPending}
            >
              Сохранить
            </Button>
          </Group>
        </Stack>
      </Modal>
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
