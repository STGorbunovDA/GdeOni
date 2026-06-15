import { Button, type ButtonProps } from '@mantine/core';
import type { ComponentPropsWithoutRef } from 'react';

/**
 * F2. Mobile-эквивалент style="GhostButton". Прозрачный фон, Azure
 * бордер и текст, bold 16px, 24px radius. Mantine variant='outline'
 * + hover/pressed tonal Sky генерирует автоматически.
 *
 * Polymorphic component={Link} НЕ поддержан — см. PrimaryButton.tsx.
 */
type Props = ButtonProps &
  Omit<ComponentPropsWithoutRef<'button'>, keyof ButtonProps>;

export function GhostButton(props: Props) {
  return <Button variant="outline" radius={24} size="md" fw={700} {...props} />;
}
