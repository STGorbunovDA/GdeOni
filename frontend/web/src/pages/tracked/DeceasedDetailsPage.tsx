import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Loader,
  Modal,
  Stack,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Archive,
  ChevronLeft,
  Edit,
  HelpCircle,
  MapPin,
  Route as RouteIcon,
  RotateCcw,
  UserRound,
} from 'lucide-react';
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
} from '../../api/endpoints/trackedDeceasedApi';
import { routingApi } from '../../api/endpoints/routingApi';
import { requestGeolocationOnce } from '../../utils/requestGeolocation';
import { buildYandexLookupUrl } from '../../utils/routing';
import { formatError } from '../../auth/errorMessages';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly } from '../../utils/formatDate';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { MemoriesSection } from './MemoriesSection';
import { MediaSection } from './MediaSection';

/**
 * F11. Карточка умершего (~ E9 на mobile). Страница `/tracked/:deceasedId`.
 *
 * Зеркало DeceasedDetailsViewModel + DeceasedDetailsPage.xaml на mobile.
 * Грузит детали через trackedDeceasedApi.getDetails — там сразу и
 * Deceased (биография, место, фото), и Tracking (моё отношение, заметки,
 * уведомления, status).
 *
 * Действия в шапке:
 *  - Архивировать / Восстановить — PATCH со всеми tracking-полями (как F10).
 *  - "Изменить" — только админу (D26/F11.1, обычный юзер шлёт в поддержку).
 *
 * Блоки "Воспоминания" (F12) и "Фото/Документы" (F13) — заглушки;
 * по плану они отдельные F-блоки.
 */
