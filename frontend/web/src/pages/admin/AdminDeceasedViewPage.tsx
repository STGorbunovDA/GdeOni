import { useState } from 'react';
import { Alert, Badge, Button, Group, Loader, Modal, Stack } from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  ChevronLeft,
  Eye,
  EyeOff,
  History,
  MapPin,
  Route as RouteIcon,
  ShieldCheck,
  ShieldOff,
  Trash2,
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
import { deceasedApi, type DeceasedMemory } from '../../api/endpoints/deceasedApi';
import { memoriesApi } from '../../api/endpoints/memoriesApi';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly, formatDateTime } from '../../utils/formatDate';
import { buildYandexLookupUrl, openYandexRoute } from '../../utils/routing';
import { formatError } from '../../auth/errorMessages';
import { MediaSection } from '../tracked/MediaSection';

/**
 * F17.1. Admin-view карточки умершего. Открывается с
 * /admin/deceased по клику на строку.
 *
 * Использует публичный GET /api/deceased-records/{id} — без
 * tracking-гейта, который был у /api/users/me/tracked-deceased/{id}
 * и валился "Current user does not track this deceased" для админа,
 * не подписанного на эту карточку.
 *
 * Воспоминания показываются read-only (модерация — F17.4 отдельно).
 * Media-галерея переиспользует MediaSection (D26: admin имеет полный
 * upload/delete доступ).
 */
