import { Alert, Group, Loader, SimpleGrid, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import {
  BadgeCheck,
  BookHeart,
  CalendarClock,
  CreditCard,
  FileText,
  Gift,
  Image,
  LifeBuoy,
  MapPin,
  PencilLine,
  ShieldCheck,
  Star,
  UserMinus,
  UserPlus,
  Users,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { adminStatsApi, type AdminStats } from '../../api/endpoints/adminStatsApi';
import { formatError } from '../../auth/errorMessages';

/**
 * F38. «Информация» — справочная страница админки. Показывает счётчики по
 * системе: люди, карточки, контент, обращения, деньги.
 *
 * Никаких действий на странице нет — это именно справка. Поэтому и вёрстка
 * плоская: группы карточек с числом и подписью, без таблиц и фильтров.
 */
export function AdminInfoPage() {
  const query = useQuery({
    queryKey: ['admin-stats'],
    queryFn: () => adminStatsApi.get(),
    // Цифры живут своей жизнью — перетягиваем при возврате на вкладку.
    refetchOnWindowFocus: true,
  });

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Информация</TitleLabel>
        <CaptionLabel>
          Сводка по системе. Только справка — ничего изменить отсюда нельзя.
        </CaptionLabel>
      </Stack>

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.data && <StatsBody stats={query.data} />}
    </Stack>
  );
}

function StatsBody({ stats }: { stats: AdminStats }) {
  const { users, deceased, content, support, payments } = stats;

  return (
    <Stack gap="lg">
      <Section title="Пользователи">
        <Metric icon={Users} label="Всего зарегистрировано" value={users.total} />
        <Metric icon={UserPlus} label="Новых за 7 дней" value={users.newLast7Days} />
        <Metric icon={UserPlus} label="Новых за 30 дней" value={users.newLast30Days} />
        <Metric
          icon={CalendarClock}
          label="Заходили за 30 дней"
          value={users.activeLast30Days}
        />
        <Metric icon={ShieldCheck} label="Администраторов" value={users.admins} />
        <Metric icon={UserMinus} label="Заблокировано" value={users.blocked} />
      </Section>

      <Section title="Доступ и подписки">
        <Metric
          icon={Star}
          label="С активной подпиской"
          value={users.withActiveSubscription}
        />
        <Metric icon={CalendarClock} label="На пробном периоде" value={users.onTrial} />
        <Metric
          icon={Gift}
          label="Бесплатный доступ от админа"
          value={users.withComplimentaryAccess}
        />
      </Section>

      <Section title="Карточки умерших">
        <Metric icon={BookHeart} label="Всего карточек" value={deceased.total} />
        <Metric
          icon={CalendarClock}
          label="Создано за 30 дней"
          value={deceased.newLast30Days}
        />
        <Metric icon={BadgeCheck} label="Подтверждённых" value={deceased.verified} />
        <Metric
          icon={MapPin}
          label="С координатами"
          value={deceased.withCoordinates}
          hint="Без них не работают маршрут и «найти рядом»"
        />
        <Metric
          icon={Image}
          label="С главным фото"
          value={deceased.withMainPhoto}
          hint="Остальные показываются в поиске без превью"
        />
        <Metric
          icon={Users}
          label="Подписок на отслеживание"
          value={deceased.trackedRecords}
          hint="Записей «пользователь ↔ карточка», а не людей"
        />
      </Section>

      <Section title="Контент">
        <Metric icon={Image} label="Фото умерших" value={content.photos} />
        <Metric icon={Image} label="Фото могил" value={content.gravePhotos} />
        <Metric icon={FileText} label="Документов" value={content.documents} />
        <Metric icon={BookHeart} label="Воспоминаний" value={content.memories} />
        <Metric icon={PencilLine} label="Правок карточек" value={content.edits} />
      </Section>

      <Section title="Поддержка">
        <Metric icon={LifeBuoy} label="Всего обращений" value={support.total} />
        <Metric
          icon={LifeBuoy}
          label="Ждут ответа"
          value={support.open}
          hint="Открытые и в работе"
          highlight={support.open > 0}
        />
        <Metric icon={BadgeCheck} label="Решено" value={support.resolved} />
      </Section>

      <Section title="Платежи">
        <Metric
          icon={CreditCard}
          label="Успешных платежей"
          value={payments.succeededCount}
        />
        <Metric
          icon={CreditCard}
          label="Всего получено"
          value={formatRub(payments.totalRub)}
        />
        <Metric
          icon={CreditCard}
          label="За 30 дней"
          value={formatRub(payments.last30DaysRub)}
        />
      </Section>

      <CaptionLabel>
        Данные на {new Date(stats.generatedAtUtc).toLocaleString('ru-RU')}
      </CaptionLabel>
    </Stack>
  );
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <Stack gap="sm">
      <SubTitleLabel>{title}</SubTitleLabel>
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
        {children}
      </SimpleGrid>
    </Stack>
  );
}

/**
 * Плитка со счётчиком. highlight — для того, что требует внимания
 * (например, открытые обращения): цифра красится в акцент.
 */
function Metric({
  icon: Icon,
  label,
  value,
  hint,
  highlight = false,
}: {
  icon: LucideIcon;
  label: string;
  value: number | string;
  hint?: string;
  highlight?: boolean;
}) {
  return (
    <CloudCard>
      <Group align="flex-start" gap="md" wrap="nowrap">
        <div
          style={{
            width: 40,
            height: 40,
            flexShrink: 0,
            borderRadius: 10,
            background: cloudColors.sky,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: cloudColors.azureDeep,
          }}
        >
          <Icon size={20} strokeWidth={1.75} />
        </div>
        <Stack gap={2} style={{ minWidth: 0 }}>
          <BodyLabel
            fz={26}
            fw={700}
            lh={1.1}
            c={highlight ? cloudColors.azure : cloudColors.inkBlue}
          >
            {value}
          </BodyLabel>
          <CaptionLabel>{label}</CaptionLabel>
          {hint && (
            <CaptionLabel fz={11} c={cloudColors.captionGray}>
              {hint}
            </CaptionLabel>
          )}
        </Stack>
      </Group>
    </CloudCard>
  );
}

/** Рубли без копеек — суммы подписки целые, дробная часть только шумит. */
function formatRub(value: number): string {
  return `${value.toLocaleString('ru-RU', { maximumFractionDigits: 0 })} ₽`;
}
