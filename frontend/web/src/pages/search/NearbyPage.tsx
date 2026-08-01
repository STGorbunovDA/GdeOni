import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Checkbox,
  Group,
  Loader,
  Slider,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MapPin, Navigation } from 'lucide-react';
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
  type NearbyDeceasedItem,
} from '../../api/endpoints/deceasedApi';
import {
  RelationshipTypes,
  trackedDeceasedApi,
} from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { requestGeolocationOnce } from '../../utils/requestGeolocation';
import { formatDistance } from '../../utils/formatDistance';
import { formatDateOnly } from '../../utils/formatDate';
import { AuthAvatar } from '../../components/media/AuthAvatar';

/**
 * F36 / E21. «Найти рядом» — юзер стоит на кладбище, берём GPS и
 * показываем карточки умерших в радиусе. Зеркало mobile
 * NearbySearchViewModel + NearbySearchPage.xaml.
 *
 * Клик по карточке ведёт на /preview/:id, а не подписывает сразу —
 * как на mobile: сначала превью, потом решение (защита от промаха).
 * Для «отметил несколько и добавил разом» есть чекбоксы + кнопка
 * внизу; уже отслеживаемые приходят пред-отмеченными и подняты вверх.
 */

/** Совпадает с mobile: дефолт 100 м, слайдер до 500 м. */
const DEFAULT_RADIUS = 100;
const MIN_RADIUS = 50;
const MAX_RADIUS = 500;

/** Максимум, который принимает валидатор бэка на tracked-list. */
const TRACKED_PROBE_SIZE = 100;

/**
 * Ошибка геолокации, а не API. Разделяем, потому что лечит их юзер
 * по-разному: геолокацию — разрешением в браузере (жёлтый Alert),
 * ошибку бэка — только повтором (красный).
 */
class GeoFailure extends Error {}

type SearchResult = {
  items: NearbyDeceasedItem[];
  trackedIds: Set<string>;
  radiusMeters: number;
};

