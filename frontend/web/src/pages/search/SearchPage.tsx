import { useState } from 'react';
import {
  Group,
  Pagination,
  Stack,
  TextInput,
  UnstyledButton,
} from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Search } from 'lucide-react';
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
  type SearchDeceasedParams,
} from '../../api/endpoints/deceasedApi';
import { formatError } from '../../auth/errorMessages';

import '@mantine/dates/styles.css';

/**
 * F6. Поиск умершего перед добавлением. Зеркало mobile
 * DeceasedSearchPage / DeceasedSearchViewModel.
 *
 * Шесть фильтров (имя/фамилия/отчество/город/дата рожд./дата смерти),
 * кнопка 'Найти' активна если хоть одно поле заполнено (E17.4.1 —
 * поиск только по датам допустим). При клике на результат — переход
 * на /preview/:id (F7). Внизу — кнопка 'Создать новую' (F8).
 *
 * Стейт формы (что в инпутах) и стейт активного поиска (что отправили
 * на бэк) разделены: пока юзер набирает, запросы не идут. Только по
 * клику 'Найти' переключаем activeParams и TanStack Query дёргает API.
 */
const PAGE_SIZE = 20;

type FormState = {
  firstName: string;
  lastName: string;
  middleName: string;
  city: string;
  birthDate: Date | null;
  deathDate: Date | null;
};

const EMPTY_FORM: FormState = {
  firstName: '',
  lastName: '',
  middleName: '',
  city: '',
  birthDate: null,
  deathDate: null,
};

function toDateOnly(d: Date | null): string | undefined {
  if (!d) return undefined;
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function formToParams(form: FormState, page: number): SearchDeceasedParams {
  return {
    firstName: form.firstName.trim() || undefined,
    lastName: form.lastName.trim() || undefined,
    middleName: form.middleName.trim() || undefined,
    city: form.city.trim() || undefined,
    birthDate: toDateOnly(form.birthDate),
    deathDate: toDateOnly(form.deathDate),
    page,
    pageSize: PAGE_SIZE,
  };
}

function hasAnyFilter(form: FormState): boolean {
  return (
    form.firstName.trim() !== '' ||
    form.lastName.trim() !== '' ||
    form.middleName.trim() !== '' ||
    form.city.trim() !== '' ||
    form.birthDate !== null ||
    form.deathDate !== null
  );
}

export function SearchPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [activeForm, setActiveForm] = useState<FormState | null>(null);
  const [page, setPage] = useState(1);

  const canSearch = hasAnyFilter(form);

  // Query запускается только когда activeForm установлен.
  const query = useQuery({
    queryKey: ['deceased-search', activeForm, page],
    queryFn: () => deceasedApi.search(formToParams(activeForm!, page)),
    enabled: activeForm !== null,
    placeholderData: (prev) => prev, // плавная пагинация — не моргает
  });

  function handleSearch() {
    if (!canSearch) return;
    setActiveForm(form);
    setPage(1);
  }

  function handleReset() {
    setForm(EMPTY_FORM);
    setActiveForm(null);
    setPage(1);
  }

  const totalPages =
    query.data && query.data.pageSize > 0
      ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize))
      : 1;

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Поиск умершего</TitleLabel>
        <CaptionLabel>
          Перед созданием новой карточки проверьте, не завёл ли её кто-то
          раньше. Если нашли — откройте превью и подпишитесь. Если не
          нашли — нажмите "Создать новую" внизу страницы.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Фильтры</SubTitleLabel>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Фамилия"
              placeholder="Иванов"
              value={form.lastName}
              onChange={(e) =>
                setForm({ ...form, lastName: e.currentTarget.value })
              }
            />
            <TextInput
              label="Имя"
              placeholder="Иван"
              value={form.firstName}
              onChange={(e) =>
                setForm({ ...form, firstName: e.currentTarget.value })
              }
            />
            <TextInput
              label="Отчество"
              placeholder="Иванович"
              value={form.middleName}
              onChange={(e) =>
                setForm({ ...form, middleName: e.currentTarget.value })
              }
            />
          </Group>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Город"
              placeholder="Москва"
              value={form.city}
              onChange={(e) =>
                setForm({ ...form, city: e.currentTarget.value })
              }
            />
            <DatePickerInput
              label="Дата рождения"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.birthDate}
              onChange={(v) =>
                setForm({
                  ...form,
                  birthDate: v ? new Date(v as unknown as string) : null,
                })
              }
            />
            <DatePickerInput
              label="Дата смерти"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={form.deathDate}
              onChange={(v) =>
                setForm({
                  ...form,
                  deathDate: v ? new Date(v as unknown as string) : null,
                })
              }
            />
          </Group>
          <Group justify="space-between">
            <GhostButton onClick={handleReset}>Сбросить</GhostButton>
            <PrimaryButton
              onClick={handleSearch}
              disabled={!canSearch}
              leftSection={<Search size={16} />}
              loading={query.isFetching && query.isLoading}
            >
              Найти
            </PrimaryButton>
          </Group>
        </Stack>
      </CloudCard>

      {query.isError && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <BodyLabel c={cloudColors.errorRed}>
            {formatError(query.error)}
          </BodyLabel>
        </CloudCard>
      )}

      {activeForm !== null && query.data && (
        <Stack gap="md">
          <CaptionLabel>
            Найдено: {query.data.totalCount}{' '}
            {query.data.totalCount > 0 &&
              `(страница ${query.data.page} из ${totalPages})`}
          </CaptionLabel>

          {query.data.items.length === 0 ? (
            <CloudCard>
              <Stack gap="sm" align="center" py="md">
                <SubTitleLabel>Никого не нашли</SubTitleLabel>
                <BodyLabel>
                  По вашим параметрам в базе нет карточек. Если вы уверены,
                  что нужного человека там нет — нажмите "Создать новую".
                </BodyLabel>
                <PrimaryButton onClick={() => navigate('/at-grave')}>
                  Создать новую карточку
                </PrimaryButton>
              </Stack>
            </CloudCard>
          ) : (
            query.data.items.map((item) => (
              <ResultCard
                key={item.id}
                item={item}
                onClick={() => navigate(`/preview/${item.id}`)}
              />
            ))
          )}

          {totalPages > 1 && (
            <Group justify="center">
              <Pagination
                value={page}
                onChange={setPage}
                total={totalPages}
                color="azure"
              />
            </Group>
          )}

          {query.data.items.length > 0 && (
            <CloudCard>
              <Stack gap="xs" align="center">
                <CaptionLabel>
                  Если среди результатов нет того, кого вы искали:
                </CaptionLabel>
                <GhostButton onClick={() => navigate('/at-grave')}>
                  Создать новую карточку
                </GhostButton>
              </Stack>
            </CloudCard>
          )}
        </Stack>
      )}

      {activeForm === null && (
        <CloudCard>
          <Stack gap="xs">
            <CaptionLabel>
              Заполните хотя бы одно поле и нажмите "Найти". Поиск
              только по дате — допустимый сценарий: например, "все
              умершие 09.06.2026 в Москве".
            </CaptionLabel>
          </Stack>
        </CloudCard>
      )}
    </Stack>
  );
}

