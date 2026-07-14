import { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  TextInput,
  Textarea,
} from '@mantine/core';
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
import { MapPicker } from '../../components/MapPicker';
import { cloudColors } from '../../design/theme';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { geoApi } from '../../api/endpoints/geoApi';
import { RelationshipTypes } from '../../api/endpoints/trackedDeceasedApi';
import { formatError } from '../../auth/errorMessages';
import { mergeAutofilled } from '../../utils/addressAutofill';
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
  // Даты храним как строки «yyyy-MM-dd» — это ровно то, что отдаёт и
  // принимает нативный <input type="date">. Никакого round-trip через
  // Date: он на ре-рендере (например, от геолокации) ронял значение инпута.
  const [birthDate, setBirthDate] = useState('');
  const [deathDate, setDeathDate] = useState('');
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

  // Разбор координат должен идти ДО эффекта автоадреса — он от них зависит.
  const lat = tryParseLatitude(latInput);
  const lon = tryParseLongitude(lonInput);

  // ----- D41. Автоопределение адреса по координатам -----
  //
  // Эффект висит на самих координатах, а не на кнопке GPS: так он ловит
  // разом все три способа их задать — геолокацию, клик по карте и ручной
  // ввод. Debounce нужен именно из-за ручного ввода: без него запрос уходил
  // бы на каждый набранный символ.
  //
  // 'Россия' в autoRef — это НАША подстановка по умолчанию, а не ввод
  // юзера, поэтому её мы вправе перезаписать (вдруг захоронение в Казахстане).
  const autoAddressRef = useRef({ country: 'Россия', city: '' });
  const [addressResolving, setAddressResolving] = useState(false);

  useEffect(() => {
    if (lat === null || lon === null) return;

    let cancelled = false;
    const timer = setTimeout(async () => {
      setAddressResolving(true);
      try {
        const address = await geoApi.reverse(lat, lon);
        if (cancelled) return;

        setCountry((prev) =>
          mergeAutofilled(prev, autoAddressRef.current.country, address.country),
        );
        setCity((prev) =>
          mergeAutofilled(prev, autoAddressRef.current.city, address.city),
        );
        autoAddressRef.current = {
          country: address.country ?? '',
          city: address.city ?? '',
        };
      } catch {
        // Геокодер молчит или адреса там нет (лес, море). Это не ошибка
        // сценария: поля просто останутся пустыми, юзер впишет сам.
      } finally {
        if (!cancelled) setAddressResolving(false);
      }
    }, 700);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [lat, lon]);

  // ----- Submit -----
  const [validationError, setValidationError] = useState<string | null>(null);

  const submitMutation = useMutation({
    mutationFn: deceasedApi.addAtGrave,
    onSuccess: (resp) => navigate(`/tracked/${resp.deceasedId}`),
  });

  const acc = accInput.trim() === '' ? null : tryParseAccuracy(accInput);
  const accInvalid = accInput.trim() !== '' && acc === null;
  const accuracyLow = typeof acc === 'number' && acc > 50;

  const canSubmit =
    lat !== null &&
    lon !== null &&
    !accInvalid &&
    firstName.trim() !== '' &&
    lastName.trim() !== '' &&
    deathDate !== '' &&
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
      birthDate: birthDate || null,
      deathDate: deathDate,
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

          {/* SimpleGrid вместо Group grow — на телефоне поля в столбец,
              на десктопе в ряд (см. коммент в SearchPage). */}
          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md" verticalSpacing="md">
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
          </SimpleGrid>

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

          <CaptionLabel>
            Или найдите место на карте и нажмите на точку — координаты
            подставятся автоматически.
          </CaptionLabel>
          <MapPicker
            latitude={lat}
            longitude={lon}
            onPick={(pickedLat, pickedLon) => {
              setLatInput(pickedLat.toFixed(6));
              setLonInput(pickedLon.toFixed(6));
              // Клик по карте — ручная точка, GPS-точности нет.
              setAccInput('');
            }}
          />
        </Stack>
      </CloudCard>

      {/* ---------- Кто это ---------- */}
      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Кто это</SubTitleLabel>
          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md" verticalSpacing="md">
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
          </SimpleGrid>
          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md" verticalSpacing="md">
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
          </SimpleGrid>
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
          <Group justify="space-between" align="center">
            <SubTitleLabel>Где захоронение</SubTitleLabel>
            {addressResolving && (
              <Group gap="xs">
                <Loader size="xs" color="azure" />
                <CaptionLabel>Определяем адрес…</CaptionLabel>
              </Group>
            )}
          </Group>
          {/* D41. Страна и город подставляются по координатам. Если юзер
              поправил их руками — больше не перезаписываем (mergeAutofilled). */}
          <CaptionLabel>
            Страна и город заполняются автоматически по координатам. Если
            определилось неточно — исправьте, ваш вариант сохранится.
          </CaptionLabel>
          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md" verticalSpacing="md">
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
          </SimpleGrid>
          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md" verticalSpacing="md">
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
          </SimpleGrid>
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
