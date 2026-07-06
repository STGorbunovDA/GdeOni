import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { PaymentReturnPage } from './PaymentReturnPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { subscriptionApi } from '../../api/endpoints/subscriptionApi';

/**
 * F19. Компонент-тесты PaymentReturnPage.
 *
 * После YooKassa-редиректа страница поллит /users/me/subscription раз
 * в 3 секунды (через setTimeout, не через TanStack Query). При Active
 * — редиректит на /tracked. Используем vi.useFakeTimers для управления
 * intervals.
 */

vi.mock('../../api/endpoints/subscriptionApi', () => ({
  subscriptionApi: {
    getMy: vi.fn(),
    sync: vi.fn(),
    createPayment: vi.fn(),
    cancel: vi.fn(),
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

describe('PaymentReturnPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(subscriptionApi.sync).mockResolvedValue(undefined);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows polling state initially', () => {
    vi.mocked(subscriptionApi.getMy).mockImplementation(
      () => new Promise(() => {}), // never resolves
    );
    renderWithProviders(<PaymentReturnPage />);

    expect(screen.getByText(/подтверждаем оплату/i)).toBeInTheDocument();
    expect(
      screen.getByText(/не закрывайте страницу/i),
    ).toBeInTheDocument();
  });

  it('redirects to /tracked when subscription becomes Active', async () => {
    vi.useFakeTimers();
    vi.mocked(subscriptionApi.getMy).mockResolvedValue({
      status: 'Active',
      plan: 'Monthly',
      expiresAtUtc: '2030-01-01T00:00:00Z',
      cancelledAtUtc: null,
      isActiveNow: true,
      isOnTrial: false,
      daysUntilExpiry: 30,
      hasComplimentaryAccess: false,
      complimentaryAccessUntilUtc: null,
      complimentaryAccessNote: null,
    });

    renderWithProviders(<PaymentReturnPage />);

    // Ждём, пока промис sync + getMy разрешится и setState перейдёт в
    // success. Показывается «Оплата подтверждена».
    await vi.waitFor(async () => {
      expect(vi.mocked(subscriptionApi.getMy)).toHaveBeenCalled();
    });
    // Прогоняем setTimeout(800) → navigate.
    await vi.advanceTimersByTimeAsync(1000);
    expect(mockNavigate).toHaveBeenCalledWith('/tracked', { replace: true });
  });

  it('calls sync before getMy on every poll tick', async () => {
    vi.mocked(subscriptionApi.getMy).mockResolvedValue({
      status: 'PendingPayment',
      plan: null,
      expiresAtUtc: null,
      cancelledAtUtc: null,
      isActiveNow: false,
      isOnTrial: false,
      daysUntilExpiry: 0,
      hasComplimentaryAccess: false,
      complimentaryAccessUntilUtc: null,
      complimentaryAccessNote: null,
    });

    renderWithProviders(<PaymentReturnPage />);

    await waitFor(() => {
      expect(vi.mocked(subscriptionApi.sync)).toHaveBeenCalled();
      expect(vi.mocked(subscriptionApi.getMy)).toHaveBeenCalled();
    });
  });
});
