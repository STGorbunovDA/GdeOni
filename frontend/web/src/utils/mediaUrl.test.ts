import { describe, it, expect } from 'vitest';
import { buildMediaUrl } from './mediaUrl';

/**
 * F19. Тесты buildMediaUrl: сборка URL к MinIO с dev-хостовой подменой
 * 10.0.2.2 → localhost и корректным encodeURIComponent для storage_key.
 */
describe('buildMediaUrl', () => {
  it('returns null when mediaBaseUrl is missing', () => {
    expect(buildMediaUrl(undefined, 'photos', 'key.jpg')).toBeNull();
  });

  it('returns null when bucket is missing', () => {
    expect(
      buildMediaUrl('http://localhost:9000', null, 'key.jpg'),
    ).toBeNull();
    expect(
      buildMediaUrl('http://localhost:9000', undefined, 'key.jpg'),
    ).toBeNull();
  });

  it('returns null when storageKey is missing', () => {
    expect(
      buildMediaUrl('http://localhost:9000', 'photos', null),
    ).toBeNull();
    expect(
      buildMediaUrl('http://localhost:9000', 'photos', undefined),
    ).toBeNull();
  });

  it('joins base + bucket + storageKey', () => {
    expect(
      buildMediaUrl('http://localhost:9000', 'photos', 'a/b/c.jpg'),
    ).toBe('http://localhost:9000/photos/a%2Fb%2Fc.jpg');
  });

  it('strips trailing slashes from mediaBaseUrl', () => {
    expect(
      buildMediaUrl('http://localhost:9000///', 'photos', 'k.jpg'),
    ).toBe('http://localhost:9000/photos/k.jpg');
  });

  it('rewrites http://10.0.2.2 → http://localhost for dev', () => {
    expect(
      buildMediaUrl('http://10.0.2.2:9000', 'photos', 'k.jpg'),
    ).toBe('http://localhost:9000/photos/k.jpg');
  });

  it('rewrites https://10.0.2.2 → https://localhost', () => {
    expect(
      buildMediaUrl('https://10.0.2.2:9000', 'photos', 'k.jpg'),
    ).toBe('https://localhost:9000/photos/k.jpg');
  });

  it('does not touch production hosts', () => {
    expect(
      buildMediaUrl('https://files.gdeoni.ru', 'photos', 'k.jpg'),
    ).toBe('https://files.gdeoni.ru/photos/k.jpg');
  });

  it('encodes special characters in storageKey', () => {
    expect(
      buildMediaUrl('http://localhost:9000', 'photos', 'a b?c.jpg'),
    ).toBe('http://localhost:9000/photos/a%20b%3Fc.jpg');
  });
});
