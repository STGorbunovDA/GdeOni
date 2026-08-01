import { useState } from 'react';
import {
  Alert,
  Button,
  Checkbox,
  Group,
  Loader,
  Pagination,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { ChevronRight, Navigation, Share2, UserRound } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  trackedDeceasedApi,
  TrackStatuses,
  type TrackedDeceasedListItem,
} from '../../api/endpoints/trackedDeceasedApi';
import { shareApi } from '../../api/endpoints/shareApi';
import { ShareQrModal } from '../../components/share/ShareQrModal';
import { formatError } from '../../auth/errorMessages';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F9. Список отслеживаемых (E8 на mobile). Страница `/tracked`.
 *
 * D46. Добавлен режим мультивыбора: кнопка «Поделиться» включает галочки,
 * отмеченные карточки уходят в подборку (короткая ссылка + QR), которую
 * получатель добавляет себе.
 */

const PAGE_SIZE = 20;

export function TrackedListPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const features = useAppFeatures();

  // D46. Режим «Поделиться»: галочки на карточках + подборка выбранных
  // (deceasedId). Выбор переживает смену страницы пагинации.
  const [selectMode, setSelectMode] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [shareUrl, setShareUrl] = useState<string | null>(null);
  const [shareCount, setShareCount] = useState(0);
  const [creating, setCreating] = useState(false);

  const query = useQuery({
    queryKey: ['tracked-list', page],
    queryFn: () => trackedDeceasedApi.list(page, PAGE_SIZE),
    refetchOnWindowFocus: true,
    placeholderData: (prev) => prev,
  });

  const visibleItems = (query.data?.items ?? []).filter(
    (item) => item.status !== TrackStatuses.Archived,
  );

  const totalPages =
    query.data && query.data.pageSize > 0
      ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize))
      : 1;

  const showEmptyState =
    query.isSuccess && !query.isFetching && visibleItems.length === 0;

  function toggleSelect(deceasedId: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(deceasedId)) next.delete(deceasedId);
      else next.add(deceasedId);
      return next;
    });
  }

  function exitSelectMode() {
    setSelectMode(false);
    setSelectedIds(new Set());
  }

  async function handleShare() {
    if (selectedIds.size === 0) return;
    setCreating(true);
    try {
      const res = await shareApi.create([...selectedIds]);
      setShareCount(selectedIds.size);
      setShareUrl(`${window.location.origin}/s/${res.code}`);
      exitSelectMode();
    } catch (e) {
      notifications.show({
        title: 'Не удалось создать ссылку',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setCreating(false);
    }
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start" wrap="wrap">
        <Stack gap="xs">
          <TitleLabel>Отслеживаемые</TitleLabel>
          <CaptionLabel>
            {selectMode
              ? 'Отметьте карточки, которыми хотите поделиться.'
              : 'Здесь карточки умерших, за которыми вы следите.'}
          </CaptionLabel>
        </Stack>

        {selectMode ? (
          <GhostButton onClick={exitSelectMode}>Отмена</GhostButton>
        ) : (
          <Group gap="xs" wrap="nowrap">
            <GhostButton
              onClick={() => navigate('/nearby')}
              leftSection={<Navigation size={16} />}
            >
              Найти рядом
            </GhostButton>
            <GhostButton
              onClick={() => setSelectMode(true)}
              leftSection={<Share2 size={16} />}
            >
              Поделиться
            </GhostButton>
          </Group>
        )}
      </Group>

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

      {visibleItems.map((item) => (
        <TrackedCard
          key={item.trackingId}
          item={item}
          mediaBaseUrl={features.data?.mediaBaseUrl}
          selectMode={selectMode}
          selected={selectedIds.has(item.deceasedId)}
          onOpen={() => navigate(`/tracked/${item.deceasedId}`)}
          onToggleSelect={() => toggleSelect(item.deceasedId)}
        />
      ))}

      {showEmptyState && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Пока никого не отслеживаете</SubTitleLabel>
            <BodyLabel>
              Найдите карточку через «Поиск» в меню слева или создайте новую
              через «Добавить умершего».
            </BodyLabel>
          </Stack>
        </CloudCard>
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

      {/* D46. Нижняя панель действия в режиме выбора. */}
      {selectMode && (
        <Group justify="center" mt="xs">
          <Button
            color="azure"
            radius="xl"
            size="md"
            fw={700}
            leftSection={<Share2 size={18} />}
            disabled={selectedIds.size === 0}
            loading={creating}
            onClick={handleShare}
          >
            {selectedIds.size === 0
              ? 'Выберите карточки'
              : `Поделиться выбранными (${selectedIds.size})`}
          </Button>
        </Group>
      )}

      <ShareQrModal
        url={shareUrl}
        count={shareCount}
        onClose={() => setShareUrl(null)}
      />
    </Stack>
  );
}

/**
 * Карточка одного отслеживаемого. Вне режима выбора кликабельна целиком
 * (→ детали). В режиме выбора клик переключает галочку.
 */
function TrackedCard({
  item,
  mediaBaseUrl,
  selectMode,
  selected,
  onOpen,
  onToggleSelect,
}: {
  item: TrackedDeceasedListItem;
  mediaBaseUrl: string | undefined;
  selectMode: boolean;
  selected: boolean;
  onOpen: () => void;
  onToggleSelect: () => void;
}) {
  const [hovered, setHovered] = useState(false);
  const photoUrl = buildMediaUrl(
    mediaBaseUrl,
    item.mainPhotoBucket,
    item.mainPhotoStorageKey,
  );
  const subtitle = `${relationshipDisplay(item.relationshipType)} · † ${formatDateOnly(item.deathDate)}`;

  const highlighted = selectMode && selected;

  return (
    <UnstyledButton
      onClick={selectMode ? onToggleSelect : onOpen}
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
          borderColor:
            highlighted || hovered ? cloudColors.azure : cloudColors.cloudBorder,
        }}
      >
        <Group align="center" gap="md" wrap="nowrap">
          {selectMode && (
            <Checkbox
              checked={selected}
              onChange={onToggleSelect}
              onClick={(e) => e.stopPropagation()}
              color="azure"
              aria-label="Выбрать карточку"
            />
          )}
          <Avatar url={photoUrl} />
          <Stack gap={4} style={{ flex: 1, minWidth: 0 }}>
            <Group gap={6} align="center">
              <SubTitleLabel>{item.fullName}</SubTitleLabel>
              {item.isVerified && (
                <CaptionLabel c={cloudColors.azureDeep}>✓</CaptionLabel>
              )}
            </Group>
            <CaptionLabel>{subtitle}</CaptionLabel>
          </Stack>
          {!selectMode && (
            <ChevronRight size={24} color={cloudColors.azure} />
          )}
        </Group>
      </CloudCard>
    </UnstyledButton>
  );
}

/**
 * Круглая 56×56 аватарка. UserRound из lucide вместо 🕊 — по той же
 * причине что в SearchPage/PreviewPage (color-emoji иногда не
 * рендерится в Яндекс.Браузере на Windows).
 */
function Avatar({ url }: { url: string | null }) {
  const [failed, setFailed] = useState(false);
  const show = url && !failed;

  return (
    <div
      style={{
        width: 56,
        height: 56,
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
      {show ? (
        <img
          src={url}
          alt=""
          width={56}
          height={56}
          style={{ objectFit: 'cover', display: 'block' }}
          onError={() => setFailed(true)}
        />
      ) : (
        <UserRound size={28} strokeWidth={1.5} />
      )}
    </div>
  );
}
