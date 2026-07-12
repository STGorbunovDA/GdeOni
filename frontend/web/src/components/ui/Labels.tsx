import { Text, type TextProps } from '@mantine/core';
import { cloudColors } from '../../design/theme';
import type { ReactNode } from 'react';

/**
 * Типографические пресеты в стиле шаблона Medilab:
 *  - заголовки: Poppins, heading-color #2c4964;
 *  - основной текст: Roboto, default-color #444444;
 *  - подписи: приглушённый серо-синий.
 */
type LabelProps = Omit<TextProps, 'children'> & { children: ReactNode };

// Poppins не содержит кириллицы — русские заголовки подхватывает
// Montserrat (см. design/theme.ts).
const headingFont = '"Poppins", "Montserrat", "Roboto", system-ui, sans-serif';

/** 32px bold, heading-color. Заголовок страницы. */
export function TitleLabel({ children, ...rest }: LabelProps) {
  return (
    <Text
      ff={headingFont}
      fz={32}
      fw={700}
      c={cloudColors.inkBlue}
      lh={1.2}
      {...rest}
    >
      {children}
    </Text>
  );
}

/** 20px semibold, heading-color. Заголовок секции. */
export function SubTitleLabel({ children, ...rest }: LabelProps) {
  return (
    <Text ff={headingFont} fz={20} fw={600} c={cloudColors.inkBlue} {...rest}>
      {children}
    </Text>
  );
}

/** 15px regular, default-color #444444. Основной текст. */
export function BodyLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={15} c={cloudColors.text} lh={1.5} {...rest}>
      {children}
    </Text>
  );
}

/** 13px regular, приглушённый. Подпись/мета. */
export function CaptionLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={13} c={cloudColors.captionGray} {...rest}>
      {children}
    </Text>
  );
}