export function AdminDeceasedViewPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const features = useAppFeatures();
  const { id } = useParams<{ id: string }>();
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Построить маршрут к могиле, НЕ добавляя карточку в свой tracked-список.
  // Открываем Яндекс синхронно (в обработчике клика) — иначе popup-
  // блокировщик режет вкладку. «Откуда» пустой: админ жмёт «Моё
  // местоположение» в самих Картах (нативное определение точнее).
  function handleBuildRoute(latitude: number, longitude: number) {
    const url = buildYandexLookupUrl({ id: id ?? '', latitude, longitude });
    openYandexRoute(url);
  }

  const query = useQuery({
    queryKey: ['admin-deceased-details', id],
    queryFn: () => deceasedApi.getById(id!),
    enabled: !!id,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deceasedApi.remove(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-deceased'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      queryClient.invalidateQueries({ queryKey: ['route-candidates'] });
      navigate('/admin/deceased');
    },
  });

  // F17.3. Toggle verify — без confirm-модали (обратимое действие).
  const toggleVerifyMutation = useMutation({
    mutationFn: () =>
      query.data?.isVerified
        ? deceasedApi.unverify(id!)
        : deceasedApi.verify(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-deceased'] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
  });

  // F17.4. Reject memory — модераторское «Скрыть» прямо со страницы
  // admin-view. Memories здесь отрисованы inline (read-only), поэтому
  // mutation живёт в самой странице, а не в переиспользуемой
  // MemoriesSection (которая тащит ещё Edit/Delete для author'а).
  const [pendingRejectMemory, setPendingRejectMemory] =
    useState<DeceasedMemory | null>(null);
  const rejectMemoryMutation = useMutation({
    mutationFn: (memoryId: string) => memoriesApi.reject(id!, memoryId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      setPendingRejectMemory(null);
    },
  });

  // F17.4. Восстановление скрытого воспоминания обратно в Approved.
  // Без confirm-модали — обратимое действие.
  const approveMemoryMutation = useMutation({
    mutationFn: (memoryId: string) => memoriesApi.approve(id!, memoryId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
    },
  });

  if (!id) {
    return (
      <Stack gap="lg">
        <BackButton onClick={() => navigate('/admin/deceased')} />
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
        <BackButton onClick={() => navigate('/admin/deceased')} />
        <Alert color="red" variant="light">
          {query.error ? formatError(query.error) : 'Карточка не найдена.'}
        </Alert>
      </Stack>
    );
  }

  const d = query.data;
  const photoUrl = buildMediaUrl(
    features.data?.mediaBaseUrl,
    d.mainPhotoBucket,
    d.mainPhotoStorageKey,
  );
  const hasBurial =
    d.hasBurialLocation || d.country || d.city || d.cemeteryName;

  return (
    <Stack gap="lg">
      <Group justify="space-between" wrap="wrap">
        <BackButton onClick={() => navigate('/admin/deceased')} />
        <Group gap="sm">
          <GhostButton
            leftSection={<History size={16} />}
            onClick={() => navigate(`/admin/deceased/${id}/edits`)}
          >
            История правок
          </GhostButton>
          <GhostButton
            leftSection={
              d.isVerified ? <ShieldOff size={16} /> : <ShieldCheck size={16} />
            }
            onClick={() => toggleVerifyMutation.mutate()}
            loading={toggleVerifyMutation.isPending}
          >
            {d.isVerified ? 'Снять подтверждение' : 'Подтвердить'}
          </GhostButton>
          <GhostButton
            leftSection={<Trash2 size={16} />}
            onClick={() => setConfirmDelete(true)}
            style={{ color: cloudColors.errorRed }}
          >
            Удалить карточку
          </GhostButton>
        </Group>
      </Group>

      {toggleVerifyMutation.isError && (
        <Alert color="red" variant="light">
          {formatError(toggleVerifyMutation.error)}
        </Alert>
      )}

      <Stack align="center" gap="md">
        <Avatar url={photoUrl} />
        <Stack gap={4} align="center">
          <TitleLabel>{d.fullName}</TitleLabel>
          <CaptionLabel>
            {d.birthDate ? formatDateOnly(d.birthDate) : '?'} —{' '}
            {formatDateOnly(d.deathDate)}
          </CaptionLabel>
          {d.isVerified && (
            <CaptionLabel c={cloudColors.azureDeep}>
              ✓ Карточка верифицирована
            </CaptionLabel>
          )}
        </Stack>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Аудит</SubTitleLabel>
          <Field label="Создана" value={formatDateTime(d.createdAtUtc)} />
          {d.updatedAtUtc && (
            <Field label="Обновлена" value={formatDateTime(d.updatedAtUtc)} />
          )}
          <Field label="Автор (userId)" value={d.createdByUserId} />
        </Stack>
      </CloudCard>

      {(d.shortDescription || d.biography) && (
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>О человеке</SubTitleLabel>
            {d.shortDescription && (
              <Field label="Кратко" value={d.shortDescription} />
            )}
            {d.biography && <Field label="Биография" value={d.biography} />}
          </Stack>
        </CloudCard>
      )}

      {hasBurial && (
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Место захоронения</SubTitleLabel>
            {d.country && <Field label="Страна" value={d.country} />}
            {d.city && <Field label="Город" value={d.city} />}
            {d.cemeteryName && (
              <Field label="Кладбище" value={d.cemeteryName} />
            )}
            {(d.plotNumber || d.graveNumber) && (
              <Field
                label="Участок / могила"
                value={[d.plotNumber, d.graveNumber].filter(Boolean).join(' / ')}
              />
            )}
            {typeof d.latitude === 'number' &&
              typeof d.longitude === 'number' && (
                <>
                  <Group gap={6}>
                    <MapPin size={16} color={cloudColors.azureDeep} />
                    <BodyLabel>
                      {d.latitude.toFixed(6)}, {d.longitude.toFixed(6)}
                      {typeof d.accuracyMeters === 'number' &&
                        ` (±${Math.round(d.accuracyMeters)} м)`}
                    </BodyLabel>
                  </Group>
                  <Group>
                    <PrimaryButton
                      leftSection={<RouteIcon size={16} />}
                      onClick={() =>
                        handleBuildRoute(d.latitude!, d.longitude!)
                      }
                    >
                      Построить маршрут
                    </PrimaryButton>
                  </Group>
                </>
              )}
          </Stack>
        </CloudCard>
      )}

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Воспоминания</SubTitleLabel>
          {d.memories.length === 0 && (
            <BodyLabel>Пока никто ничего не написал.</BodyLabel>
          )}
          {d.memories.map((m) => {
            const isMemoryRejected = m.moderationStatus === 'Rejected';
            return (
              <CloudCard
                key={m.id}
                style={{
                  background: isMemoryRejected ? '#FFFBEB' : cloudColors.cloud,
                  padding: 12,
                  border: isMemoryRejected ? '1px solid #F5C462' : undefined,
                  opacity: isMemoryRejected ? 0.85 : 1,
                }}
              >
                <Stack gap={4}>
                  <Group gap="xs">
                    <CaptionLabel c={cloudColors.azureDeep}>
                      {m.authorName ?? 'Аноним'}
                    </CaptionLabel>
                    {isMemoryRejected && (
                      <Badge color="yellow" variant="light" size="sm">
                        Скрыто
                      </Badge>
                    )}
                  </Group>
                  <BodyLabel>{m.text}</BodyLabel>
                  <Group justify="space-between" align="center">
                    <CaptionLabel>
                      {formatDateTime(m.createdAtUtc)}
                      {m.updatedAtUtc ? ' · отредактировано' : ''}
                    </CaptionLabel>
                    {isMemoryRejected ? (
                      <Button
                        variant="subtle"
                        color="green"
                        size="xs"
                        leftSection={<Eye size={14} />}
                        loading={
                          approveMemoryMutation.isPending &&
                          approveMemoryMutation.variables === m.id
                        }
                        onClick={() => approveMemoryMutation.mutate(m.id)}
                      >
                        Восстановить
                      </Button>
                    ) : (
                      <Button
                        variant="subtle"
                        color="yellow"
                        size="xs"
                        leftSection={<EyeOff size={14} />}
                        onClick={() => setPendingRejectMemory(m)}
                      >
                        Скрыть
                      </Button>
                    )}
                  </Group>
                </Stack>
              </CloudCard>
            );
          })}
          {approveMemoryMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(approveMemoryMutation.error)}
            </Alert>
          )}
        </Stack>
      </CloudCard>

      <MediaSection deceasedId={id} />

      {/* F17.4. Confirm reject memory. */}
      <Modal
        opened={pendingRejectMemory !== null}
        onClose={() =>
          !rejectMemoryMutation.isPending && setPendingRejectMemory(null)
        }
        title="Скрыть воспоминание?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            Воспоминание будет скрыто от всех, кроме автора и
            администраторов. Сама запись сохранится для аудита.
          </BodyLabel>
          {rejectMemoryMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(rejectMemoryMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setPendingRejectMemory(null)}
              disabled={rejectMemoryMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="yellow"
              onClick={() =>
                pendingRejectMemory &&
                rejectMemoryMutation.mutate(pendingRejectMemory.id)
              }
              loading={rejectMemoryMutation.isPending}
            >
              Скрыть
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={confirmDelete}
        onClose={() => !deleteMutation.isPending && setConfirmDelete(false)}
        title="Удалить карточку"
        centered
      >
        <Stack gap="md">
          <BodyLabel>
            Удалить карточку <b>{d.fullName}</b> безвозвратно? Вместе с
            карточкой пропадут все воспоминания, фото и записи об
            отслеживании у всех пользователей.
          </BodyLabel>
          {deleteMutation.isError && (
            <Alert color="red" variant="light">
              {formatError(deleteMutation.error)}
            </Alert>
          )}
          <Group justify="flex-end">
            <GhostButton
              onClick={() => setConfirmDelete(false)}
              disabled={deleteMutation.isPending}
            >
              Отмена
            </GhostButton>
            <PrimaryButton
              onClick={() => deleteMutation.mutate()}
              loading={deleteMutation.isPending}
              style={{ background: cloudColors.errorRed }}
            >
              Удалить
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <CaptionLabel>{label}</CaptionLabel>
      <BodyLabel>{value}</BodyLabel>
    </Stack>
  );
}

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <GhostButton leftSection={<ChevronLeft size={16} />} onClick={onClick}>
      Назад
    </GhostButton>
  );
}

function Avatar({ url }: { url: string | null }) {
  return (
    <div
      style={{
        width: 120,
        height: 120,
        borderRadius: '50%',
        background: cloudColors.sky,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        color: cloudColors.azureDeep,
      }}
    >
      {url ? (
        <img
          src={url}
          alt=""
          width={120}
          height={120}
          style={{ objectFit: 'cover', display: 'block' }}
        />
      ) : (
        <UserRound size={56} strokeWidth={1.5} />
      )}
    </div>
  );
}
