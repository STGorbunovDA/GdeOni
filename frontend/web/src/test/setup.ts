import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

/**
 * F19. Общий setup для Vitest тестов.
 *
 * afterEach cleanup — тестам React Testing Library нужен чистый DOM
 * между рендерами, иначе `screen.getByText` находит остатки с прошлых
 * тестов и падает с multiple matches.
 *
 * jest-dom матчеры (`toBeInTheDocument`, `toHaveTextContent`, ...) —
 * подключены через side-effect импорт.
 */
afterEach(() => {
  cleanup();
});

/**
 * F5. `navigator.geolocation` в jsdom отсутствует. Дефолтный stub
 * возвращает координаты Москвы; тесты, которым нужна другая, сами
 * переопределяют через Object.defineProperty перед render'ом.
 */
if (typeof globalThis.navigator !== 'undefined' && !globalThis.navigator.geolocation) {
  Object.defineProperty(globalThis.navigator, 'geolocation', {
    configurable: true,
    value: {
      getCurrentPosition: (
        success: PositionCallback,
      ) => {
        success({
          coords: {
            latitude: 55.7558,
            longitude: 37.6173,
            accuracy: 10,
            altitude: null,
            altitudeAccuracy: null,
            heading: null,
            speed: null,
            toJSON: () => ({}),
          },
          timestamp: Date.now(),
          toJSON: () => ({}),
        } as GeolocationPosition);
      },
      watchPosition: () => 0,
      clearWatch: () => {},
    },
  });
}

/**
 * jsdom по умолчанию не реализует `window.matchMedia` — Mantine его
 * дёргает при инициализации useMediaQuery. Возвращаем всегда `false`
 * (десктоп) — тестам это подходит.
 */
if (typeof window !== 'undefined' && !window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  });
}
