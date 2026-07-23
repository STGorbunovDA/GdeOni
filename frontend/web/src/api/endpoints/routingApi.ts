import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F14. Эндпоинты построения маршрута. Backend отдаёт массив deep-link'ов
 * по провайдерам (yandex / google / 2gis) — клиент решает какой
 * открыть. По решению 2026-05-13 на UI используется только Яндекс,
 * остальные приходят в ответе но игнорируются.
 */

export type RouteMode = 'auto' | 'pedestrian' | 'masstransit' | 'bicycle';

export type RouteLink = {
  provider: string;
  url: string;
};

export type RouteResponse = {
  deceasedId: string;
  graveLocation: {
    latitude: number;
    longitude: number;
    accuracyMeters: number | null;
  };
  from: {
    latitude: number;
    longitude: number;
  };
  mode: string;
  links: RouteLink[];
};

export const routingApi = {
  /**
   * GET /api/users/me/tracked-deceased/{deceasedId}/route — backend
   * проверяет что юзер активный tracker, иначе 403; 409 если у могилы
   * нет координат. Бэк строит и сами deep-link'и (через
   * ExternalMapsService).
   */
  async singleRoute(
    deceasedId: string,
    fromLat: number,
    fromLon: number,
    mode: RouteMode = 'auto',
  ): Promise<RouteResponse> {
    return unwrap(
      apiClient.get<ApiEnvelope<RouteResponse>>(
        `/api/users/me/tracked-deceased/${deceasedId}/route`,
        { params: { fromLat, fromLon, mode } },
      ),
    );
  },
};
