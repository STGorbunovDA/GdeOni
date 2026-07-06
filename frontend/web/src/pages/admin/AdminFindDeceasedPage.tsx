import { useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  SimpleGrid,
  Stack,
  TextInput,
  Select,
  Button,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Search as SearchIcon, RotateCcw, UserRound } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  deceasedApi,
  type DeceasedListItem,
} from '../../api/endpoints/deceasedApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateOnly } from '../../utils/formatDate';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F17.15 / D27. Админский поиск умерших по всем характеристикам —
 * зеркало AdminFindDeceasedViewModel на mobile.
 *
 * В отличие от юзерского поиска (SearchPage → PreviewPage → «добавить
 * в отслеживание»), здесь тап по карточке ведёт на /admin/deceased/{id}
 * (admin-view без tracking-гейта). Админ управляет чужой карточкой,
 * не подписываясь на неё в своём личном архиве.
 *
 * Фильтры прикладываются вручную по кнопке «Найти» (без реактивного
 * дебаунса). Кнопки-переключатели «isVerified: Все / Только проверенные
 * / Только непроверенные» и диапазон дат создания — фильтры сверх
 * стандартного E17.5 (D27 расширил бэк для этого).
 */
const PAGE_SIZE = 20;

type FormState = {
  search: string;
  firstName: string;
  lastName: string;
  middleName: string;
  country: string;
  city: string;
  birthDate: Date | null;
  deathDate: Date | null;
  createdFrom: Date | null;
  createdTo: Date | null;
  verifiedOption: 'all' | 'verified' | 'unverified';
};

const EMPTY_FORM: FormState = {
  search: '',
  firstName: '',
  lastName: '',
  middleName: '',
  country: '',
  city: '',
  birthDate: null,
  deathDate: null,
  createdFrom: null,
  createdTo: null,
  verifiedOption: 'all',
};

export function AdminFindDeceasedPage() {
  const navigate = useNavigate();

  // Форма — «черновик», редактируется до нажатия «Найти».
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  // Активные фильтры — применены и участвуют в текущем поиске.
  const [applied, setApplied] = useState<FormState | null>(null);
  // Инкрементальный список: аккумулируем items между «Показать ещё».
  const [items, setItems] = useState<DeceasedListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);

  // Аккумулируем страницы в один список: page=1 сбрасывает, page>1
  // докидывает. Делаем это императивно, потому что useQuery+dependent
  // queryKey хуже сочетается с pattern «Показать ещё» (или отдельный
  // pinned-накопитель, или useInfiniteQuery — оба сложнее для этого
  // objema).
  const runSearchMutation = useMutation({
    mutationFn: async (opts: { form: FormState; nextPage: number; reset: boolean }) => {
      return deceasedApi.search({
        ...toSearchParams(opts.form),
        page: opts.nextPage,
        pageSize: PAGE_SIZE,
      });
    },
    onSuccess: (data, vars) => {
      if (vars.reset) {
        setItems(data.items);
      } else {
        setItems((prev) => [...prev, ...data.items]);
      }
      setTotalCount(data.totalCount);
      setApplied(vars.form);
      setPage(vars.nextPage);
    },
  });

  function submitSearch() {
    runSearchMutation.mutate({ form, nextPage: 1, reset: true });
  }

  function loadMore() {
    runSearchMutation.mutate({ form: applied ?? form, nextPage: page + 1, reset: false });
  }

  function resetFilters() {
    setForm(EMPTY_FORM);
    setApplied(EMPTY_FORM);
    runSearchMutation.mutate({ form: EMPTY_FORM, nextPage: 1, reset: true });
  }

  const isLoading = runSearchMutation.isPending;
  const errorMessage = runSearchMutation.isError
    ? formatError(runSearchMutation.error)
    : null;
  const hasNoItems =
    applied !== null && !isLoading && items.length === 0 && !errorMessage;
  const canLoadMore = items.length < totalCount && !isLoading;

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Найти умершего</TitleLabel>
        <CaptionLabel>
          Поиск по всем карточкам системы. Тап по карточке открывает
          админ-просмотр без добавления в ваши отслеживаемые.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Фильтры</SubTitleLabel>

          <TextInput
            label="Быстрый поиск"
            placeholder="Часть имени, фамилии, города — любое поле"
            value={form.search}
            onChange={(e) => setForm({ ...form, search: e.currentTarget.value })}
          />

          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="sm">
            <TextInput
              label="Имя"
              value={form.firstName}
              onChange={(e) => setForm({ ...form, firstName: e.currentTarget.value })}
            />
            <TextInput
              label="Фамилия"
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.currentTarget.value })}
            />
            <TextInput
              label="Отчество"
              value={form.middleName}
              onChange={(e) => setForm({ ...form, middleName: e.currentTarget.value })}
            />
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
            <TextInput
              label="Страна"
              value={form.country}
              onChange={(e) => setForm({ ...form, country: e.currentTarget.value })}
            />
            <TextInput
              label="Город"
              value={form.city}
              onChange={(e) => setForm({ ...form, city: e.currentTarget.value })}
            />
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
            <DateInput
              label="Дата рождения"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.birthDate}
              onChange={(v) => setForm({ ...form, birthDate: v })}
            />
            <DateInput
              label="Дата смерти"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.deathDate}
              onChange={(v) => setForm({ ...form, deathDate: v })}
            />
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
            <DateInput
              label="Создано с"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.createdFrom}
              onChange={(v) => setForm({ ...form, createdFrom: v })}
            />
            <DateInput
              label="Создано по"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.createdTo}
              onChange={(v) => setForm({ ...form, createdTo: v })}
            />
          </SimpleGrid>

          <Select
            label="Верификация"
            data={[
              { value: 'all', label: 'Все' },
              { value: 'verified', label: 'Только проверенные' },
              { value: 'unverified', label: 'Только непроверенные' },
            ]}
            value={form.verifiedOption}
            onChange={(v) =>
              setForm({
                ...form,
                verifiedOption: (v as FormState['verifiedOption']) ?? 'all',
              })
            }
            allowDeselect={false}
          />

          <Group>
            <PrimaryButton
              leftSection={<SearchIcon size={16} />}
              onClick={submitSearch}
              loading={isLoading}
            >
              Найти
            </PrimaryButton>
            <GhostButton
              leftSection={<RotateCcw size={16} />}
              onClick={resetFilters}
              disabled={isLoading}
            >
              Сбросить
            </GhostButton>
          </Group>
        </Stack>
      </CloudCard>

      {errorMessage && (
        <Alert color="red" variant="light">
          {errorMessage}
        </Alert>
      )}

      {applied !== null && (
        <CloudCard>
          <Stack gap="md">
            <Group justify="space-between" align="center">
              <SubTitleLabel>
                Найдено: {totalCount}
              </SubTitleLabel>
              {items.length > 0 && (
                <CaptionLabel>
                  Показано {items.length} из {totalCount}
                </CaptionLabel>
              )}
            </Group>

            {isLoading && items.length === 0 && (
              <Stack align="center" py="md">
                <Loader color="azure" />
              </Stack>
            )}

            {hasNoItems && (
              <BodyLabel c="dimmed">Никого не нашли. Попробуйте другие фильтры.</BodyLabel>
            )}

            {items.length > 0 && (
              <Stack gap="sm">
                {items.map((it) => (
                  <ResultCard
                    key={it.id}
                    item={it}
                    onOpen={() => navigate(`/admin/deceased/${it.id}`)}
                  />
                ))}
                {canLoadMore && (
                  <Button
                    variant="light"
                    onClick={loadMore}
                    loading={isLoading}
                  >
                    Показать ещё
                  </Button>
                )}
              </Stack>
            )}
          </Stack>
        </CloudCard>
      )}
    </Stack>
  );
}

