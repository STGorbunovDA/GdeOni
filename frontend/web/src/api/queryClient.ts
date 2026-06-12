import { QueryClient } from '@tanstack/react-query';

/**
 * F1. TanStack Query клиент с дефолтами:
 * - 5 минут staleTime (как на mobile для features-кеша);
 * - 1 retry на сетевой сбой;
 * - refetchOnWindowFocus — параллель OnAppearing/LoadAsync в MAUI:
 *   когда юзер возвращается на вкладку, данные обновляются.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
});
