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
  Search,
  Shield,
  User,
  UserPlus,
  Users,
  UsersRound,
} from 'lucide-react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore, useIsAdmin } from '../../auth/authStore';
import { authApi } from '../../api/endpoints/authApi';
import { cloudColors } from '../../design/theme';
import { CURRENT_APP_VERSION } from '../../hooks/useAppVersion';
import { CaptionLabel } from '../ui/Labels';
import { ThemeToggle } from '../ui/ThemeToggle';
import { OutdatedLegalModal } from '../legal/OutdatedLegalModal';
import { EmailConfirmationBanner } from '../auth/EmailConfirmationBanner';
import { AppUpdateBanner } from './AppUpdateBanner';
import { EventsPopup } from '../events/EventsPopup';
import { InstallPwaBanner } from '../pwa/InstallPwaBanner';
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
          <Group h="100%" px="md" gap="md" wrap="nowrap">
            <Burger
              opened={opened}
              onClick={toggle}
              size="sm"
              color={cloudColors.inkBlue}
              aria-label="Меню"
            />
            <Group gap={8} wrap="nowrap">
              <Cloud size={22} color={cloudColors.azureDeep} />
              <Text fw={700} c={cloudColors.inkBlue}>
                ГдеОни
              </Text>
              {/* На мобильном сайдбар спрятан в Drawer — переключатель
                  должен быть виден и без его открытия. */}
              <ThemeToggle size="md" />
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
            визуальный якорь Cloud-стиля, общий с mobile-приложением.
            F37: справа от названия — переключатель темы. Он ВНЕ ссылки
            <a href="/">, иначе клик по нему уводил бы на главную. */}
        <AppShell.Section>
          <Group justify="space-between" wrap="nowrap" mb="md">
            <a
              href="/"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                textDecoration: 'none',
                color: 'inherit',
              }}
            >
              <Cloud size={28} color={cloudColors.azureDeep} />
              <Text fz={22} fw={800} c={cloudColors.azureDeep}>
                ГдеОни
              </Text>
            </a>
            <ThemeToggle size="md" />
          </Group>
        </AppShell.Section>

        <AppShell.Section grow component={ScrollArea}>
          <Stack gap={4}>
            {/* «Поиск» — поиск карточек в базе (страница /search). «Добавить
                умершего» — создание новой карточки. Список отслеживаемых —
                отдельным пунктом «Отслеживаемые». */}
            <NavItem to="/search" icon={Search} label="Поиск" onNavigate={close} />
            <NavItem
              to="/at-grave"
              icon={UserPlus}
              label="Добавить умершего"
              onNavigate={close}
            />
            <NavItem to="/tracked" icon={Users} label="Отслеживаемые" onNavigate={close} />
            <NavItem to="/events" icon={CalendarHeart} label="События" onNavigate={close} />
            <NavItem to="/route" icon={Map} label="Маршрут" onNavigate={close} />
            <NavItem
              to="/relatives"
              icon={UsersRound}
              label="Родственники"
              onNavigate={close}
            />
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
        {/* Плашка «Доступно обновление» — когда выкатили новую сборку, а
            вкладка/PWA всё ещё на старой (см. useAppUpdate). */}
        <AppUpdateBanner />
        {/* D45. Баннер «Подтвердите email» для «старых» пользователей —
            над контентом на всех приватных страницах. */}
        <EmailConfirmationBanner />
        <Outlet />
      </AppShell.Main>

      {/* F24 / D19. Блокирующая модалка при HasOutdatedLegalAcceptance.
          Живёт на уровне layout, чтобы работать на всех приватных
          страницах без дублирования. */}
      <OutdatedLegalModal />

      {/* Единый попап «События сегодня» (памятные даты + праздники) —
          всплывает при каждом заходе, пока событие актуально. */}
      <EventsPopup />

      {/* PWA: подсказка «установить приложение на главный экран». */}
      <InstallPwaBanner />
    </AppShell>
  );
}
