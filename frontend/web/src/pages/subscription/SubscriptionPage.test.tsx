import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { render } from '@testing-library/react';
import { SubscriptionPage } from './SubscriptionPage';
import { subscriptionApi, type MySubscription } from '../../api/endpoints/subscriptionApi';
import { useAuthStore } from '../../auth/authStore';
import { theme } from '../../design/theme';

/**
 * F19. Компонент-тесты SubscriptionPage.
 *
 * Рендерим страницу через локальный wrapper, потому что нужны
 * <Notifications /> (Mantine notifications.show звонит через них) и
 * стабильный QueryClient, чтобы вручную инвалидировать между шагами.
 * useSubscription лезет в реальный TanStack Query, поэтому мокируем
 * подложку — сам subscriptionApi.
 */

vi.mock('../../api/endpoints/subscriptionApi', () => ({
  subscriptionApi: {
    getMy: vi.fn(),
    createPayment: vi.fn(),
    cancel: vi.fn(),
    sync: vi.fn(),
    cancelPending: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>(
    'react-router-dom',
  );
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

function renderPage(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0, refetchInterval: false },
      mutations: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme}>
        <Notifications />
        <MemoryRouter initialEntries={['/subscription']}>{ui}</MemoryRouter>
      </MantineProvider>
    </QueryClientProvider>,
  );
}

function trialData(overrides?: Partial<MySubscription>): MySubscription {
  return {
    status: 'Trial',
    plan: null,
    expiresAtUtc: '2030-01-01T00:00:00Z',
    cancelledAtUtc: null,
    isActiveNow: true,
    isOnTrial: true,
    daysUntilExpiry: 20,
    hasComplimentaryAccess: false,
    complimentaryAccessUntilUtc: null,
    complimentaryAccessNote: null,
    ...overrides,
  };
}

describe('SubscriptionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      user: {
        id: 'user-1',
        email: 'user@example.com',
        userName: 'alice',
        fullName: null,
        role: 'User',
      },
      isBootstrapping: false,
    });
    vi.mocked(subscriptionApi.sync).mockResolvedValue(undefined);
  });

  it('shows Trial status with days-until-expiry', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(trialData());
    renderPage(<SubscriptionPage />);

    // "Пробный период" встречается и в Badge, и в описании — findAll.
    const matches = await screen.findAllByText(/пробный период/i);
    expect(matches.length).toBeGreaterThan(0);
    // Кнопка «Оплатить сейчас» доступна в Trial.
    expect(
      screen.getByRole('button', { name: /оплатить сейчас/i }),
    ).toBeInTheDocument();
  });

  it('shows Active status with cancel button', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({
        status: 'Active',
        isOnTrial: false,
        plan: 'Monthly',
      }),
    );
    renderPage(<SubscriptionPage />);

    expect(await screen.findByText(/^активна$/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /отменить подписку/i }),
    ).toBeInTheDocument();
  });

  it('shows PendingPayment with Continue + Refresh + Cancel', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({ status: 'PendingPayment', isOnTrial: false }),
    );
    renderPage(<SubscriptionPage />);

    expect(await screen.findByText(/ожидание оплаты/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /продолжить оплату/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /обновить статус/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /отменить оплату/i }),
    ).toBeInTheDocument();
  });

  it('shows Expired status without cancel button', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({
        status: 'Expired',
        isActiveNow: false,
        isOnTrial: false,
        daysUntilExpiry: 0,
      }),
    );
    renderPage(<SubscriptionPage />);

    expect(await screen.findByText(/^истекла$/i)).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /отменить подписку/i }),
    ).not.toBeInTheDocument();
  });

  it('redirects to YooKassa checkout on Continue payment', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({ status: 'PendingPayment', isOnTrial: false }),
    );
    vi.mocked(subscriptionApi.createPayment).mockResolvedValue({
      checkoutUrl: 'https://yoomoney.example/checkout/123',
      externalPaymentId: 'ext-123',
    });

    // window.location.href мы не можем реально изменить в jsdom, но
    // можно проследить set через spy на assign / replace / href-setter.
    const hrefSpy = vi.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: {
        ...window.location,
        set href(value: string) {
          hrefSpy(value);
        },
      },
    });

    const user = userEvent.setup();
    renderPage(<SubscriptionPage />);
    await user.click(
      await screen.findByRole('button', { name: /продолжить оплату/i }),
    );

    await waitFor(() => {
      expect(vi.mocked(subscriptionApi.createPayment)).toHaveBeenCalledWith(
        'Monthly',
      );
    });
    await waitFor(() => {
      expect(hrefSpy).toHaveBeenCalledWith(
        'https://yoomoney.example/checkout/123',
      );
    });
  });

  it('cancels pending payment via API', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({ status: 'PendingPayment', isOnTrial: false }),
    );
    vi.mocked(subscriptionApi.cancelPending).mockResolvedValue(undefined);

    const user = userEvent.setup();
    renderPage(<SubscriptionPage />);

    await user.click(
      await screen.findByRole('button', { name: /отменить оплату/i }),
    );

    await waitFor(() => {
      expect(vi.mocked(subscriptionApi.cancelPending)).toHaveBeenCalled();
    });
  });

  it('cancels active subscription after modal confirmation', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue(
      trialData({ status: 'Active', isOnTrial: false, plan: 'Monthly' }),
    );
    vi.mocked(subscriptionApi.cancel).mockResolvedValue(undefined);

    const user = userEvent.setup();
    renderPage(<SubscriptionPage />);

    await user.click(
      await screen.findByRole('button', { name: /отменить подписку/i }),
    );
    // Модалка — подтверждающая кнопка «Да, отменить».
    await user.click(
      await screen.findByRole('button', { name: /да, отменить/i }),
    );

    await waitFor(() => {
      expect(vi.mocked(subscriptionApi.cancel)).toHaveBeenCalled();
    });
  });
});
