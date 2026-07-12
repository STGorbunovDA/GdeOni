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
 * D38. Модалка «сегодня годовщина» — всплывает поверх всего один раз в
 * день после входа, если у кого-то из отслеживаемых сегодня день
 * рождения или годовщина смерти. Две кнопки: «Закрыть» и «Перейти в
 * события».
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
};

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

    for (const item of items) {
      if (item.status === 'Archived') continue;

      const deathYears = anniversaryYearsToday(item.deathDate, today);
      if (deathYears !== null) {
        result.push({
          deceasedId: item.deceasedId,
          fullName: item.fullName,
          kind: 'death',
          years: deathYears,
        });
      }

      if (item.birthDate) {
        const birthYears = anniversaryYearsToday(item.birthDate, today);
        if (birthYears !== null) {
          result.push({
            deceasedId: item.deceasedId,
            fullName: item.fullName,
            kind: 'birth',
            years: birthYears,
          });
        }
      }
    }

    return result;
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

  const hasBirthday = events.some((e) => e.kind === 'birth');
  const title = hasBirthday ? 'Сегодня день рождения' : 'Сегодня годовщина';

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
          {hasBirthday
            ? 'У кого-то из ваших близких сегодня день рождения. Хороший повод вспомнить о нём.'
            : 'Сегодня годовщина у кого-то из ваших близких. Хороший повод вспомнить о нём.'}
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
                {e.kind === 'birth' ? '🎂 ' : '🕯 '}
                {e.fullName}
              </SubTitleLabel>
              <CaptionLabel>
                {e.kind === 'birth'
                  ? `День рождения · исполнилось бы ${e.years} ${yearsWord(e.years)}`
                  : `Годовщина смерти · ${e.years} ${yearsWord(e.years)}`}
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