/**
 * Карточка результата. Кликабельна целиком (паттерн как в AdminPage).
 */
function ResultCard({
  item,
  onClick,
}: {
  item: DeceasedListItem;
  onClick: () => void;
}) {
  const [hovered, setHovered] = useState(false);

  const lifePeriod = `${item.birthDate ?? '?'} — ${item.deathDate}`;
  const locationParts = [item.country, item.city, item.cemeteryName]
    .filter(Boolean)
    .join(', ');

  return (
    <UnstyledButton
      onClick={onClick}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{ display: 'block', width: '100%', textAlign: 'left' }}
    >
      <CloudCard
        style={{
          cursor: 'pointer',
          transition: 'box-shadow 120ms ease, border-color 120ms ease',
          boxShadow: hovered
            ? '0 6px 18px rgba(30, 58, 95, 0.14)'
            : '0 4px 14px rgba(30, 58, 95, 0.08)',
          borderColor: hovered ? cloudColors.azure : cloudColors.cloudBorder,
        }}
      >
        <Stack gap={6}>
          <Group justify="space-between">
            <SubTitleLabel>{item.fullName}</SubTitleLabel>
            {item.isVerified && (
              <CaptionLabel c={cloudColors.azureDeep}>
                ✓ верифицирован
              </CaptionLabel>
            )}
          </Group>
          <BodyLabel>{lifePeriod}</BodyLabel>
          {locationParts && <CaptionLabel>{locationParts}</CaptionLabel>}
        </Stack>
      </CloudCard>
    </UnstyledButton>
  );
}
