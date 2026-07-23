import { useQuery } from '@tanstack/react-query';
import { appApi, type AppFeatures } from '../api/endpoints/appApi';
import { useIsAuthenticated } from '../auth/authStore';

/**
 * D36 / F22. Тянет /api/app/features один раз на сессию и кеширует
 * через TanStack Query. Используется в buildMediaUrl и в F22 paywall.
 *
 * staleTime=∞ — features не меняются между релизами; обновление
 * прилетит автоматически при перезагрузке страницы (новый build).
 *
 * Запрос идёт только если юзер авторизован, иначе бэк отдаст 401.
 */
export function useAppFeatures(): {
  data: AppFeatures | undefined;
  isLoading: boolean;
  isError: boolean;
} {
  const isAuthenticated = useIsAuthenticated();
  const query = useQuery({
    queryKey: ['app-features'],
    queryFn: () => appApi.features(),
    enabled: isAuthenticated,
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
