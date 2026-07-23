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
  Info,
  LifeBuoy,
  LogOut,
  Search as SearchIcon,
  Shield,
  Users as UsersIcon,
  Skull,
} from 'lucide-react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore, useIsSuperAdmin } from '../../auth/authStore';
import { authApi } from '../../api/endpoints/authApi';
import { cloudColors } from '../../design/theme';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { CaptionLabel } from '../ui/Labels';
import { ThemeToggle } from '../ui/ThemeToggle';
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
 *  - Обращения (F17.14)       — /admin/support-tickets
 *  - Найти умершего (F17.15)  — /admin/find-deceased
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
  // D44. Раздел обращений — только для владельца сервиса.
  const isSuperAdmin = useIsSuperAdmin();
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
                ГдеОни · Admin
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
          {/* F37. Переключатель темы — справа от названия, как в основном
              layout: одно и то же место в обоих сайдбарах. */}
          <Group justify="space-between" mb="md" wrap="nowrap">
            <Group gap={8} wrap="nowrap">
              <Shield size={28} color={cloudColors.azureDeep} />
              <Stack gap={0}>
                <Text fz={20} fw={800} c={cloudColors.azureDeep}>
                  ГдеОни
                </Text>
                <CaptionLabel>Админка</CaptionLabel>
              </Stack>
            </Group>
            <ThemeToggle size="md" />
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
            {/* D44. Обращения видит только владелец сервиса: в переписке
                платёжные реквизиты и договорённости о переводах. */}
            {isSuperAdmin && (
              <NavItem
                to="/admin/support-tickets"
                icon={LifeBuoy}
                label="Обращения"
                onNavigate={close}
              />
            )}
            <NavItem
              to="/admin/find-deceased"
              icon={SearchIcon}
              label="Найти умершего"
              onNavigate={close}
            />
            {/* F38. Справка по системе — последним пунктом: это не рабочий
                инструмент, а «посмотреть цифры». */}
            <NavItem
              to="/admin/info"
              icon={Info}
              label="Информация"
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
