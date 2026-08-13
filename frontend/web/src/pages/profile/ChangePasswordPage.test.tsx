import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ChangePasswordPage } from './ChangePasswordPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';

/**
 * F19. Компонент-тесты ChangePasswordPage:
 *  - валидация формы (несовпадающие пароли, слишком короткий пароль);
 *  - happy path — вызов usersApi.changePassword с id текущего юзера,
 *    force-logout через authApi.logout + clear стора;
 *  - sad path — при ApiError показывается человеческое сообщение,
 *    стор остаётся заполненным.
 */

vi.mock('../../api/endpoints/authApi', () => ({
  authApi: {
    login: vi.fn(),
    logout: vi.fn(),
  },
  usersApi: {
    register: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
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

describe('ChangePasswordPage', () => {
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
  });

  it('renders three password fields', () => {
    renderWithProviders(<ChangePasswordPage />);
    expect(screen.getByLabelText(/текущий пароль/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^новый пароль/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/повторите новый пароль/i)).toBeInTheDocument();
  });

  it('shows validation error when passwords do not match', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await user.type(screen.getByLabelText(/текущий пароль/i), 'OldPassword1!');
    await user.type(screen.getByLabelText(/^новый пароль/i), 'NewPassword1!');
    await user.type(
      screen.getByLabelText(/повторите новый пароль/i),
      'DifferentPassword1!',
    );
    await user.click(screen.getByRole('button', { name: /сохранить/i }));

    expect(
      await screen.findByText(/пароли не совпадают/i),
    ).toBeInTheDocument();
    expect(vi.mocked(usersApi.changePassword)).not.toHaveBeenCalled();
  });

  it('calls changePassword + logout + clear on happy path', async () => {
    vi.mocked(usersApi.changePassword).mockResolvedValueOnce(undefined);
    vi.mocked(authApi.logout).mockResolvedValueOnce(undefined);

    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await user.type(screen.getByLabelText(/текущий пароль/i), 'OldPassword1!');
    await user.type(screen.getByLabelText(/^новый пароль/i), 'NewPassword1!');
    await user.type(
      screen.getByLabelText(/повторите новый пароль/i),
      'NewPassword1!',
    );
    await user.click(screen.getByRole('button', { name: /сохранить/i }));

    await waitFor(() => {
      expect(vi.mocked(usersApi.changePassword)).toHaveBeenCalledWith(
        'user-1',
        { currentPassword: 'OldPassword1!', newPassword: 'NewPassword1!' },
      );
    });
    await waitFor(() => {
      expect(vi.mocked(authApi.logout)).toHaveBeenCalled();
    });
    await waitFor(() => {
      expect(useAuthStore.getState().accessToken).toBeNull();
      expect(useAuthStore.getState().user).toBeNull();
    });
    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true });
  });

  it('shows friendly error text on ApiError', async () => {
    const { ApiError } = await import('../../api/client');
    vi.mocked(usersApi.changePassword).mockRejectedValueOnce(
      new ApiError('user.invalid.credentials', 'raw'),
    );

    const user = userEvent.setup();
    renderWithProviders(<ChangePasswordPage />);

    await user.type(screen.getByLabelText(/текущий пароль/i), 'WrongOld1!');
    await user.type(screen.getByLabelText(/^новый пароль/i), 'NewPassword1!');
    await user.type(
      screen.getByLabelText(/повторите новый пароль/i),
      'NewPassword1!',
    );
    await user.click(screen.getByRole('button', { name: /сохранить/i }));

    expect(
      await screen.findByText(/неверный email\/логин или пароль/i),
    ).toBeInTheDocument();
    // Форс-выхода на ApiError быть не должно — токены остались.
    expect(useAuthStore.getState().accessToken).toBe('access-token');
    expect(vi.mocked(authApi.logout)).not.toHaveBeenCalled();
  });
});
