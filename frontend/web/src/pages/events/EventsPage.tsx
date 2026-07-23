import { useMemo } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Stack,
  UnstyledButton,
} from '@mantine/core';
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
import { eventsApi, type Holiday } from '../../api/endpoints/eventsApi';
import { formatError } from '../../auth/errorMessages';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import { anniversaryYearsToday, yearsWord } from '../../utils/anniversary';
import { useNavigate } from 'react-router-dom';

/**
 * Вкладка «События». Сверху — памятные даты сегодня среди отслеживаемых
 * (день памяти / година), тап ведёт на карточку умершего.
 * Ниже — праздники: сегодняшние и ближайшие, сгруппированные по
 * категориям (поминальные, православные, мусульманские, государственные).
 *
 * Годовщины считаются на клиенте из tracked-списка (даты уже приходят).
 * Праздники — с backend GET /api/events/holidays (подвижные даты
 * считает сервер).
 */

const UPCOMING_DAYS = 30;

type CategoryMeta = { label: string; color: string; order: number };

const CATEGORY_META: Record<string, CategoryMeta> = {
  Memorial: { label: 'Поминальные дни', color: 'grape', order: 0 },
  Orthodox: { label: 'Православные', color: 'indigo', order: 1 },
  Muslim: { label: 'Мусульманские', color: 'teal', order: 2 },
  State: { label: 'Государственные', color: 'red', order: 3 },
  // D38.1. Пост — не праздник, поэтому отдельная группа и последняя
  // в порядке вывода.
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
  const toIso = useMemo(() => {
    const d = new Date(today);
    d.setDate(d.getDate() + UPCOMING_DAYS);
    return isoDate(d);
  }, [today]);

  const trackedQuery = useQuery({
    queryKey: ['events-tracked'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
  });

  const holidaysQuery = useQuery({
    queryKey: ['events-holidays', todayIso, toIso],
    queryFn: () => eventsApi.getHolidays(todayIso, toIso),
  });

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
        result.push({
          deceasedId: item.deceasedId,
          fullName: item.fullName,
          photoUrl,
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
            photoUrl,
            kind: 'birth',
            years: birthYears,
          });
        }
      }
    }

    return result;
  }, [trackedQuery.data, features.data, today]);

  const todayHolidays = useMemo(
    () => (holidaysQuery.data ?? []).filter((h) => h.date === todayIso),
    [holidaysQuery.data, todayIso],
  );

  const upcomingByCategory = useMemo(() => {
    const upcoming = (holidaysQuery.data ?? []).filter((h) => h.date > todayIso);
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

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>События</TitleLabel>
        <CaptionLabel>
          Памятные даты ваших близких и ближайшие праздники.
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

        {!trackedQuery.isLoading &&
          !trackedQuery.isError &&
          anniversaries.length === 0 && (
            <CloudCard>
              <BodyLabel>
                Сегодня памятных дат среди отслеживаемых нет.
              </BodyLabel>
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

      {/* Праздники сегодня */}
      <Stack gap="sm">
        <SubTitleLabel>Праздники сегодня</SubTitleLabel>

        {holidaysQuery.isLoading && (
          <Group justify="center" py="md">
            <Loader color="azure" size="sm" />
          </Group>
        )}

        {holidaysQuery.isError && (
          <Alert color="red" variant="light">
            {formatError(holidaysQuery.error)}
          </Alert>
        )}

        {!holidaysQuery.isLoading &&
          !holidaysQuery.isError &&
          todayHolidays.length === 0 && (
            <CloudCard>
              <BodyLabel>Сегодня праздников нет.</BodyLabel>
            </CloudCard>
          )}

        {todayHolidays.length > 0 && (
          <CloudCard>
            <Stack gap="sm">
              {todayHolidays.map((h, i) => (
                <HolidayRow key={`${h.date}-${h.name}-${i}`} holiday={h} showDate={false} />
              ))}
            </Stack>
          </CloudCard>
        )}
      </Stack>

      {/* Ближайшие праздники по категориям */}
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
          <span style={{ whiteSpace: 'nowrap' }}>
            {formatDateOnly(holiday.date)}
          </span>
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
