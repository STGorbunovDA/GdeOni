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

/** Прямое геокодирование: текст адреса → координаты. */
export type ForwardGeocodeResult = {
  latitude: number;
  longitude: number;
  displayName: string | null;
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

  /**
   * GET /api/geo/search?query= — координаты по тексту адреса (город /
   * кладбище). Бросает ApiError, если ничего не нашлось или геокодер
   * недоступен — вызывающий проглатывает: это подсказка, а не шаг сценария.
   */
  async search(query: string): Promise<ForwardGeocodeResult> {
    return unwrap(
      apiClient.get<ApiEnvelope<ForwardGeocodeResult>>('/api/geo/search', {
        params: { query },
      }),
    );
  },
};
