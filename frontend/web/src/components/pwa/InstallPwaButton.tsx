import { useEffect, useState } from 'react';
import { Anchor, Button, Modal, Stack, Text } from '@mantine/core';
import { Link } from 'react-router-dom';
import { Smartphone } from 'lucide-react';
import { cloudColors } from '../../design/theme';
import {
  clearInstallPrompt,
  getInstallPrompt,
  onInstallChange,
} from '../../pwa/installPrompt';

/**
 * Кнопка «Скачать на смартфон» — установка сайта как приложения (PWA).
 *  - Android/Chrome, где браузер уже прислал beforeinstallprompt: клик
 *    вызывает нативную установку.
 *  - Иначе (iPhone, или prompt ещё не пришёл): открываем окно с
 *    инструкцией под платформы + ссылка на подробную страницу /download.
 * PWA работает и на Android, и на iPhone (магазины не нужны).
 */
type Props = {
  label?: string;
  variant?: string;
  size?: string;
  fw?: number;
  radius?: number | string;
};

export function InstallPwaButton({
  label = 'Скачать на смартфон',
  variant,
  size = 'md',
  fw = 700,
  radius = 24,
}: Props) {
  const [hasPrompt, setHasPrompt] = useState(() => getInstallPrompt() !== null);
  const [opened, setOpened] = useState(false);

  useEffect(
    () => onInstallChange(() => setHasPrompt(getInstallPrompt() !== null)),
    [],
  );

  async function handleClick() {
    const prompt = getInstallPrompt();
    if (prompt) {
      await prompt.prompt();
      await prompt.userChoice;
      clearInstallPrompt();
      return;
    }
    // Нативного prompt нет (iPhone или Android, где событие ещё не пришло /
    // приложение уже установлено) — показываем инструкцию.
    setOpened(true);
  }

  // hasPrompt влияет только на путь клика; кнопку показываем всегда, чтобы
  // работал и вариант с инструкцией.
  void hasPrompt;

  return (
    <>
      <Button
        onClick={handleClick}
        leftSection={<Smartphone size={16} />}
        variant={variant}
        size={size}
        fw={fw}
        radius={radius}
      >
        {label}
      </Button>

      <Modal
        opened={opened}
        onClose={() => setOpened(false)}
        centered
        title="Установить на смартфон"
      >
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            Приложение — это сам сайт, добавленный на главный экран. Открывается
            на весь экран, как обычное приложение.
          </Text>

          <Stack gap={4}>
            <Text fw={700}>Android</Text>
            <Text size="sm" c="dimmed">
              Меню браузера (⋮ или ≡) → «Добавить на главный экран» (в Chrome —
              «Установить приложение»). Иногда браузер сам показывает плашку внизу.
            </Text>
          </Stack>

          <Stack gap={4}>
            <Text fw={700}>iPhone</Text>
            <Text size="sm" c="dimmed">
              Откройте сайт в Safari → «Поделиться» (квадрат со стрелкой вверх) →
              «На экран „Домой"».
            </Text>
          </Stack>

          <Anchor component={Link} to="/download" c={cloudColors.azureDeep}>
            Подробная инструкция
          </Anchor>
        </Stack>
      </Modal>
    </>
  );
}