export function NearbyPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [radius, setRadius] = useState(DEFAULT_RADIUS);
  const [result, setResult] = useState<SearchResult | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [addStatus, setAddStatus] = useState<string | null>(null);

  const searchMutation = useMutation({
    mutationFn: async (radiusMeters: number): Promise<SearchResult> => {
      let coords;
      try {
        coords = await requestGeolocationOnce();
      } catch (e) {
        throw new GeoFailure((e as Error).message);
      }

      const [nearby, trackedIds] = await Promise.all([
        deceasedApi.nearby({
          latitude: coords.latitude,
          longitude: coords.longitude,
          radiusMeters,
        }),
        loadTrackedIds(),
      ]);

      // Отслеживаемые — наверх, внутри групп порядок бэка (по возрастанию
      // расстояния) сохраняется: sort в JS стабилен.
      const items = [...nearby.items].sort(
        (a, b) =>
          Number(trackedIds.has(b.id)) - Number(trackedIds.has(a.id)),
      );

      return { items, trackedIds, radiusMeters };
    },
    onSuccess: (data) => {
      setResult(data);
      // Пред-отмечаем уже отслеживаемых — как на mobile.
      setSelected(new Set(data.items.filter((x) => data.trackedIds.has(x.id)).map((x) => x.id)));
      setAddStatus(null);
    },
  });

  const addMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      // Batch-эндпоинта на бэке нет — шлём параллельные POST.
      // allSettled: одна упавшая подписка не должна отменять остальные.
      const outcomes = await Promise.allSettled(
        ids.map((id) =>
          trackedDeceasedApi.track(id, {
            relationshipType: RelationshipTypes.Other,
            personalNotes: null,
            // F42. Напоминание о годовщине смерти включено по умолчанию («в день»).
            notifyOnDeathAnniversary: true,
            notifyOnBirthAnniversary: false,
          }),
        ),
      );
      const added = ids.filter((_, i) => outcomes[i].status === 'fulfilled');
      return { added, failed: ids.length - added.length };
    },
    onSuccess: ({ added, failed }) => {
      setSelected((prev) => {
        const next = new Set(prev);
        added.forEach((id) => next.delete(id));
        return next;
      });
      setResult((prev) =>
        prev
          ? {
              ...prev,
              trackedIds: new Set([...prev.trackedIds, ...added]),
            }
          : prev,
      );
      setAddStatus(
        failed === 0
          ? `Добавлено: ${added.length}.`
          : `Добавлено: ${added.length}, не удалось: ${failed}.`,
      );
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
  });

  function toggle(id: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  const geoError =
    searchMutation.error instanceof GeoFailure ? searchMutation.error : null;
  const apiError =
    searchMutation.error && !geoError ? searchMutation.error : null;

  const selectedIds = useMemo(() => [...selected], [selected]);
  const showEmpty =
    result !== null && result.items.length === 0 && !searchMutation.isPending;

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Найти рядом</TitleLabel>
        <CaptionLabel>
          Покажем карточки умерших с координатами рядом с вами. Радиус по
          умолчанию 100 м — если никого не нашли, увеличьте его.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Радиус: {formatDistance(radius)}</SubTitleLabel>
          <Slider
            value={radius}
            onChange={setRadius}
            min={MIN_RADIUS}
            max={MAX_RADIUS}
            step={10}
            color="azure"
            label={(v) => formatDistance(v)}
            marks={[
              { value: 50, label: '50 м' },
              { value: 100, label: '100 м' },
              { value: 200, label: '200 м' },
              { value: 500, label: '500 м' },
            ]}
            mb="md"
          />
          <Group justify="flex-end">
            <PrimaryButton
              onClick={() => searchMutation.mutate(radius)}
              loading={searchMutation.isPending}
              leftSection={<Navigation size={16} />}
            >
              Найти рядом
            </PrimaryButton>
          </Group>
        </Stack>
      </CloudCard>

      {geoError && (
        <Alert color="yellow" variant="light">
          {geoError.message}
        </Alert>
      )}

      {apiError && (
        <Alert color="red" variant="light">
          {formatError(apiError)}
        </Alert>
      )}

      {searchMutation.isPending && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
          <CaptionLabel>Определяем ваше местоположение…</CaptionLabel>
        </Stack>
      )}

      {result && result.items.length > 0 && (
        <CaptionLabel>
          Найдено: {result.items.length} в радиусе{' '}
          {formatDistance(result.radiusMeters)}
        </CaptionLabel>
      )}

      {result?.items.map((item) => (
        <NearbyCard
          key={item.id}
          item={item}
          checked={selected.has(item.id)}
          alreadyTracked={result.trackedIds.has(item.id)}
          onToggle={() => toggle(item.id)}
          onOpen={() => navigate(`/preview/${item.id}`)}
        />
      ))}

      {showEmpty && (
        <CloudCard>
          <Stack gap="xs" align="center" py="md">
            <SubTitleLabel>Никого не нашли</SubTitleLabel>
            <BodyLabel ta="center">
              В радиусе {formatDistance(result.radiusMeters)} нет карточек с
              координатами. Увеличьте радиус или создайте новую карточку у
              могилы.
            </BodyLabel>
            <Group gap="sm">
              <GhostButton onClick={() => navigate('/search')}>
                Искать по имени
              </GhostButton>
              <PrimaryButton onClick={() => navigate('/at-grave')}>
                Создать карточку
              </PrimaryButton>
            </Group>
          </Stack>
        </CloudCard>
      )}

      {addStatus && (
        <Alert color="green" variant="light">
          {addStatus}
        </Alert>
      )}

      {addMutation.isError && (
        <Alert color="red" variant="light">
          {formatError(addMutation.error)}
        </Alert>
      )}

      {/* Липнет к низу вьюпорта, пока юзер листает длинный список. */}
      {selectedIds.length > 0 && (
        <Box style={{ position: 'sticky', bottom: 16, zIndex: 2 }}>
          <CloudCard>
            <Group justify="space-between" wrap="wrap">
              <CaptionLabel>Выбрано: {selectedIds.length}</CaptionLabel>
              <PrimaryButton
                onClick={() => addMutation.mutate(selectedIds)}
                loading={addMutation.isPending}
              >
                Добавить выбранных ({selectedIds.length})
              </PrimaryButton>
            </Group>
          </CloudCard>
        </Box>
      )}
    </Stack>
  );
}

