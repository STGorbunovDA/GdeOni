import { Button, type ButtonProps } from '@mantine/core';
import type { ComponentPropsWithoutRef } from 'react';

/**
 * F2. Mobile-эквивалент style="PrimaryButton". Azure-фон, белый текст,
 * скруглённость 24px, bold 16px. Pressed состояние Mantine генерирует
 * из Azure[7] (AzureDeep) автоматически.
 *
 * Polymorphic component={Link} здесь сознательно НЕ поддержан —
 * типизация Mantine 7 polymorphic API сильно усложняет обёртку.
 * Если нужно <Button as Link>, используй Mantine <Button> напрямую
 * с теми же визуальными пропсами: variant=default, radius={24}, fw={700}.
 * Стили централизованы в кнопке-обёртке здесь — единая точка истины.
 */
type Props = ButtonProps &
  Omit<ComponentPropsWithoutRef<'button'>, keyof ButtonProps>;

export function PrimaryButton(props: Props) {
  return <Button radius={24} size="md" fw={700} {...props} />;
}
