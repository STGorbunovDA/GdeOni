import { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Group,
  Loader,
  SimpleGrid,
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
import { MapPicker } from '../../components/MapPicker';
import { cloudColors } from '../../design/theme';
import { trackedDeceasedApi } from '../../api/endpoints/trackedDeceasedApi';
import { deceasedApi } from '../../api/endpoints/deceasedApi';
import { geoApi } from '../../api/endpoints/geoApi';
import { formatError } from '../../auth/errorMessages';
import { mergeAutofilled } from '../../utils/addressAutofill';
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

  // D41. Адрес: подставляется по координатам, правится руками.
  const [country, setCountry] = useState('');
  const [city, setCity] = useState('');
  const [addressResolving, setAddressResolving] = useState(false);

  // Что мы подставили сами в прошлый раз — чтобы не затирать ручные правки.
  const autoAddressRef = useRef({ country: '', city: '' });
  // Координаты, с которыми страница открылась: по ним понимаем, двигал ли
  // юзер точку.
  const initialCoordsRef = useRef({ lat: '', lon: '' });

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

    // D41. Адрес из карточки — стартовые значения полей.
    setCountry(d.country ?? '');
    setCity(d.city ?? '');

    // Считаем адрес карточки «автоматическим»: при сдвиге точки его можно
    // перезаписать. А вот исходные координаты запоминаем, чтобы НЕ дёргать
    // геокодер при простом открытии страницы — иначе город молча менялся бы
    // сам, хотя юзер ничего не двигал.
    autoAddressRef.current = { country: d.country ?? '', city: d.city ?? '' };
    initialCoordsRef.current = {
      lat: typeof d.latitude === 'number' ? d.latitude.toFixed(6) : '',
      lon: typeof d.longitude === 'number' ? d.longitude.toFixed(6) : '',
    };
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

  // ----- D41. Автоопределение адреса по координатам -----
  //
  // Дёргаем геокодер в двух случаях:
  //   1) город пуст — заполнить его нечем, кроме координат;
  //   2) юзер сдвинул точку — старый город может относиться к другому месту.
  // При простом открытии карточки с уже заполненным городом молчим: иначе
  // город менялся бы сам, хотя человек ничего не трогал.
  //
  // Debounce нужен из-за ручного ввода координат — иначе запрос уходил бы
  // на каждый набранный символ.
  const coordsChanged =
    latInput !== initialCoordsRef.current.lat ||
    lonInput !== initialCoordsRef.current.lon;
  const cityIsEmpty = city.trim() === '';

  useEffect(() => {
    if (lat === null || lon === null) return;
    if (!coordsChanged && !cityIsEmpty) return;

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
        // Адреса по этой точке нет или геокодер молчит — не беда,
        // поля остаются под ручной ввод.
      } finally {
        if (!cancelled) setAddressResolving(false);
      }
    }, 700);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [lat, lon, coordsChanged, cityIsEmpty]);

  const acc =
    accInput === null || accInput.trim() === ''
      ? null
      : tryParseAccuracy(accInput);
  const accInvalid =
    accInput !== null && accInput.trim() !== '' && acc === null;
  const accuracyLow = typeof acc === 'number' && acc > 50;

  // D41. Раньше слали setBurialFromGps — он сохраняет ТОЛЬКО координаты и
  // намеренно не трогает адрес. Теперь адрес правится на этой же странице,
  // поэтому шлём PATCH burial-location целиком. Поля, которых здесь нет
  // (регион, кладбище, участок, могила), передаём как есть из карточки —
  // иначе PATCH их обнулит.
  const submitMutation = useMutation({
    mutationFn: () => {
      const d = query.data!.deceased;
      return deceasedApi.updateBurialLocation(id!, {
        latitude: lat!,
        longitude: lon!,
        accuracyMeters: acc,
        country: country.trim() || null,
        region: d.region,
        city: city.trim() || null,
        cemeteryName: d.cemeteryName,
        plotNumber: d.plotNumber,
        graveNumber: d.graveNumber,
      });
    },
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
          Кладбище, участок и номер могилы не изменятся. Страна и город
          подставятся по координатам — их можно поправить руками.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <SubTitleLabel>Координаты</SubTitleLabel>
          <CaptionLabel>
            Нажмите «Получить координаты» либо введите значения вручную.
            Допускается точка или запятая в десятичной части.
          </CaptionLabel>

          {/* SimpleGrid вместо Group grow — на телефоне поля в столбец. */}
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
          </SimpleGrid>

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
              setAccInput('');
            }}
          />
        </Stack>
      </CloudCard>

      {/* D41. Адрес определяется по координатам. Правки руками мы не
          затираем: как только юзер вписал своё — поле его. */}
      <CloudCard>
        <Stack gap="md">
          <Group justify="space-between" align="center">
            <SubTitleLabel>Адрес</SubTitleLabel>
            {addressResolving && (
              <Group gap="xs">
                <Loader size="xs" color="azure" />
                <CaptionLabel>Определяем адрес…</CaptionLabel>
              </Group>
            )}
          </Group>
          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md" verticalSpacing="md">
            <TextInput
              label="Страна"
              placeholder="определится по координатам"
              value={country}
              onChange={(e) => setCountry(e.currentTarget.value)}
            />
            <TextInput
              label="Город"
              placeholder="определится по координатам"
              value={city}
              onChange={(e) => setCity(e.currentTarget.value)}
            />
          </SimpleGrid>
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
