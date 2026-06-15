import {
  AppShell,
  Burger,
  Group,
  ScrollArea,
  Stack,
  Text,
} from '@mantine/core';
import { useDisclosure, useMediaQuery } from '@mantine/hooks';
import { Archive, LogOut, Map, Shield, User, Users } from 'lucide-react';
import { Outlet } from 'react-router-dom';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { cloudColors } from '../../design/theme';
import { CaptionLabel } from '../ui/Labels';
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
 * F2.1 показывает версию плейсхолдером 'dev-86f0e74'. В F22 здесь
 * будет реальный VITE_APP_VERSION из import.meta.env.
 *
 * Header рендерится только на мобильном (useMediaQuery). На десктопе
 * — не объявляем header в AppShell конфигурации, чтобы Mantine не
 * резервировал место сверху (header.collapsed принимает только
 * boolean, не разделение по brk).
 */
const APP_VERSION_PLACEHOLDER = 'dev-86f0e74';
const MOBILE_BREAKPOINT = '(max-width: 48em)'; // Mantine sm = 48em

export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure();
  const isMobile = useMediaQuery(MOBILE_BREAKPOINT);
  const isAdmin = useIsAdmin();
  const clear = useAuthStore((s) => s.clear);

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
            <Text fw={700} c={cloudColors.inkBlue}>
              GdeOni
            </Text>
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
        {/* Логотип/название поверх меню. На десктопе он также служит
            "брендингом" в шапке (header'а на desktop нет). */}
        <AppShell.Section>
          <Group justify="space-between" mb="md">
            <Text
              fz={22}
              fw={800}
              c={cloudColors.azureDeep}
              component="a"
              href="/"
              style={{ textDecoration: 'none' }}
            >
              GdeOni
            </Text>
          </Group>
        </AppShell.Section>

        <AppShell.Section grow component={ScrollArea}>
          <Stack gap={4}>
            <NavItem to="/tracked" icon={Users} label="Отслеживаемые" onNavigate={close} />
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
            <NavItem
              icon={LogOut}
              label="Выйти"
              onClick={() => {
                clear();
                close();
              }}
            />
            <CaptionLabel>Версия: {APP_VERSION_PLACEHOLDER}</CaptionLabel>
          </Stack>
        </AppShell.Section>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
