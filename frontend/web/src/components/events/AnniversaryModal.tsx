import { useMemo, useState } from 'react';
import { Button, Group, Modal, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import { usersApi } from '../../api/endpoints/authApi';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { useSubscription } from '../../hooks/useSubscription';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { anniversaryYearsToday, yearsWord } from '../../utils/anniversary';
import { BodyLabel, CaptionLabel, SubTitleLabel } from '../ui';
import { cloudColors } from '../../design/theme';

/**
 * D38. Модалка «сегодня памятная дата» — всплывает поверх всего один раз
 * в день после входа, если у кого-то из отслеживаемых сегодня день памяти
 * (годовщина рождения) или година (годовщина смерти). Две кнопки:
 * «Закрыть» и «Перейти в события».
 *
 * Живёт на уровне AppLayout (как OutdatedLegalModal), поэтому работает
 * на всех приватных страницах.
 *
 * Два важных гарда:
 *  1. Список отслеживаемых закрыт paywall'ом (403 subscription.required →
 *     axios-интерсептор редиректит на /subscription-required). Поэтому
 *     запрос включаем ТОЛЬКО когда гейт пройден — иначе юзера без
 *     подписки выкинуло бы на paywall прямо из профиля.
 *  2. Пока висит блокирующая legal-модалка (F24), свою не показываем —
 *     иначе две модалки друг на друге.
 */

/** Ключ «уже показывали сегодня»: один показ в сутки на пользователя. */
const DISMISS_KEY = 'gdeoni:anniversary-modal-dismissed';

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
    // Приватный режим / заблокированный storage — просто показываем модалку.
    return null;
  }
}

function writeDismissed(stamp: string): void {
  try {
    window.localStorage.setItem(DISMISS_KEY, stamp);
  } catch {
    // Не смогли запомнить — не страшно, покажем ещё раз при перезагрузке.
  }
}

type TodayEvent = {
  deceasedId: string;
  fullName: string;
  kind: 'birth' | 'death';
  years: number;
  /** F42. За сколько дней до годовщины (0 = сегодня, 1, 3, 7). */
  daysUntil: number;
};

/** Дата + N дней (локально, без таймзонного сдвига). */
function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** Русский префикс «когда» для напоминания за N дней. */
function whenLabel(daysUntil: number): string {
  switch (daysUntil) {
    case 0:
      return 'Сегодня';
    case 1:
      return 'Завтра';
    case 7:
      return 'Через неделю';
    default:
      return `Через ${daysUntil} дня`;
  }
}

export function AnniversaryModal() {
  const navigate = useNavigate();
  const features = useAppFeatures();
  const subscription = useSubscription();
  const isAdmin = useIsAdmin();
  const userId = useAuthStore((s) => s.user?.id ?? null);

  const today = useMemo(() => new Date(), []);
  const todayIso = isoToday(today);
  const dismissStamp = `${userId ?? 'anon'}:${todayIso}`;

  const [dismissed, setDismissed] = useState(
    () => readDismissed() === dismissStamp,
  );

  // Гард 1: paywall. Зеркало RequireSubscription — пока флаги/подписка
  // грузятся, запрос не пускаем.
  const subscriptionEnabled = features.data?.subscriptionEnabled ?? false;
  const gatePassed =
    !features.isLoading &&
    !subscription.isLoading &&
    (!subscriptionEnabled || isAdmin || (subscription.data?.isActiveNow ?? false));

  // Гард 2: блокирующая legal-модалка.
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });
  const legalModalOpen = meQuery.data?.hasOutdatedLegalAcceptance === true;

  const enabled = gatePassed && !legalModalOpen && !dismissed && userId !== null;

  const trackedQuery = useQuery({
    queryKey: ['anniversary-modal-tracked'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
    enabled,
    staleTime: 5 * 60 * 1000,
  });

  const events = useMemo<TodayEvent[]>(() => {
    const items = trackedQuery.data?.items ?? [];
    const result: TodayEvent[] = [];

    // Для годовщины и набора «за сколько дней» ищем упреждение, для которого
    // сегодня + N = день годовщины. Совпадёт максимум одно (даты различны).
    const findDue = (eventIso: string, leadDays: number[]) => {
      for (const lead of leadDays) {
        const years = anniversaryYearsToday(eventIso, addDays(today, lead));
        if (years !== null) return { years, daysUntil: lead };
      }
      return null;
    };

    for (const item of items) {
      if (item.status === 'Archived') continue;

      const death = findDue(item.deathDate, item.deathAnniversaryLeadDays ?? []);
      if (death) {
        result.push({
          deceasedId: item.deceasedId,
          fullName: item.fullName,
          kind: 'death',
          years: death.years,
          daysUntil: death.daysUntil,
        });
      }

      if (item.birthDate) {
        const birth = findDue(item.birthDate, item.birthAnniversaryLeadDays ?? []);
        if (birth) {
          result.push({
            deceasedId: item.deceasedId,
            fullName: item.fullName,
            kind: 'birth',
            years: birth.years,
            daysUntil: birth.daysUntil,
          });
        }
      }
    }

    // Сначала сегодняшние, затем ближайшие по возрастанию упреждения.
    return result.sort((a, b) => a.daysUntil - b.daysUntil);
  }, [trackedQuery.data, today]);

  const opened = enabled && events.length > 0;

  function handleClose() {
    writeDismissed(dismissStamp);
    setDismissed(true);
  }

  function handleGoToEvents() {
    handleClose();
    navigate('/events');
  }

  const hasToday = events.some((e) => e.daysUntil === 0);
  const title = hasToday ? 'Сегодня памятная дата' : 'Скоро памятная дата';

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={title}
      centered
      size="md"
    >
      <Stack gap="md">
        <BodyLabel>
          {hasToday
            ? 'Сегодня памятная дата у кого-то из ваших близких. Хороший повод вспомнить о нём.'
            : 'Приближается памятная дата у кого-то из ваших близких.'}
        </BodyLabel>

        <Stack gap="xs">
          {events.map((e) => (
            <Stack
              key={`${e.deceasedId}-${e.kind}`}
              gap={2}
              style={{
                padding: '10px 12px',
                borderRadius: 12,
                background: cloudColors.sky,
              }}
            >
              <SubTitleLabel>
                {e.kind === 'birth' ? '🌷 ' : '🕯 '}
                {e.fullName}
              </SubTitleLabel>
              <CaptionLabel>
                {`${whenLabel(e.daysUntil)} · `}
                {e.kind === 'birth'
                  ? `День памяти · исполнилось бы ${e.years} ${yearsWord(e.years)}`
                  : `Година · ${e.years} ${yearsWord(e.years)}`}
              </CaptionLabel>
            </Stack>
          ))}
        </Stack>

        <Group grow>
          <Button variant="subtle" color="gray" radius={24} onClick={handleClose}>
            Закрыть
          </Button>
          <Button radius={24} fw={700} onClick={handleGoToEvents}>
            Перейти в события
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
