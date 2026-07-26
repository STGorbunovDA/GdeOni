import { useEffect, useState } from 'react';
import { CloseButton, Group, Paper, Stack, Text } from '@mantine/core';
import { Download, Share } from 'lucide-react';
import { PrimaryButton } from '../ui';
import { cloudColors } from '../../design/theme';
import {
  clearInstallPrompt,
  getInstallPrompt,
  isIosSafari,
  isStandalone,
  onInstallChange,
} from '../../pwa/installPrompt';

/**
 * PWA-подсказка «Установить приложение».
 *  - Android/Chrome: показываем кнопку, которая вызывает нативный prompt
 *    (перехваченный заранее в pwa/installPrompt).
 *  - iPhone/Safari: авто-события нет — показываем инструкцию
 *    «Поделиться → На экран „Домой"».
 * Прячется, если приложение уже установлено (standalone) или юзер закрыл
 * баннер (запоминаем в localStorage).
 */
const DISMISS_KEY = 'gdeoni:pwa-install-dismissed';

function readDismissed(): boolean {
  try {
    return window.localStorage.getItem(DISMISS_KEY) === '1';
  } catch {
    return false;
  }
}

export function InstallPwaBanner() {
  const [hasPrompt, setHasPrompt] = useState(() => getInstallPrompt() !== null);
  const [dismissed, setDismissed] = useState(readDismissed);
  const iosHint = !dismissed && !isStandalone() && isIosSafari();

  useEffect(() => {
    return onInstallChange(() => setHasPrompt(getInstallPrompt() !== null));
  }, []);

  function close() {
    setDismissed(true);
    try {
      window.localStorage.setItem(DISMISS_KEY, '1');
    } catch {
      // приватный режим — просто не запомним, покажем снова позже
    }
  }

  async function handleInstall() {
    const prompt = getInstallPrompt();
    if (!prompt) return;
    await prompt.prompt();
    await prompt.userChoice;
    clearInstallPrompt();
    close();
  }

  if (dismissed || isStandalone()) return null;
  const showAndroid = hasPrompt;
  if (!showAndroid && !iosHint) return null;

  return (
    <Paper
      shadow="md"
      radius="lg"
      p="md"
      style={{
        position: 'fixed',
        left: '50%',
        transform: 'translateX(-50%)',
        bottom: 16,
        zIndex: 300,
        width: 'min(440px, calc(100vw - 24px))',
        background: cloudColors.cloud,
        border: `1px solid ${cloudColors.cloudBorder}`,
      }}
    >
      <Group align="flex-start" gap="sm" wrap="nowrap">
        <Stack gap={showAndroid ? 8 : 4} style={{ flex: 1, minWidth: 0 }}>
          {showAndroid ? (
            <>
              <Text fw={700} c={cloudColors.inkBlue}>
                Установить приложение
              </Text>
              <Text size="sm" c={cloudColors.captionGray}>
                Добавьте «ГдеОни» на главный экран — открывается как приложение,
                на весь экран.
              </Text>
              <PrimaryButton
                onClick={handleInstall}
                leftSection={<Download size={16} />}
              >
                Установить
              </PrimaryButton>
            </>
          ) : (
            <>
              <Text fw={700} c={cloudColors.inkBlue}>
                Установить на главный экран
              </Text>
              <Text size="sm" c={cloudColors.captionGray}>
                Нажмите{' '}
                <Share
                  size={14}
                  style={{ verticalAlign: 'middle', display: 'inline' }}
                />{' '}
                «Поделиться» внизу Safari, затем «На экран „Домой"».
              </Text>
            </>
          )}
        </Stack>
        <CloseButton onClick={close} aria-label="Закрыть" />
      </Group>
    </Paper>
  );
}
