import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LoginPage } from './LoginPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { authApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';

/**
 * F19. Компонент-тесты LoginPage: happy path (валидные данные →
 * authApi.login → setSession) и sad path (неверные данные →
 * человеческое сообщение в форме).
 */

vi.mock('../../api/endpoints/authApi', () => ({
  authApi: {
    login: vi.fn(),
    logout: vi.fn(),
  },
  usersApi: {},
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: false,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders email and password fields', () => {
    renderWithProviders(<LoginPage />);

    expect(screen.getByRole('textbox', { name: /email/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/пароль/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /войти/i })).toBeInTheDocument();
  });

  it('shows validation errors for empty submit', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.click(screen.getByRole('button', { name: /войти/i }));

    expect(await screen.findByText(/введите email/i)).toBeInTheDocument();
    // authApi.login не должен вызываться при валидационной ошибке.
    expect(vi.mocked(authApi.login)).not.toHaveBeenCalled();
  });

  it('calls authApi.login on happy path and stores session', async () => {
    vi.mocked(authApi.login).mockResolvedValueOnce({
      id: 'user-1',
      email: 'user@example.com',
      userName: 'alice',
      fullName: null,
      role: 'User',
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2030-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2030-01-01T00:00:00Z',
      isEmailConfirmed: true,
    });

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com');
    await user.type(screen.getByLabelText(/пароль/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /войти/i }));

    await waitFor(() => {
      expect(vi.mocked(authApi.login)).toHaveBeenCalledWith(
        'user@example.com',
        'Password123!',
      );
    });

    await waitFor(() => {
      expect(useAuthStore.getState().accessToken).toBe('access-token');
      expect(useAuthStore.getState().user?.email).toBe('user@example.com');
    });
  });

  it('shows friendly error text when login fails with ApiError', async () => {
    const { ApiError } = await import('../../api/client');
    vi.mocked(authApi.login).mockRejectedValueOnce(
      new ApiError('user.invalid.credentials', 'raw'),
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com');
    await user.type(screen.getByLabelText(/пароль/i), 'wrongpass');
    await user.click(screen.getByRole('button', { name: /войти/i }));

    expect(
      await screen.findByText(/неверный email или пароль/i),
    ).toBeInTheDocument();
    // Сессия НЕ должна проставиться при ошибке.
    expect(useAuthStore.getState().accessToken).toBeNull();
  });
});