/**
 * Строчка результата: аватарка (главное фото или 🕊-плейсхолдер), имя,
 * годы жизни, город, бейдж «✓ проверено». Клик открывает admin-view.
 */
function ResultCard({
  item,
  onOpen,
}: {
  item: DeceasedListItem;
  onOpen: () => void;
}) {
  const features = useAppFeatures();
  const photoUrl = buildMediaUrl(
    features.data?.mediaBaseUrl,
    item.mainPhotoBucket,
    item.mainPhotoStorageKey,
  );
  const lifePeriod =
    (item.birthDate ? formatDateOnly(item.birthDate) : '?') +
    ' — ' +
    formatDateOnly(item.deathDate);
  const location = [item.country, item.city].filter(Boolean).join(', ');

  return (
    <div
      role="button"
      onClick={onOpen}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        padding: 10,
        border: `1px solid ${cloudColors.cloudBorder}`,
        borderRadius: 12,
        cursor: 'pointer',
        background: cloudColors.cloud,
      }}
    >
      <Avatar url={photoUrl} />
      <Stack gap={2} style={{ flex: 1, minWidth: 0 }}>
        <Group gap={8}>
          <BodyLabel style={{ fontWeight: 600 }}>{item.fullName}</BodyLabel>
          {item.isVerified && (
            <Badge color="azure" variant="light" size="xs">
              ✓ проверено
            </Badge>
          )}
        </Group>
        <CaptionLabel>{lifePeriod}</CaptionLabel>
        {location && <CaptionLabel>{location}</CaptionLabel>}
      </Stack>
    </div>
  );
}

function Avatar({ url }: { url: string | null }) {
  return (
    <div
      style={{
        width: 48,
        height: 48,
        borderRadius: '50%',
        background: cloudColors.sky,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        color: cloudColors.azureDeep,
        flexShrink: 0,
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

/**
 * Преобразует Date в 'yyyy-MM-dd' без учёта таймзоны — так бэк примет
 * DateOnly без сюрпризов от смещения. Тот же трюк, что в RegisterPage.
 */
function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * Диапазон createdTo включаем «до конца дня» — иначе поиск «до
 * 12.06» пропускает карточки, созданные в 12.06 днём.
 */
function toIsoDateTimeEndOfDay(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}T23:59:59`;
}

function toIsoDateTimeStartOfDay(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}T00:00:00`;
}

function toSearchParams(f: FormState) {
  return {
    search: f.search.trim() || undefined,
    firstName: f.firstName.trim() || undefined,
    lastName: f.lastName.trim() || undefined,
    middleName: f.middleName.trim() || undefined,
    country: f.country.trim() || undefined,
    city: f.city.trim() || undefined,
    birthDate: f.birthDate ? toIsoDate(f.birthDate) : undefined,
    deathDate: f.deathDate ? toIsoDate(f.deathDate) : undefined,
    createdFrom: f.createdFrom
      ? toIsoDateTimeStartOfDay(f.createdFrom)
      : undefined,
    createdTo: f.createdTo ? toIsoDateTimeEndOfDay(f.createdTo) : undefined,
    isVerified:
      f.verifiedOption === 'verified'
        ? true
        : f.verifiedOption === 'unverified'
          ? false
          : undefined,
  };
}
