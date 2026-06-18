import { useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  Stack,
  TextInput,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, MapPin } from 'lucide-react';
import { AxiosError } from 'axios';
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
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { formatError } from '../../auth/errorMessages';
import { useGeolocation } from '../../hooks/useGeolocation';
import {
  tryParseAccuracy,
  tryParseLatitude,
  tryParseLongitude,
} from '../../utils/coordinateParser';

/**
 * F15. Правка координат места захоронения (~ E20 на mobile).
 * Открывается по `/tracked/:id/edit-coords` с карточки. Адресные поля
 * (страна/город/кладбище/участок) бэк сохраняет — правится только
 * lat/lon/accuracy.
 *
 * Pre-fill из getDetails. Кнопка "Получить координаты" — через
 * useGeolocation (F5). 403 → friendly message про автора/админа.
 */
export function EditCoordsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ['tracked-details', id],
    queryFn: () => trackedDeceasedApi.getDetails(id!),
    enabled: !!id,
  });

  const [latInput, setLatInput] = useState<string | null>(null);
  const [lonInput, setLonInput] = useState<string | null>(null);
  const [accInput, setAccInput] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  // Pre-fill ровно один раз — после первой успешной загрузки. После
  // мы НЕ перезаписываем поля при refetch, иначе ввод юзера затрётся.
  if (latInput === null && query.data) {
    const d = query.data.deceased;
    setLatInput(typeof d.latitude === 'number' ? d.latitude.toFixed(6) : '');
    setLonInput(typeof d.longitude === 'number' ? d.longitude.toFixed(6) : '');
    setAccInput(
      typeof d.accuracyMeters === 'number'
        ? Math.round(d.accuracyMeters).toString()
        : '',
    );
  }

  const geo = useGeolocation();
  // Заполняем поля, когда geo-запрос вернул координаты.
  if (geo.position && geo.status === 'success') {
    const lat = geo.position.latitude.toFixed(6);
    const lon = geo.position.longitude.toFixed(6);
    const acc = Math.round(geo.position.accuracyMeters).toString();
    if (latInput !== lat || lonInput !== lon || accInput !== acc) {
      setLatInput(lat);
      setLonInput(lon);
      setAccInput(acc);
      geo.reset();
    }
  }

  const lat = latInput !== null ? tryParseLatitude(latInput) : null;
  const lon = lonInput !== null ? tryParseLongitude(lonInput) : null;
  const acc =
    accInput === null || accInput.trim() === ''
      ? null
      : tryParseAccuracy(accInput);
  const accInvalid =
    accInput !== null && accInput.trim() !== '' && acc === null;
  const accuracyLow = typeof acc === 'number' && acc > 50;

  const submitMutation = useMutation({
    mutationFn: () =>
      deceasedApi.setBurialFromGps(id!, {
        latitude: lat!,
        longitude: lon!,
        accuracyMeters: acc,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      navigate(`/tracked/${id}`);
    },
  });

  const canSubmit =
    lat !== null &&
    lon !== null &&
    !accInvalid &&
    !submitMutation.isPending &&
    geo.status !== 'requesting';

  function handleSubmit() {
    setValidationError(null);
    if (lat === null) {
      setValidationError('Широта должна быть числом в диапазоне [-90, 90].');
      return;
    }
    if (lon === null) {
      setValidationError('Долгота должна быть числом в диапазоне [-180, 180].');
      return;
    }
    if (accInvalid) {
      setValidationError(
        'Точность должна быть неотрицательным числом (метры).',
      );
      return;
    }
    submitMutation.mutate();
  }

  // 403 → отдельное friendly message (бэк ICanEditDeceasedPolicy
  // блокирует не-автора и не-админа).
  function formatSubmitError(err: unknown): string {
    if (err instanceof AxiosError && err.response?.status === 403) {
      return 'Координаты можно править только автору карточки или администратору.';
    }
    return formatError(err);
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

  if (query.isLoading || latInput === null) {
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
          {query.error ? formatError(query.error) : 'Карточка не найдена.'}
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack gap="lg">
      <Group>
        <BackButton onClick={() => navigate(`/tracked/${id}`)} />
      </Group>

      <Stack gap="xs">
        <TitleLabel>Поправить координаты</TitleLabel>
        <CaptionLabel>
          Адрес, кладбище, участок и номер могилы не изменятся — обновятся
          только координаты на карте.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Координаты</SubTitleLabel>
          <CaptionLabel>
            Нажмите «Получить координаты» либо введите значения вручную.
            Допускается точка или запятая в десятичной части.
          </CaptionLabel>

          <Group grow align="flex-start">
            <TextInput
              label="Широта"
              placeholder="например, 55.755826"
              value={latInput}
              onChange={(e) => setLatInput(e.currentTarget.value)}
              error={
                latInput.trim() !== '' && lat === null
                  ? 'Должно быть в диапазоне [-90, 90]'
                  : undefined
              }
            />
            <TextInput
              label="Долгота"
              placeholder="например, 37.617300"
              value={lonInput!}
              onChange={(e) => setLonInput(e.currentTarget.value)}
              error={
                lonInput && lonInput.trim() !== '' && lon === null
                  ? 'Должно быть в диапазоне [-180, 180]'
                  : undefined
              }
            />
            <TextInput
              label="Точность, м"
              placeholder="необязательно"
              value={accInput ?? ''}
              onChange={(e) => setAccInput(e.currentTarget.value)}
              error={accInvalid ? 'Должно быть неотрицательным числом' : undefined}
            />
          </Group>

          {accuracyLow && (
            <Alert color="yellow" variant="light">
              Точность низкая (более 50м). Это нормально для GPS на
              открытой местности — можно сохранять.
            </Alert>
          )}

          {geo.status === 'error' && geo.error && (
            <Alert color="red" variant="light">
              {geo.error.message}
            </Alert>
          )}

          <Group>
            <PrimaryButton
              onClick={geo.request}
              loading={geo.status === 'requesting'}
              leftSection={<MapPin size={16} />}
            >
              Получить координаты
            </PrimaryButton>
            {geo.status === 'requesting' && (
              <Group gap="xs">
                <Loader size="xs" color="azure" />
                <CaptionLabel>Запрашиваем геолокацию у браузера…</CaptionLabel>
              </Group>
            )}
          </Group>
        </Stack>
      </CloudCard>

      {(validationError || submitMutation.isError) && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <BodyLabel c={cloudColors.errorRed}>
            {validationError ?? formatSubmitError(submitMutation.error)}
          </BodyLabel>
        </CloudCard>
      )}

      <Group justify="space-between">
        <GhostButton onClick={() => navigate(`/tracked/${id}`)}>
          Отмена
        </GhostButton>
        <PrimaryButton
          onClick={handleSubmit}
          disabled={!canSubmit}
          loading={submitMutation.isPending}
        >
          Сохранить
        </PrimaryButton>
      </Group>
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
