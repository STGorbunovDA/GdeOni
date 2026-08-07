import { useState } from 'react';
import {
  ActionIcon,
  Button,
  Divider,
  Group,
  Indicator,
  Popover,
  ScrollArea,
  Stack,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import { Bell } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  notificationsApi,
  type NotificationItem,
} from '../../api/endpoints/notificationsApi';
import { cloudColors } from '../../design/theme';

/**
 * F40. «Колокольчик» уведомлений в шапке. Счётчик непрочитанных опрашиваем
 * раз в минуту (без вебсокетов). Список тянем только когда открыли выпадашку.
 * Клик по уведомлению помечает прочитанным и ведёт по link (если он есть).
 * Показывается всем авторизованным: пользователю приходят ответы/решения
 * админа, админам — новые обращения/жалобы.
 */
function formatWhen(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function NotificationBell() {
  const [opened, setOpened] = useState(false);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const unread = useQuery({
    queryKey: ['notifications-unread'],
    queryFn: notificationsApi.unreadCount,
    // Без вебсокетов опрашиваем непрочитанные раз в минуту + на фокус вкладки.
    refetchInterval: 60_000,
    refetchOnWindowFocus: true,
  });

  const list = useQuery({
    queryKey: ['notifications-list'],
    queryFn: () => notificationsApi.list(20),
    enabled: opened,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['notifications-unread'] });
    queryClient.invalidateQueries({ queryKey: ['notifications-list'] });
  };

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: invalidate,
  });

  const markAll = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: invalidate,
  });

  const count = unread.data ?? 0;

  const handleClick = (n: NotificationItem) => {
    if (!n.isRead) markRead.mutate(n.id);
    setOpened(false);
    if (n.link) navigate(n.link);
  };

  const items = list.data ?? [];

  return (
    <Popover
      opened={opened}
      onChange={setOpened}
      position="bottom-end"
      width={340}
      shadow="md"
      withArrow
      trapFocus={false}
    >
      <Popover.Target>
        <Indicator
          disabled={count === 0}
          label={count > 99 ? '99+' : count}
          size={16}
          offset={5}
          color="red"
          withBorder
        >
          <Tooltip label="Уведомления" withArrow>
            <ActionIcon
              variant="subtle"
              color="gray"
              size="lg"
              aria-label="Уведомления"
              onClick={() => setOpened((o) => !o)}
            >
              <Bell size={18} color={cloudColors.azureDeep} />
            </ActionIcon>
          </Tooltip>
        </Indicator>
      </Popover.Target>

      <Popover.Dropdown p={0}>
        <Group justify="space-between" px="sm" py="xs" wrap="nowrap">
          <Text fw={700} c={cloudColors.inkBlue}>
            Уведомления
          </Text>
          {count > 0 && (
            <Button
              variant="subtle"
              size="compact-xs"
              onClick={() => markAll.mutate()}
              loading={markAll.isPending}
            >
              Прочитать все
            </Button>
          )}
        </Group>
        <Divider />

        <ScrollArea.Autosize mah={360}>
          {list.isLoading ? (
            <Text c={cloudColors.captionGray} size="sm" p="md">
              Загрузка…
            </Text>
          ) : items.length === 0 ? (
            <Text c={cloudColors.captionGray} size="sm" p="md">
              Пока нет уведомлений
            </Text>
          ) : (
            <Stack gap={0}>
              {items.map((n) => (
                <UnstyledButton
                  key={n.id}
                  onClick={() => handleClick(n)}
                  p="sm"
                  style={{
                    background: n.isRead ? undefined : cloudColors.sky,
                    borderBottom: `1px solid ${cloudColors.cloudBorder}`,
                  }}
                >
                  <Text
                    fw={n.isRead ? 500 : 700}
                    size="sm"
                    c={cloudColors.inkBlue}
                  >
                    {n.title}
                  </Text>
                  {n.body && (
                    <Text size="xs" c={cloudColors.text} lineClamp={2}>
                      {n.body}
                    </Text>
                  )}
                  <Text size="xs" c={cloudColors.captionGray} mt={2}>
                    {formatWhen(n.createdAtUtc)}
                  </Text>
                </UnstyledButton>
              ))}
            </Stack>
          )}
        </ScrollArea.Autosize>
      </Popover.Dropdown>
    </Popover>
  );
}
