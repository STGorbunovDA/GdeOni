import { useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  Pagination,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Archive, ChevronRight, Plus, UserRound } from 'lucide-react';
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
  trackedDeceasedApi,
  TrackStatuses,
  type TrackedDeceasedListItem,
} from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F9. Список отслеживаемых (E8 на mobile). Страница `/tracked`.
 *
 * Зеркало TrackedListViewModel + TrackedListPage.xaml:
 *  - GET /api/users/me/tracked-deceased через TanStack Query;
 *    refetchOnWindowFocus заменяет mobile pull-to-refresh.
 *  - Archived фильтруется на клиенте (как mobile) — для них F10.
 *  - Карточки кликабельны целиком → /tracked/:deceasedId (F11).
 *  - Шапка: "Добавить умершего" → /search (F6, анти-дубликат),
 *    "Архив" → /tracked/archive (F10).
 */

const PAGE_SIZE = 20;

export function TrackedListPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const features = useAppFeatures();

  const query = useQuery({
    queryKey: ['tracked-list', page],
    queryFn: () => trackedDeceasedApi.list(page, PAGE_SIZE),
    refetchOnWindowFocus: true,
    placeholderData: (prev) => prev,
  });

  // Archived не показываем — для них отдельный экран /tracked/archive (F10).
  // Muted остаётся: это статус уведомлений, не отслеживания.
  const visibleItems = (query.data?.items ?? []).filter(
    (item) => item.status !== TrackStatuses.Archived,
  );

  const totalPages =
    query.data && query.data.pageSize > 0
      ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize))
      : 1;

  const showEmptyState =
    query.isSuccess && !query.isFetching && visibleItems.length === 0;

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start" wrap="wrap">
        <Stack gap="xs">
          <TitleLabel>Отслеживаемые</TitleLabel>
          <CaptionLabel>
            Здесь карточки умерших, за которыми вы следите.
          </CaptionLabel>
        </Stack>
        <Group gap="sm">
          <PrimaryButton
            onClick={() => navigate('/search')}
            leftSection={<Plus size={16} />}
          >
            Добавить умершего
          </PrimaryButton>
          <GhostButton
            onClick={() => navigate('/tracked/archive')}
            leftSection={<Archive size={16} />}
          >
            Архив
          </GhostButton>
        </Group>
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
          onClick={() => navigate(`/tracked/${item.deceasedId}`)}
        />
      ))}

      {showEmptyState && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Пока никого не отслеживаете</SubTitleLabel>
            <BodyLabel>
              Нажмите «Добавить умершего» вверху, чтобы найти карточку в
              базе или создать новую у могилы.
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
    </Stack>
  );
}

/**
 * Карточка одного отслеживаемого. Кликабельна целиком (тот же паттерн,
 * что в SearchPage.ResultCard). Аватарка 56×56, под именем —
 * "relationship · † dd.mm.yyyy", шеврон справа.
 */
function TrackedCard({
  item,
  mediaBaseUrl,
  onClick,
}: {
  item: TrackedDeceasedListItem;
  mediaBaseUrl: string | undefined;
  onClick: () => void;
}) {
  const [hovered, setHovered] = useState(false);
  const photoUrl = buildMediaUrl(
    mediaBaseUrl,
    item.mainPhotoBucket,
    item.mainPhotoStorageKey,
  );
  const subtitle = `${relationshipDisplay(item.relationshipType)} · † ${formatDateOnly(item.deathDate)}`;

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
        <Group align="center" gap="md" wrap="nowrap">
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
          <ChevronRight size={24} color={cloudColors.azure} />
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
