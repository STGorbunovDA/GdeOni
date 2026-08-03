import { useEffect, useState } from 'react';
import {
  Button,
  Checkbox,
  Group,
  Modal,
  Stack,
  TextInput,
} from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { Trash2 } from 'lucide-react';
import {
  customEventsApi,
  type CustomEvent,
} from '../../api/endpoints/eventsApi';
import { LEAD_OPTIONS } from '../../utils/holidayReminders';
import { formatError } from '../../auth/errorMessages';
import { CaptionLabel } from '../ui';

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

/**
 * Создание/правка ручного события (например, «ДР друга»). Дата — нативный
 * date-input (значение уже ISO yyyy-MM-dd). Напоминания — те же галки «за
 * сколько дней», что у праздников. event=null → режим создания.
 */
export function CustomEventModal({
  opened,
  onClose,
  event,
}: {
  opened: boolean;
  onClose: () => void;
  event: CustomEvent | null;
}) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState('');
  const [dateIso, setDateIso] = useState('');
  const [leadDays, setLeadDays] = useState<number[]>([0]);

  useEffect(() => {
    if (!opened) return;
    setTitle(event?.title ?? '');
    setDateIso(event?.date ?? todayIso());
    setLeadDays(event?.leadDays ?? [0]);
  }, [opened, event]);

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['events-custom'] });
    queryClient.invalidateQueries({ queryKey: ['events-popup-custom'] });
  }

  const saveMutation = useMutation({
    mutationFn: async () => {
      const sorted = [...leadDays].sort((a, b) => a - b);
      if (event) {
        await customEventsApi.update(event.id, title.trim(), dateIso, sorted);
      } else {
        await customEventsApi.create(title.trim(), dateIso, sorted);
      }
    },
    onSuccess: () => {
      invalidate();
      onClose();
      notifications.show({ title: 'Сохранено', message: '', color: 'green' });
    },
    onError: (e) =>
      notifications.show({
        title: 'Не удалось сохранить',
        message: formatError(e),
        color: 'red',
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => customEventsApi.remove(event!.id),
    onSuccess: () => {
      invalidate();
      onClose();
      notifications.show({ title: 'Событие удалено', message: '', color: 'green' });
    },
    onError: (e) =>
      notifications.show({
        title: 'Не удалось удалить',
        message: formatError(e),
        color: 'red',
      }),
  });

  const off = leadDays.length === 0;
  const busy = saveMutation.isPending || deleteMutation.isPending;

  function toggle(d: number, checked: boolean) {
    setLeadDays(checked ? [...leadDays, d] : leadDays.filter((x) => x !== d));
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      centered
      title={event ? 'Изменить событие' : 'Новое событие'}
    >
      <Stack gap="md">
        <TextInput
          label="Название"
          placeholder="Например, ДР друга"
          value={title}
          maxLength={200}
          onChange={(e) => setTitle(e.currentTarget.value)}
        />
        <TextInput
          type="date"
          label="Дата (повторяется каждый год)"
          value={dateIso}
          onChange={(e) => setDateIso(e.currentTarget.value)}
        />

        <Stack gap="xs">
          <CaptionLabel>Напоминать за сколько дней</CaptionLabel>
          <Group gap="md">
            {LEAD_OPTIONS.map((o) => (
              <Checkbox
                key={o.days}
                label={o.label}
                color="green"
                checked={leadDays.includes(o.days)}
                onChange={(e) => toggle(o.days, e.currentTarget.checked)}
              />
            ))}
          </Group>
          <Checkbox
            label="Отключено"
            color="blue"
            checked={off}
            onChange={(e) => setLeadDays(e.currentTarget.checked ? [] : [0])}
          />
        </Stack>

        <Group justify="space-between">
          {event ? (
            <Button
              variant="outline"
              color="red"
              leftSection={<Trash2 size={16} />}
              onClick={() => deleteMutation.mutate()}
              loading={deleteMutation.isPending}
              disabled={saveMutation.isPending}
            >
              Удалить
            </Button>
          ) : (
            <span />
          )}
          <Group gap="sm">
            <Button variant="default" onClick={onClose} disabled={busy}>
              Отмена
            </Button>
            <Button
              color="azure"
              onClick={() => saveMutation.mutate()}
              loading={saveMutation.isPending}
              disabled={title.trim().length === 0 || !dateIso}
            >
              Сохранить
            </Button>
          </Group>
        </Group>
      </Stack>
    </Modal>
  );
}
