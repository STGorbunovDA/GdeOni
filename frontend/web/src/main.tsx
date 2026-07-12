import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
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

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme} defaultColorScheme="light">
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
