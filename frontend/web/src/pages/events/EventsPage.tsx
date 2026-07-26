import { useMemo, useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Stack,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import { Calendar } from '@mantine/dates';
import { useQuery } from '@tanstack/react-query';
import { ChevronRight, Cross, Flower2, UserRound } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import {
  eventsApi,
  holidayRemindersApi,
  type Holiday,
} from '../../api/endpoints/eventsApi';
import { formatError } from '../../auth/errorMessages';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import {
  buildOverridesMap,
  effectiveLeadDays,
  shiftIso,
} from '../../utils/holidayReminders';
import { anniversaryYearsToday, yearsWord } from '../../utils/anniversary';
import { useNavigate } from 'react-router-dom';
import { HolidayReminderModal } from '../../components/events/HolidayReminderModal';

/**
 * F42. Вкладка «События»: годовщины близких сегодня, праздники сегодня (если
 * есть), большой календарь праздников на месяц (подсветка дат, тултип по
 * наведению, клик → окно напоминаний) и список ближайших праздников под ним.
 * Напоминания хранятся за юзером на сервере; попап «сегодня/скоро праздник»
 * показывается глобально при заходе (HolidayReminderPopup в AppLayout).
 */

const UPCOMING_DAYS = 30;

type CategoryMeta = { label: string; color: string; order: number };

const CATEGORY_META: Record<string, CategoryMeta> = {
  Memorial: { label: 'Поминальные дни', color: 'grape', order: 0 },
  Orthodox: { label: 'Православные', color: 'indigo', order: 1 },
  Muslim: { label: 'Мусульманские', color: 'teal', order: 2 },
  State: { label: 'Государственные', color: 'red', order: 3 },
  Fast: { label: 'Посты', color: 'gray', order: 4 },
};

function categoryMeta(category: string): CategoryMeta {
  return CATEGORY_META[category] ?? { label: category, color: 'gray', order: 99 };
}

function isoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

type TodayAnniversary = {
  deceasedId: string;
  fullName: string;
  photoUrl: string | null;
  kind: 'birth' | 'death';
  years: number;
};

export function EventsPage() {
  const navigate = useNavigate();
  const features = useAppFeatures();

  const today = useMemo(() => new Date(), []);
  const todayIso = isoDate(today);

  // Один широкий запрос праздников: текущий месяц + ~год вперёд. Покрывает
  // и календарь (подсветка), и «сегодня», и «ближайшие».
  const rangeFromIso = useMemo(
    () => isoDate(new Date(today.getFullYear(), today.getMonth(), 1)),
    [today],
  );
  const rangeToIso = useMemo(() => shiftIso(rangeFromIso, 364), [rangeFromIso]);

  const trackedQuery = useQuery({
    queryKey: ['events-tracked'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
  });

  const holidaysQuery = useQuery({
    queryKey: ['events-holidays', rangeFromIso, rangeToIso],
    queryFn: () => eventsApi.getHolidays(rangeFromIso, rangeToIso),
  });

  const remindersQuery = useQuery({
    queryKey: ['events-holiday-reminders'],
    queryFn: () => holidayRemindersApi.getMine(),
  });

  const overrides = useMemo(
    () => buildOverridesMap(remindersQuery.data ?? []),
    [remindersQuery.data],
  );

  // Map «дата (ISO) → праздники дня» для календаря и окна напоминаний.
  const holidaysByDate = useMemo(() => {
    const map = new Map<string, Holiday[]>();
    for (const h of holidaysQuery.data ?? []) {
      const list = map.get(h.date) ?? [];
      list.push(h);
      map.set(h.date, list);
    }
    return map;
  }, [holidaysQuery.data]);

  const anniversaries = useMemo<TodayAnniversary[]>(() => {
    const items = trackedQuery.data?.items ?? [];
    const result: TodayAnniversary[] = [];
    for (const item of items) {
      if (item.status === 'Archived') continue;
      const photoUrl = buildMediaUrl(
        features.data?.mediaBaseUrl,
        item.mainPhotoBucket,
        item.mainPhotoStorageKey,
      );
      const deathYears = anniversaryYearsToday(item.deathDate, today);
      if (deathYears !== null) {
        result.push({ deceasedId: item.deceasedId, fullName: item.fullName, photoUrl, kind: 'death', years: deathYears });
      }
      if (item.birthDate) {
        const birthYears = anniversaryYearsToday(item.birthDate, today);
        if (birthYears !== null) {
          result.push({ deceasedId: item.deceasedId, fullName: item.fullName, photoUrl, kind: 'birth', years: birthYears });
        }
      }
    }
    return result;
  }, [trackedQuery.data, features.data, today]);

  const todayHolidays = useMemo(
    () => holidaysByDate.get(todayIso) ?? [],
    [holidaysByDate, todayIso],
  );

  const upcomingByCategory = useMemo(() => {
    const toIso = shiftIso(todayIso, UPCOMING_DAYS);
    const upcoming = (holidaysQuery.data ?? []).filter(
      (h) => h.date > todayIso && h.date <= toIso,
    );
    const groups = new Map<string, Holiday[]>();
    for (const h of upcoming) {
      const list = groups.get(h.category) ?? [];
      list.push(h);
      groups.set(h.category, list);
    }
    return [...groups.entries()].sort(
      (a, b) => categoryMeta(a[0]).order - categoryMeta(b[0]).order,
    );
  }, [holidaysQuery.data, todayIso]);

  // Окно редактирования напоминаний по клику на дату календаря.
  const [selectedDateIso, setSelectedDateIso] = useState<string | null>(null);
  const modalHolidays = selectedDateIso
    ? holidaysByDate.get(selectedDateIso) ?? []
    : [];
  const modalEffective = useMemo(() => {
    const map = new Map<string, number[]>();
    for (const h of modalHolidays) map.set(h.name, effectiveLeadDays(h, overrides));
    return map;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDateIso, overrides, holidaysByDate]);

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>События</TitleLabel>
        <CaptionLabel>
          Памятные даты ваших близких, праздники и напоминания.
        </CaptionLabel>
      </Stack>

      {/* Годовщины сегодня */}
      <Stack gap="sm">
        <SubTitleLabel>Сегодня</SubTitleLabel>

        {trackedQuery.isLoading && (
          <Group justify="center" py="md">
            <Loader color="azure" size="sm" />
          </Group>
        )}
        {trackedQuery.isError && (
          <Alert color="red" variant="light">
            {formatError(trackedQuery.error)}
          </Alert>
        )}
        {!trackedQuery.isLoading && !trackedQuery.isError && anniversaries.length === 0 && (
          <CloudCard>
            <BodyLabel>Сегодня памятных дат среди отслеживаемых нет.</BodyLabel>
          </CloudCard>
        )}
        {anniversaries.map((a) => (
          <AnniversaryRow
            key={`${a.deceasedId}-${a.kind}`}
            anniversary={a}
            onClick={() => navigate(`/tracked/${a.deceasedId}`)}
          />
        ))}
      </Stack>

      {/* Праздники сегодня — только если они есть */}
      {todayHolidays.length > 0 && (
        <Stack gap="sm">
          <SubTitleLabel>Праздники сегодня</SubTitleLabel>
          <CloudCard>
            <Stack gap="sm">
              {todayHolidays.map((h, i) => (
                <HolidayRow key={`${h.date}-${h.name}-${i}`} holiday={h} showDate={false} />
              ))}
            </Stack>
          </CloudCard>
        </Stack>
      )}

      {/* Большой календарь праздников */}
      <Stack gap="sm">
        <SubTitleLabel>Календарь праздников</SubTitleLabel>
        <CaptionLabel>
          Точка под числом — есть праздник. Наведите, чтобы увидеть какой;
          нажмите на дату, чтобы настроить напоминание.
        </CaptionLabel>
        <CloudCard>
          {holidaysQuery.isLoading ? (
            <Group justify="center" py="md">
              <Loader color="azure" size="sm" />
            </Group>
          ) : (
            <Group justify="center">
              <Calendar
                size="xl"
                defaultDate={today}
                getDayProps={(date) => {
                  const iso = isoDate(date);
                  if (!holidaysByDate.has(iso)) return {};
                  return { onClick: () => setSelectedDateIso(iso) };
                }}
                renderDay={(date) => {
                  const iso = isoDate(date);
                  const dayHolidays = holidaysByDate.get(iso);
                  const dayNum = date.getDate();
                  if (!dayHolidays || dayHolidays.length === 0) {
                    return <span>{dayNum}</span>;
                  }
                  return (
                    <Tooltip
                      withArrow
                      multiline
                      maw={280}
                      label={
                        <Stack gap={2}>
                          {dayHolidays.map((h) => (
                            <Text key={h.name} size="xs">
                              {h.name}
                            </Text>
                          ))}
                        </Stack>
                      }
                    >
                      <div
                        style={{
                          position: 'relative',
                          width: '100%',
                          height: '100%',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontWeight: 700,
                        }}
                      >
                        <span>{dayNum}</span>
                        <span
                          style={{
                            position: 'absolute',
                            bottom: 3,
                            width: 5,
                            height: 5,
                            borderRadius: '50%',
                            background: cloudColors.azure,
                          }}
                        />
                      </div>
                    </Tooltip>
                  );
                }}
              />
            </Group>
          )}
        </CloudCard>
      </Stack>

      {/* Ближайшие праздники по категориям — под календарём */}
      {upcomingByCategory.length > 0 && (
        <Stack gap="sm">
          <SubTitleLabel>Ближайшие праздники</SubTitleLabel>
          {upcomingByCategory.map(([category, list]) => (
            <CloudCard key={category}>
              <Stack gap="sm">
                <Badge color={categoryMeta(category).color} variant="light">
                  {categoryMeta(category).label}
                </Badge>
                <Stack gap="xs">
                  {list.map((h, i) => (
                    <HolidayRow key={`${h.date}-${h.name}-${i}`} holiday={h} showDate />
                  ))}
                </Stack>
              </Stack>
            </CloudCard>
          ))}
        </Stack>
      )}

      <HolidayReminderModal
        opened={selectedDateIso !== null}
        onClose={() => setSelectedDateIso(null)}
        dateIso={selectedDateIso}
        holidays={modalHolidays}
        effectiveByName={modalEffective}
      />
    </Stack>
  );
}

function AnniversaryRow({
  anniversary,
  onClick,
}: {
  anniversary: TodayAnniversary;
  onClick: () => void;
}) {
  const isBirth = anniversary.kind === 'birth';
  const label = isBirth
    ? `День памяти · исполнилось бы ${anniversary.years} ${yearsWord(anniversary.years)}`
    : `Година · ${anniversary.years} ${yearsWord(anniversary.years)}`;

  return (
    <UnstyledButton
      onClick={onClick}
      style={{ display: 'block', width: '100%', textAlign: 'left' }}
    >
      <CloudCard style={{ cursor: 'pointer' }}>
        <Group align="center" gap="md" wrap="nowrap">
          <Avatar url={anniversary.photoUrl} />
          <Stack gap={4} style={{ flex: 1, minWidth: 0 }}>
            <SubTitleLabel>{anniversary.fullName}</SubTitleLabel>
            <Group gap={6} align="center">
              {isBirth ? (
                <Flower2 size={15} color={cloudColors.azureDeep} />
              ) : (
                <Cross size={15} color={cloudColors.azureDeep} />
              )}
              <CaptionLabel>{label}</CaptionLabel>
            </Group>
          </Stack>
          <ChevronRight size={20} color={cloudColors.captionGray} />
        </Group>
      </CloudCard>
    </UnstyledButton>
  );
}

function HolidayRow({
  holiday,
  showDate,
}: {
  holiday: Holiday;
  showDate: boolean;
}) {
  return (
    <Group gap="sm" align="baseline" wrap="nowrap">
      {showDate && (
        <CaptionLabel>
          <span style={{ whiteSpace: 'nowrap' }}>{formatDateOnly(holiday.date)}</span>
        </CaptionLabel>
      )}
      <BodyLabel>{holiday.name}</BodyLabel>
    </Group>
  );
}

function Avatar({ url }: { url: string | null }) {
  return (
    <div
      style={{
        width: 48,
        height: 48,
        flexShrink: 0,
        borderRadius: '50%',
        background: cloudColors.sky,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        color: cloudColors.azureDeep,
      }}
    >
      {url ? (
        <img
          src={url}
          alt=""
          width={48}
          height={48}
          style={{ objectFit: 'cover', display: 'block' }}
        />
      ) : (
        <UserRound size={24} strokeWidth={1.5} />
      )}
    </div>
  );
}
