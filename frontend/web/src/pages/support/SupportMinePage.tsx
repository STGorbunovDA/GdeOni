import { useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Pagination,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, MessagesSquare, Plus } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { supportApi, type SupportTicket } from '../../api/endpoints/supportApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';
import {
  KIND_LABELS,
  SEVERITY_COLORS,
  SEVERITY_LABELS,
  STATUS_COLORS,
  STATUS_LABELS,
} from './supportLabels';

/**
 * F17.14. Список моих обращений. Сортировка на бэке — самые свежие
 * сверху. Пагинация 20 на страницу (page-size selector — backlog).
 */
export function SupportMinePage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const query = useQuery({
    queryKey: ['support-mine', page, pageSize],
    queryFn: () => supportApi.listMine(page, pageSize),
    placeholderData: (prev) => prev,
  });

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / pageSize))
    : 1;

  return (
    <Stack gap="lg">
      <Group justify="space-between" wrap="wrap">
        <GhostButton
          leftSection={<ChevronLeft size={16} />}
          onClick={() => navigate('/profile')}
        >
          Назад
        </GhostButton>
        <PrimaryButton
          leftSection={<Plus size={16} />}
          onClick={() => navigate('/support/new')}
        >
          Новое обращение
        </PrimaryButton>
      </Group>

      <Stack gap="xs">
        <TitleLabel>Мои обращения</TitleLabel>
        <CaptionLabel>
          История ваших переписок с администраторами.
        </CaptionLabel>
      </Stack>

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.data &&
        (query.data.items.length === 0 ? (
          <CloudCard>
            <Stack gap="xs" align="center" py="md">
              <MessagesSquare size={32} color={cloudColors.azureDeep} />
              <BodyLabel>Вы пока ни разу не писали в поддержку.</BodyLabel>
            </Stack>
          </CloudCard>
        ) : (
          <Stack gap="sm">
            {query.data.items.map((item) => (
              <TicketCard
                key={item.id}
                item={item}
                onClick={() => navigate(`/support/${item.id}`)}
              />
            ))}
          </Stack>
        ))}

      {query.data && totalPages > 1 && (
        <Group justify="center">
          <Pagination
            total={totalPages}
            value={page}
            onChange={setPage}
            color="azure"
            size="sm"
          />
        </Group>
      )}
    </Stack>
  );
}

function TicketCard({
  item,
  onClick,
}: {
  item: SupportTicket;
  onClick: () => void;
}) {
  return (
    <UnstyledButton
      onClick={onClick}
      style={{ display: 'block', width: '100%', textAlign: 'left' }}
    >
      <CloudCard
        style={{
          cursor: 'pointer',
          // Urgent-обводку снимаем на Resolved — вопрос уже закрыт.
          borderColor:
            item.severity === 'Urgent' && item.status !== 'Resolved'
              ? cloudColors.errorRed
              : undefined,
        }}
      >
        <Stack gap="xs">
        <Group justify="space-between" wrap="wrap">
          <Group gap="xs" wrap="wrap">
            <Badge color={STATUS_COLORS[item.status]} variant="light">
              {STATUS_LABELS[item.status]}
            </Badge>
            <Badge color={SEVERITY_COLORS[item.severity]} variant="light">
              {SEVERITY_LABELS[item.severity]}
            </Badge>
            <Badge color="gray" variant="light">
              {KIND_LABELS[item.kind]}
            </Badge>
            {item.acceptedByUser && (
              <Badge color="green" variant="filled">
                ✓ Решено
              </Badge>
            )}
            {item.reopenedCount > 0 && (
              <Badge color="red" variant="filled">
                Переоткрыто {item.reopenedCount}
              </Badge>
            )}
          </Group>
          <CaptionLabel>{formatDateTime(item.createdAtUtc)}</CaptionLabel>
        </Group>
        <SubTitleLabel>{item.title}</SubTitleLabel>
        </Stack>
      </CloudCard>
    </UnstyledButton>
  );
}
