import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Loader,
  Modal,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  RotateCcw,
  Trash2,
  UserRound,
} from 'lucide-react';
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
import { formatError } from '../../auth/errorMessages';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F10. Архив отслеживаемых (~ E9.1 + E17.2). Страница `/tracked/archive`.
 *
 * Зеркало mobile ArchivePage + ArchiveViewModel (вариант A,
 * подтверждённый юзером 2026-06-16):
 *  - Восстановление НЕ делается прямо отсюда — клик на карточку
 *    ведёт на F11 (DeceasedDetailsPage), где есть кнопка "Восстановить".
 *  - Прямо на карточке есть только кнопка "Удалить навсегда" → confirm
 *    modal → DELETE /api/users/me/tracked-deceased/{id}.
 *
 * Бэк (на 2026-06-16) НЕ поддерживает фильтр по статусу — отдаёт ВСЁ.
 * Поэтому грузим pageSize=100 и фильтруем Archived на клиенте, как
 * делает mobile.
 */
export function ArchivePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const features = useAppFeatures();
  const [pendingDelete, setPendingDelete] =
    useState<TrackedDeceasedListItem | null>(null);

  const query = useQuery({
    queryKey: ['tracked-archive'],
    queryFn: () => trackedDeceasedApi.list(1, 100),
  });

  const archivedItems = (query.data?.items ?? []).filter(
    (item) => item.status === TrackStatuses.Archived,
  );

  const deleteMutation = useMutation({
    mutationFn: (id: string) => trackedDeceasedApi.untrack(id),
    onSuccess: () => {
      // Обновляем и архив, и основной список — DELETE влияет на оба
      // (хотя в основном Archived и не показывался, но totalCount там
      // меняется, и при возврате юзера — должен быть актуальный).
      queryClient.invalidateQueries({ queryKey: ['tracked-archive'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      setPendingDelete(null);
    },
  });

  /**
   * Восстановление: бэк PATCH требует ВСЕ tracking-поля сразу,
   * иначе personalNotes/notify-флаги затрутся дефолтами. Поэтому
   * сначала GET getDetails, потом PATCH с status=Active поверх
   * актуальных полей.
   */
  const restoreMutation = useMutation({
    mutationFn: async (id: string) => {
      const details = await trackedDeceasedApi.getDetails(id);
      const t = details.tracking;
      await trackedDeceasedApi.update(id, {
        relationshipType: t.relationshipType,
        personalNotes: t.personalNotes,
        notifyOnDeathAnniversary: t.notifyOnDeathAnniversary,
        notifyOnBirthAnniversary: t.notifyOnBirthAnniversary,
        trackStatus: TrackStatuses.Active,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-archive'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
  });

  const showEmptyState =
    query.isSuccess && !query.isFetching && archivedItems.length === 0;

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/tracked')}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>Архив</TitleLabel>
        <CaptionLabel>
          Карточки, которые вы перенесли в архив. Их можно вернуть в
          основной список или удалить навсегда — кнопками справа.
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

      {deleteMutation.isError && (
        <Alert color="red" variant="light">
          {formatError(deleteMutation.error)}
        </Alert>
      )}

      {restoreMutation.isError && (
        <Alert color="red" variant="light">
          {formatError(restoreMutation.error)}
        </Alert>
      )}

      {archivedItems.map((item) => (
        <ArchivedCard
          key={item.trackingId}
          item={item}
          mediaBaseUrl={features.data?.mediaBaseUrl}
          onOpen={() => navigate(`/tracked/${item.deceasedId}`)}
          onRestore={() => restoreMutation.mutate(item.deceasedId)}
          onDelete={() => setPendingDelete(item)}
          isRestoring={
            restoreMutation.isPending &&
            restoreMutation.variables === item.deceasedId
          }
          isDeleting={
            deleteMutation.isPending &&
            pendingDelete?.trackingId === item.trackingId
          }
        />
      ))}

      {showEmptyState && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Архив пустой</SubTitleLabel>
            <BodyLabel>
              Здесь будут карточки, которые вы перенесёте в архив со страницы
              карточки умершего. Они не пропадут — можно вернуть.
            </BodyLabel>
          </Stack>
        </CloudCard>
      )}

      <Modal
        opened={pendingDelete !== null}
        onClose={() => !deleteMutation.isPending && setPendingDelete(null)}
        title="Удалить из отслеживания?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            <b>{pendingDelete?.fullName}</b> полностью пропадёт из вашего
            списка. Сама карточка умершего останется в системе — её сможет
            найти другой юзер через поиск.
          </BodyLabel>
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setPendingDelete(null)}
              disabled={deleteMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="red"
              onClick={() =>
                pendingDelete && deleteMutation.mutate(pendingDelete.deceasedId)
              }
              loading={deleteMutation.isPending}
            >
              Удалить навсегда
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

/**
 * Карточка в архиве. Тело кликабельно (открыть F11 — там кнопка
 * "Восстановить"), справа отдельная иконка-кнопка "Удалить навсегда".
 * Чтобы клик по иконке не открывал карточку — stopPropagation в onClick.
 *
 * Стиль приглушённый (opacity 0.85), как в mobile XAML — визуально
 * показывает "это не активный список".
 */
function ArchivedCard({
  item,
  mediaBaseUrl,
  onOpen,
  onRestore,
  onDelete,
  isRestoring,
  isDeleting,
}: {
  item: TrackedDeceasedListItem;
  mediaBaseUrl: string | undefined;
  onOpen: () => void;
  onRestore: () => void;
  onDelete: () => void;
  isRestoring: boolean;
  isDeleting: boolean;
}) {
  const [hovered, setHovered] = useState(false);
  const photoUrl = buildMediaUrl(
    mediaBaseUrl,
    item.mainPhotoBucket,
    item.mainPhotoStorageKey,
  );
  const subtitle = `${relationshipDisplay(item.relationshipType)} · † ${formatDateOnly(item.deathDate)}`;
  const busy = isRestoring || isDeleting;

  return (
    <UnstyledButton
      onClick={onOpen}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      disabled={busy}
      style={{
        display: 'block',
        width: '100%',
        textAlign: 'left',
        opacity: busy ? 0.5 : 0.85,
      }}
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
          <Button
            variant="subtle"
            color="azure"
            onClick={(e) => {
              e.stopPropagation();
              onRestore();
            }}
            leftSection={<RotateCcw size={16} />}
            loading={isRestoring}
            disabled={busy}
          >
            Восстановить
          </Button>
          <Button
            variant="subtle"
            color="red"
            onClick={(e) => {
              e.stopPropagation();
              onDelete();
            }}
            leftSection={<Trash2 size={16} />}
            disabled={busy}
          >
            Удалить
          </Button>
          <ChevronRight size={20} color={cloudColors.azureDeep} />
        </Group>
      </CloudCard>
    </UnstyledButton>
  );
}

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
