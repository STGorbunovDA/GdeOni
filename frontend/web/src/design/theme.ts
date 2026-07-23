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
/**
 * F37. Семантические токены палитры. ЗНАЧЕНИЯ живут в CSS
 * (src/styles.css: `:root` и `[data-mantine-color-scheme='dark']`), здесь
 * — только ссылки на них.
 *
 * Почему не хексы: этими токенами покрашены 147 мест в 37 файлах, почти
 * везде inline-стилями. С хексами тёмная тема означала бы «прокинуть
 * colorScheme в каждый компонент». С var() тема меняет значения
 * переменных, и браузер перекрашивает всё сам — код страниц не меняется
 * вообще.
 *
 * Токены СЕМАНТИЧЕСКИЕ, а не буквальные: `cloud` — это «поверхность
 * карточки», в тёмной теме она тёмно-синяя, а не белая. `azureDeep` —
 * «усиленный акцент»: в светлой теме он темнее azure, в тёмной светлее.
 */
export const cloudColors = {
  /** Тональный фон: аватарки, выбранные карточки, активный пункт меню. */
  sky: 'var(--cloud-sky)',
  /** Поверхность: карточки, сайдбар, модалки. */
  cloud: 'var(--cloud-surface)',
  /** Фон страницы. */
  mist: 'var(--cloud-bg)',
  /** Акцент бренда. */
  azure: 'var(--cloud-azure)',
  /** Усиленный акцент: иконки, ссылки, активные состояния. */
  azureDeep: 'var(--cloud-azure-deep)',
  /** Цвет заголовков. */
  inkBlue: 'var(--cloud-ink)',
  /** Основной текст. */
  text: 'var(--cloud-text)',
  /** Тонкая рамка/разделитель. */
  cloudBorder: 'var(--cloud-border)',
  /** Подписи, мета, второстепенный текст. */
  captionGray: 'var(--cloud-caption)',
  errorRed: 'var(--cloud-error)',
  /** Тень карточки — тоже токен: на тёмном фоне она плотнее. */
  shadow: 'var(--cloud-shadow)',
  /** «Утопленный» фон: <pre>, read-only панели. */
  sunken: 'var(--cloud-sunken)',
  /** Подсветка «требует внимания» (срочный тикет в списке). */
  dangerSurface: 'var(--cloud-danger-surface)',
  /** Пузыри переписки. */
  bubbleMine: 'var(--cloud-bubble-mine)',
  bubbleOther: 'var(--cloud-bubble-other)',
  bubbleBorder: 'var(--cloud-bubble-border)',
  /** Тёплая «предупреждающая» заливка (отклонённое медиа/воспоминание). */
  warnSurface: 'var(--cloud-warn-surface)',
} as const;

/**
 * Сырые значения светлой темы. Нужны там, где var() не подходит: Mantine
 * вычисляет из theme.white/black производные цвета в JS (autoContrast,
 * генерация CSS-переменных), и строку `var(...)` там разобрать нельзя.
 */
const rawLight = {
  cloud: '#FFFFFF',
  inkBlue: '#2C4964',
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

/**
 * F37. Тёмная палитра самой Mantine (её используют Modal, Menu, Input,
 * Alert и прочие компоненты, которые мы не красим руками). Дефолтная
 * dark-палитра Mantine нейтрально-серая, а у нас всё сине-серое —
 * без этой замены модалки выглядели бы чужеродно на фоне карточек.
 *
 * Значения зеркалят --cloud-* из styles.css: [6] — поверхность карточки,
 * [7] — фон страницы.
 */
const dark: MantineColorsTuple = [
  '#E6EDF3', // 0 — основной текст
  '#C2CCD6', // 1
  '#8A99A8', // 2 — dimmed
  '#5C6B7A', // 3
  '#3A4A5A', // 4 — рамки
  '#2A3947', // 5
  '#16202B', // 6 — поверхность (карточки, модалки)
  '#0F1720', // 7 — фон страницы
  '#0B1219', // 8
  '#070C11', // 9
];

export const theme = createTheme({
  primaryColor: 'azure',
  // В тёмной теме берём светлый оттенок акцента (4), иначе outline-кнопки
  // и ссылки цветом #1977CC на тёмном фоне не дотягивают до контраста 3:1.
  primaryShade: { light: 5, dark: 4 },
  // Следствие светлого акцента: на залитой кнопке белый текст стал бы
  // нечитаемым. autoContrast сам выберет тёмный текст там, где заливка
  // светлая. В светлой теме ничего не меняет — #1977CC остаётся с белым.
  autoContrast: true,
  colors: {
    azure,
    dark,
  },
  // Шаблон использует небольшие скругления у полей ввода (~4px).
  // Кнопки и карточки задают свой радиус сами.
  defaultRadius: 'sm',
  fontFamily: bodyFont,
  headings: {
    fontFamily: headingFont,
    fontWeight: '700',
  },
  // Только сырые хексы: Mantine считает из них производные в JS.
  black: rawLight.inkBlue,
  white: rawLight.cloud,
});
