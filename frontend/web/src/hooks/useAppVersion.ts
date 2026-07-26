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
 * Web-версия приложения для футера — короткий git-SHA + дата сборки
 * (или явный VITE_APP_VERSION из окружения). Значение подставляет Vite на
 * этапе build через `define` (см. vite.config.ts → `__APP_VERSION__`),
 * поэтому оно меняется КАЖДЫМ деплоем, а не «застывает» на 1.0.0.
 * В локальной разработке / если git недоступен — 'dev'.
 */
declare const __APP_VERSION__: string;
export const CURRENT_APP_VERSION: string = __APP_VERSION__;
