import { Alert, Button, Group, Text } from '@mantine/core';
import { RefreshCw } from 'lucide-react';
import { cloudColors } from '../../design/theme';
import { useAppUpdate } from '../../hooks/useAppUpdate';

/**
 * Плашка «Доступно обновление» для уже открытой сессии. Когда на сервер
 * выкатили новую сборку (useAppUpdate засёк смену хэша бандла), показываем
 * её над контентом; кнопка перезагружает страницу и подтягивает новую
 * версию. Живёт в AppLayout — на всех приватных страницах.
 *
 * Не навязываемся автоперезагрузкой: человек мог заполнять форму, поэтому
 * решение обновиться — за ним.
 */
export function AppUpdateBanner() {
  const { updateAvailable, reload } = useAppUpdate();

  if (!updateAvailable) return null;

  return (
    <Alert
      variant="light"
      color="azure"
      icon={<RefreshCw size={20} />}
      mb="md"
      title="Доступно обновление"
      styles={{ title: { color: cloudColors.inkBlue } }}
    >
      <Group justify="space-between" align="center" wrap="wrap" gap="sm">
        <Text size="sm" c={cloudColors.text}>
          Вышла новая версия приложения. Обновите, чтобы получить последние
          изменения.
        </Text>
        <Button
          variant="filled"
          color="azure"
          size="xs"
          radius="xl"
          leftSection={<RefreshCw size={14} />}
          onClick={reload}
        >
          Обновить
        </Button>
      </Group>
    </Alert>
  );
}
