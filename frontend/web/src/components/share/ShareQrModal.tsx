import { useState } from 'react';
import { Button, Group, Modal, Stack, TextInput } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { QRCodeSVG } from 'qrcode.react';
import { Copy, Share2 } from 'lucide-react';
import { BodyLabel, CaptionLabel } from '../ui';
import { cloudColors } from '../../design/theme';

/**
 * D46. Модалка «Поделиться подборкой»: QR + ссылка + копировать/поделиться.
 *
 * Ссылку/QR получатель открывает у себя, входит и добавляет карточки в
 * отслеживание. Ссылка действует 24 часа (срок задаётся на бэке).
 */
export function ShareQrModal({
  url,
  count,
  onClose,
}: {
  url: string | null;
  count: number;
  onClose: () => void;
}) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      notifications.show({
        title: 'Не удалось скопировать',
        message: 'Скопируйте ссылку вручную из поля выше.',
        color: 'red',
      });
    }
  }

  // Web Share API есть не везде (в основном мобильные браузеры) — кнопку
  // показываем только когда поддерживается.
  const canNativeShare =
    typeof navigator !== 'undefined' && typeof navigator.share === 'function';

  async function handleNativeShare() {
    if (!url) return;
    try {
      await navigator.share({
        title: 'Карточки в «Где Они»',
        text: 'Я поделился с вами карточками для отслеживания',
        url,
      });
    } catch {
      // Пользователь отменил шэр — молча игнорируем.
    }
  }

  return (
    <Modal
      opened={url !== null}
      onClose={onClose}
      title="Поделиться карточками"
      centered
      size="sm"
    >
      <Stack gap="md" align="center">
        <BodyLabel ta="center">
          {count === 1
            ? 'Карточка готова к отправке.'
            : `Готово к отправке: ${count} ${cardsWord(count)}.`}
        </BodyLabel>

        {url && (
          <div
            style={{
              background: '#ffffff',
              padding: 12,
              borderRadius: 12,
              border: `1px solid ${cloudColors.cloudBorder}`,
            }}
          >
            {/* QR всегда на белом фоне с тёмным узором — иначе камеры
                плохо считывают. Явные цвета, не зависящие от темы. */}
            <QRCodeSVG value={url} size={200} level="M" fgColor="#1f2933" bgColor="#ffffff" />
          </div>
        )}

        <CaptionLabel ta="center">
          Отсканируйте QR или отправьте ссылку. Получатель войдёт и добавит
          карточки к себе. Ссылка действует 24 часа.
        </CaptionLabel>

        <TextInput value={url ?? ''} readOnly w="100%" onFocus={(e) => e.currentTarget.select()} />

        <Group w="100%" grow>
          <Button
            variant="light"
            color="azure"
            leftSection={<Copy size={16} />}
            onClick={handleCopy}
          >
            {copied ? 'Скопировано' : 'Скопировать ссылку'}
          </Button>
          {canNativeShare && (
            <Button
              variant="light"
              color="azure"
              leftSection={<Share2 size={16} />}
              onClick={handleNativeShare}
            >
              Поделиться
            </Button>
          )}
        </Group>
      </Stack>
    </Modal>
  );
}

function cardsWord(n: number): string {
  const mod100 = n % 100;
  const mod10 = n % 10;
  if (mod100 >= 11 && mod100 <= 14) return 'карточек';
  if (mod10 === 1) return 'карточка';
  if (mod10 >= 2 && mod10 <= 4) return 'карточки';
  return 'карточек';
}
