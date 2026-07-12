import { Button, type ButtonProps } from '@mantine/core';
import type { ComponentPropsWithoutRef } from 'react';

/**
 * Вторичная кнопка в стиле Medilab: прозрачный фон, accent-бордер и
 * текст, «пилюля» 50px. Tonal hover Mantine генерирует из azure[0].
 *
 * Polymorphic component={Link} НЕ поддержан — см. PrimaryButton.tsx.
 */
type Props = ButtonProps &
  Omit<ComponentPropsWithoutRef<'button'>, keyof ButtonProps>;

export function GhostButton(props: Props) {
  return <Button variant="outline" radius={50} size="md" fw={600} {...props} />;
}
