import { useAppFeatures } from './useAppFeatures';

/**
 * F39. Цена подписки для UI — берётся с бэка (/api/app/features), из того
 * же конфига, откуда её берёт создание платежа.
 *
 * Раньше «49 ₽/мес» было вписано текстом в пяти местах, и смена тарифа
 * означала бы: на кнопке одна сумма, а с карты спишется другая.
 *
 * Пока features не загрузились, возвращаем null — вызывающий показывает
 * кнопку без суммы, а не с неверной. Соврать про цену хуже, чем промолчать.
 */
export function useSubscriptionPrice(): {
  /** Цена в рублях; null пока не загрузилось. */
  priceRub: number | null;
  /** Готовая подпись «99 ₽/мес» или пустая строка. */
  priceLabel: string;
} {
  const features = useAppFeatures();
  const priceRub = features.data?.monthlyPriceRub ?? null;

  return {
    priceRub,
    priceLabel: priceRub === null ? '' : `${formatRub(priceRub)}/мес`,
  };
}

/** Целые рубли: копеек в тарифе нет, «99.00 ₽» только шумит. */
function formatRub(value: number): string {
  return `${value.toLocaleString('ru-RU', { maximumFractionDigits: 0 })} ₽`;
}
