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
  ArrowLeft,
  CreditCard,
  History,
  LifeBuoy,
  LogOut,
  Shield,
  Users as UsersIcon,
  Skull,
} from 'lucide-react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../auth/authStore';
import { authApi } from '../../api/endpoints/authApi';
import { cloudColors } from '../../design/theme';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { CaptionLabel } from '../ui/Labels';
import { NavItem } from './NavItem';

/**
 * F17.13. Отдельный layout для админки. Mantine AppShell с собственным
 * sidebar — основное приложение и админка визуально и навигационно
 * разделены, чтобы случайно не путать «карточки моих отслеживаемых»
 * с «вся таблица умерших по системе».
 *
 * Сайдбар:
 *  - Карточки умерших (F17.1) — /admin/deceased
 *  - Пользователи (F17.7)     — /admin/users
 *  - Платежи (F17.8)          — /admin/payments
 *  - История правок (F17.9)   — /admin/edits
 *  - Проблемы (F17.14)        — /admin/support-tickets
 *
 * Внизу — email текущего админа, «К приложению» (выйти из admin-mode
 * без logout) и «Выйти» (полный logout). Стилистика та же что и в
 * AppLayout — облачко в брендинге, NavItem, MOBILE_BREAKPOINT.
 *
 * Защита уровня роутера обеспечивается AdminRoute обёрткой (см.
 * AppRouter); этот layout сам по себе не проверяет роль.
 */
const MOBILE_BREAKPOINT = '(max-width: 48em)';

export function AdminLayout() {
  const [opened, { toggle, close }] = useDisclosure();
  const isMobile = useMediaQuery(MOBILE_BREAKPOINT);
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const navigate = useNavigate();

  async function handleLogout() {
    close();
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
              <Shield size={22} color={cloudColors.azureDeep} />
              <Text fw={700} c={cloudColors.inkBlue}>
                GdeOni · Admin
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
        <AppShell.Section>
          <Group gap={8} mb="md" wrap="nowrap">
            <Shield size={28} color={cloudColors.azureDeep} />
            <Stack gap={0}>
              <Text fz={20} fw={800} c={cloudColors.azureDeep}>
                GdeOni
              </Text>
              <CaptionLabel>Админка</CaptionLabel>
            </Stack>
          </Group>
        </AppShell.Section>

        <AppShell.Section grow component={ScrollArea}>
          <Stack gap={4}>
            <NavItem
              to="/admin/deceased"
              icon={Skull}
              label="Карточки умерших"
              onNavigate={close}
            />
            <NavItem
              to="/admin/users"
              icon={UsersIcon}
              label="Пользователи"
              onNavigate={close}
            />
            <NavItem
              to="/admin/payments"
              icon={CreditCard}
              label="Платежи"
              onNavigate={close}
            />
            <NavItem
              to="/admin/edits"
              icon={History}
              label="История правок"
              onNavigate={close}
            />
            <NavItem
              to="/admin/support-tickets"
              icon={LifeBuoy}
              label="Проблемы"
              onNavigate={close}
            />
          </Stack>
        </AppShell.Section>

        <AppShell.Section>
          <Stack gap="xs" mt="md">
            {user?.email && (
              <CaptionLabel>{user.email}</CaptionLabel>
            )}
            <NavItem
              icon={ArrowLeft}
              label="К приложению"
              onClick={() => {
                close();
                navigate('/tracked');
              }}
            />
            <NavItem icon={LogOut} label="Выйти" onClick={handleLogout} />
            <CaptionLabel>Версия: {CURRENT_APP_VERSION}</CaptionLabel>
          </Stack>
        </AppShell.Section>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
