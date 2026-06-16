/**
 * F6. Dev-only workaround: бэкенд в локальной разработке отдаёт media-
 * URL'ы с хостом 10.0.2.2 — это специальный IP Android-эмулятора,
 * по которому он достигает хост-машину. Web-браузер в Windows этого
 * IP не понимает (нет ADB-проксирования), поэтому подменяем хост
 * на localhost.
 *
 * На production бэк должен отдавать публичный домен MinIO/CDN —
 * туда веб попадёт нормально. Тогда эта функция станет no-op
 * (никаких 10.0.2.2 в URL не будет).
 */
export function rewriteEmulatorHost(url: string | null): string | null {
  if (!url) return url;
  return url
    .replace(/^http:\/\/10\.0\.2\.2(?=[:/])/, 'http://localhost')
    .replace(/^https:\/\/10\.0\.2\.2(?=[:/])/, 'https://localhost');
}
