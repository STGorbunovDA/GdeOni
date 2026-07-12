import {
  AppShell,
  Burger,
  Group,
  ScrollArea,
  Stack,
  Text,
} from '@mantine/core';
import { useDisclosure, useMediaQuery } from '@mantine/hooks';
import {
  Archive,
  CalendarHeart,
  Cloud,
  LogOut,
  Map,
  Shield,
  User,
  Users,
} from 'lucide-react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { authApi } from '../../api/endpoints/authApi';
import { cloudColors } from '../../design/theme';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { CaptionLabel } from '../ui/Labels';
import { OutdatedLegalModal } from '../legal/OutdatedLegalModal';
import { AnniversaryModal } from '../events/AnniversaryModal';
import { NavItem } from './NavItem';

/**
 * F2.1. Корневой layout приватных страниц. Mantine AppShell с боковой
 * навигацией (240px) и автоматическим Drawer-режимом на мобильных.
 *
 * Mobile-аналог: AppShell + AppBottomBar. На вебе боковая навигация,
 * потому что desktop имеет горизонтальное место и tabbed-навигация
 * смотрится дико.
 *
 * На уровне layout позже поселятся PaywallGuard (F22) и
 * AnniversaryModal (F11.2) — они должны переживать переходы между
 * страницами через Outlet.
 *
 * Header рендерится только на мобильном (useMediaQuery). На десктопе
 * — не объявляем header в AppShell конфигурации, чтобы Mantine не
 * резервировал место сверху (header.collapsed принимает только
 * boolean, не разделение по brk).
 */
const MOBILE_BREAKPOINT = '(max-width: 48em)'; // Mantine sm = 48em

export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure();
  const isMobile = useMediaQuery(MOBILE_BREAKPOINT);
  const isAdmin = useIsAdmin();
  const clear = useAuthStore((s) => s.clear);
  const navigate = useNavigate();

  async function handleLogout() {
    close();
    // POST /api/auth/logout — best-effort, серверу нужен refresh-token
    // для ревокации. Если упадёт (например, 401 от просроченного access)
    // — игнорируем, главное вычистить локальное состояние.
    await authApi.logout();
    clear();
    navigate('/login', { replace: true });
  }

  return (
    <AppShell
      header={isMobile ? { height: 56 } : undefined}
      navbar={{
        width: 240,
        breakpoint: 'sm',
        collapsed: { mobile: !opened },
      }}
      padding="md"
    >
      {isMobile && (
        <AppShell.Header
          style={{
            borderBottom: `1px solid ${cloudColors.cloudBorder}`,
            background: cloudColors.cloud,
          }}
        >
          <Group h="100%" px="md" gap="md">
            <Burger opened={opened} onClick={toggle} size="sm" />
            <Group gap={8}>
              <Cloud size={22} color={cloudColors.azureDeep} />
              <Text fw={700} c={cloudColors.inkBlue}>
                ГдеОни
              </Text>
            </Group>
          </Group>
        </AppShell.Header>
      )}

      <AppShell.Navbar
        p="md"
        style={{
          background: cloudColors.cloud,
          borderRight: `1px solid ${cloudColors.cloudBorder}`,
        }}
      >
        {/* Логотип = облачко + название. На десктопе он также служит
            "брендингом" в шапке (header'а на desktop нет). Облачко —
            визуальный якорь Cloud-стиля, общий с mobile-приложением. */}
        <AppShell.Section>
          <a
            href="/"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              marginBottom: 16,
              textDecoration: 'none',
              color: 'inherit',
            }}
          >
            <Cloud size={28} color={cloudColors.azureDeep} />
            <Text fz={22} fw={800} c={cloudColors.azureDeep}>
              ГдеОни
            </Text>
          </a>
        </AppShell.Section>

        <AppShell.Section grow component={ScrollArea}>
          <Stack gap={4}>
            <NavItem to="/tracked" icon={Users} label="Отслеживаемые" onNavigate={close} />
            <NavItem to="/events" icon={CalendarHeart} label="События" onNavigate={close} />
            <NavItem to="/route" icon={Map} label="Маршрут" onNavigate={close} />
            <NavItem
              to="/tracked/archive"
              icon={Archive}
              label="Архив"
              onNavigate={close}
            />
            <NavItem to="/profile" icon={User} label="Профиль" onNavigate={close} />

            {isAdmin && (
              <NavItem
                to="/admin"
                icon={Shield}
                label="Админка"
                onNavigate={close}
              />
            )}
          </Stack>
        </AppShell.Section>

        <AppShell.Section>
          <Stack gap="xs" mt="md">
            <NavItem icon={LogOut} label="Выйти" onClick={handleLogout} />
            <CaptionLabel>Версия: {CURRENT_APP_VERSION}</CaptionLabel>
          </Stack>
        </AppShell.Section>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>

      {/* F24 / D19. Блокирующая модалка при HasOutdatedLegalAcceptance.
          Живёт на уровне layout, чтобы работать на всех приватных
          страницах без дублирования. */}
      <OutdatedLegalModal />

      {/* D38. Модалка «сегодня годовщина/день рождения» — один показ в
          сутки после входа. Сама проверяет paywall-гейт и не показывается
          поверх блокирующей legal-модалки. */}
      <AnniversaryModal />
    </AppShell>
  );
}
