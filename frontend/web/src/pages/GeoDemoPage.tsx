import { Container, Group, Loader, Stack, TextInput } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, MapPin, MapPinOff } from 'lucide-react';
import { useState } from 'react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { useGeolocation, type GeoPosition } from '../hooks/useGeolocation';
import { cloudColors } from '../design/theme';
import {
  tryParseLatitude,
  tryParseLongitude,
} from '../utils/coordinateParser';

/**
 * F5. Demo страница для ручной проверки useGeolocation. Публичная.
 *
 * Включает:
 *  - Кнопку 'Определить' (трёхступенчатый запрос с фоллбэком).
 *  - Ручной ввод координат — на случай когда geolocation глухо
 *    висит (VPN, провайдер блокирует Google Location Service).
 *  - HTTPS-warning для не-localhost не-https.
 *
 * В F8 (создание у могилы) и F15 (правка координат) будет тот же
 * паттерн: кнопка GPS + поля для ручного ввода как fallback.
 */
export function GeoDemoPage() {
  const navigate = useNavigate();
  const geo = useGeolocation();
  const [manualPos, setManualPos] = useState<GeoPosition | null>(null);
  const [manualLat, setManualLat] = useState('');
  const [manualLon, setManualLon] = useState('');

  const isSecure =
    location.protocol === 'https:' || location.hostname === 'localhost';

  const parsedLat = tryParseLatitude(manualLat);
  const parsedLon = tryParseLongitude(manualLon);
  const canSubmitManual = parsedLat !== null && parsedLon !== null;

  function applyManual() {
    if (parsedLat === null || parsedLon === null) return;
    setManualPos({
      latitude: parsedLat,
      longitude: parsedLon,
      accuracyMeters: 0,
    });
  }

  const shownPos = geo.position ?? manualPos;
  const shownSource: 'gps' | 'manual' | null = geo.position
    ? 'gps'
    : manualPos
      ? 'manual'
      : null;

  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <Group>
          <GhostButton
            onClick={() => navigate('/tracked')}
            leftSection={<ChevronLeft size={16} />}
          >
            Назад
          </GhostButton>
        </Group>

        <Stack gap="xs">
          <TitleLabel>Геолокация (F5)</TitleLabel>
          <CaptionLabel>
            Ручная проверка useGeolocation. Будет переиспользован в
            F8 (создание карточки у могилы) и F15 (правка координат).
          </CaptionLabel>
        </Stack>

        {!isSecure && (
          <CloudCard style={{ borderColor: cloudColors.errorRed }}>
            <Stack gap="xs">
              <SubTitleLabel>HTTPS обязателен</SubTitleLabel>
              <BodyLabel>
                navigator.geolocation работает только на HTTPS или
                localhost. Текущий протокол:{' '}
                <code>{location.protocol}</code>. Запросы будут падать
                с PERMISSION_DENIED.
              </BodyLabel>
            </Stack>
          </CloudCard>
        )}

        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Запрос через браузер</SubTitleLabel>
            <BodyLabel>
              Браузер сам покажет prompt с разрешением. Если все три
              попытки упадут (например, провайдер блокирует Google
              Location Service) — введите координаты ниже вручную.
            </BodyLabel>
            <Group>
              <PrimaryButton
                onClick={geo.request}
                disabled={geo.status === 'requesting'}
                leftSection={<MapPin size={16} />}
              >
                Определить местоположение
              </PrimaryButton>
              <GhostButton onClick={geo.reset}>Сбросить</GhostButton>
            </Group>

            {geo.status === 'requesting' && (
              <Group gap="xs">
                <Loader size="xs" />
                <CaptionLabel>
                  Запрашиваем GPS… (до 55 секунд на три попытки)
                </CaptionLabel>
              </Group>
            )}
          </Stack>
        </CloudCard>

        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Ручной ввод</SubTitleLabel>
            <BodyLabel>
              Если GPS не отвечает — скопируйте координаты с Яндекс.Карт
              или Google Maps. Запятая и точка допустимы как
              десятичный разделитель.
            </BodyLabel>
            <Group grow>
              <TextInput
                label="Широта"
                placeholder="55.752345"
                value={manualLat}
                onChange={(e) => setManualLat(e.currentTarget.value)}
                error={
                  manualLat !== '' && parsedLat === null
                    ? 'Введите число от -90 до 90'
                    : null
                }
              />
              <TextInput
                label="Долгота"
                placeholder="37.617800"
                value={manualLon}
                onChange={(e) => setManualLon(e.currentTarget.value)}
                error={
                  manualLon !== '' && parsedLon === null
                    ? 'Введите число от -180 до 180'
                    : null
                }
              />
            </Group>
            <Group>
              <PrimaryButton onClick={applyManual} disabled={!canSubmitManual}>
                Применить
              </PrimaryButton>
            </Group>
          </Stack>
        </CloudCard>

        {shownPos && (
          <CloudCard>
            <Stack gap="xs">
              <Group gap="xs">
                <MapPin size={20} color={cloudColors.azureDeep} />
                <SubTitleLabel>
                  Координаты{' '}
                  {shownSource === 'gps' ? '(GPS)' : '(введены вручную)'}
                </SubTitleLabel>
              </Group>
              <BodyLabel>
                Широта: <code>{shownPos.latitude.toFixed(6)}</code>
              </BodyLabel>
              <BodyLabel>
                Долгота: <code>{shownPos.longitude.toFixed(6)}</code>
              </BodyLabel>
              {shownSource === 'gps' && (
                <BodyLabel>
                  Точность:{' '}
                  <code>±{Math.round(shownPos.accuracyMeters)} м</code>
                </BodyLabel>
              )}
              <CaptionLabel>
                На production эти числа улетят в backend как поля
                latitude/longitude/accuracyMeters в BurialLocation.
              </CaptionLabel>
            </Stack>
          </CloudCard>
        )}

        {geo.status === 'error' && geo.error && (
          <CloudCard style={{ borderColor: cloudColors.errorRed }}>
            <Stack gap="xs">
              <Group gap="xs">
                <MapPinOff size={20} color={cloudColors.errorRed} />
                <SubTitleLabel>Ошибка GPS</SubTitleLabel>
              </Group>
              <BodyLabel c={cloudColors.errorRed}>{geo.error.message}</BodyLabel>
              <CaptionLabel>code: {geo.error.code}</CaptionLabel>
            </Stack>
          </CloudCard>
        )}
      </Stack>
    </Container>
  );
}
