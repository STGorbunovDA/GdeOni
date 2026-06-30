import { Alert, Group, Loader, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, MapPin, UserRound } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatDateOnly, formatDateTime } from '../../utils/formatDate';
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
  const features = useAppFeatures();
  const { id } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ['admin-deceased-details', id],
    queryFn: () => deceasedApi.getById(id!),
    enabled: !!id,
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
      <Group>
        <BackButton onClick={() => navigate('/admin/deceased')} />
      </Group>

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
                <Group gap={6}>
                  <MapPin size={16} color={cloudColors.azureDeep} />
                  <BodyLabel>
                    {d.latitude.toFixed(6)}, {d.longitude.toFixed(6)}
                    {typeof d.accuracyMeters === 'number' &&
                      ` (±${Math.round(d.accuracyMeters)} м)`}
                  </BodyLabel>
                </Group>
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
          {d.memories.map((m) => (
            <CloudCard
              key={m.id}
              style={{ background: cloudColors.cloud, padding: 12 }}
            >
              <Stack gap={4}>
                <CaptionLabel c={cloudColors.azureDeep}>
                  {m.authorName ?? 'Аноним'}
                </CaptionLabel>
                <BodyLabel>{m.text}</BodyLabel>
                <CaptionLabel>
                  {formatDateTime(m.createdAtUtc)}
                  {m.updatedAtUtc ? ' · отредактировано' : ''}
                </CaptionLabel>
              </Stack>
            </CloudCard>
          ))}
        </Stack>
      </CloudCard>

      <MediaSection deceasedId={id} />
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
