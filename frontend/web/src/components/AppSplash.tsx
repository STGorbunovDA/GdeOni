import { useEffect } from 'react';
import { useAuthStore } from '../auth/authStore';

/**
 * Снимает стартовый анимированный сплэш (#app-splash из index.html), когда
 * приложение готово: сессия догидрирована (isBootstrapping=false) и прошло
 * минимальное время показа (чтобы анимация не мелькала). Плавно гасит и
 * удаляет из DOM. Сам ничего не рисует — сплэш живёт в index.html, чтобы
 * появиться мгновенно, ещё до загрузки бандла.
 */
const MIN_VISIBLE_MS = 600;
const startedAt = Date.now();

export function AppSplash() {
  const bootstrapping = useAuthStore((s) => s.isBootstrapping);

  useEffect(() => {
    if (bootstrapping) return;

    const el = document.getElementById('app-splash');
    if (!el) return;

    const wait = Math.max(0, MIN_VISIBLE_MS - (Date.now() - startedAt));
    const hideTimer = window.setTimeout(() => {
      el.style.pointerEvents = 'none';
      el.style.opacity = '0';
      window.setTimeout(() => el.remove(), 450);
    }, wait);

    return () => window.clearTimeout(hideTimer);
  }, [bootstrapping]);

  return null;
}
