import { Container, Group, Loader, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, MapPin, MapPinOff } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { useGeolocation } from '../hooks/useGeolocation';
import { cloudColors } from '../design/theme';

/**
 * F5. Demo страница для ручной проверки useGeolocation. Публичная,
 * без auth — нужна геолокация браузера, не наш бэк.
 *
 * Сценарии:
 *  - Кнопка 'Определить' → браузер всплывает с разрешением.
 *  - Разрешил → показываются lat/lon/accuracy.
 *  - Отказал → текст подсказки про замочек.
 *  - HTTPS warning (если URL не https и не localhost).
 *
 * Будет переиспользована в F8 (создание карточки у могилы) и
 * F15 (правка координат).
 */
export function GeoDemoPage() {
  const navigate = useNavigate();
  const geo = useGeolocation();

  const isSecure =
    location.protocol === 'https:' || location.hostname === 'localhost';

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
          <CloudCard
            style={{
              borderColor: cloudColors.errorRed,
            }}
          >
            <Stack gap="xs">
              <SubTitleLabel>HTTPS обязателен</SubTitleLabel>
              <BodyLabel>
                navigator.geolocation работает только на HTTPS или
                localhost. Текущий протокол: <code>{location.protocol}</code>.
                Запросы будут падать с PERMISSION_DENIED.
              </BodyLabel>
            </Stack>
          </CloudCard>
        )}

        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Запрос</SubTitleLabel>
            <BodyLabel>
              Браузер сам покажет prompt с разрешением. Если уже отказал
              permanently — клик на замочек в адресной строке → разрешить →
              перезагрузить.
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
                <CaptionLabel>Запрашиваем GPS…</CaptionLabel>
              </Group>
            )}
          </Stack>
        </CloudCard>

        {geo.status === 'success' && geo.position && (
          <CloudCard>
            <Stack gap="xs">
              <Group gap="xs">
                <MapPin size={20} color={cloudColors.azureDeep} />
                <SubTitleLabel>Координаты получены</SubTitleLabel>
              </Group>
              <BodyLabel>
                Широта: <code>{geo.position.latitude.toFixed(6)}</code>
              </BodyLabel>
              <BodyLabel>
                Долгота: <code>{geo.position.longitude.toFixed(6)}</code>
              </BodyLabel>
              <BodyLabel>
                Точность: <code>±{Math.round(geo.position.accuracyMeters)} м</code>
              </BodyLabel>
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
                <SubTitleLabel>Ошибка</SubTitleLabel>
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
