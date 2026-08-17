import { Stack } from '@mantine/core';
import type { SupportTicketMessage } from '../../api/endpoints/supportApi';
import { BodyLabel, CaptionLabel } from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { formatDateTime } from '../../utils/formatDate';

/**
 * D25.2. Чат-переписка тикета в стиле Telegram web:
 *  - viewerIsAdmin=true: свои (Admin) справа белые, чужие (User) слева жёлтые;
 *  - viewerIsAdmin=false: свои (User) справа белые, чужие (Admin) слева жёлтые.
 *
 * Backend отдаёт messages ASC по CreatedAtUtc — рендерим как есть.
 */
export function MessagesChat({
  messages,
  viewerIsAdmin,
}: {
  messages: SupportTicketMessage[];
  viewerIsAdmin: boolean;
}) {
  if (messages.length === 0) {
    return (
      <CaptionLabel>
        Пока сообщений нет. Опишите проблему в новом обращении или дождитесь
        ответа администратора.
      </CaptionLabel>
    );
  }

  return (
    <Stack gap="xs">
      {messages.map((m) => {
        const isMine =
          viewerIsAdmin ? m.authorKind === 'Admin' : m.authorKind === 'User';
        const authorLabel =
          m.authorKind === 'Admin' ? 'Администратор' : 'Пользователь';
        return (
          <div
            key={m.id}
            style={{
              display: 'flex',
              justifyContent: isMine ? 'flex-end' : 'flex-start',
            }}
          >
            <div
              style={{
                // 85% вместо 60%: на телефоне узкий пузырь рвал каждое слово.
                maxWidth: '85%',
                // Без minWidth flex-элемент не даёт себя сжать, и длинный
                // текст выдавливает пузырь за края карточки.
                minWidth: 0,
                padding: '10px 12px',
                borderRadius: 12,
                background: isMine
                  ? cloudColors.bubbleMine
                  : cloudColors.bubbleOther,
                border: `1px solid ${cloudColors.bubbleBorder}`,
                boxShadow: cloudColors.shadow,
              }}
            >
              {/* overflowWrap: anywhere — иначе «слово» без пробелов
                  (случайный набор символов, длинная ссылка) не переносится
                  и вылезает за плашку. */}
              <BodyLabel
                style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}
              >
                {m.text}
              </BodyLabel>
              <CaptionLabel>
                {authorLabel} · {formatDateTime(m.createdAtUtc)}
              </CaptionLabel>
            </div>
          </div>
        );
      })}
    </Stack>
  );
}
