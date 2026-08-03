import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Детект выката новой версии для УЖЕ ОТКРЫТОЙ сессии.
 *
 * `index.html` отдаётся с `Cache-Control: no-cache` (nginx), поэтому его
 * можно дёшево перезапрашивать и сверять имя главного бандла
 * (`/assets/index-<hash>.js`): при каждой сборке хэш меняется. Если у
 * сервера бандл стал другим, чем тот, с которым запущена вкладка, — значит
 * вышло обновление, показываем плашку «Обновить».
 *
 * Проверяем: при монтировании, при возврате в приложение (visibilitychange /
 * focus — главный кейс для PWA: свернул-развернул) и раз в 5 минут для
 * долго открытых вкладок. В dev-режиме бездействует (нет /assets/index-*).
 */
const BUNDLE_RE = /\/assets\/index-[A-Za-z0-9_-]+\.js/;
const POLL_MS = 5 * 60 * 1000;

function bundleFromHtml(html: string): string | null {
  const m = html.match(BUNDLE_RE);
  return m ? m[0] : null;
}

/** Имя бандла, с которым РЕАЛЬНО запущена текущая вкладка — из DOM. */
function runningBundle(): string | null {
  const el = document.querySelector('script[src*="/assets/index-"]');
  const src = el?.getAttribute('src');
  if (!src) return null;
  const m = src.match(BUNDLE_RE);
  return m ? m[0] : null;
}

export function useAppUpdate(): { updateAvailable: boolean; reload: () => void } {
  const [updateAvailable, setUpdateAvailable] = useState(false);
  const knownRef = useRef<string | null>(runningBundle());

  useEffect(() => {
    if (!import.meta.env.PROD) return;

    let stopped = false;

    async function check() {
      if (document.visibilityState === 'hidden') return;
      try {
        const res = await fetch('/index.html', { cache: 'no-store' });
        if (!res.ok) return;
        const latest = bundleFromHtml(await res.text());
        if (!latest || stopped) return;
        if (knownRef.current === null) {
          // Свою сборку из DOM прочитать не удалось — первый ответ = baseline.
          knownRef.current = latest;
          return;
        }
        if (latest !== knownRef.current) setUpdateAvailable(true);
      } catch {
        // офлайн / сбой сети — тихо, повторим на следующей проверке
      }
    }

    check();
    const interval = window.setInterval(check, POLL_MS);
    const onVisible = () => {
      if (document.visibilityState === 'visible') check();
    };
    document.addEventListener('visibilitychange', onVisible);
    window.addEventListener('focus', onVisible);

    return () => {
      stopped = true;
      window.clearInterval(interval);
      document.removeEventListener('visibilitychange', onVisible);
      window.removeEventListener('focus', onVisible);
    };
  }, []);

  const reload = useCallback(() => window.location.reload(), []);

  return { updateAvailable, reload };
}
