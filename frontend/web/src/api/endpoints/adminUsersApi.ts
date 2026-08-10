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

  /**
   * F17.10. PUT /api/users/{id}/block — заблокировать юзера навсегда.
   * Reason (≤500) отображается в админке. Бэк ротирует SecurityStamp →
   * мгновенный logout юзера при следующем запросе; login после этого
   * отдаёт 403 user.account.blocked ПОСЛЕ проверки пароля (защита
   * от timing-oracle).
   *
   * Иерархия (бэк отдаёт 403 с явными кодами):
   *  - себя — user.block.self.forbidden;
   *  - SuperAdmin — user.block.super_admin.forbidden;
   *  - Admin блокирует Admin — user.block.peer_admin.forbidden.
   */
  async block(id: string, reason: string | null): Promise<void> {
    await apiClient.put(`/api/users/${id}/block`, { reason });
  },

  /**
   * F17.10. DELETE /api/users/{id}/block — снять блокировку. После
   * успеха юзер сможет снова залогиниться (SecurityStamp обновится
   * при следующем login).
   */
  async unblock(id: string): Promise<void> {
    await apiClient.delete(`/api/users/${id}/block`);
  },

  /**
   * F17.11. DELETE /api/users/{id} — удалить пользователя навсегда.
   * Только SuperAdmin (бэк требует Roles=SuperAdmin, а не общий
   * Admin/SuperAdmin).
   *
   * Бэк каскадно переуступает Deceased.CreatedByUserId,
   * DeceasedMedia.UploadedByUserId и TrackedDeceased текущему
   * SuperAdmin'у; для каждой карточки создаётся audit-запись
   * DeceasedEditKind.Reassignment с email удалённого юзера в
   * ChangesJson.PreviousAuthor. Платежи, история правок и
   * воспоминания остаются как есть (author_user_id → null).
   *
   * Ошибки 403: user.delete.self.forbidden, user.delete.super_admin.
   * forbidden, user.delete.peer_admin.forbidden, user.delete.has_content
   * (последний если что-то пошло не так с переуступкой).
   */
  async remove(id: string): Promise<void> {
    await unwrap(
      apiClient.delete<ApiEnvelope<{ userId: string }>>(
        `/api/users/${id}`,
      ),
    );
  },

  /**
   * F17.12. GET /api/admin/users/{userId}/tracked-deceased —
   * все отслеживания юзера с пагинацией.
   */
  async listTracked(
    userId: string,
    page: number,
    pageSize: number,
  ): Promise<PagedResponse<AdminUserTrackedItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<AdminUserTrackedItem>>>(
        `/api/admin/users/${userId}/tracked-deceased`,
        { params: { page, pageSize } },
      ),
    );
  },

  /**
   * F17.12. DELETE /api/admin/users/{userId}/tracked-deceased/{deceasedId}
   * — снять одно отслеживание. 204.
   */
  async removeTracking(userId: string, deceasedId: string): Promise<void> {
    await apiClient.delete(
      `/api/admin/users/${userId}/tracked-deceased/${deceasedId}`,
    );
  },

  /**
   * F17.12. DELETE /api/admin/users/{userId}/tracked-deceased — снять
   * все отслеживания разом. Возвращает { removedCount }.
   */
  async removeAllTracking(userId: string): Promise<{ removedCount: number }> {
    return unwrap(
      apiClient.delete<ApiEnvelope<{ removedCount: number }>>(
        `/api/admin/users/${userId}/tracked-deceased`,
      ),
    );
  },

  /**
   * F17.6+. POST /api/admin/complimentary-access/grant-all — массово выдать
   * бесплатный доступ ВСЕМ пользователям на N дней (по умолчанию 30). Только
   * SuperAdmin. Только продлевает (не укорачивает уже выданный более поздний
   * комплимент). Возвращает { affectedCount, untilUtc }.
   */
  async grantComplimentaryToAll(
    durationDays = 30,
  ): Promise<{ affectedCount: number; untilUtc: string }> {
    return unwrap(
      apiClient.post<ApiEnvelope<{ affectedCount: number; untilUtc: string }>>(
        '/api/admin/complimentary-access/grant-all',
        { durationDays },
      ),
    );
  },
};

/** Зеркало UserTrackedDeceasedItem с бэка. */
export type AdminUserTrackedItem = {
  deceasedId: string;
  fullName: string;
  birthDate: string | null;
  deathDate: string;
  relationshipType: string;
  status: string;
  trackedAtUtc: string;
};
