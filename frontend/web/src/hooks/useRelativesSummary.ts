import { useQuery } from '@tanstack/react-query';
import {
  relativesApi,
  type RelativesSummary,
} from '../api/endpoints/relativesApi';
import { useIsAuthenticated } from '../auth/authStore';

/**
 * Функция «Родственники» (Фаза 4). Сводка для попапа «События» (новые
 * родственники + непрочитанные сообщения) и бейджа вкладки «Родственники».
 *
 * Общий queryKey ['relatives-summary'] — TanStack Query дедуплицирует запрос,
 * поэтому AppLayout (бейдж) и EventsPopup (попап) используют одну загрузку.
 *
 * Доступно любому вошедшему (эндпоинт BasicAuthenticated, без paywall).
 * refetchInterval поддерживает бейдж свежим: после чтения диалога
 * непрочитанные подтянутся сами, даже если инвалидация не сработала.
 */
export const RELATIVES_SUMMARY_KEY = ['relatives-summary'] as const;

export function useRelativesSummary(): {
  data: RelativesSummary | undefined;
  isLoading: boolean;
} {
  const isAuthenticated = useIsAuthenticated();
  const query = useQuery({
    queryKey: RELATIVES_SUMMARY_KEY,
    queryFn: () => relativesApi.getSummary(),
    enabled: isAuthenticated,
    staleTime: 30_000,
    refetchInterval: 60_000,
    refetchOnWindowFocus: true,
  });

  return { data: query.data, isLoading: query.isLoading };
}

/** Число «требующих внимания» для бейджа: новые родственники + непрочитанные диалоги. */
export function relativesBadgeCount(summary: RelativesSummary | undefined): number {
  if (!summary) return 0;
  return summary.newRelatives.length + summary.unreadConversations.length;
}
