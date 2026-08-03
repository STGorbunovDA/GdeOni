import { useMemo, useState } from 'react';
import {
  Badge,
  Group,
  Loader,
  Stack,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import { Calendar } from '@mantine/dates';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { CalendarPlus, ChevronRight, Cross, Flower2, Plus } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import {
  customEventsApi,
  eventsApi,
  holidayRemindersApi,
  type CustomEvent,
  type Holiday,
} from '../../api/endpoints/eventsApi';
import { CustomEventModal } from '../../components/events/CustomEventModal';
import { LEAD_OPTIONS } from '../../utils/holidayReminders';
import { AuthAvatar } from '../../components/media/AuthAvatar';
import { formatDateOnly } from '../../utils/formatDate';
import {
  buildOverridesMap,
  effectiveLeadDays,
  shiftIso,
} from '../../utils/holidayReminders';
import { anniversaryYearsToday, yearsWord } from '../../utils/anniversary';
import { useNavigate } from 'react-router-dom';
import {
  HolidayReminderModal,
  type DeceasedAnniversaryRow,
} from '../../components/events/HolidayReminderModal';

/**
 * F42. Вкладка «События»: годовщины близких сегодня, праздники сегодня (если
 * есть), большой календарь (листается по месяцам стрелками; праздники —
 * синяя/зелёная точка по состоянию напоминания, памятные даты умерших —
 * красная точка; клик по дате → окно напоминаний) и список ближайших
 * праздников под ним. Напоминания хранятся за юзером на сервере.
 */

const UPCOMING_DAYS = 30;

/** Зелёный «включено» для точки праздника (в палитре нет green-токена). */
const REMINDER_ON_GREEN = '#2F9E44';

/** Жёлтая точка — памятная дата близкого (годовщина смерти/рождения). */
const DECEASED_DOT_YELLOW = '#FAB005';

/** Красная точка — своё (ручное) событие пользователя. */
const CUSTOM_DOT_RED = '#E03131';

/** «за сколько дней» → короткая подпись для строки события. */
function leadDaysSummary(leadDays: number[]): string {
  if (leadDays.length === 0) return 'Напоминание выключено';
  const labels = LEAD_OPTIONS.filter((o) => leadDays.includes(o.days)).map(
    (o) => o.label.toLowerCase(),
  );
  return `Напоминать: ${labels.join(', ')}`;
}

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

/** «MM-DD» из ISO yyyy-MM-dd — ключ годовщины (повторяется каждый год). */
function monthDay(iso: string): string {
  return iso.slice(5, 10);
}

function monthDayOf(d: Date): string {
  return `${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

type TodayAnniversary = {
  deceasedId: string;
  fullName: string;
  photoUrl: string | null;
  kind: 'birth' | 'death';
  years: number;
};

/** Годовщины умершего для одного дня календаря (по MM-DD). */
type DayDeceased = {
  deaths: DeceasedAnniversaryRow[];
  births: DeceasedAnniversaryRow[];
};

export function EventsPage() {
  const navigate = useNavigate();

  const today = useMemo(() => new Date(), []);
  const todayIso = isoDate(today);

  // Месяц, показанный в календаре (управляемый — чтобы стрелки листали и
  // подтягивали праздники нужного месяца).
  const [displayDate, setDisplayDate] = useState<Date>(() => new Date());

  // Широкий запрос праздников: текущий месяц + ~год вперёд. Покрывает
  // «сегодня» и «ближайшие».
  const rangeFromIso = useMemo(
    () => isoDate(new Date(today.getFullYear(), today.getMonth(), 1)),
    [today],
  );
  const rangeToIso = useMemo(() => shiftIso(rangeFromIso, 364), [rangeFromIso]);

  // Праздники видимого месяца (± неделя на «хвосты» соседних месяцев в сетке).
  const monthFromIso = useMemo(
    () =>
      shiftIso(
        isoDate(new Date(displayDate.getFullYear(), displayDate.getMonth(), 1)),
        -6,
      ),
    [displayDate],
  );
  const monthToIso = useMemo(
    () =>
      shiftIso(
        isoDate(
          new Date(displayDate.getFullYear(), displayDate.getMonth() + 1, 0),
        ),
        6,
      ),
    [displayDate],
  );

  const trackedQuery = useQuery({
    queryKey: ['events-tracked'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
  });

  const holidaysQuery = useQuery({
    queryKey: ['events-holidays', rangeFromIso, rangeToIso],
    queryFn: () => eventsApi.getHolidays(rangeFromIso, rangeToIso),
  });

  const calendarHolidaysQuery = useQuery({
    queryKey: ['events-calendar-holidays', monthFromIso, monthToIso],
    queryFn: () => eventsApi.getHolidays(monthFromIso, monthToIso),
    placeholderData: keepPreviousData,
  });

  const remindersQuery = useQuery({
    queryKey: ['events-holiday-reminders'],
    queryFn: () => holidayRemindersApi.getMine(),
  });

  const customQuery = useQuery({
    queryKey: ['events-custom'],
    queryFn: () => customEventsApi.list(),
  });

  // Модалка добавления/правки своего события. null = режим создания.
  const [eventModalOpen, setEventModalOpen] = useState(false);
  const [editEvent, setEditEvent] = useState<CustomEvent | null>(null);

  function openNewEvent() {
    setEditEvent(null);
    setEventModalOpen(true);
  }
  function openEditEvent(ev: CustomEvent) {
    setEditEvent(ev);
    setEventModalOpen(true);
  }

  const overrides = useMemo(
    () => buildOverridesMap(remindersQuery.data ?? []),
    [remindersQuery.data],
  );

  // Map «дата (ISO) → праздники дня» широкого запроса (для «сегодня»).
  const holidaysByDate = useMemo(() => {
    const map = new Map<string, Holiday[]>();
    for (const h of holidaysQuery.data ?? []) {
      const list = map.get(h.date) ?? [];
      list.push(h);
      map.set(h.date, list);
    }
    return map;
  }, [holidaysQuery.data]);

  // Map «дата (ISO) → праздники» видимого месяца (для календаря и окна).
  const calendarHolidaysByDate = useMemo(() => {
    const map = new Map<string, Holiday[]>();
    for (const h of calendarHolidaysQuery.data ?? []) {
      const list = map.get(h.date) ?? [];
      list.push(h);
      map.set(h.date, list);
    }
    return map;
  }, [calendarHolidaysQuery.data]);

  // Map «MM-DD → годовщины умерших» — повторяется каждый год, поэтому ключ
  // без года: одна карта покрывает любой показанный месяц.
  const deceasedByMonthDay = useMemo(() => {
    const map = new Map<string, DayDeceased>();
    for (const item of trackedQuery.data?.items ?? []) {
      if (item.status === 'Archived') continue;
      const push = (iso: string, kind: 'birth' | 'death') => {
        const md = monthDay(iso);
        const bucket = map.get(md) ?? { deaths: [], births: [] };
        const row: DeceasedAnniversaryRow = {
          deceasedId: item.deceasedId,
          fullName: item.fullName,
          kind,
          leadDays:
            kind === 'death'
              ? item.deathAnniversaryLeadDays
              : item.birthAnniversaryLeadDays,
        };
        (kind === 'death' ? bucket.deaths : bucket.births).push(row);
        map.set(md, bucket);
      };
      if (item.deathDate) push(item.deathDate, 'death');
      if (item.birthDate) push(item.birthDate, 'birth');
    }
    return map;
  }, [trackedQuery.data]);

  // Map «MM-DD → мои события» — повторяются каждый год по дню/месяцу.
  const customByMonthDay = useMemo(() => {
    const map = new Map<string, CustomEvent[]>();
    for (const ev of customQuery.data ?? []) {
      const md = monthDay(ev.date);
      const list = map.get(md) ?? [];
      list.push(ev);
      map.set(md, list);
    }
    return map;
  }, [customQuery.data]);

  const anniversaries = useMemo<TodayAnniversary[]>(() => {
    const items = trackedQuery.data?.items ?? [];
    const result: TodayAnniversary[] = [];
    for (const item of items) {
      if (item.status === 'Archived') continue;
      const photoUrl = item.mainPhotoUrl;
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
  }, [trackedQuery.data, today]);

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
    ? calendarHolidaysByDate.get(selectedDateIso) ?? []
    : [];
  const modalDeceased = useMemo<DeceasedAnniversaryRow[]>(() => {
    if (!selectedDateIso) return [];
    const bucket = deceasedByMonthDay.get(monthDay(selectedDateIso));
    if (!bucket) return [];
    return [...bucket.deaths, ...bucket.births];
  }, [selectedDateIso, deceasedByMonthDay]);
  const modalEffective = useMemo(() => {
    const map = new Map<string, number[]>();
    for (const h of modalHolidays) map.set(h.name, effectiveLeadDays(h, overrides));
    return map;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDateIso, overrides, calendarHolidaysByDate]);

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>События</TitleLabel>
        <CaptionLabel>
          Памятные даты ваших близких, праздники и напоминания.
        </CaptionLabel>
      </Stack>

      {/* Годовщины сегодня — секция видна только когда они есть */}
      {anniversaries.length > 0 && (
        <Stack gap="sm">
          <SubTitleLabel>Сегодня</SubTitleLabel>
          {anniversaries.map((a) => (
            <AnniversaryRow
              key={`${a.deceasedId}-${a.kind}`}
              anniversary={a}
              onClick={() => navigate(`/tracked/${a.deceasedId}`)}
            />
          ))}
        </Stack>
      )}

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

      {/* Большой календарь */}
      <Stack gap="sm">
        <SubTitleLabel>Календарь</SubTitleLabel>
        <CaptionLabel>
          Стрелками листайте месяцы. Точка под числом: синяя — праздник без
          напоминания, зелёная — напоминание включено, жёлтая — памятная дата
          близкого, красная — ваше событие. Нажмите на дату, чтобы настроить.
        </CaptionLabel>
        <CloudCard>
          {calendarHolidaysQuery.isLoading && !calendarHolidaysQuery.data ? (
            <Group justify="center" py="md">
              <Loader color="azure" size="sm" />
            </Group>
          ) : (
            <Group justify="center">
              <Calendar
                size="xl"
                date={displayDate}
                onDateChange={setDisplayDate}
                highlightToday
                getDayProps={(date) => {
                  const iso = isoDate(date);
                  const md = monthDayOf(date);
                  const hasHoliday = calendarHolidaysByDate.has(iso);
                  const hasDeceased = deceasedByMonthDay.has(md);
                  const custom = customByMonthDay.get(md);
                  if (!hasHoliday && !hasDeceased && !custom) return {};
                  return {
                    onClick: () => {
                      // Дата только со своим событием → сразу правка события.
                      if (!hasHoliday && !hasDeceased && custom && custom.length > 0) {
                        openEditEvent(custom[0]);
                      } else {
                        setSelectedDateIso(iso);
                      }
                    },
                  };
                }}
                renderDay={(date) => {
                  const iso = isoDate(date);
                  const md = monthDayOf(date);
                  const dayNum = date.getDate();
                  const dayHolidays = calendarHolidaysByDate.get(iso) ?? [];
                  const dayDeceased = deceasedByMonthDay.get(md);
                  const deceasedRows = dayDeceased
                    ? [...dayDeceased.deaths, ...dayDeceased.births]
                    : [];
                  const dayCustom = customByMonthDay.get(md) ?? [];
                  if (
                    dayHolidays.length === 0 &&
                    deceasedRows.length === 0 &&
                    dayCustom.length === 0
                  ) {
                    return <span>{dayNum}</span>;
                  }

                  const holidayOn = dayHolidays.some(
                    (h) => effectiveLeadDays(h, overrides).length > 0,
                  );
                  const holidayColor = holidayOn
                    ? REMINDER_ON_GREEN
                    : cloudColors.azure;

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
                          {deceasedRows.map((r) => (
                            <Text key={`${r.deceasedId}-${r.kind}`} size="xs">
                              {r.kind === 'death' ? 'Година: ' : 'День памяти: '}
                              {r.fullName}
                            </Text>
                          ))}
                          {dayCustom.map((c) => (
                            <Text key={c.id} size="xs">
                              {c.title}
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
                        <div
                          style={{
                            position: 'absolute',
                            bottom: 3,
                            display: 'flex',
                            gap: 3,
                          }}
                        >
                          {dayHolidays.length > 0 && (
                            <Dot color={holidayColor} />
                          )}
                          {deceasedRows.length > 0 && (
                            <Dot color={DECEASED_DOT_YELLOW} />
                          )}
                          {dayCustom.length > 0 && (
                            <Dot color={CUSTOM_DOT_RED} />
                          )}
                        </div>
                      </div>
                    </Tooltip>
                  );
                }}
              />
            </Group>
          )}
        </CloudCard>
      </Stack>

      {/* Мои события — ручные, приватные, повторяются каждый год */}
      <Stack gap="sm">
        <Group justify="space-between" align="center">
          <SubTitleLabel>Мои события</SubTitleLabel>
          <PrimaryButton leftSection={<Plus size={16} />} onClick={openNewEvent}>
            Добавить событие
          </PrimaryButton>
        </Group>
        <CaptionLabel>
          Свои даты (например, ДР близкого) с напоминаниями. Повторяются каждый
          год, видны только вам.
        </CaptionLabel>

        {customQuery.data && customQuery.data.length === 0 && (
          <CloudCard>
            <Group gap="sm" align="center">
              <CalendarPlus size={20} color={cloudColors.azureDeep} />
              <CaptionLabel>
                Пока нет своих событий. Нажмите «Добавить событие».
              </CaptionLabel>
            </Group>
          </CloudCard>
        )}

        {customQuery.data?.map((ev) => (
          <UnstyledButton
            key={ev.id}
            onClick={() => openEditEvent(ev)}
            style={{ display: 'block', width: '100%', textAlign: 'left' }}
          >
            <CloudCard style={{ cursor: 'pointer' }}>
              <Group align="center" gap="md" wrap="nowrap">
                <div
                  style={{
                    width: 10,
                    height: 10,
                    borderRadius: '50%',
                    background: CUSTOM_DOT_RED,
                    flexShrink: 0,
                  }}
                />
                <Stack gap={2} style={{ flex: 1, minWidth: 0 }}>
                  <SubTitleLabel>{ev.title}</SubTitleLabel>
                  <CaptionLabel>
                    {formatDateOnly(ev.date)} · {leadDaysSummary(ev.leadDays)}
                  </CaptionLabel>
                </Stack>
                <ChevronRight size={20} color={cloudColors.captionGray} />
              </Group>
            </CloudCard>
          </UnstyledButton>
        ))}
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
        deceased={modalDeceased}
      />

      <CustomEventModal
        opened={eventModalOpen}
        onClose={() => setEventModalOpen(false)}
        event={editEvent}
      />
    </Stack>
  );
}

function Dot({ color }: { color: string }) {
  return (
    <span
      style={{
        width: 5,
        height: 5,
        borderRadius: '50%',
        background: color,
      }}
    />
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
          <AuthAvatar src={anniversary.photoUrl} size={48} />
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

