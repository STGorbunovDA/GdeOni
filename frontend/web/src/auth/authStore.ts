import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/**
 * F1 / F2.1. Auth store на Zustand. Хранит токены + роль в
 * localStorage (persist middleware).
 *
 * F2.1: добавлено поле `role` — пока mock. В F4 значение будет
 * приходить из JWT-claim `ClaimTypes.Role`. Mobile-аналог —
 * AuthService.CurrentRole. На вебе используется в AppLayout для
 * показа пункта "Админка" только админам.
 */
export type UserRole = 'User' | 'Admin' | 'SuperAdmin';

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  role: UserRole | null;
  setSession: (access: string, refresh: string, role: UserRole) => void;
  clear: () => void;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      role: null,
      setSession: (access, refresh, role) =>
        set({ accessToken: access, refreshToken: refresh, role }),
      clear: () => set({ accessToken: null, refreshToken: null, role: null }),
    }),
    { name: 'gdeoni-auth' },
  ),
);

export const useIsAuthenticated = () =>
  useAuthStore((s) => s.accessToken !== null);

/** True для Admin или SuperAdmin. Зеркало ICurrentUserService.IsAdmin() на бэке. */
export const useIsAdmin = () =>
  useAuthStore((s) => s.role === 'Admin' || s.role === 'SuperAdmin');
