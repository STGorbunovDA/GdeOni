import { useMemo, useState } from 'react';
import { Button, Modal, Stack, Text } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import {
  eventsApi,
  holidayRemindersApi,
} from '../../api/endpoints/eventsApi';
import { usersApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';
import {
  buildOverridesMap,
  computeTodayPopupItems,
  shiftIso,
} from '../../utils/holidayReminders';
import { BodyLabel, CaptionLabel } from '../ui';
import { cloudColors } from '../../design/theme';

/**
 * F42. Попап «сегодня/скоро праздник» — всплывает поверх всего один раз в
 * день после входа, если сегодня крупный праздник (дефолт «в день») или день
 * напоминания по празднику, который юзер включил («за неделю/3/1»). Кнопка
 * «ОК». Живёт на уровне AppLayout (как AnniversaryModal), поэтому работает на
 * всех приватных страницах и переживает переходы через Outlet.
 *
 * Праздники не за paywall'ом (эндпоинт только [Authorize]), поэтому paywall-
 * гарда нет. Но пока висит блокирующая legal-модалка — не показываемся.
 */

const DISMISS_KEY = 'gdeoni:holiday-popup-dismissed';

function isoToday(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function readDismissed(): string | null {
  try {
    return window.localStorage.getItem(DISMISS_KEY);
  } catch {
    return null;
  }
}

function writeDismissed(stamp: string): void {
  try {
    window.localStorage.setItem(DISMISS_KEY, stamp);
  } catch {
    // Приватный режим — не запомнили, покажем ещё раз при перезагрузке.
  }
}

function leadLabel(days: number): string {
  switch (days) {
    case 0:
      return 'Сегодня';
    case 1:
      return 'Завтра';
    case 3:
      return 'Через 3 дня';
    case 7:
      return 'Через неделю';
    default:
      return `Через ${days} дн.`;
  }
}

export function HolidayReminderPopup() {
  const userId = useAuthStore((s) => s.user?.id ?? null);
  const today = useMemo(() => new Date(), []);
  const todayIso = isoToday(today);
  const dismissStamp = `${userId ?? 'anon'}:${todayIso}`;

  const [dismissed, setDismissed] = useState(
    () => readDismissed() === dismissStamp,
  );

  // Гард: блокирующая legal-модалка — не показываемся поверх неё.
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });
  const legalModalOpen = meQuery.data?.hasOutdatedLegalAcceptance === true;

  const enabled = !legalModalOpen && !dismissed && userId !== null;

  // На 7 дней вперёд хватает: максимальное упреждение — «за неделю».
  const holidaysQuery = useQuery({
    queryKey: ['holiday-popup-holidays', todayIso],
    queryFn: () => eventsApi.getHolidays(todayIso, shiftIso(todayIso, 7)),
    enabled,
    staleTime: 5 * 60 * 1000,
  });
  const remindersQuery = useQuery({
    queryKey: ['events-holiday-reminders'],
    queryFn: () => holidayRemindersApi.getMine(),
    enabled,
    staleTime: 5 * 60 * 1000,
  });

  const items = useMemo(() => {
    if (!holidaysQuery.data || !remindersQuery.data) return [];
    return computeTodayPopupItems(
      holidaysQuery.data,
      buildOverridesMap(remindersQuery.data),
      todayIso,
    );
  }, [holidaysQuery.data, remindersQuery.data, todayIso]);

  const opened = enabled && items.length > 0;

  function handleClose() {
    writeDismissed(dismissStamp);
    setDismissed(true);
  }

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title="Праздники"
      centered
      size="md"
    >
      <Stack gap="md">
        <BodyLabel>Ближайшие важные даты:</BodyLabel>

        <Stack gap="xs">
          {items.map((it, i) => (
            <Stack
              key={`${it.holiday.name}-${it.leadDays}-${i}`}
              gap={2}
              style={{
                padding: '10px 12px',
                borderRadius: 12,
                background: cloudColors.sky,
              }}
            >
              <Text fw={700} c={cloudColors.inkBlue}>
                {it.holiday.name}
              </Text>
              <CaptionLabel>{leadLabel(it.leadDays)}</CaptionLabel>
            </Stack>
          ))}
        </Stack>

        <Button radius={24} fw={700} fullWidth onClick={handleClose}>
          ОК
        </Button>
      </Stack>
    </Modal>
  );
}
