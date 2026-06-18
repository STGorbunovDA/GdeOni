import { useEffect, useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  Select,
  Stack,
  Switch,
  TextInput,
  Textarea,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useMutation } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ChevronLeft, MapPin } from 'lucide-react';
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
import { RelationshipTypes } from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { useGeolocation } from '../../hooks/useGeolocation';
import {
  tryParseAccuracy,
  tryParseLatitude,
  tryParseLongitude,
} from '../../utils/coordinateParser';

import '@mantine/dates/styles.css';

/**
 * F8. Добавление умершего "у могилы" (E7 на mobile).
 *
 * Открывается из F6 (поиск) кнопкой "Создать новую" — query-string
 * приносит firstName/lastName/middleName/city, ими pre-fill'им форму.
 *
 * Структура — зеркало AtGravePage.xaml + AtGraveViewModel.cs:
 *  1. CloudCard "Координаты" — три TextInput + кнопка "Получить
 *     координаты" (F5 useGeolocation). Парсим только на Submit.
 *  2. CloudCard "Кто это" — ФИО + даты + два Textarea.
 *  3. CloudCard "Где захоронение" — страна/город/кладбище/участок/номер.
 *  4. CloudCard "Кем приходится" — Select 9 вариантов + notes + два
 *     Switch (notify on death / birth anniversary; birth скрыт без
 *     birthDate как на mobile).
 *  5. PrimaryButton "Сохранить" — disabled пока нет валидных координат
 *     + имени + фамилии (≈ CanSubmit на mobile).
 *
 * VPN warning из mobile НЕ ПРИМЕНИМО — браузер не видит VPN и сам
 * сценарий Android-specific (E7.1).
 */

const RELATIONSHIP_OPTIONS = [
  { value: RelationshipTypes.Parent, label: 'Родитель' },
  { value: RelationshipTypes.Grandparent, label: 'Бабушка/дедушка' },
  { value: RelationshipTypes.Child, label: 'Ребёнок' },
  { value: RelationshipTypes.Spouse, label: 'Супруг(а)' },
  { value: RelationshipTypes.Sibling, label: 'Брат/сестра' },
  { value: RelationshipTypes.Relative, label: 'Родственник' },
  { value: RelationshipTypes.Friend, label: 'Друг' },
  { value: RelationshipTypes.Acquaintance, label: 'Знакомый' },
  { value: RelationshipTypes.Other, label: 'Другое' },
];

