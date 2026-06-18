import { useRef, useState } from 'react';
import {
  Alert,
  Button,
  FileButton,
  Group,
  Menu,
  Modal,
  Progress,
  Stack,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  FileText,
  MoreVertical,
  Star,
  Trash2,
  Upload,
  X,
} from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  SubTitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  MediaKinds,
  MEDIA_LIMITS,
  mediaApi,
  type MediaListItem,
  type MediaKindValue,
} from '../../api/endpoints/mediaApi';
import { useIsAdmin } from '../../auth/authStore';
import { useAppFeatures } from '../../hooks/useAppFeatures';
import { buildMediaUrl } from '../../utils/mediaUrl';
import { formatError } from '../../auth/errorMessages';

/**
 * F13. Три галереи на карточке умершего (Фото умершего / Фото могилы /
 * Документы). Зеркало DeceasedDetailsPage media-блоков на mobile.
 *
 *  - Чтение всем авторизованным.
 *  - Upload / Delete / Set-main — admin-only (D26 на бэке). Кнопки
 *    скрыты для не-админа (UI не показывает заведомо невозможное).
 *  - Тап на фото → fullscreen viewer (modal). Документ → новая вкладка
 *    (presigned URL открывается напрямую в браузерном PDF-viewer'е).
 *  - Главное фото отмечено синим кружком со ★ в правом верхнем углу.
 *  - Auto-promote: если админ загрузил первое DeceasedPhoto и main
 *    не было — автоматически назначаем main (иначе карточка не
 *    получит превью в поиске, см. план F13).
 */
export function MediaSection({ deceasedId }: { deceasedId: string }) {
  return (
    <Stack gap="lg">
      <Gallery
        deceasedId={deceasedId}
        kind={MediaKinds.DeceasedPhoto}
        title="Фото умершего"
        emptyText="Фото пока не загружены."
      />
      <Gallery
        deceasedId={deceasedId}
        kind={MediaKinds.GravePhoto}
        title="Фото могилы"
        emptyText="Фото могилы пока не загружены."
      />
      <Gallery
        deceasedId={deceasedId}
        kind={MediaKinds.Document}
        title="Документы"
        emptyText="Документы пока не загружены."
      />
    </Stack>
  );
}

