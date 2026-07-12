import { useEffect, useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  Stack,
  Switch,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, MapPin } from 'lucide-react';
import {
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { MapPicker } from '../../components/MapPicker';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { formatError } from '../../auth/errorMessages';
import { useGeolocation } from '../../hooks/useGeolocation';
import {
  tryParseAccuracy,
  tryParseLatitude,
  tryParseLongitude,
} from '../../utils/coordinateParser';

function nullIfEmpty(s: string): string | null {
  const t = s.trim();
  return t === '' ? null : t;
}

/**
 * F11.1 / D24. Редактирование карточки умершего (admin, доступ по
 * `/admin/deceased/:id/edit`). Три секции с раздельными PATCH'ами
 * (main-info / metadata / burial-location) — зеркало mobile
 * EditDeceasedViewModel. Каждая секция сохраняется своей кнопкой.
 */
export function EditDeceasedPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ['admin-deceased-details', id],
    queryFn: () => deceasedApi.getById(id!),
    enabled: !!id,
  });

  // ----- Основное -----
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [middleName, setMiddleName] = useState('');
  const [birthDate, setBirthDate] = useState('');
  const [deathDate, setDeathDate] = useState('');
  const [shortDescription, setShortDescription] = useState('');
  const [biography, setBiography] = useState('');

  // ----- Дополнительно (метаданные) -----
  const [epitaph, setEpitaph] = useState('');
  const [religion, setReligion] = useState('');
  const [source, setSource] = useState('');
  const [isMilitaryService, setIsMilitaryService] = useState(false);
  const [additionalInfo, setAdditionalInfo] = useState('');

  // ----- Место захоронения -----
  const [latInput, setLatInput] = useState('');
  const [lonInput, setLonInput] = useState('');
  const [accInput, setAccInput] = useState('');
  const [country, setCountry] = useState('');
  const [region, setRegion] = useState('');
  const [city, setCity] = useState('');
  const [cemeteryName, setCemeteryName] = useState('');
  const [plotNumber, setPlotNumber] = useState('');
  const [graveNumber, setGraveNumber] = useState('');

  // Префилл один раз после загрузки — дальше правки юзера не затираем.
  const [prefilled, setPrefilled] = useState(false);
  useEffect(() => {
    if (!query.data || prefilled) return;
    const d = query.data;
    setFirstName(d.firstName);
    setLastName(d.lastName);
    setMiddleName(d.middleName ?? '');
    setBirthDate(d.birthDate ?? '');
    setDeathDate(d.deathDate);
    setShortDescription(d.shortDescription ?? '');
    setBiography(d.biography ?? '');
    setEpitaph(d.metadata?.epitaph ?? '');
    setReligion(d.metadata?.religion ?? '');
    setSource(d.metadata?.source ?? '');
    setIsMilitaryService(d.metadata?.isMilitaryService ?? false);
    setAdditionalInfo(d.metadata?.additionalInfo ?? '');
    setLatInput(typeof d.latitude === 'number' ? d.latitude.toFixed(6) : '');
    setLonInput(typeof d.longitude === 'number' ? d.longitude.toFixed(6) : '');
    setAccInput(
      typeof d.accuracyMeters === 'number'
        ? Math.round(d.accuracyMeters).toString()
        : '',
    );
    setCountry(d.country ?? '');
    setRegion(d.region ?? '');
    setCity(d.city ?? '');
    setCemeteryName(d.cemeteryName ?? '');
    setPlotNumber(d.plotNumber ?? '');
    setGraveNumber(d.graveNumber ?? '');
    setPrefilled(true);
  }, [query.data, prefilled]);

  // Геолокация для секции координат.
  const geo = useGeolocation();
  useEffect(() => {
    if (geo.position) {
      setLatInput(geo.position.latitude.toFixed(6));
      setLonInput(geo.position.longitude.toFixed(6));
      setAccInput(Math.round(geo.position.accuracyMeters).toString());
    }
  }, [geo.position]);

  function invalidateCard() {
    queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', id] });
    queryClient.invalidateQueries({ queryKey: ['tracked-details', id] });
    queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    queryClient.invalidateQueries({ queryKey: ['admin-deceased'] });
  }

  const mainMutation = useMutation({
    mutationFn: () =>
      deceasedApi.updateMainInfo(id!, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        middleName: nullIfEmpty(middleName),
        birthDate: birthDate || null,
        deathDate,
        shortDescription: nullIfEmpty(shortDescription),
        biography: nullIfEmpty(biography),
      }),
    onSuccess: invalidateCard,
  });

  const metaMutation = useMutation({
    mutationFn: () =>
      deceasedApi.updateMetadata(id!, {
        epitaph: nullIfEmpty(epitaph),
        religion: nullIfEmpty(religion),
        source: nullIfEmpty(source),
        isMilitaryService,
        additionalInfo: nullIfEmpty(additionalInfo),
      }),
    onSuccess: invalidateCard,
  });

  const locLat = tryParseLatitude(latInput);
  const locLon = tryParseLongitude(lonInput);
  const locAcc = accInput.trim() === '' ? null : tryParseAccuracy(accInput);
  const bothCoordsEmpty = latInput.trim() === '' && lonInput.trim() === '';

  const locMutation = useMutation({
    mutationFn: () =>
      // Обе координаты пустые → удаляем координаты (null на бэке).
      deceasedApi.updateBurialLocation(id!, {
        latitude: bothCoordsEmpty ? null : locLat,
        longitude: bothCoordsEmpty ? null : locLon,
        accuracyMeters: bothCoordsEmpty ? null : locAcc,
        country: nullIfEmpty(country),
        region: nullIfEmpty(region),
        city: nullIfEmpty(city),
        cemeteryName: nullIfEmpty(cemeteryName),
        plotNumber: nullIfEmpty(plotNumber),
        graveNumber: nullIfEmpty(graveNumber),
      }),
    onSuccess: invalidateCard,
  });

  const mainValid =
    firstName.trim() !== '' && lastName.trim() !== '' && deathDate !== '';
  const coordsValid = bothCoordsEmpty || (locLat !== null && locLon !== null);
  const accValid = accInput.trim() === '' || locAcc !== null;
  const locValid = coordsValid && accValid;

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

  if (query.isLoading || !prefilled) {
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
        <BackButton onClick={() => navigate(-1)} />
      </Group>

      <TitleLabel>Редактирование: {query.data.fullName}</TitleLabel>

      {/* ---------- Основное ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Основное</SubTitleLabel>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Фамилия"
              required
              value={lastName}
              onChange={(e) => setLastName(e.currentTarget.value)}
            />
            <TextInput
              label="Имя"
              required
              value={firstName}
              onChange={(e) => setFirstName(e.currentTarget.value)}
            />
            <TextInput
              label="Отчество"
              value={middleName}
              onChange={(e) => setMiddleName(e.currentTarget.value)}
            />
          </Group>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              type="date"
              label="Дата рождения"
              value={birthDate}
              onChange={(e) => setBirthDate(e.currentTarget.value)}
            />
            <TextInput
              type="date"
              label="Дата смерти"
              required
              value={deathDate}
              onChange={(e) => setDeathDate(e.currentTarget.value)}
            />
          </Group>
          <Textarea
            label="Краткое описание"
            autosize
            minRows={2}
            value={shortDescription}
            onChange={(e) => setShortDescription(e.currentTarget.value)}
          />
          <Textarea
            label="Биография"
            autosize
            minRows={3}
            value={biography}
            onChange={(e) => setBiography(e.currentTarget.value)}
          />
          <SectionResult
            mutationIsError={mainMutation.isError}
            error={mainMutation.error}
            success={mainMutation.isSuccess}
            successText="Основная информация сохранена."
          />
          <Group justify="flex-end">
            <PrimaryButton
              onClick={() => mainMutation.mutate()}
              disabled={!mainValid}
              loading={mainMutation.isPending}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </CloudCard>

      {/* ---------- Дополнительно ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Дополнительно</SubTitleLabel>
          <TextInput
            label="Эпитафия"
            value={epitaph}
            onChange={(e) => setEpitaph(e.currentTarget.value)}
          />
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Религия"
              value={religion}
              onChange={(e) => setReligion(e.currentTarget.value)}
            />
            <TextInput
              label="Источник"
              value={source}
              onChange={(e) => setSource(e.currentTarget.value)}
            />
          </Group>
          <Switch
            label="Военная служба"
            checked={isMilitaryService}
            onChange={(e) => setIsMilitaryService(e.currentTarget.checked)}
            color="azure"
          />
          <Textarea
            label="Дополнительная информация"
            autosize
            minRows={2}
            value={additionalInfo}
            onChange={(e) => setAdditionalInfo(e.currentTarget.value)}
          />
          <SectionResult
            mutationIsError={metaMutation.isError}
            error={metaMutation.error}
            success={metaMutation.isSuccess}
            successText="Дополнительная информация сохранена."
          />
          <Group justify="flex-end">
            <PrimaryButton
              onClick={() => metaMutation.mutate()}
              loading={metaMutation.isPending}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </CloudCard>

      {/* ---------- Место захоронения ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Место захоронения</SubTitleLabel>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Широта"
              placeholder="например, 55.755826"
              value={latInput}
              onChange={(e) => setLatInput(e.currentTarget.value)}
              error={
                latInput.trim() !== '' && locLat === null
                  ? 'Диапазон [-90, 90]'
                  : undefined
              }
            />
            <TextInput
              label="Долгота"
              placeholder="например, 37.617300"
              value={lonInput}
              onChange={(e) => setLonInput(e.currentTarget.value)}
              error={
                lonInput.trim() !== '' && locLon === null
                  ? 'Диапазон [-180, 180]'
                  : undefined
              }
            />
            <TextInput
              label="Точность, м"
              placeholder="необязательно"
              value={accInput}
              onChange={(e) => setAccInput(e.currentTarget.value)}
              error={
                accInput.trim() !== '' && locAcc === null
                  ? 'Неотрицательное число'
                  : undefined
              }
            />
          </Group>
          <Group>
            <PrimaryButton
              onClick={geo.request}
              loading={geo.status === 'requesting'}
              leftSection={<MapPin size={16} />}
            >
              Получить координаты
            </PrimaryButton>
          </Group>
          {geo.status === 'error' && geo.error && (
            <Alert color="red" variant="light">
              {geo.error.message}
            </Alert>
          )}
          <CaptionLabel>
            Или найдите место на карте и нажмите на точку — координаты
            подставятся автоматически.
          </CaptionLabel>
          <MapPicker
            latitude={locLat}
            longitude={locLon}
            onPick={(pickedLat, pickedLon) => {
              setLatInput(pickedLat.toFixed(6));
              setLonInput(pickedLon.toFixed(6));
              setAccInput('');
            }}
          />
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Страна"
              value={country}
              onChange={(e) => setCountry(e.currentTarget.value)}
            />
            <TextInput
              label="Регион"
              value={region}
              onChange={(e) => setRegion(e.currentTarget.value)}
            />
            <TextInput
              label="Город"
              value={city}
              onChange={(e) => setCity(e.currentTarget.value)}
            />
          </Group>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Кладбище"
              value={cemeteryName}
              onChange={(e) => setCemeteryName(e.currentTarget.value)}
            />
            <TextInput
              label="Участок"
              value={plotNumber}
              onChange={(e) => setPlotNumber(e.currentTarget.value)}
            />
            <TextInput
              label="Номер могилы"
              value={graveNumber}
              onChange={(e) => setGraveNumber(e.currentTarget.value)}
            />
          </Group>
          <SectionResult
            mutationIsError={locMutation.isError}
            error={locMutation.error}
            success={locMutation.isSuccess}
            successText="Место захоронения сохранено."
          />
          <Group justify="flex-end">
            <PrimaryButton
              onClick={() => locMutation.mutate()}
              disabled={!locValid}
              loading={locMutation.isPending}
            >
              Сохранить
            </PrimaryButton>
          </Group>
        </Stack>
      </CloudCard>
    </Stack>
  );
}

function SectionResult({
  mutationIsError,
  error,
  success,
  successText,
}: {
  mutationIsError: boolean;
  error: unknown;
  success: boolean;
  successText: string;
}) {
  if (mutationIsError) {
    return (
      <Alert color="red" variant="light">
        {formatError(error)}
      </Alert>
    );
  }
  if (success) {
    return (
      <Alert color="green" variant="light">
        {successText}
      </Alert>
    );
  }
  return null;
}

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <GhostButton leftSection={<ChevronLeft size={16} />} onClick={onClick}>
      Назад
    </GhostButton>
  );
}
