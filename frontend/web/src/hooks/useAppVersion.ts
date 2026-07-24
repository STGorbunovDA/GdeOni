import { useQuery } from '@tanstack/react-query';
import { appApi, type AppVersion } from '../api/endpoints/appApi';

/**
 * F22 / D17. Тянет /api/app/version при старте приложения.
 * AllowAnonymous на бэке — работает и без авторизации, чтобы
 * можно было показать блокирующую модалку "обновите страницу"
 * даже когда refresh упал.
 *
 * staleTime=∞ — версия проверяется один раз на сессию.
 */
export function useAppVersion(): {
  data: AppVersion | undefined;
  isLoading: boolean;
  isError: boolean;
} {
  const query = useQuery({
    queryKey: ['app-version'],
    queryFn: () => appApi.version(),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: 1,
  });

  return {
    data: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}

/**
 * Web-версия приложения — это commit SHA или build timestamp,
 * подставляемый Vite через `define`/`VITE_APP_VERSION`.
 *
 * fallback 'dev' используется в локальной разработке, когда
 * скрипта, подставляющего SHA, ещё нет.
 */
export const CURRENT_APP_VERSION: string =
  import.meta.env.VITE_APP_VERSION ?? 'dev';

/**
 * F27. Прямая ссылка на файл APK для кнопок «Скачать APK».
 *
 * Берём из `VITE_APK_FALLBACK_URL` (в .env.production —
 * `https://gdeoni.ru/apk/latest.apk`). Это именно ФАЙЛ, а не страница:
 * `AppVersion.downloadUrl` с бэка ведёт на лендинг `/download` и для
 * прямого скачивания не годится (кнопка на самой `/download` ушла бы
 * в рекурсию). nginx на `/apk/` уже отдаёт файл с
 * `Content-Disposition: attachment`, поэтому браузер сразу скачивает.
 *
 * Fallback `/apk/latest.apk` — same-origin путь на случай, если
 * переменную не задали при сборке.
 */
export const APK_DOWNLOAD_URL: string =
  import.meta.env.VITE_APK_FALLBACK_URL ?? '/apk/latest.apk';