function Gallery({
  deceasedId,
  kind,
  title,
  emptyText,
}: {
  deceasedId: string;
  kind: MediaKindValue;
  title: string;
  emptyText: string;
}) {
  const queryClient = useQueryClient();
  const isAdmin = useIsAdmin();
  const features = useAppFeatures();
  const [viewing, setViewing] = useState<MediaListItem | null>(null);
  const [pendingDelete, setPendingDelete] = useState<MediaListItem | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState<number | null>(null);

  const query = useQuery({
    queryKey: ['media', deceasedId, kind],
    queryFn: () => mediaApi.list(deceasedId, kind),
  });

  const items = query.data?.items ?? [];

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      setUploadError(null);
      setUploadProgress(0);
      const result = await mediaApi.upload(deceasedId, file, kind, null, (p) =>
        setUploadProgress(Math.round((p.loaded / p.total) * 100)),
      );
      return result;
    },
    onSuccess: async (resp) => {
      setUploadProgress(null);
      // Auto-promote: первое DeceasedPhoto и main ещё нет → назначаем главным.
      if (
        kind === MediaKinds.DeceasedPhoto &&
        !items.some((m) => m.isMainPhoto)
      ) {
        try {
          await mediaApi.setMain(deceasedId, resp.mediaId);
        } catch {
          /* не блокируем основной flow если auto-promote упал */
        }
      }
      queryClient.invalidateQueries({ queryKey: ['media', deceasedId, kind] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', deceasedId] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
    onError: (err) => {
      setUploadProgress(null);
      setUploadError(formatError(err));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (mediaId: string) => mediaApi.remove(deceasedId, mediaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['media', deceasedId, kind] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', deceasedId] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
      setPendingDelete(null);
    },
  });

  const setMainMutation = useMutation({
    mutationFn: (mediaId: string) => mediaApi.setMain(deceasedId, mediaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['media', deceasedId, kind] });
      queryClient.invalidateQueries({ queryKey: ['tracked-details', deceasedId] });
      queryClient.invalidateQueries({ queryKey: ['tracked-list'] });
    },
  });

  /**
   * F13. Документ открывается через прокси-эндпоинт, потому что
   * presigned URL из MinIO содержит host 10.0.2.2 (dev) — браузер
   * на Windows его не разрешает. Бэк сам тянет файл, клиент получает
   * blob и открывает через blob: URL.
   *
   * Открываем через программный клик по `<a target="_blank">`, а не
   * window.open: с флагом `noopener` window.open возвращает null
   * даже при успешном открытии (специфика спеки), а без него mutation
   * считала бы успех ошибкой. User-initiated `<a>`-клик popup-blocker
   * не режет, так что отдельной обработки блокировки тоже не нужно.
   *
   * Blob URL не revoke'аем — браузер сам соберёт его при закрытии
   * вкладки. Premature revoke ломает загрузку PDF в открытой вкладке.
   */
  const documentOpenMutation = useMutation({
    mutationFn: async (mediaId: string) => {
      const blob = await mediaApi.downloadBlob(deceasedId, mediaId);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.target = '_blank';
      link.rel = 'noopener';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    },
  });

  function validateFile(file: File): string | null {
    const isPhoto =
      kind === MediaKinds.DeceasedPhoto || kind === MediaKinds.GravePhoto;
    if (isPhoto) {
      if (!MEDIA_LIMITS.allowedPhotoTypes.includes(file.type)) {
        return 'Разрешены только JPG, PNG и WebP.';
      }
      if (file.size > MEDIA_LIMITS.maxPhotoSizeBytes) {
        return 'Фото больше 10 МБ.';
      }
    } else {
      if (!MEDIA_LIMITS.allowedDocumentTypes.includes(file.type)) {
        return 'Разрешены только PDF.';
      }
      if (file.size > MEDIA_LIMITS.maxDocumentSizeBytes) {
        return 'Документ больше 25 МБ.';
      }
    }
    return null;
  }

  function handleFile(file: File | null) {
    if (!file) return;
    const validation = validateFile(file);
    if (validation) {
      setUploadError(validation);
      return;
    }
    uploadMutation.mutate(file);
  }

  const accept =
    kind === MediaKinds.Document
      ? MEDIA_LIMITS.allowedDocumentTypes.join(',')
      : MEDIA_LIMITS.allowedPhotoTypes.join(',');

  return (
    <CloudCard>
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <SubTitleLabel>{title}</SubTitleLabel>
          {isAdmin && (
            <FileButton onChange={handleFile} accept={accept}>
              {(props) => (
                <GhostButton
                  {...props}
                  leftSection={<Upload size={16} />}
                  loading={uploadMutation.isPending}
                >
                  Загрузить
                </GhostButton>
              )}
            </FileButton>
          )}
        </Group>

        {uploadProgress !== null && (
          <Progress value={uploadProgress} color="azure" striped animated />
        )}

        {uploadError && (
          <Alert color="red" variant="light">
            {uploadError}
          </Alert>
        )}

        {deleteMutation.isError && (
          <Alert color="red" variant="light">
            {formatError(deleteMutation.error)}
          </Alert>
        )}

        {setMainMutation.isError && (
          <Alert color="red" variant="light">
            {formatError(setMainMutation.error)}
          </Alert>
        )}

        {documentOpenMutation.isError && (
          <Alert color="red" variant="light">
            {formatError(documentOpenMutation.error)}
          </Alert>
        )}

        {query.isLoading ? (
          <CaptionLabel>Загружаем…</CaptionLabel>
        ) : items.length === 0 ? (
          <CaptionLabel>{emptyText}</CaptionLabel>
        ) : kind === MediaKinds.Document ? (
          <Stack gap="xs">
            {items.map((item) => (
              <DocumentRow
                key={item.id}
                item={item}
                isAdmin={isAdmin}
                isOpening={
                  documentOpenMutation.isPending &&
                  documentOpenMutation.variables === item.id
                }
                onDelete={() => setPendingDelete(item)}
                onOpen={() => documentOpenMutation.mutate(item.id)}
              />
            ))}
          </Stack>
        ) : (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
              gap: 12,
            }}
          >
            {items.map((item) => (
              <PhotoTile
                key={item.id}
                item={item}
                mediaBaseUrl={features.data?.mediaBaseUrl}
                isAdmin={isAdmin}
                canBeMain={
                  kind === MediaKinds.DeceasedPhoto && !item.isMainPhoto
                }
                onOpen={() => setViewing(item)}
                onSetMain={() => setMainMutation.mutate(item.id)}
                onDelete={() => setPendingDelete(item)}
              />
            ))}
          </div>
        )}

        <FullScreenViewer
          item={viewing}
          mediaBaseUrl={features.data?.mediaBaseUrl}
          onClose={() => setViewing(null)}
        />

        <Modal
          opened={pendingDelete !== null}
          onClose={() =>
            !deleteMutation.isPending && setPendingDelete(null)
          }
          title="Удалить файл?"
          centered
          size="md"
        >
          <Stack gap="md">
            <BodyLabel>
              «{pendingDelete?.originalFileName}» будет удалён без
              возможности восстановления.
            </BodyLabel>
            <Group justify="flex-end" gap="sm">
              <Button
                variant="default"
                onClick={() => setPendingDelete(null)}
                disabled={deleteMutation.isPending}
              >
                Отмена
              </Button>
              <Button
                color="red"
                onClick={() =>
                  pendingDelete && deleteMutation.mutate(pendingDelete.id)
                }
                loading={deleteMutation.isPending}
              >
                Удалить
              </Button>
            </Group>
          </Stack>
        </Modal>
      </Stack>
    </CloudCard>
  );
}

