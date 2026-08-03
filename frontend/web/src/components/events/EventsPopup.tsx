import { useMemo, useState } from 'react';
import { Button, Modal, Stack, Text } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import { eventsApi, holidayRemindersApi } from '../../api/endpoints/eventsApi';
import { usersApi } from '../../api/endpoints/authApi';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { useSubscription } from '../../hooks/useSubscription';
import { useRelativesSummary } from '../../hooks/useRelativesSummary';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { anniversaryYearsToday, yearsWord } from '../../utils/anniversary';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import {
  buildOverridesMap,
  computeTodayPopupItems,
  shiftIso,
} from '../../utils/holidayReminders';
import { BodyLabel, CaptionLabel } from '../ui';
import { cloudColors } from '../../design/theme';

/**
 * Единый попап «События сегодня» — всплывает поверх всего ПРИ КАЖДОМ заходе
 * на сайт (перезагрузке), пока есть актуальные события. Показывает сразу и
 * памятные даты близких (година / день памяти), и праздники (сегодня/скоро
 * по включённым напоминаниям). Кнопка «ОК» закрывает — до следующего захода.
 *
 * Заменяет два прежних попапа (AnniversaryModal D38 + HolidayReminderPopup
 * F42): чтобы события не всплывали двумя окнами друг на друге.
 *
 * Живёт в AppLayout: показ «раз за заход» = состояние компонента (без
 * localStorage). AppLayout переживает переходы между роутами, поэтому после
 * «ОК» попап не всплывает при навигации, но появляется снова при полной
 * перезагрузке / повторном открытии сайта.
 *
 * Гарды:
 *  1. Пока висит блокирующая legal-модалка — не показываемся.
 *  2. Годовщины берутся из tracked-списка (за paywall'ом) — этот запрос
 *     включаем только когда гейт подписки пройден, иначе юзера без подписки
 *     выкинуло бы на paywall. Праздники не за paywall'ом — грузятся всегда.
 */

