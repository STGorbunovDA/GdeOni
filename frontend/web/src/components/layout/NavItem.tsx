import { Badge, NavLink as MantineNavLink } from '@mantine/core';
import type { LucideIcon } from 'lucide-react';
import { useLocation, useNavigate } from 'react-router-dom';
import { navFont } from '../../design/theme';

/**
 * F2.1. Пункт меню в sidebar. Тонкая обёртка над Mantine NavLink:
 *  - подсвечивает активный пункт (matches либо точный путь, либо
 *    префикс — например /tracked подсветится и на /tracked/:id);
 *  - переходит через useNavigate (полноценно интегрирован с React
 *    Router, без перезагрузки страницы);
 *  - закрывает Drawer на мобильном после клика (onNavigate).
 *
 * Если to не передан — это action-кнопка (например "Выйти"). Тогда
 * onClick вызывается напрямую без навигации.
 */
type Props = {
  /** Путь для перехода. Если не задан — пункт работает как кнопка. */
  to?: string;
  /** Лейбл пункта меню. */
  label: string;
  /** Иконка lucide-react. */
  icon: LucideIcon;
  /** Колбэк на навигацию (закрытие Drawer). */
  onNavigate?: () => void;
  /** Альтернатива to: произвольное действие. */
  onClick?: () => void;
  /** Счётчик-бейдж справа (0/undefined — не показывать). */
  badge?: number;
};

export function NavItem({
  to,
  label,
  icon: Icon,
  onNavigate,
  onClick,
  badge,
}: Props) {
  const location = useLocation();
  const navigate = useNavigate();

  const isActive = to
    ? to === '/'
      ? location.pathname === '/'
      : location.pathname === to || location.pathname.startsWith(`${to}/`)
    : false;

  function handleClick() {
    if (to) {
      navigate(to);
      onNavigate?.();
    } else {
      onClick?.();
    }
  }

  const showBadge = typeof badge === 'number' && badge > 0;

  return (
    <MantineNavLink
      label={label}
      leftSection={<Icon size={18} />}
      rightSection={
        showBadge ? (
          <Badge size="sm" variant="filled" color="red">
            {badge > 99 ? '99+' : badge}
          </Badge>
        ) : undefined
      }
      active={isActive}
      onClick={handleClick}
      variant="filled"
      // Medilab: навигация — Raleway, 600, скруглённые пункты.
      style={{ borderRadius: 8, fontFamily: navFont, fontWeight: 600 }}
    />
  );
}
