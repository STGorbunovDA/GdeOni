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