function isoToday(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
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

type AnniversaryEvent = {
  id: string;
  fullName: string;
  kind: 'birth' | 'death';
  years: number;
};

export function EventsPopup() {
  const features = useAppFeatures();
  const subscription = useSubscription();
  const relativesSummary = useRelativesSummary();
  const isAdmin = useIsAdmin();
  const navigate = useNavigate();
  const userId = useAuthStore((s) => s.user?.id ?? null);

  const today = useMemo(() => new Date(), []);
  const todayIso = isoToday(today);

  // Показ «раз за заход»: состояние компонента, без localStorage. На полной
  // перезагрузке AppLayout перемонтируется → dismissed=false → покажем снова.
  const [dismissed, setDismissed] = useState(false);

  // Гард 1: блокирующая legal-модалка.
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });
  const legalModalOpen = meQuery.data?.hasOutdatedLegalAcceptance === true;

  const enabled = !legalModalOpen && !dismissed && userId !== null;

  // Гард 2: paywall — только для годовщин (tracked). Праздники не за paywall.
  const subscriptionEnabled = features.data?.subscriptionEnabled ?? false;
  const gatePassed =
    !features.isLoading &&
    !subscription.isLoading &&
    (!subscriptionEnabled || isAdmin || (subscription.data?.isActiveNow ?? false));

  const trackedQuery = useQuery({
    queryKey: ['events-popup-tracked'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
    enabled: enabled && gatePassed,
    staleTime: 5 * 60 * 1000,
  });

  // На 7 дней вперёд — максимальное упреждение «за неделю».
  const holidaysQuery = useQuery({
    queryKey: ['events-popup-holidays', todayIso],
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

  const anniversaries = useMemo<AnniversaryEvent[]>(() => {
    const items = trackedQuery.data?.items ?? [];
    const result: AnniversaryEvent[] = [];
    for (const item of items) {
      if (item.status === 'Archived') continue;
      const deathYears = anniversaryYearsToday(item.deathDate, today);
      if (deathYears !== null) {
        result.push({
          id: `${item.deceasedId}-death`,
          fullName: item.fullName,
          kind: 'death',
          years: deathYears,
        });
      }
      if (item.birthDate) {
        const birthYears = anniversaryYearsToday(item.birthDate, today);
        if (birthYears !== null) {
          result.push({
            id: `${item.deceasedId}-birth`,
            fullName: item.fullName,
            kind: 'birth',
            years: birthYears,
          });
        }
      }
    }
    return result;
  }, [trackedQuery.data, today]);

  const holidayItems = useMemo(() => {
    if (!holidaysQuery.data || !remindersQuery.data) return [];
    return computeTodayPopupItems(
      holidaysQuery.data,
      buildOverridesMap(remindersQuery.data),
      todayIso,
    );
  }, [holidaysQuery.data, remindersQuery.data, todayIso]);

  // Функция «Родственники» (Фаза 4): новые родственники + непрочитанные.
  const newRelatives = relativesSummary.data?.newRelatives ?? [];
  const unreadConversations = relativesSummary.data?.unreadConversations ?? [];

  function goToRelatives() {
    setDismissed(true);
    navigate('/relatives');
  }

  const hasEvents =
    anniversaries.length > 0 ||
    holidayItems.length > 0 ||
    newRelatives.length > 0 ||
    unreadConversations.length > 0;
  const opened = enabled && hasEvents;

  return (
    <Modal
      opened={opened}
      onClose={() => setDismissed(true)}
      title="События сегодня"
      centered
      size="md"
    >
      <Stack gap="md">
        {anniversaries.length > 0 && (
          <Stack gap="xs">
            <BodyLabel>Памятные даты близких:</BodyLabel>
            {anniversaries.map((e) => (
              <Stack
                key={e.id}
                gap={2}
                style={{
                  padding: '10px 12px',
                  borderRadius: 12,
                  background: cloudColors.sky,
                }}
              >
                <Text fw={700} c={cloudColors.inkBlue}>
                  {e.kind === 'birth' ? '🌷 ' : '🕯 '}
                  {e.fullName}
                </Text>
                <CaptionLabel>
                  {e.kind === 'birth'
                    ? `День памяти · исполнилось бы ${e.years} ${yearsWord(e.years)}`
                    : `Година · ${e.years} ${yearsWord(e.years)}`}
                </CaptionLabel>
              </Stack>
            ))}
          </Stack>
        )}

        {holidayItems.length > 0 && (
          <Stack gap="xs">
            <BodyLabel>Праздники:</BodyLabel>
            {holidayItems.map((it, i) => (
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
        )}

        {newRelatives.length > 0 && (
          <Stack gap="xs">
            <BodyLabel>Новые родственники:</BodyLabel>
            {newRelatives.map((r) => (
              <Stack
                key={`${r.deceasedId}-${r.relativeUserId}`}
                gap={2}
                style={{
                  padding: '10px 12px',
                  borderRadius: 12,
                  background: cloudColors.sky,
                  cursor: 'pointer',
                }}
                onClick={goToRelatives}
              >
                <Text fw={700} c={cloudColors.inkBlue}>
                  👪 {r.relativeUserName}
                </Text>
                <CaptionLabel>
                  {relationshipDisplay(r.relationshipType)} · {r.deceasedFullName}
                </CaptionLabel>
              </Stack>
            ))}
          </Stack>
        )}

        {unreadConversations.length > 0 && (
          <Stack gap="xs">
            <BodyLabel>Новые сообщения от родственников:</BodyLabel>
            {unreadConversations.map((c) => (
              <Stack
                key={c.conversationId}
                gap={2}
                style={{
                  padding: '10px 12px',
                  borderRadius: 12,
                  background: cloudColors.sky,
                  cursor: 'pointer',
                }}
                onClick={() => {
                  setDismissed(true);
                  navigate(`/relatives/chat/${c.conversationId}`);
                }}
              >
                <Text fw={700} c={cloudColors.inkBlue}>
                  ✉️ {c.otherUserName}
                </Text>
                <CaptionLabel>
                  {c.unreadCount === 1
                    ? 'Новое сообщение'
                    : `Новых сообщений: ${c.unreadCount}`}{' '}
                  · {c.deceasedFullName}
                </CaptionLabel>
              </Stack>
            ))}
          </Stack>
        )}

        <Button radius={24} fw={700} fullWidth onClick={() => setDismissed(true)}>
          ОК
        </Button>
      </Stack>
    </Modal>
  );
}