function toDateOnly(d: Date | null): string | null {
  if (!d) return null;
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function nullIfEmpty(s: string): string | null {
  const t = s.trim();
  return t === '' ? null : t;
}

export function AtGravePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  // ----- Координаты (источник истины — строки, парсим на submit) -----
  const [latInput, setLatInput] = useState('');
  const [lonInput, setLonInput] = useState('');
  const [accInput, setAccInput] = useState('');

  // ----- ФИО + даты -----
  const [firstName, setFirstName] = useState(searchParams.get('firstName') ?? '');
  const [lastName, setLastName] = useState(searchParams.get('lastName') ?? '');
  const [middleName, setMiddleName] = useState(
    searchParams.get('middleName') ?? '',
  );
  const [birthDate, setBirthDate] = useState<Date | null>(null);
  const [deathDate, setDeathDate] = useState<Date | null>(new Date());
  const [shortDescription, setShortDescription] = useState('');
  const [biography, setBiography] = useState('');

  // ----- Где -----
  const [country, setCountry] = useState('Россия');
  const [city, setCity] = useState(searchParams.get('city') ?? '');
  const [cemeteryName, setCemeteryName] = useState('');
  const [plotNumber, setPlotNumber] = useState('');
  const [graveNumber, setGraveNumber] = useState('');

  // ----- Tracking -----
  const [relationship, setRelationship] = useState<string>(
    RelationshipTypes.Friend,
  );
  const [personalNotes, setPersonalNotes] = useState('');
  const [notifyDeath, setNotifyDeath] = useState(false);
  const [notifyBirth, setNotifyBirth] = useState(false);

  // E23: birth-toggle бессмыслен без birthDate — при сбросе даты гасим.
  useEffect(() => {
    if (!birthDate && notifyBirth) setNotifyBirth(false);
  }, [birthDate, notifyBirth]);

  // ----- Геолокация -----
  const geo = useGeolocation();

  useEffect(() => {
    if (geo.position) {
      setLatInput(geo.position.latitude.toFixed(6));
      setLonInput(geo.position.longitude.toFixed(6));
      setAccInput(Math.round(geo.position.accuracyMeters).toString());
    }
  }, [geo.position]);

  // ----- Submit -----
  const [validationError, setValidationError] = useState<string | null>(null);

  const submitMutation = useMutation({
    mutationFn: deceasedApi.addAtGrave,
    onSuccess: (resp) => navigate(`/tracked/${resp.deceasedId}`),
  });

  const lat = tryParseLatitude(latInput);
  const lon = tryParseLongitude(lonInput);
  const acc = accInput.trim() === '' ? null : tryParseAccuracy(accInput);
  const accInvalid = accInput.trim() !== '' && acc === null;
  const accuracyLow = typeof acc === 'number' && acc > 50;

  const canSubmit =
    lat !== null &&
    lon !== null &&
    !accInvalid &&
    firstName.trim() !== '' &&
    lastName.trim() !== '' &&
    deathDate !== null &&
    geo.status !== 'requesting' &&
    !submitMutation.isPending;

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
      setValidationError('Точность должна быть неотрицательным числом (метры).');
      return;
    }
    if (!deathDate) {
      setValidationError('Укажите дату смерти.');
      return;
    }
    submitMutation.mutate({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      middleName: nullIfEmpty(middleName),
      birthDate: toDateOnly(birthDate),
      deathDate: toDateOnly(deathDate)!,
      shortDescription: nullIfEmpty(shortDescription),
      biography: nullIfEmpty(biography),
      graveLocation: {
        latitude: lat,
        longitude: lon,
        accuracyMeters: acc,
        country: nullIfEmpty(country),
        city: nullIfEmpty(city),
        cemeteryName: nullIfEmpty(cemeteryName),
        plotNumber: nullIfEmpty(plotNumber),
        graveNumber: nullIfEmpty(graveNumber),
      },
      tracking: {
        relationshipType: relationship,
        personalNotes: nullIfEmpty(personalNotes),
        notifyOnDeathAnniversary: notifyDeath,
        notifyOnBirthAnniversary: notifyBirth,
      },
    });
  }

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

      <TitleLabel>Добавить умершего</TitleLabel>

      {/* ---------- Координаты ---------- */}
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
              value={lonInput}
              onChange={(e) => setLonInput(e.currentTarget.value)}
              error={
                lonInput.trim() !== '' && lon === null
                  ? 'Должно быть в диапазоне [-180, 180]'
                  : undefined
              }
            />
            <TextInput
              label="Точность, м"
              placeholder="необязательно"
              value={accInput}
              onChange={(e) => setAccInput(e.currentTarget.value)}
              error={accInvalid ? 'Должно быть неотрицательным числом' : undefined}
            />
          </Group>

          {accuracyLow && (
            <Alert color="yellow" variant="light">
              Точность низкая (более 50м). Когда сохраните, можно поправить
              координаты вручную в карточке умершего.
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

      {/* ---------- Кто это ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Кто это</SubTitleLabel>
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
            <DateInput
              label="Дата рождения"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              clearable
              value={birthDate}
              onChange={(v) =>
                setBirthDate(v ? new Date(v as unknown as string) : null)
              }
            />
            <DateInput
              label="Дата смерти"
              placeholder="дд.мм.гггг"
              valueFormat="DD.MM.YYYY"
              required
              value={deathDate}
              onChange={(v) =>
                setDeathDate(v ? new Date(v as unknown as string) : null)
              }
            />
          </Group>
          <Textarea
            label="Краткое описание"
            placeholder="Несколько слов о человеке"
            autosize
            minRows={2}
            value={shortDescription}
            onChange={(e) => setShortDescription(e.currentTarget.value)}
          />
          <Textarea
            label="Биография"
            placeholder="Подробнее: где жил, чем занимался, кого оставил…"
            autosize
            minRows={4}
            value={biography}
            onChange={(e) => setBiography(e.currentTarget.value)}
          />
        </Stack>
      </CloudCard>

      {/* ---------- Где захоронение ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Где захоронение</SubTitleLabel>
          <Group grow align="flex-start" wrap="wrap">
            <TextInput
              label="Страна"
              value={country}
              onChange={(e) => setCountry(e.currentTarget.value)}
            />
            <TextInput
              label="Город"
              value={city}
              onChange={(e) => setCity(e.currentTarget.value)}
            />
            <TextInput
              label="Кладбище"
              value={cemeteryName}
              onChange={(e) => setCemeteryName(e.currentTarget.value)}
            />
          </Group>
          <Group grow align="flex-start" wrap="wrap">
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
        </Stack>
      </CloudCard>

      {/* ---------- Кем приходится ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Кем приходится</SubTitleLabel>
          <Select
            label="Отношение"
            data={RELATIONSHIP_OPTIONS}
            value={relationship}
            onChange={(v) => v && setRelationship(v)}
            allowDeselect={false}
          />
          <Textarea
            label="Личные заметки"
            placeholder="То, что важно именно вам — необязательно"
            autosize
            minRows={2}
            value={personalNotes}
            onChange={(e) => setPersonalNotes(e.currentTarget.value)}
          />
          <Switch
            label="Напоминать в день смерти"
            checked={notifyDeath}
            onChange={(e) => setNotifyDeath(e.currentTarget.checked)}
            color="azure"
          />
          {birthDate && (
            <Switch
              label="Напоминать в день рождения"
              checked={notifyBirth}
              onChange={(e) => setNotifyBirth(e.currentTarget.checked)}
              color="azure"
            />
          )}
        </Stack>
      </CloudCard>

      {/* ---------- Submit ---------- */}
      {(validationError || submitMutation.isError) && (
        <CloudCard style={{ borderColor: cloudColors.errorRed }}>
          <BodyLabel c={cloudColors.errorRed}>
            {validationError ?? formatError(submitMutation.error)}
          </BodyLabel>
        </CloudCard>
      )}

      <PrimaryButton
        onClick={handleSubmit}
        disabled={!canSubmit}
        loading={submitMutation.isPending}
        fullWidth
      >
        Сохранить
      </PrimaryButton>
    </Stack>
  );
}
