import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider, localStorageColorSchemeManager } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
// Локальные шрифты (self-hosted, без обращений к Google Fonts).
// Импортируем ДО styles.css, чтобы @font-face объявились раньше использования.
import './assets/fonts/fonts.css';
import { AppRouter } from './routes/AppRouter';
import { queryClient } from './api/queryClient';
import { theme } from './design/theme';
import { SessionBootstrap } from './auth/SessionBootstrap';
import { VersionGate } from './components/version/VersionGate';
import './styles.css';

/**
 * F37. Ключ хранения схемы. Тот же ключ читает inline-скрипт в index.html,
 * который ставит атрибут на <html> ДО первой отрисовки — иначе тёмная
 * тема на долю секунды мигает белым фоном.
 */
const colorSchemeManager = localStorageColorSchemeManager({
  key: 'gdeoni-color-scheme',
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      {/* defaultColorScheme="auto" — на первом заходе берём системную
          настройку пользователя, дальше он решает сам переключателем. */}
      <MantineProvider
        theme={theme}
        defaultColorScheme="auto"
        colorSchemeManager={colorSchemeManager}
      >
        {/* F17.11. Notifications provider — используется для snack-bar'ов
            после длительных/деструктивных операций (пока только удаление
            юзера); маунтим глобально на самом верху. */}
        <Notifications position="top-right" />
        {/* F22 / D17. Проверка версии клиента — если бэк сказал
            "обновись" → блокирующая модалка. */}
        <VersionGate />
        <SessionBootstrap>
          <AppRouter />
        </SessionBootstrap>
      </MantineProvider>
    </QueryClientProvider>
  </StrictMode>,
);
