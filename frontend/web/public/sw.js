/*
 * Минимальный service worker для PWA («установить сайт как приложение»).
 *
 * Намеренно НИЧЕГО не кэшируем: единственная задача — наличие
 * fetch-обработчика, без которого Chrome не считает сайт устанавливаемым.
 * Контент всегда грузится с сервера напрямую, поэтому нет классической
 * проблемы PWA «пользователь застрял на старой версии».
 */
self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

// Passthrough: не вызываем respondWith → запрос идёт как обычно, минуя SW.
self.addEventListener('fetch', () => {});
