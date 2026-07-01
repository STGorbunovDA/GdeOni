import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F17.14 / D25.1 / D25.2. Support tickets — обращения в поддержку.
 * Два разных набора эндпоинтов:
 *  - /api/support-tickets — юзерская сторона (create, my list, details,
 *    accept, reopen);
 *  - /api/admin/support-tickets — админская (list с фильтрами, details,
 *    patch status/severity).
 *
 * DTO единый (SupportTicket), UserEmail заполняется только в админских
 * ответах; Messages/Attachments — только в GetById.
 */

export type TicketKind =
  | 'Payment'
  | 'Bug'
  | 'Complaint'
  | 'Question'
  | 'Other'
  | 'Photo';

export type TicketSource = 'Manual' | 'Auto';
export type TicketSeverity = 'Normal' | 'Urgent';
export type TicketStatus = 'Open' | 'InProgress' | 'Resolved';

export type TicketMessageAuthor = 'User' | 'Admin';

export type SupportTicketMessage = {
  id: string;
  authorKind: TicketMessageAuthor;
  authorUserId: string | null;
  text: string;
  createdAtUtc: string;
};

export type SupportTicket = {
  id: string;
  userId: string | null;
  userEmail: string | null;
  source: TicketSource;
  kind: TicketKind;
  severity: TicketSeverity;
  status: TicketStatus;
  title: string;
  description: string;
  details: string | null;
  resolutionNote: string | null;
  resolvedByUserId: string | null;
  resolvedAtUtc: string | null;
  acceptedByUser: boolean;
  acceptedByUserAtUtc: string | null;
  lastUserReply: string | null;
  lastUserReplyAtUtc: string | null;
  reopenedCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  /** Заполняется только в GetById, в листинге null. */
  messages: SupportTicketMessage[] | null;
};

export type ListAdminTicketsParams = {
  statuses?: TicketStatus[];
  severities?: TicketSeverity[];
  kind?: TicketKind;
  source?: TicketSource;
  userId?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
  search?: string;
  page: number;
  pageSize: number;
};

export const supportApi = {
  // ─────────── User side ───────────

  /**
   * POST /api/support-tickets — создать обращение. Kind/Title/Description.
   * Backend валидирует title (≤200), description (≤4000).
   * Ответ: { ticketId } (не { id } — совпадает с mobile).
   */
  async create(input: {
    kind: TicketKind;
    title: string;
    description: string;
  }): Promise<{ ticketId: string }> {
    return unwrap(
      apiClient.post<ApiEnvelope<{ ticketId: string }>>(
        '/api/support-tickets',
        input,
      ),
    );
  },

  /** GET /api/support-tickets/mine — мои тикеты. */
  async listMine(
    page: number,
    pageSize: number,
  ): Promise<PagedResponse<SupportTicket>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<SupportTicket>>>(
        '/api/support-tickets/mine',
        { params: { page, pageSize } },
      ),
    );
  },

  /**
   * GET /api/support-tickets/{id} — карточка. Юзер видит только свои,
   * админ — любой. Бэк оборачивает в { ticket: {...} } (зеркало
   * mobile), разворачиваем здесь.
   */
  async getById(id: string): Promise<SupportTicket> {
    const wrapped = await unwrap(
      apiClient.get<ApiEnvelope<{ ticket: SupportTicket }>>(
        `/api/support-tickets/${id}`,
      ),
    );
    return wrapped.ticket;
  },

  /**
   * D25.1. POST /api/support-tickets/{id}/accept — закрепить решение.
   * Только автор, только Status=Resolved. Повторно → 409
   * support_ticket.already.accepted. 204 No Content.
   */
  async accept(id: string): Promise<void> {
    await apiClient.post(`/api/support-tickets/${id}/accept`);
  },

  /**
   * D25.1. POST /api/support-tickets/{id}/reopen — переоткрыть тикет.
   * Только автор, только Status=Resolved и !AcceptedByUser. При успехе
   * ReopenedCount++, Status → Open, ResolutionNote сохраняется в
   * истории (Messages). 204 No Content.
   */
  async reopen(id: string, userReply: string | null): Promise<void> {
    await apiClient.post(`/api/support-tickets/${id}/reopen`, { userReply });
  },

  // ─────────── Admin side ───────────

  /**
   * GET /api/admin/support-tickets — админ-листинг с фильтрами.
   * statuses/severities — массивы (ASP.NET биндит multiple ?statuses=...).
   */
  async adminList(
    params: ListAdminTicketsParams,
  ): Promise<PagedResponse<SupportTicket>> {
    // axios сериализует массивы через paramsSerializer; по умолчанию
    // это будет ?statuses[]=... — ASP.NET нужен ?statuses=Open&statuses=
    // InProgress. Используем paramsSerializer с repeat.
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<SupportTicket>>>(
        '/api/admin/support-tickets',
        {
          params,
          paramsSerializer: {
            indexes: null,
          },
        },
      ),
    );
  },

  /** GET /api/admin/support-tickets/{id}. Ответ обёрнут в { ticket }. */
  async adminGetById(id: string): Promise<SupportTicket> {
    const wrapped = await unwrap(
      apiClient.get<ApiEnvelope<{ ticket: SupportTicket }>>(
        `/api/admin/support-tickets/${id}`,
      ),
    );
    return wrapped.ticket;
  },

  /**
   * PATCH /api/admin/support-tickets/{id}/status — сменить статус.
   * При Status=Resolved обязателен resolutionNote (иначе 400
   * support_ticket.resolution_note.required). При повторном Resolve →
   * 409 support_ticket.already.resolved.
   */
  async adminUpdateStatus(
    id: string,
    status: TicketStatus,
    resolutionNote: string | null,
  ): Promise<void> {
    await apiClient.patch(`/api/admin/support-tickets/${id}/status`, {
      status,
      resolutionNote,
    });
  },

  /**
   * PATCH /api/admin/support-tickets/{id}/severity — сменить severity.
   * Запрещено на Resolved (backend вернёт 409).
   */
  async adminUpdateSeverity(id: string, severity: TicketSeverity): Promise<void> {
    await apiClient.patch(`/api/admin/support-tickets/${id}/severity`, {
      severity,
    });
  },
};
