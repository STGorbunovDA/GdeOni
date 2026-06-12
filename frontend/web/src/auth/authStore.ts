import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/**
 * F1. Минимальный auth store на Zustand. Хранит access + refresh
 * токены в localStorage (persist middleware). Для F1 — только для того,
 * чтобы ProtectedRoute мог проверить isAuthenticated. Реальный
 * login/logout flow приходит в F4. На mobile аналог — AuthService.
 */
type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  setTokens: (access: string, refresh: string) => void;
  clear: () => void;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      setTokens: (access, refresh) => set({ accessToken: access, refreshToken: refresh }),
      clear: () => set({ accessToken: null, refreshToken: null }),
    }),
    { name: 'gdeoni-auth' },
  ),
);

export const useIsAuthenticated = () =>
  useAuthStore((s) => s.accessToken !== null);
