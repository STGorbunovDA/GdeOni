/**
 * PWA. Раннее перехватывание события `beforeinstallprompt` (Android/Chrome).
 * Браузер шлёт его один раз и рано — если ждать, пока смонтируется React-
 * компонент, событие можно пропустить. Поэтому слушаем на уровне модуля
 * (импортируется в main.tsx до рендера) и храним отложенный prompt здесь.
 */
export type InstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
};

let deferred: InstallPromptEvent | null = null;
const listeners = new Set<() => void>();

function notify(): void {
  for (const l of listeners) l();
}

if (typeof window !== 'undefined') {
  window.addEventListener('beforeinstallprompt', (e) => {
    // Гасим нативную мини-плашку — показываем свою кнопку в баннере.
    e.preventDefault();
    deferred = e as InstallPromptEvent;
    notify();
  });

  window.addEventListener('appinstalled', () => {
    deferred = null;
    notify();
  });
}

export function getInstallPrompt(): InstallPromptEvent | null {
  return deferred;
}

export function clearInstallPrompt(): void {
  deferred = null;
  notify();
}

/** Подписка на появление/сброс prompt. Возвращает отписку. */
export function onInstallChange(cb: () => void): () => void {
  listeners.add(cb);
  return () => {
    listeners.delete(cb);
  };
}

/** Приложение уже запущено «как приложение» (установлено на экран). */
export function isStandalone(): boolean {
  try {
    return (
      window.matchMedia('(display-mode: standalone)').matches ||
      // iOS Safari сообщает факт запуска с домашнего экрана так:
      (window.navigator as unknown as { standalone?: boolean }).standalone === true
    );
  } catch {
    return false;
  }
}

export function isIos(): boolean {
  return /iphone|ipad|ipod/i.test(window.navigator.userAgent);
}

/**
 * iOS Safari — только там есть «На экран „Домой"». В Chrome/Firefox/Edge на
 * iOS такого пункта нет (это ограничение самой iOS).
 */
export function isIosSafari(): boolean {
  const ua = window.navigator.userAgent;
  return isIos() && /safari/i.test(ua) && !/crios|fxios|edgios/i.test(ua);
}
