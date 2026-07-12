import { Paper, type PaperProps } from '@mantine/core';
import { cloudColors } from '../../design/theme';
import type { CSSProperties, ReactNode } from 'react';

/**
 * Карточка в стиле Medilab (.service-item / .info-item в шаблоне):
 * белая поверхность, скругление 10px и мягкая тень
 * `0 2px 15px rgba(0,0,0,.1)` — БЕЗ рамки (в шаблоне глубину даёт
 * только тень). Padding по умолчанию 16px.
 */
type Props = PaperProps & {
  children: ReactNode;
  /** Дополнительные inline-стили (для редких случаев — лучше через className). */
  style?: CSSProperties;
};

export function CloudCard({ children, style, ...rest }: Props) {
  return (
    <Paper
      p="md"
      style={{
        backgroundColor: cloudColors.cloud,
        // Прозрачная рамка «про запас»: вызывающие (например, выбор
        // карточки на /route) переопределяют borderColor — без неё
        // подсветка выбранного элемента бы пропала, а layout не поедет.
        border: '1px solid transparent',
        borderRadius: 10,
        boxShadow: '0 2px 15px rgba(0, 0, 0, 0.1)',
        ...style,
      }}
      {...rest}
    >
      {children}
    </Paper>
  );
}
