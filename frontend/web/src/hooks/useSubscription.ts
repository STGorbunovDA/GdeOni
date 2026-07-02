import { useQuery } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { subscriptionApi, type MySubscription } from '../api/endpoints/subscriptionApi';
import { useIsAuthenticated } from '../auth/authStore';

/**
 * F22 / D16. Текущая подписка юзера. Используется для paywall'а
 * (см. RequireSubscription в AppRouter) и блока "Подписка" в
 * ProfilePage.
 *
 * 404 на бэке = "у юзера ещё нет записи". Для админов это норма —
 * они через RegisterUserUseCase не проходят и не получают Trial.
 * Такой случай возвращает `null`, чтобы вызывающий код мог отличить
 * "нет данных" от "ещё грузится".
 *
 * staleTime = 30s: между тиками gate-запросов не пересыпаем, но
 * достаточно быстро подхватываем изменения (например, после
 * успешной оплаты — при возврате на /payment/return).
 */
export function useSubscription(): {
  data: MySubscription | null | undefined;
  isLoading: boolean;
  isError: boolean;
  refetch: () => Promise<unknown>;
} {
  const isAuthenticated = useIsAuthenticated();
  const query = useQuery<MySubscription | null>({
    queryKey: ['subscription', 'me'],
    queryFn: async () => {
      try {
        return await subscriptionApi.getMy();
      } catch (error) {
        if (error instanceof AxiosError && error.response?.status === 404) {
          return null;
        }
        throw error;
      }
    },
    enabled: isAuthenticated,
    staleTime: 30_000,
    retry: 1,
  });

  return {
    data: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
    refetch: query.refetch,
  };
}
