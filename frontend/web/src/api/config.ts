/**
 * F1. Конфигурация API. Реальный axios-клиент с interceptors —
 * в F3. Здесь только base URL для будущих запросов.
 */
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';
