import { useMemo, useState } from 'react';
import {
  Alert,
  Checkbox,
  Group,
  Loader,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { Route as RouteIcon } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  trackedDeceasedApi,
  TrackStatuses,
} from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { AuthAvatar } from '../../components/media/AuthAvatar';
import { formatDateOnly } from '../../utils/formatDate';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import {
  buildYandexUrl,
  openYandexRoute,
  optimizeOrder,
  type Point,
} from '../../utils/routing';

/**
 * F14.2. Multi-point маршрут (~ E19 на mobile). Юзер выбирает
 * чекбоксами карточки с координатами, мы берём текущее положение через
 * navigator.geolocation, локально оптимизируем порядок через
 * nearest-neighbor TSP (та же реализация что на mobile), строим
 * Яндекс deep-link и открываем в новой вкладке.
 *
 * Архив фильтруем на клиенте (как в F9/F10) — backend пока не
 * поддерживает ?status= параметр.
 */
export function RoutePage() {
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const query = useQuery({
    queryKey: ['route-candidates'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
  });

  const candidates = useMemo(() => {
    return (query.data?.items ?? []).filter(
      (item) =>
        item.status !== TrackStatuses.Archived &&
        item.hasGraveLocation &&
        typeof item.graveLatitude === 'number' &&
        typeof item.graveLongitude === 'number',
    );
  }, [query.data]);

  function toggle(deceasedId: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(deceasedId)) next.delete(deceasedId);
      else next.add(deceasedId);
      return next;
    });
  }

  /**
   * F14.2. Строим multi-point маршрут и синхронно (в обработчике клика!)
   * открываем Яндекс Карты. origin=null — Яндекс покажет панель маршрута
   * с пустым «Откуда», юзер выберет «Моё местоположение» (нативное
   * определение точнее браузерного и не требует ~20 c ожидания трёх
   * попыток geolocation). Порядок объезда оптимизируем nearest-neighbor
   * от первой точки.
   *
   * Синхронное открытие обязательно: после await popup-блокировщик молча
   * режет новую вкладку — «ничего не происходит».
   */
  function handleBuildRoute() {
    const points: Point[] = candidates
      .filter((c) => selected.has(c.deceasedId))
      .map((c) => ({
        id: c.deceasedId,
        latitude: c.graveLatitude!,
        longitude: c.graveLongitude!,
        label: c.fullName,
      }));

    if (points.length === 0) return;

    const ordered = optimizeOrder(null, points);
    const url = buildYandexUrl(null, ordered);
    openYandexRoute(url);
  }

  const selectedCount = selected.size;

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Маршрут</TitleLabel>
        <CaptionLabel>
          Выберите тех, к кому хотите проехать сегодня. Мы построим
          оптимальный порядок объезда и откроем маршрут в Яндекс Картах.
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

      {!query.isLoading && candidates.length === 0 && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Некого вести в маршрут</SubTitleLabel>
            <BodyLabel>
              В вашем списке отслеживаемых нет карточек с указанными
              координатами места захоронения. Откройте карточку и
              добавьте координаты, чтобы она появилась здесь.
            </BodyLabel>
          </Stack>
        </CloudCard>
      )}

      {candidates.map((item) => (
        <CandidateRow
          key={item.deceasedId}
          fullName={item.fullName}
          subtitle={`${relationshipDisplay(item.relationshipType)} · † ${formatDateOnly(item.deathDate)}`}
          photoUrl={item.mainPhotoUrl}
          checked={selected.has(item.deceasedId)}
          onToggle={() => toggle(item.deceasedId)}
        />
      ))}

      {candidates.length > 0 && (
        <PrimaryButton
          leftSection={<RouteIcon size={16} />}
          disabled={selectedCount === 0}
          onClick={handleBuildRoute}
          fullWidth
        >
          {selectedCount === 0
            ? 'Выберите хотя бы одного умершего'
            : `Построить маршрут (${selectedCount})`}
        </PrimaryButton>
      )}
    </Stack>
  );
}

function CandidateRow({
  fullName,
  subtitle,
  photoUrl,
  checked,
  onToggle,
}: {
  fullName: string;
  subtitle: string;
  photoUrl: string | null;
  checked: boolean;
  onToggle: () => void;
}) {
  return (
    <UnstyledButton
      onClick={onToggle}
      style={{ display: 'block', width: '100%', textAlign: 'left' }}
    >
      <CloudCard
        style={{
          cursor: 'pointer',
          borderColor: checked ? cloudColors.azure : cloudColors.cloudBorder,
          background: checked ? cloudColors.sky : undefined,
        }}
      >
        <Group align="center" gap="md" wrap="nowrap">
          <Checkbox
            checked={checked}
            onChange={() => onToggle()}
            color="azure"
            // Сам клик по Card уже обрабатывает UnstyledButton — но
            // Checkbox внутри по умолчанию stopPropagation НЕ делает,
            // и двойного toggle нет потому что мы используем onChange
            // только как fallback для accessibility-юзеров (Tab+Space).
          />
          <AuthAvatar src={photoUrl} size={56} />
          <Stack gap={4} style={{ flex: 1, minWidth: 0 }}>
            <SubTitleLabel>{fullName}</SubTitleLabel>
            <CaptionLabel>{subtitle}</CaptionLabel>
          </Stack>
        </Group>
      </CloudCard>
    </UnstyledButton>
  );
}