function PhotoTile({
  item,
  mediaBaseUrl,
  isAdmin,
  canBeMain,
  onOpen,
  onSetMain,
  onDelete,
}: {
  item: MediaListItem;
  mediaBaseUrl: string | undefined;
  isAdmin: boolean;
  canBeMain: boolean;
  onOpen: () => void;
  onSetMain: () => void;
  onDelete: () => void;
}) {
  const photoUrl = buildMediaUrl(mediaBaseUrl, item.bucket, item.storageKey);
  const [failed, setFailed] = useState(false);

  return (
    <div
      style={{
        position: 'relative',
        aspectRatio: '1 / 1',
        borderRadius: 12,
        overflow: 'hidden',
        background: cloudColors.sky,
        cursor: 'pointer',
      }}
      onClick={onOpen}
    >
      {photoUrl && !failed ? (
        <img
          src={photoUrl}
          alt={item.originalFileName}
          style={{
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            display: 'block',
          }}
          onError={() => setFailed(true)}
        />
      ) : (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: '100%',
            height: '100%',
            color: cloudColors.azureDeep,
          }}
        >
          <FileText size={32} strokeWidth={1.5} />
        </div>
      )}

      {item.isMainPhoto && (
        <div
          style={{
            position: 'absolute',
            top: 8,
            right: 8,
            width: 28,
            height: 28,
            borderRadius: '50%',
            background: cloudColors.azure,
            color: 'white',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 2px 6px rgba(0,0,0,0.2)',
          }}
        >
          <Star size={16} fill="white" strokeWidth={0} />
        </div>
      )}

      {isAdmin && (
        <div
          style={{ position: 'absolute', bottom: 6, right: 6 }}
          onClick={(e) => e.stopPropagation()}
        >
          <Menu shadow="md" position="top-end" withArrow>
            <Menu.Target>
              <Button
                size="xs"
                variant="white"
                px={6}
                style={{ opacity: 0.92 }}
              >
                <MoreVertical size={16} />
              </Button>
            </Menu.Target>
            <Menu.Dropdown>
              {canBeMain && (
                <Menu.Item
                  leftSection={<Star size={14} />}
                  onClick={onSetMain}
                >
                  Сделать главным
                </Menu.Item>
              )}
              <Menu.Item
                color="red"
                leftSection={<Trash2 size={14} />}
                onClick={onDelete}
              >
                Удалить
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </div>
      )}
    </div>
  );
}

function DocumentRow({
  item,
  isAdmin,
  isOpening,
  onDelete,
  onOpen,
}: {
  item: MediaListItem;
  isAdmin: boolean;
  isOpening: boolean;
  onDelete: () => void;
  onOpen: () => void;
}) {
  return (
    <Group
      justify="space-between"
      align="center"
      style={{
        padding: '10px 12px',
        borderRadius: 12,
        border: `1px solid ${cloudColors.cloudBorder}`,
        background: '#FAFCFE',
        cursor: isOpening ? 'wait' : 'pointer',
        opacity: isOpening ? 0.7 : 1,
      }}
      onClick={() => !isOpening && onOpen()}
      wrap="nowrap"
    >
      <Group gap="sm" align="center" wrap="nowrap" style={{ minWidth: 0 }}>
        <FileText size={20} color={cloudColors.azureDeep} />
        <BodyLabel style={{ wordBreak: 'break-all' }}>
          {item.originalFileName}
          {isOpening && ' (открываем…)'}
        </BodyLabel>
      </Group>
      {isAdmin && (
        <Button
          variant="subtle"
          color="red"
          size="xs"
          leftSection={<Trash2 size={14} />}
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
        >
          Удалить
        </Button>
      )}
    </Group>
  );
}

/**
 * Полноэкранный просмотр фото в Modal. Зеркало D27.1 mobile
 * FullScreenPhotoPage: тёмный фон, фото AspectFit, тап по фону / ✕
 * закрывает. Для документов модалка не используется — они открываются
 * в новой вкладке браузера (presigned URL).
 */
function FullScreenViewer({
  item,
  mediaBaseUrl,
  onClose,
}: {
  item: MediaListItem | null;
  mediaBaseUrl: string | undefined;
  onClose: () => void;
}) {
  const closeRef = useRef(onClose);
  closeRef.current = onClose;
  const photoUrl = item
    ? buildMediaUrl(mediaBaseUrl, item.bucket, item.storageKey)
    : null;

  return (
    <Modal
      opened={item !== null}
      onClose={onClose}
      fullScreen
      withCloseButton={false}
      padding={0}
      styles={{ body: { background: 'black', height: '100vh' } }}
    >
      <div
        onClick={onClose}
        style={{
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          position: 'relative',
        }}
      >
        {photoUrl && (
          <img
            src={photoUrl}
            alt={item?.originalFileName}
            onClick={(e) => e.stopPropagation()}
            style={{
              maxWidth: '95vw',
              maxHeight: '95vh',
              objectFit: 'contain',
            }}
          />
        )}
        <Button
          onClick={onClose}
          variant="filled"
          color="dark"
          radius="xl"
          size="md"
          style={{
            position: 'absolute',
            top: 24,
            right: 24,
            background: 'rgba(0,0,0,0.6)',
          }}
        >
          <X size={20} />
        </Button>
      </div>
    </Modal>
  );
}

