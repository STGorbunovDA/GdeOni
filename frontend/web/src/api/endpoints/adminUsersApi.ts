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

  /**
   * F17.6 / D22. POST /api/admin/users/{userId}/complimentary-access —
   * выдать пользователю бесплатный доступ. untilUtc=null → бессрочно.
   * note — опциональная заметка для аудита (например, «друг основателя»).
   * Идемпотентно: повторный вызов с теми же параметрами не двигает GrantedAt.
   * 204 No Content.
   *
   * Себе выдать нельзя (бэк вернёт 403). Admin не может управлять
   * SuperAdmin/Admin'ом — UI прячет кнопку.
   */
  async grantComplimentaryAccess(
    id: string,
    request: { untilUtc: string | null; note: string | null },
  ): Promise<void> {
    await apiClient.post(
      `/api/admin/users/${id}/complimentary-access`,
      request,
    );
  },

  /**
   * F17.6. DELETE /api/admin/users/{userId}/complimentary-access — отозвать
   * выданный комплимент. Silent no-op если доступа не было. 204.
   */
  async revokeComplimentaryAccess(id: string): Promise<void> {
    await apiClient.delete(`/api/admin/users/${id}/complimentary-access`);
  },

  /**
   * F17.6. POST /api/admin/users/{userId}/subscription/trial — перезапустить
   * пробный период. durationDays=null → бэк возьмёт TrialDurationDays из
   * SubscriptionOptions (по умолчанию 30). Работает из любого статуса —
   * переводит юзера в Trial с новым сроком. 204.
   */
  async restartTrial(id: string, durationDays: number | null = null): Promise<void> {
    await apiClient.post(`/api/admin/users/${id}/subscription/trial`, {
      durationDays,
    });
  },

  /**
   * F17.6. DELETE /api/admin/users/{userId}/subscription — моментально
   * снять подписку (Status → Expired, ExpiresAtUtc → now). Себе снять
   * нельзя (бэк вернёт 403). 204.
   */
  async revokeSubscription(id: string): Promise<void> {
    await apiClient.delete(`/api/admin/users/${id}/subscription`);
  },
};
