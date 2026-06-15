import { Stack, UnstyledButton } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { useState } from 'react';

/**
 * F2.1. Главная страница админки. Список разделов — строго как в
 * mobile AdminPage.xaml (5 пунктов, заголовки и описания дословно).
 * Реальные страницы за каждой карточкой — заглушки, наполнятся в F17.
 *
 * Web-нюанс: вся карточка кликабельна (заголовок + описание).
 * Кнопки 'Открыть' нет — это лишний шум на desktop, где клик по
 * заголовку — нормальный UX-паттерн. На mobile XAML кнопка нужна,
 * потому что тап по тексту в MAUI не очевиден; на вебе hover/cursor
 * сразу намекают, что карточка кликабельна.
 */
type AdminSection = {
  title: string;
  description: string;
  to: string;
};

const SECTIONS: AdminSection[] = [
  {
    title: 'История правок',
    description: 'Лента всех изменений карточек умерших по всей системе.',
    to: '/admin/edits',
  },
  {
    title: 'Пользователи',
    description: 'Список пользователей, смена ролей, выдача бесплатного доступа.',
    to: '/admin/users',
  },
  {
    title: 'Платежи',
    description: 'История всех платежей подписки.',
    to: '/admin/payments',
  },
  {
    title: 'Обращения',
    description: 'Обращения пользователей и автоматические инциденты.',
    to: '/admin/support-tickets',
  },
  {
    title: 'Найти умершего',
    description:
      'Поиск по имени, датам, городу, координатам и пр. Полный просмотр без подписки.',
    to: '/admin/find-deceased',
  },
];

export function AdminPage() {
  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Админка</TitleLabel>
        <CaptionLabel>
          Управление пользователями, платежами и историей правок.
        </CaptionLabel>
      </Stack>

      {SECTIONS.map((section) => (
        <SectionCard key={section.to} section={section} />
      ))}
    </Stack>
  );
}

/**
 * Кликабельная карточка раздела. Hover-эффект через локальный state
 * (вместо CSS-модулей — единственная hover-зависимая карточка в F2.1,
 * не оправдано тащить отдельный CSS-файл).
 */
function SectionCard({ section }: { section: AdminSection }) {
  const navigate = useNavigate();
  const [hovered, setHovered] = useState(false);

  return (
    <UnstyledButton
      onClick={() => navigate(section.to)}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{ display: 'block', width: '100%', textAlign: 'left' }}
    >
      <CloudCard
        style={{
          cursor: 'pointer',
          transition: 'box-shadow 120ms ease, border-color 120ms ease',
          boxShadow: hovered
            ? '0 6px 18px rgba(30, 58, 95, 0.14)'
            : '0 4px 14px rgba(30, 58, 95, 0.08)',
          borderColor: hovered ? cloudColors.azure : cloudColors.cloudBorder,
        }}
      >
        <Stack gap="xs">
          <SubTitleLabel>{section.title}</SubTitleLabel>
          <BodyLabel>{section.description}</BodyLabel>
        </Stack>
      </CloudCard>
    </UnstyledButton>
  );
}
