import { Accordion, Anchor, Button, Container, Group, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import { Cloud, Download, Globe, Smartphone } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { cloudColors } from '../design/theme';
import { useAppVersion } from '../hooks/useAppVersion';

/**
 * F27. Публичная страница `/download`.
 *  - Landing для новых юзеров, куда ведёт mobile BlockingUpdatePage
 *    и упоминания «у нас есть мобильное приложение» из веба.
 *  - Кнопка «Скачать APK» использует downloadUrl из
 *    <c>GET /api/app/version</c> (D17.1). Если бэк недоступен —
 *    fallback на <c>VITE_APK_FALLBACK_URL</c> из .env.
 *  - Anonymous-роут (вне ProtectedRoute) — юзер не обязан быть
 *    залогинен.
 */
const APK_FALLBACK_URL: string =
  import.meta.env.VITE_APK_FALLBACK_URL ?? 'https://gdeoni.ru/apk/latest.apk';

export function DownloadPage() {
  const { data, isLoading } = useAppVersion();

  const downloadUrl = data?.downloadUrl ?? APK_FALLBACK_URL;
  const latestVersion = data?.latestVersion;

  return (
    <Container size="sm" pt={64} pb={64}>
      <Stack gap="xl">
        <Stack gap="sm" align="center">
          <Cloud size={56} color={cloudColors.azureDeep} />
          <TitleLabel>GdeOni</TitleLabel>
          <BodyLabel style={{ textAlign: 'center', maxWidth: 480 }}>
            Каталог мест захоронений с GPS-координатами. Помогает быстро
            находить могилы близких и делиться местом с родственниками.
          </BodyLabel>
        </Stack>

        {/* Android APK */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Smartphone size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Android-приложение</SubTitleLabel>
            </Group>
            <BodyLabel>
              Установите APK-файл на телефон. Работает на Android 8 и выше.
            </BodyLabel>
            <Group>
              {/* Mantine polymorphic Button — визуально совпадает с
                  PrimaryButton (radius=24, fw=700), но принимает
                  component="a" для нативного <a href>. */}
              <Button
                component="a"
                href={downloadUrl}
                leftSection={<Download size={18} />}
                loading={isLoading}
                radius={24}
                fw={700}
                size="lg"
              >
                Скачать APK
              </Button>
            </Group>
            {latestVersion && (
              <CaptionLabel>Версия {latestVersion}</CaptionLabel>
            )}
          </Stack>
        </CloudCard>

        {/* FAQ */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Как установить APK</SubTitleLabel>
            <Accordion variant="separated" radius="md">
              <Accordion.Item value="step-1">
                <Accordion.Control>1. Скачайте файл</Accordion.Control>
                <Accordion.Panel>
                  Нажмите «Скачать APK» — файл сохранится в папку «Загрузки».
                </Accordion.Panel>
              </Accordion.Item>
              <Accordion.Item value="step-2">
                <Accordion.Control>
                  2. Откройте файл на телефоне
                </Accordion.Control>
                <Accordion.Panel>
                  Android покажет предупреждение об установке из неизвестного
                  источника — это ожидаемо, приложение распространяется без
                  Play Market.
                </Accordion.Panel>
              </Accordion.Item>
              <Accordion.Item value="step-3">
                <Accordion.Control>
                  3. Разрешите установку
                </Accordion.Control>
                <Accordion.Panel>
                  На Android 8+ разрешение даётся для конкретного браузера или
                  файлового менеджера: <b>Настройки → Приложения → [ваш браузер]
                  → Установка неизвестных приложений</b>. Затем вернитесь к
                  файлу.
                </Accordion.Panel>
              </Accordion.Item>
              <Accordion.Item value="step-4">
                <Accordion.Control>4. Нажмите «Установить»</Accordion.Control>
                <Accordion.Panel>
                  Через несколько секунд появится значок GdeOni. Откройте
                  приложение и войдите или зарегистрируйтесь.
                </Accordion.Panel>
              </Accordion.Item>
            </Accordion>
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
              href="mailto:support@gdeoni.ru"
              c={cloudColors.azureDeep}
            >
              support@gdeoni.ru
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
