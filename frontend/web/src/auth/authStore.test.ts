import { describe, it, expect, beforeEach } from 'vitest';
import {
  useAuthStore,
  useIsAuthenticated,
  useIsAdmin,
  type AuthUser,
} from './authStore';
import { renderHook } from '@testing-library/react';

/**
 * F19. Тесты Zustand-стора аутентификации: setSession/setTokens/clear
 * и селекторы useIsAuthenticated/useIsAdmin.
 */

const sampleUser: AuthUser = {
  id: 'user-1',
  email: 'user@example.com',
  userName: 'alice',
  fullName: 'Alice Test',
  role: 'User',
};

const adminUser: AuthUser = {
  ...sampleUser,
  id: 'admin-1',
  email: 'admin@example.com',
  role: 'Admin',
};

const superAdminUser: AuthUser = {
  ...sampleUser,
  id: 'super-1',
  role: 'SuperAdmin',
};

describe('useAuthStore', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: false,
    });
    // На случай, если persist-middleware записал localStorage —
    // чистим, чтобы следующий вызов create не подхватил.
    localStorage.clear();
  });

  it('setSession populates tokens and user', () => {
    useAuthStore.getState().setSession('access', 'refresh', sampleUser);
    const state = useAuthStore.getState();
    expect(state.accessToken).toBe('access');
    expect(state.refreshToken).toBe('refresh');
    expect(state.user).toEqual(sampleUser);
  });

  it('setTokens updates only tokens, keeps user', () => {
    useAuthStore.getState().setSession('a', 'r', sampleUser);
    useAuthStore.getState().setTokens('new-a', 'new-r');
    const state = useAuthStore.getState();
    expect(state.accessToken).toBe('new-a');
    expect(state.refreshToken).toBe('new-r');
    expect(state.user).toEqual(sampleUser);
  });

  it('clear resets tokens and user, keeps isBootstrapping', () => {
    useAuthStore.setState({ isBootstrapping: true });
    useAuthStore.getState().setSession('a', 'r', sampleUser);
    useAuthStore.getState().clear();
    const state = useAuthStore.getState();
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.user).toBeNull();
    expect(state.isBootstrapping).toBe(true);
  });

  it('setBootstrapping flips the flag', () => {
    useAuthStore.getState().setBootstrapping(true);
    expect(useAuthStore.getState().isBootstrapping).toBe(true);
    useAuthStore.getState().setBootstrapping(false);
    expect(useAuthStore.getState().isBootstrapping).toBe(false);
  });
});

describe('useIsAuthenticated', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: false,
    });
  });

  it('returns false when accessToken is null', () => {
    const { result } = renderHook(() => useIsAuthenticated());
    expect(result.current).toBe(false);
  });

  it('returns true when accessToken is set', () => {
    useAuthStore.getState().setSession('access', 'refresh', sampleUser);
    const { result } = renderHook(() => useIsAuthenticated());
    expect(result.current).toBe(true);
  });
});

describe('useIsAdmin', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: false,
    });
  });

  it('returns false for a regular User', () => {
    useAuthStore.getState().setSession('a', 'r', sampleUser);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(false);
  });

  it('returns true for Admin', () => {
    useAuthStore.getState().setSession('a', 'r', adminUser);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(true);
  });

  it('returns true for SuperAdmin', () => {
    useAuthStore.getState().setSession('a', 'r', superAdminUser);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(true);
  });

  it('returns false when user is null', () => {
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(false);
  });
});
