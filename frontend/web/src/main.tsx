import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
import '@mantine/core/styles.css';
import { AppRouter } from './routes/AppRouter';
import { queryClient } from './api/queryClient';
import { theme } from './design/theme';
import { SessionBootstrap } from './auth/SessionBootstrap';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme} defaultColorScheme="light">
        <SessionBootstrap>
          <AppRouter />
        </SessionBootstrap>
      </MantineProvider>
    </QueryClientProvider>
  </StrictMode>,
);
