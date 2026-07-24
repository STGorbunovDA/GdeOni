/**
 * F41. Определение «встроенного браузера приложения» (in-app WebView).
 *
 * Приложения (ВКонтакте, Instagram, Facebook, Telegram и т. п.) открывают
 * ссылки не в Safari/Chrome, а в своём урезанном WebView. На новом или
 * помеченном антивирусом домене такой WebView нередко режет сетевые
 * запросы — регистрация и вход падают с «Ошибка сети», хотя сам сайт
 * загружается. Детектим такие браузеры, чтобы предложить пользователю
 * открыть сайт в обычном браузере (см. InAppBrowserNotice).
 */
export function isInAppBrowser(): boolean {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent || '';

  // Явные маркеры встроенных браузеров популярных приложений.
  if (
    /\b(VKAndroidApp|VKClient|FBAN|FBAV|FB_IAB|Instagram|Line|MicroMessenger|Twitter|OKApp)\b/i.test(
      ua,
    )
  ) {
    return true;
  }

  // iOS: встроенный WKWebView обычно НЕ содержит токен «Safari/» и не
  // является сторонним браузером (Chrome=CriOS, Firefox=FxiOS, Edge=EdgiOS,
  // Opera=OPiOS). У настоящего Safari токен «Safari/» есть. Именно так
  // выглядит in-app браузер ВКонтакте на айфоне из скриншотов поддержки.
  const isIOS = /iPhone|iPad|iPod/i.test(ua);
  if (isIOS) {
    const isRealBrowser =
      /Safari\//.test(ua) || /CriOS|FxiOS|EdgiOS|OPiOS/i.test(ua);
    if (!isRealBrowser) return true;
  }

  return false;
}
