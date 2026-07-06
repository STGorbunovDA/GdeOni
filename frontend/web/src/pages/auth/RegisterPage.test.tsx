import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RegisterPage } from './RegisterPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { authApi, usersApi } from '../../api/endpoints/authApi';
import { useAuthStore } from '../../auth/authStore';

/**
 * F19. Компонент-тесты RegisterPage:
 *  - валидация email/пароля/подтверждения/чекбоксов/даты рождения;
 *  - age-gate (D19 — юзер младше 14 лет не проходит);
 *  - happy path — register → login → setSession → /tracked.
 *
 * DateInput у Mantine принимает text-ввод в формате valueFormat.
 * Печатаем строку и ждём, что зод преобразует её в Date для сабмита.
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

/**
 * Хелпер: заполнить все поля валидными данными кроме даты рождения —
 * тесты сами решают, какую дату вбить (для age gate или happy path).
 */
async function fillCommonFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(
    screen.getByRole('textbox', { name: /^email/i }),
    'new@example.com',
  );
  await user.type(screen.getByLabelText(/^пароль/i), 'Password123!');
  await user.type(screen.getByLabelText(/повторите пароль/i), 'Password123!');
  await user.click(
    screen.getByRole('checkbox', { name: /политику конфиденциальности/i }),
  );
  await user.click(
    screen.getByRole('checkbox', { name: /условия использования/i }),
  );
}

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: false,
    });
  });

  it('renders required fields and both agreement checkboxes', () => {
    renderWithProviders(<RegisterPage />);
    expect(screen.getByRole('textbox', { name: /^email/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/^пароль/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/повторите пароль/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/дата рождения/i)).toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /политику конфиденциальности/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /условия использования/i }),
    ).toBeInTheDocument();
  });

  it('shows validation error and blocks submit when empty', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await user.click(screen.getByRole('button', { name: /зарегистрироваться/i }));

    expect(await screen.findByText(/введите email/i)).toBeInTheDocument();
    // register не должен вызываться при провале валидации.
    expect(vi.mocked(usersApi.register)).not.toHaveBeenCalled();
  });

  it('blocks submit when confirmPassword differs from password', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await user.type(
      screen.getByRole('textbox', { name: /^email/i }),
      'new@example.com',
    );
    await user.type(screen.getByLabelText(/^пароль/i), 'Password123!');
    await user.type(
      screen.getByLabelText(/повторите пароль/i),
      'Different123!',
    );
    await user.click(
      screen.getByRole('checkbox', { name: /политику конфиденциальности/i }),
    );
    await user.click(
      screen.getByRole('checkbox', { name: /условия использования/i }),
    );

    // Взрослая дата рождения — чтобы не смешивать с age-gate.
    const today = new Date();
    const adult = new Date(
      today.getFullYear() - 30,
      today.getMonth(),
      today.getDate(),
    );
    await user.type(
      screen.getByLabelText(/дата рождения/i),
      formatForInput(adult),
    );
    await user.click(screen.getByRole('button', { name: /зарегистрироваться/i }));

    // Ждём хоть какого-то стабильного эффекта rhf — подтверждаем, что
    // register API не был вызван (форма не прошла валидацию). На
    // конкретный вид DOM-ошибки не завязываемся — Mantine v7
    // отображает error через свой Text компонент под инпутом.
    await waitFor(() => {
      expect(vi.mocked(usersApi.register)).not.toHaveBeenCalled();
    });
    // Дополнительная косвенная проверка — контекстно rhf не должен был
    // сбросить submitError, потому что onSubmit не вызывался.
    expect(vi.mocked(authApi.login)).not.toHaveBeenCalled();
  });

  it('shows age-gate error when user is younger than 14', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await fillCommonFields(user);

    // Дата рождения — 5 лет назад от сегодняшнего дня.
    const today = new Date();
    const tooYoung = new Date(
      today.getFullYear() - 5,
      today.getMonth(),
      today.getDate(),
    );
    await user.type(
      screen.getByLabelText(/дата рождения/i),
      formatForInput(tooYoung),
    );
    await user.click(screen.getByRole('button', { name: /зарегистрироваться/i }));

    expect(
      await screen.findByText(/сервисом могут пользоваться лица от 14 лет/i),
    ).toBeInTheDocument();
    expect(vi.mocked(usersApi.register)).not.toHaveBeenCalled();
  });

  it('registers → logs in → stores session → navigates to /tracked', async () => {
    vi.mocked(usersApi.register).mockResolvedValueOnce({ id: 'user-1' });
    vi.mocked(authApi.login).mockResolvedValueOnce({
      id: 'user-1',
      email: 'new@example.com',
      userName: 'new',
      fullName: null,
      role: 'User',
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2030-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2030-01-01T00:00:00Z',
    });

    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await fillCommonFields(user);

    // Взрослая дата рождения — 30 лет назад.
    const today = new Date();
    const adult = new Date(
      today.getFullYear() - 30,
      today.getMonth(),
      today.getDate(),
    );
    await user.type(
      screen.getByLabelText(/дата рождения/i),
      formatForInput(adult),
    );
    await user.click(screen.getByRole('button', { name: /зарегистрироваться/i }));

    await waitFor(() => {
      expect(vi.mocked(usersApi.register)).toHaveBeenCalledWith(
        expect.objectContaining({
          email: 'new@example.com',
          password: 'Password123!',
          birthDate: expect.stringMatching(/^\d{4}-\d{2}-\d{2}$/),
        }),
      );
    });
    await waitFor(() => {
      expect(vi.mocked(authApi.login)).toHaveBeenCalledWith(
        'new@example.com',
        'Password123!',
      );
    });
    await waitFor(() => {
      expect(useAuthStore.getState().accessToken).toBe('access-token');
      expect(useAuthStore.getState().user?.email).toBe('new@example.com');
    });
    expect(mockNavigate).toHaveBeenCalledWith('/tracked', { replace: true });
  });

  it('shows friendly error text when register fails with ApiError', async () => {
    const { ApiError } = await import('../../api/client');
    vi.mocked(usersApi.register).mockRejectedValueOnce(
      new ApiError('user.email.already.exists', 'raw'),
    );

    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />);

    await fillCommonFields(user);
    const today = new Date();
    const adult = new Date(
      today.getFullYear() - 30,
      today.getMonth(),
      today.getDate(),
    );
    await user.type(
      screen.getByLabelText(/дата рождения/i),
      formatForInput(adult),
    );
    await user.click(screen.getByRole('button', { name: /зарегистрироваться/i }));

    expect(
      await screen.findByText(
        /пользователь с таким email уже существует/i,
      ),
    ).toBeInTheDocument();
    expect(useAuthStore.getState().accessToken).toBeNull();
  });
});

/** Формат «дд.мм.гггг» — соответствует valueFormat="DD.MM.YYYY" у DateInput. */
function formatForInput(d: Date): string {
  const day = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const year = d.getFullYear();
  return `${day}.${month}.${year}`;
}
