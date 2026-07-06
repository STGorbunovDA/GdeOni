import { describe, it, expect } from 'vitest';
import {
  buildYandexUrl,
  buildYandexLookupUrl,
  buildGoogleUrl,
  build2GisUrl,
  haversine,
  optimizeOrder,
  type Point,
} from './routing';

/**
 * F19. Тесты роутинг-утилит — зеркало
 * frontend/mobile тестов на ExternalMapsService.
 * Особенно важно: invariant .toFixed(6) и правильный порядок
 * координат для 2ГИС (lon,lat!).
 */
const MOSCOW: Point = { id: 'moscow', latitude: 55.7558, longitude: 37.6173 };
const SPB: Point = { id: 'spb', latitude: 59.9343, longitude: 30.3351 };
const KAZAN: Point = { id: 'kazan', latitude: 55.796127, longitude: 49.106414 };

describe('haversine', () => {
  it('returns 0 for identical points', () => {
    expect(haversine(MOSCOW, MOSCOW)).toBe(0);
  });

  it('returns ~633 km between Moscow and SPB', () => {
    const d = haversine(MOSCOW, SPB);
    // Реальное расстояние ~633 км; допуск 5 км.
    expect(d).toBeGreaterThan(628_000);
    expect(d).toBeLessThan(638_000);
  });

  it('is symmetric', () => {
    expect(haversine(MOSCOW, SPB)).toBeCloseTo(haversine(SPB, MOSCOW), 3);
  });
});

describe('optimizeOrder', () => {
  it('returns single point unchanged', () => {
    expect(optimizeOrder(null, [MOSCOW])).toEqual([MOSCOW]);
  });

  it('returns empty array unchanged', () => {
    expect(optimizeOrder(null, [])).toEqual([]);
  });

  it('picks nearest neighbor from origin first', () => {
    // origin = Moscow → ближе Казань (~800 км), чем SPB (~633 км)?
    // Актуально: SPB ближе, значит первым идёт SPB.
    const result = optimizeOrder(MOSCOW, [KAZAN, SPB]);
    expect(result[0].id).toBe('spb');
    expect(result[1].id).toBe('kazan');
  });

  it('without origin puts first point as start and reorders rest', () => {
    // Без origin: Moscow — стартовая, потом ближайший — SPB, потом Казань.
    const result = optimizeOrder(null, [MOSCOW, KAZAN, SPB]);
    expect(result[0].id).toBe('moscow');
    expect(result[1].id).toBe('spb');
    expect(result[2].id).toBe('kazan');
  });

  it('all input points appear exactly once in output', () => {
    const points = [MOSCOW, KAZAN, SPB];
    const result = optimizeOrder(null, points);
    const ids = result.map((p) => p.id).sort();
    expect(ids).toEqual(['kazan', 'moscow', 'spb']);
  });
});

describe('buildYandexUrl', () => {
  it('includes rtext with all coords in lat,lon order joined by ~', () => {
    const url = buildYandexUrl(MOSCOW, [SPB]);
    expect(url).toContain('rtext=55.755800,37.617300~59.934300,30.335100');
  });

  it('starts rtext with tilde when origin is null (empty origin segment)', () => {
    const url = buildYandexUrl(null, [SPB]);
    expect(url).toContain('rtext=~59.934300,30.335100');
  });

  it('centers map on origin when provided (ll=lon,lat)', () => {
    const url = buildYandexUrl(MOSCOW, [SPB]);
    // Яндекс принимает ll в порядке lon,lat.
    expect(url).toContain('ll=37.617300,55.755800');
  });

  it('centers map on first point when origin is null', () => {
    const url = buildYandexUrl(null, [SPB]);
    expect(url).toContain('ll=30.335100,59.934300');
  });

  it('empty points → base yandex maps URL', () => {
    expect(buildYandexUrl(null, [])).toBe('https://yandex.ru/maps/');
  });

  it('uses toFixed(6) format (no scientific notation)', () => {
    const tiny: Point = { id: 't', latitude: 0.0000001, longitude: -0.0000001 };
    const url = buildYandexUrl(null, [tiny]);
    // 0.0000001.toFixed(6) → "0.000000", -0.0000001.toFixed(6) → "-0.000000".
    expect(url).toContain('~0.000000,-0.000000');
    expect(url).not.toContain('e-');
  });
});

describe('buildYandexLookupUrl', () => {
  it('creates single-destination route URL', () => {
    const url = buildYandexLookupUrl(SPB);
    expect(url).toBe(
      'https://yandex.ru/maps/?rtext=~59.934300,30.335100&rtt=auto&ll=30.335100,59.934300&z=17',
    );
  });
});

describe('buildGoogleUrl', () => {
  it('empty points → base URL', () => {
    expect(buildGoogleUrl(null, [])).toBe('https://www.google.com/maps/');
  });

  it('single point → destination only, no waypoints', () => {
    const url = buildGoogleUrl(MOSCOW, [SPB]);
    expect(url).toContain('destination=59.934300%2C30.335100');
    expect(url).toContain('origin=55.755800%2C37.617300');
    expect(url).not.toContain('waypoints=');
  });

  it('multi-point → last is destination, previous are waypoints joined by |', () => {
    const url = buildGoogleUrl(MOSCOW, [SPB, KAZAN]);
    expect(url).toContain('destination=55.796127%2C49.106414');
    // '|' в URL кодируется как %7C.
    expect(url).toContain('waypoints=59.934300%2C30.335100');
  });

  it('null origin → empty origin param, not "null"', () => {
    const url = buildGoogleUrl(null, [SPB]);
    // URLSearchParams склеивает как «...&origin=» (в конце строки),
    // либо «...&origin=&...». Важно, что после «origin=» не «null».
    expect(url).toMatch(/[?&]origin=(&|$)/);
    expect(url).not.toContain('origin=null');
  });
});

describe('build2GisUrl', () => {
  it('formats coords as lon,lat (2GIS-specific!) joined by |', () => {
    const url = build2GisUrl(null, [MOSCOW, SPB]);
    // ВАЖНО: 2ГИС — lon,lat, не lat,lon.
    expect(url).toBe(
      'https://2gis.ru/routeSearch/rsType/car/points/37.617300,55.755800|30.335100,59.934300',
    );
  });

  it('includes origin at start when provided', () => {
    const url = build2GisUrl(MOSCOW, [SPB]);
    expect(url).toContain('37.617300,55.755800|30.335100,59.934300');
  });
});
