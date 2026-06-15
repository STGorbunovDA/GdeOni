import { Text, type TextProps } from '@mantine/core';
import { cloudColors } from '../../design/theme';
import type { ReactNode } from 'react';

/**
 * F2. Типографические пресеты — зеркало mobile-стилей
 * TitleLabel / SubTitleLabel / BodyLabel / CaptionLabel
 * (см. Styles.xaml в frontend/mobile).
 */
type LabelProps = Omit<TextProps, 'children'> & { children: ReactNode };

/** 32px bold InkBlue. Hero-заголовок страницы. */
export function TitleLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={32} fw={700} c={cloudColors.inkBlue} lh={1.2} {...rest}>
      {children}
    </Text>
  );
}

/** 20px bold InkBlue. Заголовок секции. */
export function SubTitleLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={20} fw={700} c={cloudColors.inkBlue} {...rest}>
      {children}
    </Text>
  );
}

/** 15px regular InkBlue, line-height 1.4. Основной текст. */
export function BodyLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={15} c={cloudColors.inkBlue} lh={1.4} {...rest}>
      {children}
    </Text>
  );
}

/** 13px regular серо-голубой #6B7C8C. Подпись/мета. */
export function CaptionLabel({ children, ...rest }: LabelProps) {
  return (
    <Text fz={13} c={cloudColors.captionGray} {...rest}>
      {children}
    </Text>
  );
}
