import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * D41. Обратное геокодирование: координаты → адрес.
 *
 * Ходим в НАШ бэкенд, а не напрямую в Nominatim: прямой запрос из браузера
 * отправил бы IP пользователя во внешний сервис в ЕС, а Политика
 * конфиденциальности (5.3) обещает отсутствие трансграничной передачи ПД.
 */
export type ReverseGeocodeResult = {
  country: string | null;
  region: string | null;
  city: string | null;
};

export const geoApi = {
  /**
   * GET /api/geo/reverse. Бросает ApiError, если адрес не найден или
   * геокодер недоступен — вызывающий ДОЛЖЕН это проглотить: автозаполнение
   * города это подсказка, а не обязательный шаг сценария.
   */
  async reverse(
    latitude: number,
    longitude: number,
  ): Promise<ReverseGeocodeResult> {
    return unwrap(
      apiClient.get<ApiEnvelope<ReverseGeocodeResult>>('/api/geo/reverse', {
        params: { latitude, longitude },
      }),
    );
  },
};
