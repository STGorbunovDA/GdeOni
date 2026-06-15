import { createTheme, MantineColorsTuple } from '@mantine/core';

/**
 * F2. Cloud-палитра, синхронизированная с mobile.
 * Значения взяты из frontend/mobile/.../Resources/Styles/Colors.xaml
 * и Styles.xaml (CloudGradient / TitleLabel / CloudCard etc).
 *
 * Важно: цвета слегка отличаются от тех, что были указаны в плане
 * (PlanFull.txt:9536), но я взял реальные mobile-значения, потому
 * что цель F2 — визуальная совместимость с приложением, а не с
 * текстом плана. При расхождении приоритет у mobile.
 */
export const cloudColors = {
  sky: '#E6F4FB',
  cloud: '#FFFFFF',
  mist: '#F4F6F8',
  azure: '#5FA8D3',
  azureDeep: '#3F8AB8',
  inkBlue: '#1E3A5F',
  cloudBorder: '#E0EAF2',
  captionGray: '#6B7C8C',
  errorRed: '#C0392B',
} as const;

/**
 * Mantine ожидает палитру из 10 оттенков. Берём центральный [5] = Azure
 * и интерполируем светлее/темнее. Значения подобраны на глаз, чтобы
 * hover/disabled выглядели мягко в Cloud-стиле.
 */
const azure: MantineColorsTuple = [
  '#E7F2F9', // 0  — фон tonal background для светлого hover
  '#CFE5F3',
  '#A8D2EA',
  '#82BFE0',
  '#6CB1DA',
  '#5FA8D3', // 5  — основной Azure (как в mobile)
  '#4F9AC7',
  '#3F8AB8', // 7  — AzureDeep, для pressed
  '#2F7CA9',
  '#1F6E9A',
];

export const theme = createTheme({
  primaryColor: 'azure',
  primaryShade: { light: 5, dark: 7 },
  colors: {
    azure,
  },
  defaultRadius: 'md', // 8px — для Input/Select. Кнопки и карточки задают свой.
  fontFamily:
    '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  headings: {
    fontFamily:
      '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    fontWeight: '700',
  },
  black: cloudColors.inkBlue,
  white: cloudColors.cloud,
});
