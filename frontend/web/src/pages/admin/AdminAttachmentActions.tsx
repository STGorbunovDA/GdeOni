import { useState } from 'react';
import { ActionIcon, Menu, Modal, Stack, Group, Button } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { MoreVertical, Star, GalleryHorizontal, MapPinned, FileText, ExternalLink } from 'lucide-react';
import { BodyLabel } from '../../components/ui';
import {
  supportApi,
  type MediaKind,
  type SupportTicketAttachment,
} from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';

/**
 * F35 / D35. Меню админских действий на одном вложении тикета.
 *
 * A. Copy attachment в media умершего. Если у тикета есть deceasedRefId
 *    (parsed из Description маркером «ID карточки: {guid}», F34), в меню
 *    появляются варианты в зависимости от типа файла:
 *      image/*        → «Сделать главным фото» + «В галерею» + «Как фото могилы»;
 *      application/pdf → «Как документ умершего».
 *    Без deceasedRefId — доступно только «Открыть» (та же ссылка что
 *    и по клику на превью).
 *
 * B. Inline PDF preview — на веб решается window.open('_blank'): Chrome
 *    PDF Viewer и Firefox PDF.js отрисуют PDF прямо во вкладке. В отличие
 *    от Android WebView, отдельного viewer'а не нужно.
 *
 * Backend copy — server-side MinIO CopyObject; клиенту файл не качается,
 * вложение в тикете остаётся (дублирование для истории).
 */

type PendingAction = {
  mediaKind: MediaKind;
  makeMain: boolean;
  label: string;
};

export function AdminAttachmentActions({
  ticketId,
  attachment,
  deceasedRefId,
}: {
  ticketId: string;
  attachment: SupportTicketAttachment;
  /** F34: guid карточки, найденный в Description тикета. Null → скрываем copy-опции. */
  deceasedRefId: string | null;
}) {
  const queryClient = useQueryClient();
  const [pending, setPending] = useState<PendingAction | null>(null);

  const isImage = attachment.contentType.startsWith('image/');
  const isPdf = attachment.contentType === 'application/pdf';

  const copyMutation = useMutation({
    mutationFn: async (action: PendingAction) => {
      if (!deceasedRefId) return;
      return supportApi.adminCopyAttachmentToDeceased({
        ticketId,
        attachmentId: attachment.id,
        deceasedId: deceasedRefId,
        mediaKind: action.mediaKind,
        makeMain: action.makeMain,
      });
    },
    onSuccess: (_, action) => {
      queryClient.invalidateQueries({ queryKey: ['admin-deceased-details', deceasedRefId] });
      notifications.show({
        color: 'green',
        title: 'Готово',
        message: `${attachment.originalFileName} → ${action.label}.`,
      });
      setPending(null);
    },
    onError: (e) => {
      notifications.show({
        color: 'red',
        title: 'Не удалось скопировать',
        message: formatError(e),
      });
    },
  });

  async function openInNewTab() {
    try {
      const resp = await supportApi.getAttachment(ticketId, attachment.id);
      window.open(resp.presignedUrl, '_blank', 'noopener,noreferrer');
    } catch (e) {
      notifications.show({
        color: 'red',
        title: 'Не удалось открыть файл',
        message: formatError(e),
      });
    }
  }

  return (
    <>
      <Menu shadow="md" width={240} position="bottom-end" withArrow>
        <Menu.Target>
          <ActionIcon
            variant="subtle"
            color="gray"
            size="sm"
            aria-label="Действия с вложением"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreVertical size={14} />
          </ActionIcon>
        </Menu.Target>
        <Menu.Dropdown>
          <Menu.Item
            leftSection={<ExternalLink size={14} />}
            onClick={(e) => {
              e.stopPropagation();
              openInNewTab();
            }}
          >
            Открыть
          </Menu.Item>

          {deceasedRefId && isImage && (
            <>
              <Menu.Divider />
              <Menu.Label>Скопировать в карточку умершего</Menu.Label>
              <Menu.Item
                leftSection={<Star size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  setPending({
                    mediaKind: 'DeceasedPhoto',
                    makeMain: true,
                    label: 'главное фото умершего',
                  });
                }}
              >
                Сделать главным фото
              </Menu.Item>
              <Menu.Item
                leftSection={<GalleryHorizontal size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  setPending({
                    mediaKind: 'DeceasedPhoto',
                    makeMain: false,
                    label: 'галерея умершего',
                  });
                }}
              >
                Добавить в галерею
              </Menu.Item>
              <Menu.Item
                leftSection={<MapPinned size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  setPending({
                    mediaKind: 'GravePhoto',
                    makeMain: false,
                    label: 'фото могилы',
                  });
                }}
              >
                Как фото могилы
              </Menu.Item>
            </>
          )}

          {deceasedRefId && isPdf && (
            <>
              <Menu.Divider />
              <Menu.Label>Скопировать в карточку умершего</Menu.Label>
              <Menu.Item
                leftSection={<FileText size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  setPending({
                    mediaKind: 'Document',
                    makeMain: false,
                    label: 'документ умершего',
                  });
                }}
              >
                Как документ умершего
              </Menu.Item>
            </>
          )}
        </Menu.Dropdown>
      </Menu>

      <Modal
        opened={pending !== null}
        onClose={() => !copyMutation.isPending && setPending(null)}
        title="Скопировать в карточку умершего?"
        centered
        size="md"
      >
        <Stack gap="md">
          <BodyLabel>
            «{attachment.originalFileName}» будет скопирован{' '}
            <b>{pending?.label}</b>. Вложение в тикете останется — для истории.
          </BodyLabel>
          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setPending(null)}
              disabled={copyMutation.isPending}
            >
              Отмена
            </Button>
            <Button
              color="azure"
              onClick={() => pending && copyMutation.mutate(pending)}
              loading={copyMutation.isPending}
            >
              Скопировать
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
