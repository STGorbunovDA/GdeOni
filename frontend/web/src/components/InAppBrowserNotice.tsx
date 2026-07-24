import { Alert, Button, Group, Stack, Text } from '@mantine/core';
import { Copy } from 'lucide-react';
import { useState } from 'react';
import { isInAppBrowser } from '../utils/inAppBrowser';

/**
 * F41. Баннер для встроенных браузеров приложений (ВКонтакте, Instagram…).
 *
 * Их WebView ломает регистрацию/вход на нашем домене (запрос не доходит,
 * пользователь видит «Ошибка сети»). Предлагаем открыть сайт в обычном
 * браузере и даём кнопку скопировать адрес — чтобы вставить в Safari/Chrome.
 *
 * Ничего не рендерит в обычном браузере (isInAppBrowser === false).
 */
export function InAppBrowserNotice() {
  const [copied, setCopied] = useState(false);

  if (!isInAppBrowser()) return null;

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.origin);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      // clipboard недоступен (нет https / отказ) — юзер скопирует адрес
      // из строки браузера сам, инструкция об этом ниже.
    }
  }

  return (
    <Alert
      color="orange"
      variant="light"
      title="Откройте сайт в браузере"
      mb="lg"
    >
      <Stack gap="xs">
        <Text size="sm">
          Похоже, сайт открыт внутри приложения (например, ВКонтакте). В таком
          встроенном браузере регистрация и вход могут не работать. Откройте
          сайт в обычном браузере — Safari или Chrome.
        </Text>
        <Text size="sm">
          Нажмите значок меню (⋯) вверху и выберите «Открыть в Safari» /
          «Открыть в браузере». Либо скопируйте ссылку и вставьте её в браузер.
        </Text>
        <Group>
          <Button
            size="xs"
            variant="light"
            color="orange"
            leftSection={<Copy size={14} />}
            onClick={copyLink}
          >
            {copied ? 'Ссылка скопирована' : 'Скопировать ссылку'}
          </Button>
        </Group>
      </Stack>
    </Alert>
  );
}