/**
 * Тянем отслеживаемых, чтобы пред-отметить их в выдаче. Ошибка здесь
 * не должна валить поиск — без галок страница остаётся рабочей.
 */
async function loadTrackedIds(): Promise<Set<string>> {
  try {
    const page = await trackedDeceasedApi.list(1, TRACKED_PROBE_SIZE);
    return new Set(page.items.map((x) => x.deceasedId));
  } catch {
    return new Set();
  }
}

/**
 * Карточка выдачи. Чекбокс — отдельный контрол рядом с кликабельным
 * телом карточки (а не внутри него): вложенная в button кнопка —
 * невалидный HTML и ломает клавиатурную навигацию.
 */
function NearbyCard({
  item,
  checked,
  alreadyTracked,
  onToggle,
  onOpen,
}: {
  item: NearbyDeceasedItem;
  checked: boolean;
  alreadyTracked: boolean;
  onToggle: () => void;
  onOpen: () => void;
}) {
  const [hovered, setHovered] = useState(false);

  const lifePeriod = `${item.birthDate ? formatDateOnly(item.birthDate) : '?'} — ${formatDateOnly(item.deathDate)}`;
  const location = [item.city, item.cemeteryName, item.plotNumber, item.graveNumber]
    .filter(Boolean)
    .join(', ');

  return (
    <CloudCard
      style={{
        transition: 'box-shadow 120ms ease, border-color 120ms ease',
        boxShadow: hovered
          ? '0 6px 18px rgba(30, 58, 95, 0.14)'
          : '0 4px 14px rgba(30, 58, 95, 0.08)',
        borderColor: hovered ? cloudColors.azure : cloudColors.cloudBorder,
      }}
    >
      <Group align="center" gap="md" wrap="nowrap">
        <Checkbox
          checked={checked}
          onChange={onToggle}
          color="azure"
          aria-label={`Выбрать ${item.fullName}`}
        />

        <UnstyledButton
          onClick={onOpen}
          onMouseEnter={() => setHovered(true)}
          onMouseLeave={() => setHovered(false)}
          style={{
            display: 'block',
            flex: 1,
            minWidth: 0,
            textAlign: 'left',
            cursor: 'pointer',
          }}
        >
          <Group align="center" gap="md" wrap="nowrap">
            <AuthAvatar src={item.mainPhotoUrl} size={56} />
            <Stack gap={4} style={{ flex: 1, minWidth: 0 }}>
              <Group justify="space-between" gap="xs" wrap="nowrap">
                <Group gap={6} align="center" style={{ minWidth: 0 }}>
                  <SubTitleLabel>{item.fullName}</SubTitleLabel>
                  {item.isVerified && (
                    <CaptionLabel c={cloudColors.azureDeep}>✓</CaptionLabel>
                  )}
                </Group>
                <Group gap={4} align="center" wrap="nowrap">
                  <MapPin size={14} color={cloudColors.azure} />
                  <CaptionLabel c={cloudColors.azure}>
                    {formatDistance(item.distanceMeters)}
                  </CaptionLabel>
                </Group>
              </Group>
              <CaptionLabel>{lifePeriod}</CaptionLabel>
              {location && <CaptionLabel>{location}</CaptionLabel>}
              {alreadyTracked && (
                <CaptionLabel c={cloudColors.azureDeep}>
                  Уже в отслеживаемых
                </CaptionLabel>
              )}
            </Stack>
          </Group>
        </UnstyledButton>
      </Group>
    </CloudCard>
  );
}

/** Круглая 56×56 аватарка — тот же паттерн, что в TrackedListPage. */
