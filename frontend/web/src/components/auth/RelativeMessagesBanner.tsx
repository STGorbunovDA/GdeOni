import { Alert, Button, Group, Text } from '@mantine/core';
import { useLocation, useNavigate } from 'react-router-dom';
import { MessagesSquare } from 'lucide-react';
import { useRelativesSummary } from '../../hooks/useRelativesSummary';
import { cloudColors } from '../../design/theme';

/**
 * Баннер «Новое сообщение от родственника» — по образцу баннеров
 * «Подтвердите email» / «Укажите город». Висит вверху, пока есть непрочитанные
 * сообщения во внутренней переписке «Родственники». Кнопка «Прочесть» ведёт
 * сразу в диалог (если он один) или на вкладку «Родственники».
 *
 * Данные — из общего useRelativesSummary (['relatives-summary']), без лишнего
 * запроса. На страницах «Родственники»/чата не показываемся, чтобы не дублить.
 */
export function RelativeMessagesBanner() {
  const navigate = useNavigate();
  const location = useLocation();
  const summary = useRelativesSummary();

  const unread = summary.data?.unreadConversations ?? [];
  if (unread.length === 0) return null;
  // На самой вкладке/в чате баннер не нужен.
  if (location.pathname.startsWith('/relatives')) return null;

  const total = summary.data?.totalUnreadMessages ?? unread.length;
  const single = unread.length === 1 ? unread[0] : null;

  const text = single
    ? `${single.otherUserName} написал вам по «${single.deceasedFullName}».`
    : `Новые сообщения от родственников: ${total} (в ${unread.length} диалогах).`;

  return (
    <Alert
      variant="light"
      color="azure"
      icon={<MessagesSquare size={20} />}
      mb="md"
      title="Новое сообщение от родственника"
      styles={{ title: { color: cloudColors.inkBlue } }}
    >
      <Group justify="space-between" align="center" wrap="wrap" gap="sm">
        <Text size="sm" c={cloudColors.text}>
          {text}
        </Text>
        <Button
          variant="light"
          color="azure"
          size="xs"
          radius="xl"
          onClick={() =>
            navigate(single ? `/relatives/chat/${single.conversationId}` : '/relatives')
          }
        >
          Прочесть
        </Button>
      </Group>
    </Alert>
  );
}
