import { useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Modal,
  Stack,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { CheckCircle2, ChevronLeft, RefreshCcw } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { supportApi } from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';
import { MessagesChat } from './MessagesChat';
import {
  KIND_LABELS,
  SEVERITY_COLORS,
  SEVERITY_LABELS,
  STATUS_COLORS,
  STATUS_LABELS,
} from './supportLabels';

/**
 * F17.14 / D25.1 / D25.2. Юзерская карточка обращения.
 *
 *  - Если Status=Resolved и !AcceptedByUser — две кнопки:
 *    «Закрепить решено» (Accept) и «Продолжить спор» (Reopen).
 *  - Если AcceptedByUser=true — «✓ Вы подтвердили решение» + дата.
 *  - Если ReopenedCount > 0 — строка «Обращение переоткрывалось N раз».
 *
 * Переписка отрисовывается chat-bubble компонентом (D25.2).
 */
export function SupportTicketPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();
  const [reopenModal, setReopenModal] = useState(false);
  const [reopenText, setReopenText] = useState('');

  const query = useQuery({
    queryKey: ['support-ticket', id],
    queryFn: () => supportApi.getById(id!),
    enabled: !!id,
  });

  const acceptMutation = useMutation({
    mutationFn: () => supportApi.accept(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['support-mine'] });
      notifications.show({
        color: 'green',
        title: 'Решение закреплено',
        message: 'Спасибо! Спор считаем закрытым.',
      });
    },
  });

  const reopenMutation = useMutation({
    mutationFn: () =>
      supportApi.reopen(id!, reopenText.trim() || null),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['support-ticket', id] });
      queryClient.invalidateQueries({ queryKey: ['support-mine'] });
      setReopenModal(false);
      notifications.show({
        color: 'azure',
        title: 'Обращение переоткрыто',
        message: 'Мы получим уведомление и вернёмся с ответом.',
      });
    },
  });

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate('/support/mine')} />
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
        <BackButton onClick={() => navigate('/support/mine')} />
        <Alert color="red" variant="light">
          {query.error ? formatError(query.error) : 'Обращение не найдено.'}
        </Alert>
      </Stack>
    );
  }

  const t = query.data;
  const messages = t.messages ?? [];
  const canAcceptOrReopen = t.status === 'Resolved' && !t.acceptedByUser;

  return (
    <Stack gap="lg">
      <Group>
        <BackButton onClick={() => navigate('/support/mine')} />
      </Group>

      <Stack gap="xs">
        <TitleLabel>{t.title}</TitleLabel>
        <Group gap="xs" wrap="wrap">
          <Badge color={STATUS_COLORS[t.status]} variant="light">
            {STATUS_LABELS[t.status]}
          </Badge>
          <Badge color={SEVERITY_COLORS[t.severity]} variant="light">
            {SEVERITY_LABELS[t.severity]}
          </Badge>
          <Badge color="gray" variant="light">
            {KIND_LABELS[t.kind]}
          </Badge>
        </Group>
        <CaptionLabel>Создано {formatDateTime(t.createdAtUtc)}</CaptionLabel>
      </Stack>

      {t.acceptedByUser && t.acceptedByUserAtUtc && (
        <Alert color="green" variant="light" icon={<CheckCircle2 size={16} />}>
          Вы подтвердили решение{' '}
          {formatDateTime(t.acceptedByUserAtUtc)}. Спор закрыт.
        </Alert>
      )}

      {t.reopenedCount > 0 && (
        <Alert color="yellow" variant="light" icon={<RefreshCcw size={16} />}>
          Обращение переоткрывалось {t.reopenedCount} раз.
        </Alert>
      )}

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Описание</SubTitleLabel>
          <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
            {t.description}
          </BodyLabel>
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Переписка</SubTitleLabel>
          <MessagesChat messages={messages} viewerIsAdmin={false} />
        </Stack>
      </CloudCard>

      {canAcceptOrReopen && (
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Решение готово</SubTitleLabel>
            <BodyLabel>
              Администратор пометил обращение как решённое. Если решение
              вам подходит — закрепите его. Если проблема осталась —
              продолжите спор с описанием, что не решено.
            </BodyLabel>
            <Group>
              <PrimaryButton
                leftSection={<CheckCircle2 size={16} />}
                onClick={() => acceptMutation.mutate()}
                loading={acceptMutation.isPending}
              >
                Закрепить решено
              </PrimaryButton>
              <GhostButton
                leftSection={<RefreshCcw size={16} />}
                onClick={() => {
                  setReopenText('');
                  setReopenModal(true);
                }}
              >
                Продолжить спор
              </GhostButton>
            </Group>
            {acceptMutation.isError && (
              <Alert color="red" variant="light">
                {formatError(acceptMutation.error)}
              </Alert>
            )}
          </Stack>
        </CloudCard>
      )}

      <Modal
        opened={reopenModal}
        onClose={() => !reopenMutation.isPending && setReopenModal(false)}
        title="Продолжить спор"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Опишите, что осталось нерешённым. После отправки обращение
            снова получит статус «Открыто» и уйдёт админам.
          </BodyLabel>
          <Textarea
            label="Ваше сообщение"
            placeholder="Например: файл всё ещё не приложен, вернитесь пожалуйста"
            value={reopenText}
            onChange={(e) => setReopenText(e.currentTarget.value)}
            autosize
            minRows={3}
            maxRows={8}
            maxLength={4000}
          />
          {reopenMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(reopenMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setReopenModal(false)}
              disabled={reopenMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="azure"
              onClick={() => reopenMutation.mutate()}
              loading={reopenMutation.isPending}
            >
              Переоткрыть
            </Button>
          </Group>
        </Stack>
      </Modal>
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
