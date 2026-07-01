import { useState } from 'react';
import {
  Alert,
  Group,
  Select,
  Stack,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { ChevronLeft } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { supportApi, type TicketKind } from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { KIND_OPTIONS } from './supportLabels';

/**
 * F17.14. Форма создания обращения. Title ≤200, Description ≤4000
 * (лимиты бэка). После успеха уходим в /support/mine со snack-bar'ом.
 */
export function SupportNewPage() {
  const navigate = useNavigate();
  const [kind, setKind] = useState<TicketKind>('Question');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  const mutation = useMutation({
    mutationFn: () =>
      supportApi.create({ kind, title: title.trim(), description: description.trim() }),
    onSuccess: () => {
      notifications.show({
        color: 'green',
        title: 'Обращение отправлено',
        message: 'Мы уже увидели ваше сообщение и вернёмся с ответом.',
      });
      navigate('/support/mine', { replace: true });
    },
  });

  const canSubmit =
    title.trim().length > 0 &&
    title.trim().length <= 200 &&
    description.trim().length > 0 &&
    description.trim().length <= 4000 &&
    !mutation.isPending;

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/profile')}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>Обращение в поддержку</TitleLabel>
        <CaptionLabel>
          Опишите проблему как можно подробнее — что вы делали, что
          получили. Приложения принимаем письмом в ответе админа
          (в первой версии сайта их отправка отдельным потоком).
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Select
            label="Тип"
            data={KIND_OPTIONS}
            value={kind}
            onChange={(v) => setKind((v as TicketKind) ?? 'Question')}
            allowDeselect={false}
          />
          <TextInput
            label="Заголовок"
            placeholder="Кратко о проблеме"
            value={title}
            onChange={(e) => setTitle(e.currentTarget.value)}
            maxLength={200}
            error={
              title.length > 200
                ? `Максимум 200 символов, сейчас ${title.length}`
                : undefined
            }
          />
          <Textarea
            label="Описание"
            placeholder="Что произошло, что вы делали, что ожидали"
            value={description}
            onChange={(e) => setDescription(e.currentTarget.value)}
            autosize
            minRows={5}
            maxRows={15}
            maxLength={4000}
            error={
              description.length > 4000
                ? `Максимум 4000 символов, сейчас ${description.length}`
                : undefined
            }
          />
          <Group justify="space-between" align="center">
            <CaptionLabel>
              {description.length} / 4000
            </CaptionLabel>
            <PrimaryButton
              onClick={() => mutation.mutate()}
              disabled={!canSubmit}
              loading={mutation.isPending}
            >
              Отправить
            </PrimaryButton>
          </Group>
          {mutation.isError && (
            <Alert color="red" variant="light">
              {formatError(mutation.error)}
            </Alert>
          )}
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="xs">
          <CaptionLabel>Уже писали?</CaptionLabel>
          <BodyLabel>
            <a
              href="/support/mine"
              onClick={(e) => {
                e.preventDefault();
                navigate('/support/mine');
              }}
            >
              Открыть мои обращения
            </a>
          </BodyLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}
