import { Button, type ButtonProps } from '@mantine/core';
import type { ComponentPropsWithoutRef } from 'react';

/**
 * Основная кнопка в стиле Medilab (.cta-btn): сплошная заливка accent
 * #1977cc, белый текст, полностью скруглённая «пилюля» (border-radius
 * 50px в шаблоне). Pressed-состояние Mantine берёт из azure[7].
 *
 * Polymorphic component={Link} здесь сознательно НЕ поддержан —
 * типизация Mantine 7 polymorphic API сильно усложняет обёртку.
 * Если нужно <Button as Link>, используй Mantine <Button> напрямую
 * с теми же визуальными пропсами: radius={50}, fw={600}.
 * Стили централизованы в кнопке-обёртке здесь — единая точка истины.
 */
type Props = ButtonProps &
  Omit<ComponentPropsWithoutRef<'button'>, keyof ButtonProps>;

export function PrimaryButton(props: Props) {
  return <Button radius={50} size="md" fw={600} {...props} />;
}
