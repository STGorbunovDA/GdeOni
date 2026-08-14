import { apiClient, unwrap } from '../api/client';
import type { ApiEnvelope } from '../api/types';

/**
 * Push-уведомления (Web Push / PWA).
 *
 * Схема: браузер выдаёт «адрес доставки» (endpoint + два ключа), мы отдаём
 * его серверу, сервер шлёт туда сообщения через VAPID. Ключ подписи
 * (публичный) приходит с бэка в /api/app/features — так его можно поменять
 * без пересборки фронта.
 *
 * Работает только по HTTPS (или на localhost) и требует зарегистрированного
 * service worker'а — см. public/sw.js.
 */

/** Поддерживает ли браузер push вообще (Safari на iOS <16.4 — нет). */
export function isPushSupported(): boolean {
  return (
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  );
}

/**
 * VAPID-ключ приходит base64url-строкой, а PushManager ждёт Uint8Array.
 * Конвертация обязательна — иначе subscribe падает с InvalidCharacterError.
 */
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const raw = window.atob(base64);
  const output = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i += 1) {
    output[i] = raw.charCodeAt(i);
  }
  return output;
}

/** Текущее разрешение браузера: 'default' | 'granted' | 'denied'. */
export function getPushPermission(): NotificationPermission {
  return isPushSupported() ? Notification.permission : 'denied';
}

/**
 * Включить уведомления: спросить разрешение, подписаться и отдать подписку
 * серверу. Бросает Error с человеческим текстом — вызывающий покажет его.
 */
export async function enablePush(publicKey: string): Promise<void> {
  if (!isPushSupported())
    throw new Error('Браузер не поддерживает push-уведомления.');

  if (!publicKey)
    throw new Error('Push-уведомления не настроены на сервере.');

  const permission = await Notification.requestPermission();
  if (permission !== 'granted') {
    throw new Error(
      'Разрешение не выдано. Включите уведомления для сайта в настройках браузера.',
    );
  }

  const registration = await navigator.serviceWorker.ready;

  // Уже подписан (повторное включение) — переиспользуем: повторный subscribe
  // с тем же ключом вернёт ту же подписку, с другим — упадёт.
  const existing = await registration.pushManager.getSubscription();
  const subscription =
    existing ??
    (await registration.pushManager.subscribe({
      // Требование Chrome: показывать уведомление на каждый push.
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey),
    }));

  const json = subscription.toJSON();
  const keys = json.keys ?? {};

  await apiClient.post('/api/push/subscriptions', {
    endpoint: subscription.endpoint,
    p256dh: keys.p256dh,
    auth: keys.auth,
  });
}

/**
 * Выключить: снять подписку в браузере и забыть её на сервере. Порядок
 * важен — сначала забираем endpoint, потом отписываемся.
 */
export async function disablePush(): Promise<void> {
  if (!isPushSupported()) return;

  const registration = await navigator.serviceWorker.ready;
  const subscription = await registration.pushManager.getSubscription();
  if (!subscription) return;

  const { endpoint } = subscription;
  await subscription.unsubscribe();
  await apiClient.delete('/api/push/subscriptions', { data: { endpoint } });
}

/** Включены ли push у пользователя хотя бы на одном устройстве (по данным сервера). */
export async function fetchPushStatus(): Promise<boolean> {
  const result = await unwrap(
    apiClient.get<ApiEnvelope<{ enabled: boolean }>>(
      '/api/push/subscriptions/status',
    ),
  );
  return result.enabled;
}
