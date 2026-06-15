import { Paper, type PaperProps } from '@mantine/core';
import { cloudColors } from '../../design/theme';
import type { CSSProperties, ReactNode } from 'react';

/**
 * F2. Mobile-эквивалент Border style="CloudCard". Белый фон,
 * скруглённые углы 16px, мягкая тень InkBlue 0.08, тонкая
 * рамка #E0EAF2. Padding по умолчанию 16px — как в mobile.
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
        border: `1px solid ${cloudColors.cloudBorder}`,
        borderRadius: 16,
        boxShadow: '0 4px 14px rgba(30, 58, 95, 0.08)',
        ...style,
      }}
      {...rest}
    >
      {children}
    </Paper>
  );
}
