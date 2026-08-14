/*
 * Service worker PWA: «установить сайт как приложение» + push-уведомления.
 *
 * Кэшированием намеренно НЕ занимаемся: единственная задача fetch-обработчика
 * — чтобы Chrome считал сайт устанавливаемым. Контент всегда грузится с
 * сервера напрямую, поэтому нет классической проблемы PWA «пользователь
 * застрял на старой версии».
 */
self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

// Passthrough: не вызываем respondWith → запрос идёт как обычно, минуя SW.
self.addEventListener('fetch', () => {});

/*
 * Push от сервера. Payload — маленький JSON {title, body, link}, который
 * шлёт WebPushSender. Разбираем защищённо: если payload не пришёл или
 * повреждён, всё равно показываем уведомление — молча проглотить нельзя,
 * браузер за это снимает разрешение на push.
 */
self.addEventListener('push', (event) => {
  let payload = {};
  try {
    payload = event.data ? event.data.json() : {};
  } catch {
    payload = {};
  }

  const title = payload.title || 'ГдеОни';
  const options = {
    body: payload.body || '',
    icon: '/pwa/icon-192.png',
    badge: '/pwa/icon-192.png',
    // Ссылка нужна обработчику клика ниже.
    data: { link: payload.link || '/' },
    // Одинаковые уведомления схлопываются, а не копятся стопкой.
    tag: payload.link || 'gdeoni',
    renotify: false,
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

/*
 * Клик по уведомлению: если вкладка приложения уже открыта — фокусируем её и
 * переходим по ссылке, иначе открываем новую. Без этого каждый клик плодил бы
 * новые вкладки.
 */
self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const link = (event.notification.data && event.notification.data.link) || '/';

  event.waitUntil(
    self.clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then((clientList) => {
        for (const client of clientList) {
          if ('focus' in client) {
            if ('navigate' in client) client.navigate(link);
            return client.focus();
          }
        }
        return self.clients.openWindow(link);
      }),
  );
});
