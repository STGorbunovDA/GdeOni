import { Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';

/**
 * F2.1. Главная страница админки. Список карточек-разделов — строго
 * как в mobile (frontend/mobile/.../Views/Admin/AdminPage.xaml):
 *   1. История правок
 *   2. Пользователи
 *   3. Платежи
 *   4. Обращения
 *   5. Найти умершего
 *
 * Заголовки и описания скопированы один-в-один. Реальные страницы
 * за каждой кнопкой "Открыть" — заглушки, наполнятся в F17.
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
  const navigate = useNavigate();

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Админка</TitleLabel>
        <CaptionLabel>
          Управление пользователями, платежами и историей правок.
        </CaptionLabel>
      </Stack>

      {SECTIONS.map((section) => (
        <CloudCard key={section.to}>
          <Stack gap="sm">
            <SubTitleLabel>{section.title}</SubTitleLabel>
            <BodyLabel>{section.description}</BodyLabel>
            <PrimaryButton onClick={() => navigate(section.to)}>
              Открыть
            </PrimaryButton>
          </Stack>
        </CloudCard>
      ))}
    </Stack>
  );
}
