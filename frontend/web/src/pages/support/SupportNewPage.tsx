import { useEffect, useRef, useState } from 'react';
import {
  ActionIcon,
  Alert,
  Group,
  Image,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useMutation } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { ChevronLeft, FileText, Paperclip, X } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { supportApi, type TicketKind } from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';
import { KIND_OPTIONS } from './supportLabels';

/**
 * F17.14 / F33 / F34. Форма создания обращения.
 *
 * F33 добавил блок «Файлы» — до 5 вложений (JPEG/PNG ≤10 MB, PDF ≤25 MB,
 * суммарно ≤50 MB). Без вложений идём в POST /api/support-tickets
 * (JSON), с вложениями — POST /with-attachments (multipart).
 *
 * F34 добавил pre-fill'ы из query: если пришли deceasedId/name/period,
 * подставляем шаблон в Title/Description с маркером «ID карточки: {guid}»
 * — админ по нему сможет открыть связанную карточку одним кликом.
 */

// D33: лимиты, те же что и на бэке (SupportTicket.MaxAttachments и т.д.).
const MAX_ATTACHMENTS = 5;
const MAX_PHOTO_BYTES = 10 * 1024 * 1024;
const MAX_PDF_BYTES = 25 * 1024 * 1024;
const MAX_TOTAL_BYTES = 50 * 1024 * 1024;
const ACCEPTED_MIME = ['image/jpeg', 'image/png', 'application/pdf'];

// D34: тот же маркер, что использует SupportDeceasedRefParser на mobile.
// Регэксп для extraction'а — в src/pages/support/deceasedRef.ts.
const DECEASED_ID_MARKER = 'ID карточки:';

export function SupportNewPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [kind, setKind] = useState<TicketKind>('Question');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [files, setFiles] = useState<File[]>([]);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);
  const templateAppliedRef = useRef(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // F34: при первом рендере, если пришёл ?deceasedId=..., заполняем
  // шаблон один раз (второй раз при перерендере не перезаписываем —
  // юзер уже мог что-то дописать).
  useEffect(() => {
    if (templateAppliedRef.current) return;
    const deceasedId = searchParams.get('deceasedId');
    const deceasedFullName = searchParams.get('deceasedFullName');
    const deceasedLifePeriod = searchParams.get('deceasedLifePeriod');
    if (!deceasedId || !deceasedFullName) return;

    const period = deceasedLifePeriod?.trim()
      ? `\nЖизнь: ${deceasedLifePeriod}`
      : '';
    setTitle(`По карточке: ${deceasedFullName}`);
    setDescription(
      `Карточка умершего: ${deceasedFullName}${period}\n` +
        `${DECEASED_ID_MARKER} ${deceasedId}\n` +
        '\n---\n\n' +
        'Опишите проблему ниже:\n',
    );
    templateAppliedRef.current = true;
  }, [searchParams]);

  const mutation = useMutation({
    mutationFn: async () => {
      const kindTitleDesc = {
        kind,
        title: title.trim(),
        description: description.trim(),
      };
      if (files.length === 0) {
        return supportApi.create(kindTitleDesc);
      }
      const resp = await supportApi.createWithAttachments({
        ...kindTitleDesc,
        files,
      });
      return { ticketId: resp.ticketId };
    },
    onSuccess: () => {
      notifications.show({
        color: 'green',
        title: 'Обращение отправлено',
        message: 'Мы уже увидели ваше сообщение и вернёмся с ответом.',
      });
      navigate('/support/mine', { replace: true });
    },
  });

  const totalBytes = files.reduce((sum, f) => sum + f.size, 0);
  const canPickMore = files.length < MAX_ATTACHMENTS && !mutation.isPending;

  const canSubmit =
    title.trim().length > 0 &&
    title.trim().length <= 200 &&
    description.trim().length > 0 &&
    description.trim().length <= 4000 &&
    !mutation.isPending;

  function openFilePicker() {
    setAttachmentError(null);
    fileInputRef.current?.click();
  }

  function onFilesPicked(e: React.ChangeEvent<HTMLInputElement>) {
    const picked = Array.from(e.target.files ?? []);
    // Сбрасываем input, чтобы можно было выбрать тот же файл повторно
    // после удаления (input.change не срабатывает на same value).
    e.target.value = '';
    if (picked.length === 0) return;

    let nextFiles = [...files];
    let firstError: string | null = null;

    for (const f of picked) {
      if (nextFiles.length >= MAX_ATTACHMENTS) {
        firstError = firstError ?? `Максимум ${MAX_ATTACHMENTS} файлов.`;
        break;
      }
      if (!ACCEPTED_MIME.includes(f.type)) {
        firstError = firstError ?? 'Только JPEG, PNG или PDF.';
        continue;
      }
      const perFileLimit = f.type === 'application/pdf' ? MAX_PDF_BYTES : MAX_PHOTO_BYTES;
      if (f.size > perFileLimit) {
        firstError = firstError ??
          (f.type === 'application/pdf'
            ? 'PDF слишком большой (макс. 25 МБ).'
            : 'Фото слишком большое (макс. 10 МБ).');
        continue;
      }
      const totalAfter =
        nextFiles.reduce((s, x) => s + x.size, 0) + f.size;
      if (totalAfter > MAX_TOTAL_BYTES) {
        firstError = firstError ?? 'Суммарный размер вложений > 50 МБ.';
        continue;
      }
      nextFiles = [...nextFiles, f];
    }

    setFiles(nextFiles);
    if (firstError) setAttachmentError(firstError);
  }

  function removeFile(index: number) {
    setFiles((prev) => prev.filter((_, i) => i !== index));
    setAttachmentError(null);
  }

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/profile')}
        >
          Назад
        </GhostButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>Обращение в поддержку</TitleLabel>
        <CaptionLabel>
          Опишите проблему как можно подробнее — что вы делали, что
          получили. Можно приложить до {MAX_ATTACHMENTS} фото или PDF.
        </CaptionLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          <Select
            label="Тип"
            data={KIND_OPTIONS}
            value={kind}
            onChange={(v) => setKind((v as TicketKind) ?? 'Question')}
            allowDeselect={false}
          />
          <TextInput
            label="Заголовок"
            placeholder="Кратко о проблеме"
            value={title}
            onChange={(e) => setTitle(e.currentTarget.value)}
            maxLength={200}
            error={
              title.length > 200
                ? `Максимум 200 символов, сейчас ${title.length}`
                : undefined
            }
          />
          <Textarea
            label="Описание"
            placeholder="Что произошло, что вы делали, что ожидали"
            value={description}
            onChange={(e) => setDescription(e.currentTarget.value)}
            autosize
            minRows={5}
            maxRows={15}
            maxLength={4000}
            error={
              description.length > 4000
                ? `Максимум 4000 символов, сейчас ${description.length}`
                : undefined
            }
          />

          {/* F33. Файлы. */}
          <Stack gap="xs">
            <Group justify="space-between" align="center">
              <BodyLabel style={{ fontWeight: 600 }}>Файлы</BodyLabel>
              <CaptionLabel>
                {files.length} / {MAX_ATTACHMENTS}, {formatMB(totalBytes)} /{' '}
                {formatMB(MAX_TOTAL_BYTES)}
              </CaptionLabel>
            </Group>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,application/pdf"
              multiple
              style={{ display: 'none' }}
              onChange={onFilesPicked}
            />
            <GhostButton
              leftSection={<Paperclip size={16} />}
              onClick={openFilePicker}
              disabled={!canPickMore}
            >
              Прикрепить файл
            </GhostButton>
            {attachmentError && (
              <Alert color="red" variant="light">
                {attachmentError}
              </Alert>
            )}
            {files.length > 0 && (
              <SimpleGrid cols={{ base: 2, sm: 3, md: 4 }} spacing="sm">
                {files.map((f, i) => (
                  <AttachmentThumb
                    key={`${f.name}-${i}`}
                    file={f}
                    onRemove={() => removeFile(i)}
                  />
                ))}
              </SimpleGrid>
            )}
          </Stack>

          <Group justify="space-between" align="center">
            <CaptionLabel>{description.length} / 4000</CaptionLabel>
            <PrimaryButton
              onClick={() => mutation.mutate()}
              disabled={!canSubmit}
              loading={mutation.isPending}
            >
              Отправить
            </PrimaryButton>
          </Group>
          {mutation.isError && (
            <Alert color="red" variant="light">
              {formatError(mutation.error)}
            </Alert>
          )}
        </Stack>
      </CloudCard>

      <CloudCard>
        <Stack gap="xs">
          <CaptionLabel>Уже писали?</CaptionLabel>
          <BodyLabel>
            <a
              href="/support/mine"
              onClick={(e) => {
                e.preventDefault();
                navigate('/support/mine');
              }}
            >
              Открыть мои обращения
            </a>
          </BodyLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}

