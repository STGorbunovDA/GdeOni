import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Loader,
  Modal,
  Select,
  Stack,
  Switch,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Archive,
  ChevronLeft,
  Edit,
  HelpCircle,
  MapPin,
  Route as RouteIcon,
  RotateCcw,
  ShieldCheck,
  ShieldOff,
  Trash2,
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
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { buildYandexLookupUrl, openYandexRoute } from '../../utils/routing';
import { formatError } from '../../auth/errorMessages';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { AuthAvatar } from '../../components/media/AuthAvatar';
import { formatDateOnly } from '../../utils/formatDate';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { RELATIONSHIP_OPTIONS } from '../../utils/relationshipOptions';
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
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Правка своего отслеживания: отношение, заметки и напоминания о годовщинах
  // (день смерти / день рождения) — включить/выключить прямо здесь.
  const [editOpen, setEditOpen] = useState(false);
  const [relInput, setRelInput] = useState<string>('');
  const [notesInput, setNotesInput] = useState('');
  const [notifyDeath, setNotifyDeath] = useState(true);
  const [notifyBirth, setNotifyBirth] = useState(false);

  function openEdit() {
    const t = query.data!.tracking;
    setRelInput(t.relationshipType);
    setNotesInput(t.personalNotes ?? '');
    setNotifyDeath(t.notifyOnDeathAnniversary);
    setNotifyBirth(t.notifyOnBirthAnniversary);
    setEditOpen(true);
  }

  const editTrackingMutation = useMutation({
    mutationFn: async () => {
      const t = query.data!.tracking;
      // Выкл → пустой набор дней (не напоминаем). Вкл → сохраняем прежние
      // «за сколько дней» (заданные в «Событиях»), а если их не было — «в день».
      const deathLead = notifyDeath
        ? t.deathAnniversaryLeadDays.length > 0
          ? t.deathAnniversaryLeadDays
          : [0]
        : [];
      const birthLead = notifyBirth
        ? t.birthAnniversaryLeadDays.length > 0
          ? t.birthAnniversaryLeadDays
          : [0]
        : [];
      await trackedDeceasedApi.update(id!, {
        relationshipType: relInput,
        personalNotes: notesInput.trim().length > 0 ? notesInput.trim() : null,
        notifyOnDeathAnniversary: notifyDeath,
        notifyOnBirthAnniversary: notifyBirth,
        deathAnniversaryLeadDays: deathLead,
        birthAnniversaryLeadDays: birthLead,
        trackStatus: t.status,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      // Синк с «Событиями» (страница + попап + модалка годовщин).
      queryClient.invalidateQueries({ queryKey: ['events-tracked'] });
      queryClient.invalidateQueries({ queryKey: ['events-popup-tracked'] });
      queryClient.invalidateQueries({ queryKey: ['anniversary-modal-tracked'] });
      // Отношение влияет на матчинг «Родственников» — обновим их кеши.
      queryClient.invalidateQueries({ queryKey: ['relatives'] });
      queryClient.invalidateQueries({ queryKey: ['relatives-summary'] });
      setEditOpen(false);
      notifications.show({ title: 'Сохранено', message: '', color: 'green' });
    },
    onError: (e) =>
      notifications.show({
        title: 'Не удалось сохранить',
        message: formatError(e),
        color: 'red',
      }),
  });

  // F17.2: удаление карточки (admin-only). После успеха ничего не
  // показываем на странице — карточки больше нет — и кидаем на
  // /tracked. Инвалидируем все кэши списков, чтобы у юзера, который
  // её трекал, она пропала из tracked-list тоже.
  const deleteMutation = useMutation({
    mutationFn: () => deceasedApi.remove(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-archive'] });
      queryClient.invalidateQueries({ queryKey: ['admin-deceased'] });
      queryClient.invalidateQueries({ queryKey: ['route-candidates'] });
      navigate('/tracked');
    },
  });

  // F17.3: verify/unverify (admin-only). Обратимое действие — без
  // confirm-модали. После успеха обновляем кэши, чтобы значок верификации
  // освежился в шапке и в админ-таблице.
  const toggleVerifyMutation = useMutation({
    mutationFn: () =>
      query.data?.deceased.isVerified
        ? deceasedApi.unverify(id!)
        : deceasedApi.verify(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-deceased'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
  });

  /**
   * F14.1. Маршрут к одной могиле. Синхронно (прямо в обработчике клика!)
   * открываем Яндекс Карты с заполненным «Куда» (координаты могилы) и
   * пустым «Откуда»: юзер жмёт «Моё местоположение» в самих Картах — их
   * нативное определение точнее браузерного (в России Chrome/Yandex часто
   * не достукиваются до Google Geolocation Service).
   *
   * Синхронное открытие обязательно: если открывать после await
   * геолокации/бэка, окно уже вне user-gesture и popup-блокировщик молча
   * режет новую вкладку — «ничего не происходит».
   *
   * Provider — только Яндекс (решение 2026-05-13).
   */
  function handleBuildRoute() {
    const d = query.data?.deceased;
    if (
      typeof d?.latitude !== 'number' ||
      typeof d?.longitude !== 'number'
    ) {
      return;
    }
    const url = buildYandexLookupUrl({
      id: id!,
      latitude: d.latitude,
      longitude: d.longitude,
    });
    openYandexRoute(url);
  }

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
          {isAdmin && (
            <GhostButton
              leftSection={
                deceased.isVerified
                  ? <ShieldOff size={16} />
                  : <ShieldCheck size={16} />
              }
              onClick={() => toggleVerifyMutation.mutate()}
              loading={toggleVerifyMutation.isPending}
            >
              {deceased.isVerified ? 'Снять подтверждение' : 'Подтвердить'}
            </GhostButton>
          )}
          {isAdmin && (
            <GhostButton
              leftSection={<Trash2 size={16} />}
              onClick={() => setConfirmDelete(true)}
              style={{ color: cloudColors.errorRed }}
            >
              Удалить
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

      {toggleVerifyMutation.isError && (
        <Alert color="red" variant="light">
          {formatError(toggleVerifyMutation.error)}
        </Alert>
      )}

      {/* ---------- Hero ---------- */}
      <Stack align="center" gap="xs">
        <AuthAvatar src={deceased.mainPhotoUrl} size={140} iconSize={64} />
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
          <Group gap="sm" wrap="wrap">
            <PrimaryButton
              leftSection={<RouteIcon size={16} />}
              disabled={!deceased.hasBurialLocation}
              onClick={handleBuildRoute}
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
          <Group justify="space-between" align="center">
            <SubTitleLabel>Ваше отслеживание</SubTitleLabel>
            <GhostButton leftSection={<Edit size={16} />} onClick={openEdit}>
              Изменить
            </GhostButton>
          </Group>
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

      {/* ---------- Изменить отслеживание (отношение + заметки) ---------- */}
      <Modal
        opened={editOpen}
        onClose={() => !editTrackingMutation.isPending && setEditOpen(false)}
        title="Изменить отслеживание"
        centered
        size="md"
      >
        <Stack gap="md">
          <Select
            label="Кем приходится"
            data={
              RELATIONSHIP_OPTIONS.some((o) => o.value === relInput) || !relInput
                ? RELATIONSHIP_OPTIONS
                : [
                    { value: relInput, label: relationshipDisplay(relInput) },
                    ...RELATIONSHIP_OPTIONS,
                  ]
            }
            value={relInput}
            onChange={(v) => v && setRelInput(v)}
            allowDeselect={false}
            comboboxProps={{ withinPortal: true }}
          />
          <Textarea
            label="Заметки (для себя)"
            placeholder="Например, участок, как ухаживать…"
            value={notesInput}
            onChange={(e) => setNotesInput(e.currentTarget.value)}
            autosize
            minRows={2}
            maxRows={6}
          />

          <Stack gap="xs">
            <BodyLabel>Напоминания о годовщинах</BodyLabel>
            <Switch
              color="azure"
              checked={notifyDeath}
              onChange={(e) => setNotifyDeath(e.currentTarget.checked)}
              label="Напоминать в день памяти (годовщина смерти)"
            />
            {deceased.birthDate && (
              <Switch
                color="azure"
                checked={notifyBirth}
                onChange={(e) => setNotifyBirth(e.currentTarget.checked)}
                label="Напоминать в день рождения"
              />
            )}
            <CaptionLabel>
              За сколько дней напоминать (в день / за день / за 3 / за неделю) —
              настраивается на вкладке «События».
            </CaptionLabel>
          </Stack>

          <Group justify="flex-end" gap="sm">
            <GhostButton
              onClick={() => setEditOpen(false)}
              disabled={editTrackingMutation.isPending}
            >
              Отмена
            </GhostButton>
            <PrimaryButton
              onClick={() => editTrackingMutation.mutate()}
              loading={editTrackingMutation.isPending}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>

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

      {/* F17.2: Confirm delete (admin-only). Видно только админу — кнопка
          в шапке скрыта для остальных, но модаль не помешает в DOM. */}
      <Modal
        opened={confirmDelete}
        onClose={() => !deleteMutation.isPending && setConfirmDelete(false)}
        title="Удалить карточку"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Удалить карточку <b>{deceased.fullName}</b> безвозвратно?
            Вместе с карточкой пропадут все воспоминания, фото и записи
            об отслеживании у всех пользователей.
          </BodyLabel>
          {deleteMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(deleteMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setConfirmDelete(false)}
              disabled={deleteMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              onClick={() => deleteMutation.mutate()}
              loading={deleteMutation.isPending}
              style={{ background: cloudColors.errorRed, color: 'white' }}
            >
              Удалить
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
