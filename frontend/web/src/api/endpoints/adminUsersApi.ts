import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F17.7. Админ-API по пользователям. Зеркало backend UserRole enum:
 * `RegularUser` / `Manager` / `Admin` / `SuperAdmin`. `Unknown`
 * умышленно не описываем — это sentinel для повреждённого JWT.
 */
export type AdminUserRole = 'RegularUser' | 'Manager' | 'Admin' | 'SuperAdmin';

/** Допустимые роли, на которые админ может переключить юзера (без SuperAdmin). */
export type AssignableUserRole = Exclude<AdminUserRole, 'SuperAdmin'>;

export type AdminUserListItem = {
  id: string;
  email: string;
  userName: string;
  fullName: string | null;
  role: AdminUserRole;
  registeredAtUtc: string;
  lastLoginAtUtc: string | null;
  trackingCount: number;
  /** F17.10. Признак блокировки — UI листинга подсвечивает строку красным. */
  isBlocked: boolean;
};

export type AdminUserDetails = {
  id: string;
  email: string;
  userName: string;
  fullName: string | null;
  role: AdminUserRole;
  registeredAtUtc: string;
  lastLoginAtUtc: string | null;
  trackingCount: number;
  subscriptionStatus: string;
  subscriptionExpiresAtUtc: string | null;
  subscriptionPlan: string | null;
  hasComplimentaryAccess: boolean;
  complimentaryAccessUntilUtc: string | null;
  complimentaryAccessNote: string | null;
  isBlocked: boolean;
  blockedAtUtc: string | null;
  blockedByUserId: string | null;
  blockedByUserEmail: string | null;
  blockedReason: string | null;
};

export type ListAdminUsersParams = {
  search?: string;
  role?: AdminUserRole;
  registeredFromUtc?: string;
  registeredToUtc?: string;
  page: number;
  pageSize: number;
};

export const adminUsersApi = {
  /**
   * GET /api/users (admin-листинг). Сам админ и SuperAdmin'ы исключены
   * на бэке — UI просто получает результат как есть, без фильтрации.
   */
  async list(
    params: ListAdminUsersParams,
  ): Promise<PagedResponse<AdminUserListItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<AdminUserListItem>>>(
        '/api/users',
        { params },
      ),
    );
  },

  async getById(id: string): Promise<AdminUserDetails> {
    return unwrap(
      apiClient.get<ApiEnvelope<AdminUserDetails>>(`/api/users/${id}`),
    );
  },

  /**
   * PUT /api/users/{id}/role. Бэк проверяет, что вызывающий имеет право
   * назначать запрашиваемую роль (Admin не может выдать Admin; только
   * SuperAdmin может — см. ChangeRoleUseCase). При смене роли бэк
   * ротирует SecurityStamp пользователя — следующая попытка с его
   * прежним access-токеном вылетит как 401 через TTL.
   */
  async changeRole(id: string, userRole: AssignableUserRole): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<{ userId: string }>>(
        `/api/users/${id}/role`,
        { userRole },
      ),
    );
  },
};
