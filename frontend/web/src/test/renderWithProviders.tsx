import type { ReactNode } from 'react';
import { render } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { theme } from '../design/theme';

/**
 * F19. Единая обёртка для компонент-тестов. Даёт минимальный набор
 * контекста, который нужен большинству страниц: Mantine (стили,
 * primaryColor), TanStack Query (useQuery/useMutation), React Router
 * (`Link`, `useNavigate`).
 *
 * Каждый вызов создаёт свежий QueryClient с disabled-retry, чтобы
 * ошибочные ответы моков не подвешивали тест на 3 retries × 1s.
 */
export function renderWithProviders(
  ui: ReactNode,
  options?: { initialRoute?: string },
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme}>
        <MemoryRouter initialEntries={[options?.initialRoute ?? '/']}>
          {ui}
        </MemoryRouter>
      </MantineProvider>
    </QueryClientProvider>,
  );
}