export function DeceasedDetailsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();
  const isAdmin = useIsAdmin();
  const currentUserId = useAuthStore((s) => s.user?.id);
  const features = useAppFeatures();
  const [archiveError, setArchiveError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ['tracked-details', id],
    queryFn: () => trackedDeceasedApi.getDetails(id!),
    enabled: !!id,
  });

  /**
   * Архивирование/восстановление через PATCH. На бэке UpdateTracking
   * принимает все поля сразу — берём актуальные из tracking, меняем
   * только trackStatus (тот же паттерн что F10 restore).
   */
  const statusMutation = useMutation({
    mutationFn: async (newStatus: 'Active' | 'Archived') => {
      const t = query.data!.tracking;
      await trackedDeceasedApi.update(id!, {
        relationshipType: t.relationshipType,
        personalNotes: t.personalNotes,
        notifyOnDeathAnniversary: t.notifyOnDeathAnniversary,
        notifyOnBirthAnniversary: t.notifyOnBirthAnniversary,
        trackStatus: newStatus,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-archive'] });
      setArchiveError(null);
    },
    onError: (err) => setArchiveError(formatError(err)),
  });

  const [confirmArchive, setConfirmArchive] = useState(false);

  const [routeFallbackInfo, setRouteFallbackInfo] = useState<string | null>(null);

  /**
   * F14.1. Single-point маршрут к одной могиле. Сначала пробуем
   * geolocation → бэк отдаёт deep-link с маршрутом. Если geolocation
   * упала (в России Chrome/Yandex часто не могут добраться до Google
   * Geolocation Service на desktop без GPS-чипа) — fallback: открываем
   * Яндекс центрированный на могиле, юзер сам поставит "откуда".
   *
   * Provider — только Яндекс (решение 2026-05-13).
   */
  const routeMutation = useMutation({
    mutationFn: async () => {
      setRouteFallbackInfo(null);
      const d = query.data!.deceased;

      let url: string;
      try {
        const origin = await requestGeolocationOnce();
        const route = await routingApi.singleRoute(
          id!,
          origin.latitude,
          origin.longitude,
          'auto',
        );
        const yandex = route.links.find((l) => l.provider.toLowerCase() === 'yandex');
        if (!yandex) throw new Error('Бэк не отдал ссылку на Яндекс Карты.');
        url = yandex.url;
      } catch {
        // Fallback: открываем Яндекс с pin на могиле. Маршрут юзер
        // дорисует в самих Яндекс Картах (там точное определение
        // местоположения работает лучше браузерной геолокации).
        if (
          typeof d.latitude !== 'number' ||
          typeof d.longitude !== 'number'
        ) {
          throw new Error('У карточки нет координат места захоронения.');
        }
        url = buildYandexLookupUrl({
          id: id!,
          latitude: d.latitude,
          longitude: d.longitude,
        });
        setRouteFallbackInfo(
          'Не удалось определить ваше местоположение. Открыли Яндекс Карты на месте захоронения — нажмите там «Определить Ваше место положение», чтобы построить путь.',
        );
      }

      const link = document.createElement('a');
      link.href = url;
      link.target = '_blank';
      link.rel = 'noopener';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    },
  });

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate('/tracked')} />
        <Alert color="red" variant="light">
          Некорректный идентификатор карточки.
        </Alert>
      </Stack>
    );
  }

  if (query.isLoading) {
    return (
      <Stack align="center" py="xl">
        <Loader color="azure" />
      </Stack>
    );
  }

  if (query.isError || !query.data) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate(-1)} />
        <Alert color="red" variant="light">
          {query.error
            ? formatError(query.error)
            : 'Карточка не найдена.'}
        </Alert>
      </Stack>
    );
  }

  const { deceased, tracking } = query.data;
  const photoUrl = buildMediaUrl(
    features.data?.mediaBaseUrl,
    deceased.mainPhotoBucket,
    deceased.mainPhotoStorageKey,
  );
  const lifePeriod = `${deceased.birthDate ? formatDateOnly(deceased.birthDate) : '?'} — ${formatDateOnly(deceased.deathDate)}`;
  const isArchived = tracking.status === TrackStatuses.Archived;
  // Кнопка "Поправить координаты" — только автору карточки или
  // админу. На бэке тот же бар: PUT burial-location требует ICanEditDeceasedPolicy,
  // 403 для остальных. Здесь скрываем UI, чтобы не сбивать с толку
  // заведомо нерабочей кнопкой.
  const canEditCoordinates = isAdmin || currentUserId === deceased.createdByUserId;

  const locationText =
    [deceased.country, deceased.city, deceased.cemeteryName]
      .filter(Boolean)
      .join(', ') || null;
  const plotInfo = buildPlotInfo(deceased.plotNumber, deceased.graveNumber);
  const coordinatesText =
    typeof deceased.latitude === 'number' &&
    typeof deceased.longitude === 'number'
      ? `${deceased.latitude.toFixed(6)}, ${deceased.longitude.toFixed(6)}`
      : null;

  /**
   * Для F11 "Написать в поддержку" — query-string зеркало mobile:
   * SupportNewPage (F33) подставит шаблон Description с ID карточки.
   */
  const supportLink = `/support/new?deceasedId=${id}&deceasedFullName=${encodeURIComponent(deceased.fullName)}&deceasedLifePeriod=${encodeURIComponent(lifePeriod)}`;

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start" wrap="wrap">
        <BackButton onClick={() => navigate('/tracked')} />
        <Group gap="sm">
          {isAdmin && (
            <GhostButton
              leftSection={<Edit size={16} />}
              onClick={() => navigate(`/admin/deceased/${id}/edit`)}
            >
              Изменить
            </GhostButton>
          )}
          {isArchived ? (
            <PrimaryButton
              leftSection={<RotateCcw size={16} />}
              loading={statusMutation.isPending}
              onClick={() => statusMutation.mutate('Active')}
            >
              Восстановить
            </PrimaryButton>
          ) : (
            <GhostButton
              leftSection={<Archive size={16} />}
              onClick={() => setConfirmArchive(true)}
            >
              В архив
            </GhostButton>
          )}
        </Group>
      </Group>

      {archiveError && (
        <Alert color="red" variant="light">
          {archiveError}
        </Alert>
      )}

      {/* ---------- Hero ---------- */}
      <Stack align="center" gap="xs">
        <Avatar url={photoUrl} />
        <TitleLabel>{deceased.fullName}</TitleLabel>
        <CaptionLabel>{lifePeriod}</CaptionLabel>
        {deceased.isVerified && (
          <CaptionLabel c={cloudColors.azureDeep}>
            ✓ Карточка верифицирована
          </CaptionLabel>
        )}
        {isArchived && (
          <CaptionLabel c={cloudColors.azureDeep}>(в архиве)</CaptionLabel>
        )}
      </Stack>

      {/* ---------- Кратко ---------- */}
      {deceased.shortDescription && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Кратко</SubTitleLabel>
            <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
              {deceased.shortDescription}
            </BodyLabel>
          </Stack>
        </CloudCard>
      )}

      {/* ---------- Биография ---------- */}
      {deceased.biography && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Биография</SubTitleLabel>
            <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
              {deceased.biography}
            </BodyLabel>
          </Stack>
        </CloudCard>
      )}

      {/* ---------- Место захоронения ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Место захоронения</SubTitleLabel>
          {locationText ? (
            <BodyLabel>{locationText}</BodyLabel>
          ) : (
            <CaptionLabel>Адрес не указан.</CaptionLabel>
          )}
          {plotInfo && <BodyLabel>{plotInfo}</BodyLabel>}
          {coordinatesText ? (
            <CaptionLabel>Координаты: {coordinatesText}</CaptionLabel>
          ) : (
            <CaptionLabel>Координат нет.</CaptionLabel>
          )}
          {routeMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(routeMutation.error)}
            </Alert>
          )}
          {routeFallbackInfo && !routeMutation.isPending && (
            <Alert color="yellow" variant="light">
              {routeFallbackInfo}
            </Alert>
          )}
          <Group gap="sm" wrap="wrap">
            <PrimaryButton
              leftSection={<RouteIcon size={16} />}
              disabled={!deceased.hasBurialLocation}
              loading={routeMutation.isPending}
              onClick={() => routeMutation.mutate()}
            >
              Построить маршрут
            </PrimaryButton>
            {canEditCoordinates && (
              <GhostButton
                leftSection={<MapPin size={16} />}
                onClick={() => navigate(`/tracked/${id}/edit-coords`)}
              >
                Поправить координаты
              </GhostButton>
            )}
          </Group>
        </Stack>
      </CloudCard>

      {/* ---------- Ваше отслеживание ---------- */}
      <CloudCard>
        <Stack gap="xs">
          <SubTitleLabel>Ваше отслеживание</SubTitleLabel>
          <BodyLabel>
            Отношение: {relationshipDisplay(tracking.relationshipType)}
          </BodyLabel>
          {tracking.personalNotes && (
            <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
              Заметки: {tracking.personalNotes}
            </BodyLabel>
          )}
          <CaptionLabel>
            Напоминать в день смерти:{' '}
            {tracking.notifyOnDeathAnniversary ? 'да' : 'нет'}
          </CaptionLabel>
          {deceased.birthDate && (
            <CaptionLabel>
              Напоминать в день рождения:{' '}
              {tracking.notifyOnBirthAnniversary ? 'да' : 'нет'}
            </CaptionLabel>
          )}
        </Stack>
      </CloudCard>

      {/* ---------- Воспоминания (F12) ---------- */}
      <MemoriesSection deceasedId={id} memories={deceased.memories} />

      {/* ---------- Фото / Документы (F13) ---------- */}
      <MediaSection deceasedId={id} />

      {/* ---------- Помощь по карточке ---------- */}
      <CloudCard>
        <Stack gap="xs">
          <SubTitleLabel>Нужна помощь по этой карточке?</SubTitleLabel>
          <BodyLabel>
            Если нашли ошибку или хотите дополнить — напишите в поддержку.
            Админ увидит карточку и поможет с правкой.
          </BodyLabel>
          <Group>
            <GhostButton
              leftSection={<HelpCircle size={16} />}
              onClick={() => navigate(supportLink)}
            >
              Написать в поддержку
            </GhostButton>
          </Group>
        </Stack>
      </CloudCard>

      {/* ---------- Confirm archive ---------- */}
      <Modal
        opened={confirmArchive}
        onClose={() => !statusMutation.isPending && setConfirmArchive(false)}
        title="Перенести в архив?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Карточка пропадёт из основного списка и переедет во вкладку
            «Архив». Из архива её можно вернуть в любой момент.
          </BodyLabel>
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmArchive(false)}
              disabled={statusMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="azure"
              onClick={() => {
                setConfirmArchive(false);
                statusMutation.mutate('Archived');
              }}
              loading={statusMutation.isPending}
            >
              В архив
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

function buildPlotInfo(
  plotNumber: string | null,
  graveNumber: string | null,
): string | null {
  const parts: string[] = [];
  if (plotNumber) parts.push(`уч. ${plotNumber}`);
  if (graveNumber) parts.push(`могила № ${graveNumber}`);
  return parts.length > 0 ? parts.join(' · ') : null;
}

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <GhostButton leftSection={<ChevronLeft size={16} />} onClick={onClick}>
      Назад
    </GhostButton>
  );
}

/**
 * Круглая hero-аватарка 140×140 — больше чем 96×96 из плана, чтобы
 * соответствовать F7-preview по визуальной массе. UserRound из lucide
 * вместо 🕊 (тот же повод что в SearchPage/PreviewPage).
 */
function Avatar({ url }: { url: string | null }) {
  const [failed, setFailed] = useState(false);
  const show = url && !failed;

  return (
    <div
      style={{
        width: 140,
        height: 140,
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
          width={140}
          height={140}
          style={{ objectFit: 'cover', display: 'block' }}
          onError={() => setFailed(true)}
        />
      ) : (
        <UserRound size={64} strokeWidth={1.5} />
      )}
    </div>
  );
}
