import { Center, Loader, Stack } from '@mantine/core';
import { Cloud } from 'lucide-react';
import { cloudColors } from '../design/theme';
import { BodyLabel } from '../components/ui';

/**
 * F4. Splash во время startup-refresh. Показывается до момента,
 * пока SessionBootstrap не разрешит сессию. Обычно меньше 200мс,
 * но при медленной сети может быть заметно — поэтому показываем
 * брендинг + loader, чтобы экран не был просто белым.
 */
export function BootstrapSplash() {
  return (
    <Center h="100vh">
      <Stack gap="md" align="center">
        <Cloud size={56} color={cloudColors.azureDeep} />
        <BodyLabel>GdeOni</BodyLabel>
        <Loader size="sm" />
      </Stack>
    </Center>
  );
}
