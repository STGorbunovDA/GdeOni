import {
  ActionIcon,
  Tooltip,
  useComputedColorScheme,
  useMantineColorScheme,
} from '@mantine/core';
import { Moon, Sun } from 'lucide-react';
import { cloudColors } from '../../design/theme';

/**
 * F37. Переключатель светлой/тёмной темы.
 *
 * Хранит выбор в localStorage (ключ gdeoni-color-scheme, см. main.tsx).
 * На первом заходе схема = 'auto' (системная настройка), но как только
 * пользователь нажал кнопку — переходим на явные 'light'/'dark' и больше
 * за системой не следим: иначе его выбор молча перетирался бы при смене
 * системной темы.
 *
 * useComputedColorScheme нужен именно потому, что colorScheme может быть
 * 'auto' — а иконку надо показать по ФАКТИЧЕСКОЙ теме.
 */
export function ThemeToggle({ size = 'lg' }: { size?: string }) {
  const { setColorScheme } = useMantineColorScheme();
  const computed = useComputedColorScheme('light', {
    getInitialValueInEffect: true,
  });

  const isDark = computed === 'dark';
  const label = isDark ? 'Светлая тема' : 'Тёмная тема';

  return (
    <Tooltip label={label} position="right" withArrow>
      <ActionIcon
        variant="subtle"
        color="gray"
        size={size}
        aria-label={label}
        onClick={() => setColorScheme(isDark ? 'light' : 'dark')}
      >
        {isDark ? (
          <Sun size={18} color={cloudColors.azureDeep} />
        ) : (
          <Moon size={18} color={cloudColors.azureDeep} />
        )}
      </ActionIcon>
    </Tooltip>
  );
}
