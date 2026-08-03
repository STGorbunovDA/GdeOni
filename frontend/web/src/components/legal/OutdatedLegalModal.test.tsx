import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { render } from '@testing-library/react';
import { OutdatedLegalModal } from './OutdatedLegalModal';
import { legalApi } from '../../api/endpoints/legalApi';
import { usersApi } from '../../api/endpoints/authApi';
import { theme } from '../../design/theme';

/**
 * F19. Тесты OutdatedLegalModal:
 *  - модалка не рендерится когда hasOutdatedLegalAcceptance=false;
 *  - модалка рендерится когда флаг true, кнопка «Принимаю» disabled
 *    до чекбокса;
 *  - при клике «Принимаю» дёргается legalApi.accept с актуальными
 *    версиями.
 */

vi.mock('../../api/endpoints/legalApi', () => ({
  legalApi: {
    getPrivacyPolicy: vi.fn(),
    getTermsOfUse: vi.fn(),
    accept: vi.fn(),
  },
}));

vi.mock('../../api/endpoints/authApi', () => ({
  usersApi: {
    me: vi.fn(),
    register: vi.fn(),
    changePassword: vi.fn(),
  },
  authApi: {
    login: vi.fn(),
    logout: vi.fn(),
  },
}));

function renderModal(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme}>
        <Notifications />
        <MemoryRouter>{ui}</MemoryRouter>
      </MantineProvider>
    </QueryClientProvider>,
  );
}

describe('OutdatedLegalModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('does not render when hasOutdatedLegalAcceptance is false', async () => {
    vi.mocked(usersApi.me).mockResolvedValue({
      id: 'u1',
      email: 'u@example.com',
      userName: 'u',
      fullName: null,
      city: null,
      role: 'User',
      privacyPolicyVersion: 1,
      termsVersion: 1,
      hasOutdatedLegalAcceptance: false,
      isEmailConfirmed: true,
      allowRelativeConnections: true,
    });
    renderModal(<OutdatedLegalModal />);

    await waitFor(() => {
      expect(vi.mocked(usersApi.me)).toHaveBeenCalled();
    });
    expect(
      screen.queryByText(/мы обновили правила/i),
    ).not.toBeInTheDocument();
  });

  it('renders modal and disables submit until checkbox is checked', async () => {
    vi.mocked(usersApi.me).mockResolvedValue({
      id: 'u1',
      email: 'u@example.com',
      userName: 'u',
      fullName: null,
      city: null,
      role: 'User',
      privacyPolicyVersion: 1,
      termsVersion: 1,
      hasOutdatedLegalAcceptance: true,
      isEmailConfirmed: true,
      allowRelativeConnections: true,
    });
    vi.mocked(legalApi.getPrivacyPolicy).mockResolvedValue({
      documentKey: 'privacy',
      version: 2,
      url: '/legal/privacy',
      bodyMarkdown: null,
    });
    vi.mocked(legalApi.getTermsOfUse).mockResolvedValue({
      documentKey: 'terms',
      version: 3,
      url: '/legal/terms',
      bodyMarkdown: null,
    });

    renderModal(<OutdatedLegalModal />);

    expect(
      await screen.findByRole('button', { name: /принимаю и продолжаю/i }),
    ).toBeDisabled();
    expect(screen.getByText(/мы обновили правила/i)).toBeInTheDocument();
  });

  it('calls accept with fetched versions when checkbox is checked', async () => {
    vi.mocked(usersApi.me).mockResolvedValue({
      id: 'u1',
      email: 'u@example.com',
      userName: 'u',
      fullName: null,
      city: null,
      role: 'User',
      privacyPolicyVersion: 1,
      termsVersion: 1,
      hasOutdatedLegalAcceptance: true,
      isEmailConfirmed: true,
      allowRelativeConnections: true,
    });
    vi.mocked(legalApi.getPrivacyPolicy).mockResolvedValue({
      documentKey: 'privacy',
      version: 5,
      url: '/legal/privacy',
      bodyMarkdown: null,
    });
    vi.mocked(legalApi.getTermsOfUse).mockResolvedValue({
      documentKey: 'terms',
      version: 7,
      url: '/legal/terms',
      bodyMarkdown: null,
    });
    vi.mocked(legalApi.accept).mockResolvedValue(undefined);

    const user = userEvent.setup();
    renderModal(<OutdatedLegalModal />);

    // Ждём, пока модалка отрендерится и подтянет метадату.
    await screen.findByRole('button', { name: /принимаю и продолжаю/i });
    await waitFor(() => {
      expect(vi.mocked(legalApi.getPrivacyPolicy)).toHaveBeenCalled();
    });

    await user.click(
      screen.getByRole('checkbox', { name: /принимаю обновлённые/i }),
    );
    await user.click(
      screen.getByRole('button', { name: /принимаю и продолжаю/i }),
    );

    await waitFor(() => {
      expect(vi.mocked(legalApi.accept)).toHaveBeenCalledWith({
        privacyPolicyVersion: 5,
        termsVersion: 7,
      });
    });
  });
});