/**
 * Превью одного вложения: для image — реальный thumbnail через
 * URL.createObjectURL, для PDF — иконка. Кнопка ✕ — удалить.
 */
function AttachmentThumb({
  file,
  onRemove,
}: {
  file: File;
  onRemove: () => void;
}) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!file.type.startsWith('image/')) return;
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    // createObjectURL создаёт запись, которую надо освободить —
    // иначе браузер держит File в памяти до closing tab.
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const sizeLabel = formatMB(file.size);

  return (
    <div
      style={{
        position: 'relative',
        border: `1px solid ${cloudColors.cloudBorder}`,
        borderRadius: 8,
        overflow: 'hidden',
        aspectRatio: '1 / 1',
        background: cloudColors.sunken,
      }}
    >
      {previewUrl ? (
        <Image
          src={previewUrl}
          alt={file.name}
          fit="cover"
          style={{ width: '100%', height: '100%' }}
        />
      ) : (
        <Stack align="center" justify="center" style={{ height: '100%' }} gap={4}>
          <FileText size={36} color="#3F8AB8" />
          <Text size="xs" ta="center" px="xs" lineClamp={2}>
            {file.name}
          </Text>
        </Stack>
      )}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          padding: '2px 6px',
          background: 'rgba(30, 58, 95, 0.65)',
          color: '#fff',
          fontSize: 10,
          display: 'flex',
          justifyContent: 'space-between',
        }}
      >
        <span
          style={{
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
            maxWidth: '70%',
          }}
        >
          {file.name}
        </span>
        <span>{sizeLabel}</span>
      </div>
      <ActionIcon
        variant="filled"
        color="red"
        radius="xl"
        size="sm"
        onClick={onRemove}
        style={{
          position: 'absolute',
          top: 4,
          right: 4,
        }}
        aria-label="Удалить файл"
      >
        <X size={14} />
      </ActionIcon>
    </div>
  );
}

function formatMB(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  if (mb >= 0.1) return `${mb.toFixed(1)} МБ`;
  return `${Math.round(bytes / 1024)} КБ`;
}
