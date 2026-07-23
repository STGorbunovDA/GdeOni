import { Anchor, Button, Container, Group, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import { Cloud, Globe, Smartphone } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { cloudColors } from '../design/theme';

/**
 * F27. Публичная страница `/download`.
 *  - Landing для новых юзеров, куда ведёт mobile BlockingUpdatePage
 *    и упоминания «у нас есть мобильное приложение» из веба.
 *  - Мобильное приложение пока в разработке: вместо кнопки «Скачать APK»
 *    показываем заглушку. Когда APK будет готов — вернуть кнопку скачивания
 *    (downloadUrl из GET /api/app/version + VITE_APK_FALLBACK_URL,
 *    хук useAppVersion) и блок-аккордеон «Как установить APK».
 *  - Anonymous-роут (вне ProtectedRoute) — юзер не обязан быть залогинен.
 */
export function DownloadPage() {
  return (
    <Container size="sm" pt={64} pb={64}>
      <Stack gap="xl">
        <Stack gap="sm" align="center">
          <Cloud size={56} color={cloudColors.azureDeep} />
          <TitleLabel>ГдеОни</TitleLabel>
          <BodyLabel style={{ textAlign: 'center', maxWidth: 480 }}>
            Каталог мест захоронений с GPS-координатами. Помогает быстро
            находить могилы близких и делиться местом с родственниками.
          </BodyLabel>
        </Stack>

        {/* Android APK — заглушка: приложение пока в разработке */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Smartphone size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Android-приложение</SubTitleLabel>
            </Group>
            <BodyLabel>
              Мобильное приложение пока в разработке — скоро будет доступно
              для скачивания. А пока пользуйтесь веб-версией ниже.
            </BodyLabel>
            <Group>
              <Button disabled radius={24} fw={700} size="lg">
                Скоро
              </Button>
            </Group>
          </Stack>
        </CloudCard>

        {/* Web-версия */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Globe size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Веб-версия</SubTitleLabel>
            </Group>
            <BodyLabel>
              Не хотите ставить APK — заходите в браузере. Все функции, кроме
              фоновых напоминаний о годовщинах, работают одинаково.
            </BodyLabel>
            <Group>
              <Button
                component={Link}
                to="/login"
                variant="default"
                radius={24}
                fw={700}
                size="md"
              >
                Открыть веб-версию
              </Button>
            </Group>
          </Stack>
        </CloudCard>

        {/* Контакты / legal */}
        <Stack gap={4} align="center">
          <CaptionLabel>
            Вопросы —{' '}
            <Anchor
              href="mailto:bous07@mail.ru"
              c={cloudColors.azureDeep}
            >
              bous07@mail.ru
            </Anchor>
          </CaptionLabel>
          <CaptionLabel>
            <Anchor component={Link} to="/legal/privacy" c={cloudColors.azureDeep}>
              Политика конфиденциальности
            </Anchor>
            {' · '}
            <Anchor component={Link} to="/legal/terms" c={cloudColors.azureDeep}>
              Условия использования
            </Anchor>
          </CaptionLabel>
        </Stack>
      </Stack>
    </Container>
  );
}
