import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { Loader } from '@mantine/core';
import { useAppFeatures } from '../../hooks/useAppFeatures';

/**
 * F5. Полноэкранный оверлей поверх всех окон на время сбора координат
 * (useGeolocation, окно из конфига Geolocation:AcquireWindowSeconds, дефолт
 * 60 с). Кружок + «Получаем координаты» с бегущими
 * точками (. → .. → ... → .) и текущей достигнутой точностью, чтобы человек
 * подождал прогрев GPS, а не хватал первый (худший) fix.
 *
 * Рендерится порталом в document.body с очень высоким z-index — перекрывает
 * карту (Leaflet), модалки и прочее. Клики сквозь него не проходят (fixed
 * inset:0), поэтому пользователь не трогает форму, пока идёт замер.
 */
export function GeoLocatingOverlay({
  opened,
  accuracyMeters,
  onCancel,
  onAccept,
}: {
  opened: boolean;
  accuracyMeters: number | null;
  /** «Отмена» — прервать получение координат, ничего не подставлять. */
  onCancel: () => void;
  /** «Пропустить» — взять лучшую собранную точку не дожидаясь конца окна. */
  onAccept: () => void;
}) {
  const [dots, setDots] = useState('.');
  const features = useAppFeatures();
  const windowSeconds = features.data?.geoAcquireWindowSeconds ?? 60;

  useEffect(() => {
    if (!opened) {
      setDots('.');
      return;
    }
    const id = setInterval(() => {
      setDots((d) => (d.length >= 3 ? '.' : d + '.'));
    }, 450);
    return () => clearInterval(id);
  }, [opened]);

  if (!opened) return null;

  return createPortal(
    <div
      role="status"
      aria-live="polite"
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 100000,
        background: 'rgba(15, 23, 42, 0.55)',
        backdropFilter: 'blur(2px)',
        WebkitBackdropFilter: 'blur(2px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 24,
      }}
    >
      <div
        style={{
          background: '#fff',
          borderRadius: 20,
          padding: '28px 32px',
          minWidth: 264,
          maxWidth: 360,
          textAlign: 'center',
          boxShadow: '0 20px 60px rgba(0, 0, 0, 0.35)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 14,
        }}
      >
        <Loader color="azure" size="lg" />

        <div style={{ fontSize: 17, fontWeight: 600, color: '#1f2933' }}>
          Получаем координаты
          {/* Фиксированная ширина, чтобы текст не «прыгал» при смене точек. */}
          <span
            style={{ display: 'inline-block', width: '1.6em', textAlign: 'left' }}
          >
            {dots}
          </span>
        </div>

        <div
          style={{
            fontSize: 14,
            color: '#1977cc',
            minHeight: 20,
            fontWeight: 500,
          }}
        >
          {accuracyMeters != null
            ? `Текущая точность ~${Math.round(accuracyMeters)} м`
            : 'Идёт поиск спутников…'}
        </div>

        <div style={{ fontSize: 12, color: '#94a3b8', lineHeight: 1.4 }}>
          Не закрывайте окно — ищем самую точную точку, это до {windowSeconds}{' '}
          сек.
        </div>

        {/* Кнопки — с фиксированными цветами: карточка всегда белая, а
            Mantine-кнопки подстраиваются под тёмную тему приложения и на белом
            фоне становятся невидимыми. Поэтому обычные button со своими цветами. */}
        <div
          style={{
            display: 'flex',
            gap: 10,
            justifyContent: 'center',
            marginTop: 4,
            width: '100%',
          }}
        >
          <button
            type="button"
            onClick={onCancel}
            style={{
              background: 'transparent',
              border: '1px solid #cbd5e1',
              color: '#475569',
              fontSize: 14,
              fontWeight: 500,
              padding: '9px 18px',
              borderRadius: 10,
              cursor: 'pointer',
            }}
          >
            Отмена
          </button>
          <button
            type="button"
            onClick={onAccept}
            disabled={accuracyMeters == null}
            style={{
              background: accuracyMeters == null ? '#cbd5e1' : '#1977cc',
              border: 'none',
              color: '#ffffff',
              fontSize: 14,
              fontWeight: 600,
              padding: '9px 18px',
              borderRadius: 10,
              cursor: accuracyMeters == null ? 'default' : 'pointer',
            }}
          >
            {accuracyMeters != null
              ? `Пропустить (~${Math.round(accuracyMeters)} м)`
              : 'Пропустить'}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
