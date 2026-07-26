import { useState } from 'react';
import {
  Badge,
  Checkbox,
  Divider,
  Group,
  Loader,
  Modal,
  Stack,
  Text,
} from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  holidayRemindersApi,
  type Holiday,
} from '../../api/endpoints/eventsApi';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import { LEAD_OPTIONS } from '../../utils/holidayReminders';
import { formatDateOnly } from '../../utils/formatDate';
import { SubTitleLabel } from '../ui';

/**
 * Годовщина умершего для окна напоминаний: kind — година (death) или день
 * памяти (birth), leadDays — текущий набор «за сколько дней» напоминать.
 */
export type DeceasedAnniversaryRow = {
  deceasedId: string;
  fullName: string;
  kind: 'birth' | 'death';
  leadDays: number[];
};

/**
 * F42. Окно напоминаний выбранного дня. И по праздникам, и по памятным датам
 * близких — единый набор галок «за сколько дней» (в день / за день / за 3 дня /
 * за неделю) + явная «Отключено». Изменения сохраняются сразу на сервер,
 * кеши инвалидируются.
 */
export function HolidayReminderModal({
  opened,
  onClose,
  dateIso,
  holidays,
  effectiveByName,
  deceased,
}: {
  opened: boolean;
  onClose: () => void;
  dateIso: string | null;
  holidays: Holiday[];
  effectiveByName: Map<string, number[]>;
  deceased: DeceasedAnniversaryRow[];
}) {
  const hasAnything = holidays.length > 0 || deceased.length > 0;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      centered
      title={dateIso ? formatDateOnly(dateIso) : ''}
    >
      <Stack gap="lg">
        {!hasAnything && (
          <Text size="sm" c="dimmed">
            В этот день событий нет.
          </Text>
        )}

        {deceased.length > 0 && (
          <Stack gap="md">
            <Text size="sm" fw={600} c="dimmed">
              Памятные даты близких
            </Text>
            {deceased.map((d) => (
              <DeceasedRow key={`${d.deceasedId}-${d.kind}`} row={d} />
            ))}
          </Stack>
        )}

        {holidays.length > 0 && deceased.length > 0 && <Divider />}

        {holidays.length > 0 && (
          <Stack gap="lg">
            {deceased.length > 0 && (
              <Text size="sm" fw={600} c="dimmed">
                Праздники
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
        )}
      </Stack>
    </Modal>
  );
}

/**
 * Набор галок «за сколько дней» + «Отключено». Локальное состояние для
 * мгновенного отклика; onSave сохраняет на сервер. Синий = выключено,
 * зелёный = включено.
 */
function LeadDaysEditor({
  initial,
  onSave,
}: {
  initial: number[];
  onSave: (next: number[]) => void;
}) {
  const [days, setDays] = useState<number[]>(initial);
  const off = days.length === 0;

  function apply(next: number[]) {
    const normalized = [...next].sort((a, b) => a - b);
    setDays(normalized);
    onSave(normalized);
  }

  function toggle(d: number, checked: boolean) {
    apply(checked ? [...days, d] : days.filter((x) => x !== d));
  }

  return (
    <>
      <Group gap="md">
        {LEAD_OPTIONS.map((o) => (
          <Checkbox
            key={o.days}
            label={o.label}
            color="green"
            checked={days.includes(o.days)}
            onChange={(e) => toggle(o.days, e.currentTarget.checked)}
          />
        ))}
      </Group>

      <Group justify="space-between" align="center">
        <Checkbox
          label="Отключено"
          color="blue"
          checked={off}
          // Ставим «Отключено» → снимаем все галки; снимаем «Отключено» →
          // включаем дефолт «в день».
          onChange={(e) => apply(e.currentTarget.checked ? [] : [0])}
        />
        {off ? (
          <Badge color="blue" variant="light">
            Выключено
          </Badge>
        ) : (
          <Badge color="green" variant="light">
            Включено
          </Badge>
        )}
      </Group>
    </>
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

  const mutation = useMutation({
    mutationFn: (next: number[]) => holidayRemindersApi.set(holiday.name, next),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events-holiday-reminders'] });
    },
  });

  return (
    <Stack gap="xs">
      <Group gap="xs" align="center" wrap="nowrap">
        <SubTitleLabel>{holiday.name}</SubTitleLabel>
        {mutation.isPending && <Loader size="xs" color="azure" />}
      </Group>
      <LeadDaysEditor initial={initial} onSave={(next) => mutation.mutate(next)} />
    </Stack>
  );
}

function DeceasedRow({ row }: { row: DeceasedAnniversaryRow }) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (next: number[]) => {
      // Тянем текущий трекинг целиком — PATCH требует все поля сразу, иначе
      // personalNotes/второй набор дней затрутся дефолтом.
      const details = await trackedDeceasedApi.getDetails(row.deceasedId);
      const t = details.tracking;
      await trackedDeceasedApi.update(row.deceasedId, {
        relationshipType: t.relationshipType,
        personalNotes: t.personalNotes,
        notifyOnDeathAnniversary: t.notifyOnDeathAnniversary,
        notifyOnBirthAnniversary: t.notifyOnBirthAnniversary,
        deathAnniversaryLeadDays:
          row.kind === 'death' ? next : t.deathAnniversaryLeadDays,
        birthAnniversaryLeadDays:
          row.kind === 'birth' ? next : t.birthAnniversaryLeadDays,
        trackStatus: t.status,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events-tracked'] });
      queryClient.invalidateQueries({ queryKey: ['anniversary-modal-tracked'] });
    },
  });

  const label = row.kind === 'death' ? 'Година' : 'День памяти';

  return (
    <Stack gap="xs">
      <Group gap="xs" align="center" wrap="nowrap">
        <SubTitleLabel>{row.fullName}</SubTitleLabel>
        {mutation.isPending && <Loader size="xs" color="azure" />}
      </Group>
      <Text size="sm" c="dimmed">
        {label}
      </Text>
      <LeadDaysEditor
        initial={row.leadDays}
        onSave={(next) => mutation.mutate(next)}
      />
    </Stack>
  );
}
