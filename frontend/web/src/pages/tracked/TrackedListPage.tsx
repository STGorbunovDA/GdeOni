import { Stack } from '@mantine/core';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';

/**
 * F2.1. Главная защищённая страница. Здесь должен быть список tracked
 * deceased — придёт в F9. Сейчас просто маркер "роут работает".
 *
 * Навигация и выход — в sidebar (AppLayout), здесь их не дублируем.
 */
export function TrackedListPage() {
  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Отслеживаемые</TitleLabel>
        <CaptionLabel>
          Заглушка для F9. Если ты видишь sidebar слева (или гамбургер
          сверху на узком экране) — F2.1 работает.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="sm">
          <BodyLabel>
            Реальный список отслеживаемых появится в F9. Сейчас навигация
            живёт в sidebar — клик по пунктам должен подсвечивать активный.
          </BodyLabel>
          <CaptionLabel>
            Пункт "Админка" виден только если ты залогинился как админ
            (используй кнопку "Войти как админ" на /login).
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}
