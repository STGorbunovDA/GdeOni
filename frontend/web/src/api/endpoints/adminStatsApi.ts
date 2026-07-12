import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F38. Справка по системе для админа. Зеркало backend
 * GdeOni.Application.Admin.Queries.GetAdminStats.Model.AdminStatsResponse.
 */
export type AdminStats = {
  users: {
    total: number;
    newLast7Days: number;
    newLast30Days: number;
    activeLast30Days: number;
    admins: number;
    blocked: number;
    withActiveSubscription: number;
    onTrial: number;
    withComplimentaryAccess: number;
  };
  deceased: {
    total: number;
    newLast30Days: number;
    verified: number;
    withCoordinates: number;
    withMainPhoto: number;
    trackedRecords: number;
  };
  content: {
    photos: number;
    gravePhotos: number;
    documents: number;
    memories: number;
    edits: number;
  };
  support: {
    total: number;
    open: number;
    resolved: number;
  };
  payments: {
    succeededCount: number;
    totalRub: number;
    last30DaysRub: number;
  };
  generatedAtUtc: string;
};

export const adminStatsApi = {
  /** GET /api/admin/stats — только для Admin/SuperAdmin. */
  async get(): Promise<AdminStats> {
    return unwrap(apiClient.get<ApiEnvelope<AdminStats>>('/api/admin/stats'));
  },
};
