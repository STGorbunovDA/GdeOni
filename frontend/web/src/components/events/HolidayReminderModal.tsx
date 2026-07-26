import { useState } from 'react';
import {
  Badge,
  Button,
  Checkbox,
  Group,
  Loader,
  Modal,
  Stack,
  Text,
} from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { BellOff } from 'lucide-react';
import {
  holidayRemindersApi,
  type Holiday,
} from '../../api/endpoints/eventsApi';
import { LEAD_OPTIONS } from '../../utils/holidayReminders';
import { formatDateOnly } from '../../utils/formatDate';
import { SubTitleLabel, CaptionLabel } from '../ui';

/**
 * F42. Окно редактирования напоминаний о праздниках выбранного дня. По каждому
 * празднику — галочки «за сколько дней» (множественный выбор) и «Отключить».
 * Изменение сохраняется сразу на сервер (per-user), список напоминаний
 * инвалидируется, чтобы попап/календарь подхватили новое.
 */
export function HolidayReminderModal({
  opened,
  onClose,
  dateIso,
  holidays,
  effectiveByName,
}: {
  opened: boolean;
  onClose: () => void;
  dateIso: string | null;
  holidays: Holiday[];
  effectiveByName: Map<string, number[]>;
}) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      centered
      title={dateIso ? formatDateOnly(dateIso) : ''}
    >
      <Stack gap="lg">
        {holidays.length === 0 && (
          <Text size="sm" c="dimmed">
            В этот день праздников нет.
          </Text>
        )}
        {holidays.map((h) => (
          <HolidayRow
            key={h.name}
            holiday={h}
            initial={effectiveByName.get(h.name) ?? []}
          />
        ))}
      </Stack>
    </Modal>
  );
}

function HolidayRow({
  holiday,
  initial,
}: {
  holiday: Holiday;
  initial: number[];
}) {
  const queryClient = useQueryClient();
  const [days, setDays] = useState<number[]>(initial);

  const mutation = useMutation({
    mutationFn: (next: number[]) =>
      holidayRemindersApi.set(holiday.name, next),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events-holiday-reminders'] });
    },
  });

  function apply(next: number[]) {
    const normalized = [...next].sort((a, b) => a - b);
    setDays(normalized);
    mutation.mutate(normalized);
  }

  function toggle(d: number, checked: boolean) {
    apply(checked ? [...days, d] : days.filter((x) => x !== d));
  }

  return (
    <Stack gap="xs">
      <Group gap="xs" align="center" wrap="nowrap">
        <SubTitleLabel>{holiday.name}</SubTitleLabel>
        {mutation.isPending && <Loader size="xs" color="azure" />}
      </Group>

      <Group gap="md">
        {LEAD_OPTIONS.map((o) => (
          <Checkbox
            key={o.days}
            label={o.label}
            checked={days.includes(o.days)}
            onChange={(e) => toggle(o.days, e.currentTarget.checked)}
          />
        ))}
      </Group>

      <Group justify="space-between" align="center">
        {days.length > 0 ? (
          <Badge color="azure" variant="light">
            Напоминание включено
          </Badge>
        ) : (
          <CaptionLabel>Напоминание выключено</CaptionLabel>
        )}
        <Button
          size="xs"
          variant="subtle"
          color="gray"
          leftSection={<BellOff size={14} />}
          disabled={days.length === 0}
          onClick={() => apply([])}
        >
          Отключить
        </Button>
      </Group>
    </Stack>
  );
}
