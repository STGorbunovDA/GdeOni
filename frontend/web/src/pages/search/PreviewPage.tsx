import { useState } from 'react';
import { Group, Loader, Stack } from '@mantine/core';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, UserRound } from 'lucide-react';
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
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import {
  RelationshipTypes,
  trackedDeceasedApi,
} from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F7. Preview карточки умершего (E17.1 на mobile).
 *
 * Промежуточный экран между F6 (поиск) и F11 (полная подписанная карточка).
 * Юзер видит фото/ФИО/даты/место/биографию и решает "тот ли это умерший?".
 *
 *  - Если уже трекает (Active/Muted/Archived) → кнопка "Открыть мою
 *    карточку" → /tracked/:id (idempotent navigate).
 *  - Если не трекает → кнопка "Добавить в отслеживание" → POST /tracked
 *    с дефолтом Friend (как mobile), затем → /tracked/:id.
 *
 * Зеркало DeceasedPreviewViewModel + DeceasedPreviewPage.xaml на mobile.
 */
export function PreviewPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const features = useAppFeatures();

  const detailsQuery = useQuery({
    queryKey: ['deceased-details', id],
    queryFn: () => deceasedApi.getById(id!),
    enabled: !!id,
  });

  // exists проверяем отдельно: даже если он упадёт (404/500), preview
  // должен работать — текст кнопки просто будет "Добавить".
  // Поэтому ошибки этого запроса не показываем, fallback false.
  const existsQuery = useQuery({
    queryKey: ['deceased-is-tracked', id],
    queryFn: () => trackedDeceasedApi.isTracked(id!),
    enabled: !!id,
    retry: 0,
  });
  const isAlreadyTracked = existsQuery.data?.tracked === true;

  const trackMutation = useMutation({
    mutationFn: () =>
      trackedDeceasedApi.track(id!, {
        relationshipType: RelationshipTypes.Friend,
        personalNotes: null,
        notifyOnDeathAnniversary: false,
        notifyOnBirthAnniversary: false,
      }),
    onSuccess: () => navigate(`/tracked/${id}`),
  });

  function handlePrimaryAction() {
    if (isAlreadyTracked) {
      navigate(`/tracked/${id}`);
      return;
    }
    trackMutation.mutate();
  }

  if (!id) {
    return (
      <ErrorBlock onBack={() => navigate('/search')}>
        Некорректный идентификатор карточки.
      </ErrorBlock>
    );
  }

  if (detailsQuery.isLoading) {
    return (
      <Stack align="center" py="xl">
        <Loader color="azure" />
      </Stack>
    );
  }

  if (detailsQuery.isError || !detailsQuery.data) {
    return (
      <ErrorBlock onBack={() => navigate(-1)}>
        {detailsQuery.error
          ? formatError(detailsQuery.error)
          : 'Карточка не найдена.'}
      </ErrorBlock>
    );
  }

  const data = detailsQuery.data;
  const photoUrl = buildMediaUrl(
    features.data?.mediaBaseUrl,
    data.mainPhotoBucket,
    data.mainPhotoStorageKey,
  );
  const lifePeriod = `${data.birthDate ?? '?'} — ${data.deathDate}`;
  const location = [data.country, data.city, data.cemeteryName]
    .filter(Boolean)
    .join(', ');

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate(-1)}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack align="center" gap="xs">
        <Avatar url={photoUrl} />
        <TitleLabel>{data.fullName}</TitleLabel>
        <CaptionLabel>{lifePeriod}</CaptionLabel>
        {data.isVerified && (
          <CaptionLabel c={cloudColors.azureDeep}>
            ✓ верифицирован
          </CaptionLabel>
        )}
      </Stack>

      {data.shortDescription && (
        <CloudCard>
          <BodyLabel>{data.shortDescription}</BodyLabel>
        </CloudCard>
      )}

      {data.biography && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Биография</SubTitleLabel>
            <BodyLabel style={{ whiteSpace: 'pre-wrap' }}>
              {data.biography}
            </BodyLabel>
          </Stack>
        </CloudCard>
      )}

      {data.hasBurialLocation && (
        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Место захоронения</SubTitleLabel>
            <BodyLabel>{location || 'место не указано'}</BodyLabel>
          </Stack>
        </CloudCard>
      )}

      <CaptionLabel ta="center">
        Это нужный вам человек? Если да — нажмите кнопку ниже, чтобы
        добавить его в отслеживание.
      </CaptionLabel>

      {trackMutation.isError && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <BodyLabel c={cloudColors.errorRed}>
            {formatError(trackMutation.error)}
          </BodyLabel>
        </CloudCard>
      )}

      <PrimaryButton
        onClick={handlePrimaryAction}
        loading={trackMutation.isPending}
        fullWidth
      >
        {isAlreadyTracked
          ? 'Открыть мою карточку'
          : 'Добавить в отслеживание'}
      </PrimaryButton>
    </Stack>
  );
}

function ErrorBlock({
  children,
  onBack,
}: {
  children: React.ReactNode;
  onBack: () => void;
}) {
  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={onBack}
        >
          Назад
        </GhostButton>
      </Group>
      <CloudCard style={{ borderColor: cloudColors.errorRed }}>
        <BodyLabel c={cloudColors.errorRed}>{children}</BodyLabel>
      </CloudCard>
    </Stack>
  );
}

/**
 * Большая круглая аватарка 140x140 (hero на preview). Зеркало
 * mobile-варианта с WidthRequest="140". Иконка UserRound вместо
 * эмодзи 🕊 — по той же причине, что и в SearchPage.ResultCard:
 * на Windows в Яндекс.Браузере color-emoji иногда не рендерится.
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
