import { createTheme, MantineColorsTuple } from '@mantine/core';

/**
 * Палитра и типографика в стиле шаблона Medilab (BootstrapMade).
 *
 * Ключевое: имена токенов оставлены прежними (azure / azureDeep /
 * inkBlue / sky / …), потому что на них завязан весь код приложения.
 * Меняются только ЗНАЧЕНИЯ — так перекрашивается всё сразу, без
 * правки сотни компонентов.
 *
 * Соответствие переменным шаблона (assets/css/main.css :root):
 *   --accent-color   #1977cc  → azure       (кнопки, ссылки, активное)
 *   --heading-color  #2c4964  → inkBlue     (заголовки)
 *   --default-color  #444444  → text        (основной текст)
 *   --background     #f1f7fc  → mist        (фон страницы, .light-background)
 *   --surface-color  #ffffff  → cloud       (карточки, сайдбар)
 *
 * Bootstrap в проект НЕ тащим: у нас Mantine со своей дизайн-системой,
 * два CSS-фреймворка конфликтовали бы на reset'ах и утилитах. Берём
 * только визуальный язык шаблона.
 */
export const cloudColors = {
  /** Светлый tonal-фон (аватарки, выбранные карточки, активный пункт меню). */
  sky: '#E8F2FB',
  /** Поверхность: карточки, сайдбар, модалки. */
  cloud: '#FFFFFF',
  /** Фон страницы (--background-color в .light-background шаблона). */
  mist: '#F1F7FC',
  /** Акцент бренда (--accent-color). */
  azure: '#1977CC',
  /** Затемнённый акцент: pressed, иконки, ссылки. */
  azureDeep: '#145A9E',
  /** Цвет заголовков (--heading-color). */
  inkBlue: '#2C4964',
  /** Основной текст (--default-color). */
  text: '#444444',
  /** Тонкая рамка/разделитель. */
  cloudBorder: '#E2ECF5',
  /** Подписи, мета, второстепенный текст. */
  captionGray: '#7A8794',
  errorRed: '#C0392B',
} as const;

/**
 * Mantine ждёт 10 оттенков. Центр [5] = accent #1977cc, вокруг —
 * интерполяция для hover / disabled / pressed.
 */
const azure: MantineColorsTuple = [
  '#E7F1FB', // 0 — tonal-фон светлого hover
  '#CFE3F6',
  '#9FC7EE',
  '#6FAAE5',
  '#4691DA',
  '#1977CC', // 5 — основной accent
  '#166BB8',
  '#145A9E', // 7 — pressed / deep
  '#0F4F8A',
  '#0B3F70',
];

/**
 * Шрифты шаблона: Roboto — текст, Poppins — заголовки, Raleway — навигация.
 * Все self-hosted (src/assets/fonts), без обращений к Google Fonts.
 *
 * ВАЖНО: в Poppins НЕТ кириллицы (только latin + devanagari), поэтому в
 * стеке заголовков вторым идёт Montserrat — геометрический шрифт с
 * кириллицей, визуально близкий к Poppins. Благодаря unicode-range в
 * @font-face браузер сам берёт латиницу из Poppins, а кириллицу из
 * Montserrat — стыка не видно. Без этого все русские заголовки молча
 * падали бы в Roboto, и heading-шрифт не работал бы вовсе.
 */
const bodyFont =
  '"Roboto", system-ui, -apple-system, "Segoe UI", "Helvetica Neue", Arial, sans-serif';
const headingFont =
  '"Poppins", "Montserrat", "Roboto", system-ui, sans-serif';
export const navFont = '"Raleway", "Roboto", system-ui, sans-serif';

export const theme = createTheme({
  primaryColor: 'azure',
  primaryShade: { light: 5, dark: 7 },
  colors: {
    azure,
  },
  // Шаблон использует небольшие скругления у полей ввода (~4px).
  // Кнопки и карточки задают свой радиус сами.
  defaultRadius: 'sm',
  fontFamily: bodyFont,
  headings: {
    fontFamily: headingFont,
    fontWeight: '700',
  },
  black: cloudColors.inkBlue,
  white: cloudColors.cloud,
});
