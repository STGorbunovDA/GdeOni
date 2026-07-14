import { useState } from 'react';
import { Alert, Group, Image, SimpleGrid, Stack, Text } from '@mantine/core';
import { FileText, Download } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  GhostButton,
  SubTitleLabel,
} from '../../components/ui';
import {
  supportApi,
  type SupportTicketAttachment,
} from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';

/**
 * F33 / F35. Секция «Вложения» на карточке тикета — общая для юзера
 * и админа. У юзера/дефолтного просмотра — миниатюра + клик на файл
 * (open image / open PDF в новой вкладке / скачать). У админа поверх
 * этого — action menu (F35), которое рендерится через слот
 * renderExtraActions.
 *
 * Presigned URL на бэке живёт 1 час — не кешируем, каждый клик =
 * свежий запрос. Не критично: небольшая ручка, TTL защищает от
 * протухания в закладках.
 */
export function AttachmentsSection({
  ticketId,
  attachments,
  renderExtraActions,
}: {
  ticketId: string;
  attachments: SupportTicketAttachment[];
  /**
   * Слот справа снизу превью — используется в админке для action
   * меню (F35 «Открыть / Скачать / Скопировать в media умершего»).
   */
  renderExtraActions?: (att: SupportTicketAttachment) => React.ReactNode;
}) {
  if (attachments.length === 0) {
    return (
      <Stack gap="xs">
        <SubTitleLabel>Вложения</SubTitleLabel>
        <BodyLabel c="dimmed">Файлов нет.</BodyLabel>
      </Stack>
    );
  }

  return (
    <Stack gap="sm">
      <SubTitleLabel>Вложения ({attachments.length})</SubTitleLabel>
      <SimpleGrid cols={{ base: 2, sm: 3, md: 4 }} spacing="sm">
        {attachments.map((a) => (
          <AttachmentCard
            key={a.id}
            ticketId={ticketId}
            attachment={a}
            extraActions={renderExtraActions?.(a)}
          />
        ))}
      </SimpleGrid>
    </Stack>
  );
}

/**
 * Одна карточка вложения. По клику на превью:
 *  - image → открывает presigned URL в новой вкладке (браузер
 *    покажет фото fullscreen прямо во вкладке);
 *  - application/pdf → тоже window.open — Chrome/Firefox PDF Viewer
 *    отрисуют inline preview (F35.B — рабочее решение для веба,
 *    в отличие от Android WebView).
 *  - Дополнительно всегда доступна кнопка «Скачать» с download-атрибутом.
 *
 * Presigned URL получаем лениво по клику — GET /attachments/{id} —
 * потом делаем window.open. Загрузка миниатюр для image превью
 * требует того же URL заранее — держим отдельным state per-card.
 */
function AttachmentCard({
  ticketId,
  attachment,
  extraActions,
}: {
  ticketId: string;
  attachment: SupportTicketAttachment;
  extraActions?: React.ReactNode;
}) {
  const isImage = attachment.contentType.startsWith('image/');
  const [thumbUrl, setThumbUrl] = useState<string | null>(null);
  const [thumbLoading, setThumbLoading] = useState(false);
  const [thumbError, setThumbError] = useState<string | null>(null);

  async function loadThumb() {
    if (thumbUrl || thumbLoading) return;
    setThumbLoading(true);
    try {
      const resp = await supportApi.getAttachment(ticketId, attachment.id);
      setThumbUrl(resp.presignedUrl);
    } catch (e) {
      setThumbError(formatError(e));
    } finally {
      setThumbLoading(false);
    }
  }

  // Автозагрузка миниатюры для image — иначе будет серая плитка.
  if (isImage && !thumbUrl && !thumbLoading && !thumbError) {
    loadThumb();
  }

  async function handleOpen() {
    try {
      // Свежий presigned URL — TTL 1 час, не полагаемся на закешированный.
      const resp = await supportApi.getAttachment(ticketId, attachment.id);
      window.open(resp.presignedUrl, '_blank', 'noopener,noreferrer');
    } catch (e) {
      setThumbError(formatError(e));
    }
  }

  async function handleDownload() {
    try {
      const resp = await supportApi.getAttachment(ticketId, attachment.id);
      // download-атрибут на <a> — стандартное браузерное скачивание,
      // не запускает preview. Кросс-доменные presigned URL — вопрос
      // Content-Disposition: attachment, что MinIO делает по умолчанию.
      const a = document.createElement('a');
      a.href = resp.presignedUrl;
      a.download = attachment.originalFileName;
      a.rel = 'noopener noreferrer';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    } catch (e) {
      setThumbError(formatError(e));
    }
  }

  return (
    <Stack gap={4}>
      <div
        style={{
          position: 'relative',
          border: `1px solid ${cloudColors.cloudBorder}`,
          borderRadius: 8,
          overflow: 'hidden',
          aspectRatio: '1 / 1',
          background: cloudColors.sunken,
          cursor: 'pointer',
        }}
        onClick={handleOpen}
        role="button"
        aria-label={`Открыть ${attachment.originalFileName}`}
      >
        {isImage && thumbUrl ? (
          <Image
            src={thumbUrl}
            alt={attachment.originalFileName}
            fit="cover"
            style={{ width: '100%', height: '100%' }}
          />
        ) : (
          <Stack
            align="center"
            justify="center"
            style={{ height: '100%' }}
            gap={4}
          >
            <FileText size={36} color="#3F8AB8" />
            <Text size="xs" ta="center" px="xs" lineClamp={2}>
              {attachment.originalFileName}
            </Text>
          </Stack>
        )}
      </div>
      <Group justify="space-between" gap={4} wrap="nowrap">
        <CaptionLabel>{formatBytes(attachment.sizeBytes)}</CaptionLabel>
        <Group gap={4} wrap="nowrap">
          <GhostButton
            size="compact-xs"
            leftSection={<Download size={12} />}
            onClick={(e) => {
              e.stopPropagation();
              handleDownload();
            }}
          >
            Скачать
          </GhostButton>
          {extraActions}
        </Group>
      </Group>
      {thumbError && (
        <Alert color="red" variant="light" p={6}>
          <Text size="xs">{thumbError}</Text>
        </Alert>
      )}
    </Stack>
  );
}

function formatBytes(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  if (mb >= 0.1) return `${mb.toFixed(1)} МБ`;
  return `${Math.round(bytes / 1024)} КБ`;
}
