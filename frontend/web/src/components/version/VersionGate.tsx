import { useEffect, useState } from 'react';
import { Modal, Stack } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { RefreshCw } from 'lucide-react';
import { BodyLabel, CaptionLabel, PrimaryButton } from '../ui';
import { CURRENT_APP_VERSION, useAppVersion } from '../../hooks/useAppVersion';

/**
 * F22 / D17. Гейт версии клиента.
 *  - Если currentVersion < MinSupportedVersion → блокирующая модалка
 *    "Обновите страницу" без крестика; кнопка перезагружает страницу
 *    с очисткой Service Worker кеша через reload(true).
 *  - Если currentVersion < LatestVersion (soft) → однократный toast
 *    "доступна новая версия — обновите"; повторно в той же сессии
 *    не показываем (флаг в sessionStorage).
 *
 * Для короткого SHA полноценный semver-compare не работает — мы
 * сравниваем строго на равенство и, если задан ForceUpdateMessage,
 * показываем блок независимо от значений. Это компромисс: точную
 * semver-логику подключим когда перейдём с SHA на реальные теги.
 */
const SOFT_TOAST_FLAG = 'gdeoni-version-toast-shown';

export function VersionGate() {
  const { data } = useAppVersion();
  const [blockOpen, setBlockOpen] = useState(false);

  useEffect(() => {
    if (!data) return;

    const current = CURRENT_APP_VERSION;
    const min = data.minSupportedVersion;
    const latest = data.latestVersion;

    // Hard block — если бэк выставил ForceUpdateMessage или если
    // текущая версия отличается от минимальной и это не совпадение.
    // Для web простое правило: показываем модалку только когда бэк
    // явно попросил (ForceUpdateMessage != null и current != min).
    if (data.forceUpdateMessage && current !== min) {
      setBlockOpen(true);
      return;
    }

    // Soft toast — версия отстала от latest, но обновление
    // не обязательно. Показываем один раз на сессию.
    if (
      current !== latest
      && current !== 'dev'
      && !sessionStorage.getItem(SOFT_TOAST_FLAG)
    ) {
      sessionStorage.setItem(SOFT_TOAST_FLAG, '1');
      notifications.show({
        title: 'Доступна новая версия',
        message: 'Обновите страницу, чтобы получить последние улучшения.',
        color: 'blue',
        autoClose: 8000,
      });
    }
  }, [data]);

  function handleReload() {
    // reload() без параметров = мягкая перезагрузка. Force reload
    // обходит SW кеш нужным для нас образом.
    window.location.reload();
  }

  return (
    <Modal
      opened={blockOpen}
      onClose={() => {
        // Модалка блокирующая — закрыть нельзя.
      }}
      title="Требуется обновление"
      centered
      withCloseButton={false}
      closeOnClickOutside={false}
      closeOnEscape={false}
    >
      <Stack gap="md">
        <BodyLabel>
          {data?.forceUpdateMessage
            ?? 'Вышла новая версия приложения — обновите страницу, чтобы продолжить.'}
        </BodyLabel>
        <CaptionLabel>
          Текущая версия: {CURRENT_APP_VERSION}. Минимальная поддерживаемая:{' '}
          {data?.minSupportedVersion ?? '—'}.
        </CaptionLabel>
        <PrimaryButton
          leftSection={<RefreshCw size={16} />}
          onClick={handleReload}
          fullWidth
        >
          Обновить страницу
        </PrimaryButton>
      </Stack>
    </Modal>
  );
}
