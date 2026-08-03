import { useState } from 'react';
import {
  Alert,
  Badge,
  Group,
  Loader,
  Modal,
  Stack,
  Switch,
  Textarea,
} from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { useNavigate } from 'react-router-dom';
import { Ban, Check, Flag } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  adminRelativeReportsApi,
  type AdminRelativeReport,
} from '../../api/endpoints/relativesApi';
import { adminUsersApi } from '../../api/endpoints/adminUsersApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateTime } from '../../utils/formatDate';

/**
 * Функция «Родственники» (Фаза 5). Админская очередь жалоб на родственников.
 * Жаловаться можно на собеседника в переписке; здесь модератор видит пары
 * «кто → на кого», текст жалобы и контекст (карточка), и может заблокировать
 * нарушителя (существующий механизм — блокировка убирает его из всей функции)
 * либо просто закрыть жалобу.
 */
export function AdminRelativeReportsPage() {
  const queryClient = useQueryClient();
  const [pendingOnly, setPendingOnly] = useState(true);

  // Модалка блокировки: причина предзаполнена текстом жалобы.
  const [blockTarget, setBlockTarget] = useState<AdminRelativeReport | null>(null);
  const [blockReason, setBlockReason] = useState('');

  const query = useQuery({
    queryKey: ['admin-relative-reports', pendingOnly],
    queryFn: () => adminRelativeReportsApi.list(pendingOnly),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['admin-relative-reports'] });

  const notifyError = (e: unknown) =>
    notifications.show({ title: 'Ошибка', message: formatError(e), color: 'red' });

  const resolveMutation = useMutation({
    mutationFn: (vars: { id: string; note: string | null }) =>
      adminRelativeReportsApi.resolve(vars.id, vars.note),
    onSuccess: () => {
      invalidate();
      notifications.show({ title: 'Жалоба закрыта', message: '', color: 'green' });
    },
    onError: notifyError,
  });

  // Заблокировать нарушителя и сразу закрыть жалобу.
  const blockMutation = useMutation({
    mutationFn: async (vars: { report: AdminRelativeReport; reason: string }) => {
      await adminUsersApi.block(vars.report.reportedUserId, vars.reason);
      await adminRelativeReportsApi.resolve(
        vars.report.id,
        `Заблокирован: ${vars.reason}`.slice(0, 1000),
      );
    },
    onSuccess: () => {
      setBlockTarget(null);
      setBlockReason('');
      invalidate();
      notifications.show({
        title: 'Пользователь заблокирован',
        message: 'Жалоба закрыта.',
        color: 'green',
      });
    },
    onError: notifyError,
  });

  function openBlock(report: AdminRelativeReport) {
    setBlockTarget(report);
    setBlockReason(report.reason);
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="center">
        <Stack gap={2}>
          <TitleLabel>Жалобы на родственников</TitleLabel>
          <CaptionLabel>
            Обращения из внутренней переписки. Клик по имени — полная карточка
            пользователя. Блокировка убирает нарушителя из функции «Родственники»
            и закрывает ему доступ к сервису.
          </CaptionLabel>
        </Stack>
        <Switch
          checked={pendingOnly}
          onChange={(e) => setPendingOnly(e.currentTarget.checked)}
          label="Только новые"
        />
      </Group>

      {query.isLoading && (
        <Stack align="center" py="xl">
          <Loader color="azure" />
        </Stack>
      )}

      {query.isError && (
        <Alert color="red" variant="light">
          {formatError(query.error)}
        </Alert>
      )}

      {query.data && query.data.length === 0 && (
        <CloudCard>
          <CaptionLabel>
            {pendingOnly ? 'Новых жалоб нет.' : 'Жалоб нет.'}
          </CaptionLabel>
        </CloudCard>
      )}

      {query.data?.map((report) => (
        <ReportCard
          key={report.id}
          report={report}
          onBlock={() => openBlock(report)}
          onResolve={() => resolveMutation.mutate({ id: report.id, note: null })}
          busy={resolveMutation.isPending || blockMutation.isPending}
        />
      ))}

      <Modal
        opened={blockTarget !== null}
        onClose={() => setBlockTarget(null)}
        title={
          blockTarget
            ? `Заблокировать ${blockTarget.reportedUserName}`
            : 'Заблокировать'
        }
        centered
      >
        <Stack gap="sm">
          <CaptionLabel>
            Причина попадёт в сообщение при попытке входа заблокированного и в
            пометку к жалобе. После блокировки жалоба закроется автоматически.
          </CaptionLabel>
          <Textarea
            value={blockReason}
            onChange={(e) => setBlockReason(e.currentTarget.value)}
            autosize
            minRows={2}
            maxRows={6}
            maxLength={500}
          />
          <Group justify="flex-end" gap="sm">
            <GhostButton onClick={() => setBlockTarget(null)}>Отмена</GhostButton>
            <PrimaryButton
              leftSection={<Ban size={16} />}
              color="red"
              onClick={() =>
                blockTarget &&
                blockMutation.mutate({ report: blockTarget, reason: blockReason })
              }
              loading={blockMutation.isPending}
              disabled={blockReason.trim().length === 0}
            >
              Заблокировать
            </PrimaryButton>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}

/**
 * Кликабельное имя участника жалобы → полная админская карточка пользователя
 * (email, подписка, блокировка и т.д.). Так супер-админ видит про человека всё.
 */
function UserLink({ id, name }: { id: string; name: string }) {
  const navigate = useNavigate();
  return (
    <button
      type="button"
      onClick={() => navigate(`/admin/users/${id}`)}
      title="Открыть полную карточку пользователя"
      style={{
        background: 'transparent',
        border: 'none',
        padding: 0,
        cursor: 'pointer',
        fontWeight: 700,
        fontSize: 'inherit',
        color: cloudColors.azureDeep,
        textDecoration: 'underline',
      }}
    >
      {name}
    </button>
  );
}

function ReportCard({
  report,
  onBlock,
  onResolve,
  busy,
}: {
  report: AdminRelativeReport;
  onBlock: () => void;
  onResolve: () => void;
  busy: boolean;
}) {
  const isPending = report.status === 'Pending';
  return (
    <CloudCard>
      <Stack gap="sm">
        <Group justify="space-between" align="flex-start" wrap="nowrap">
          <Stack gap={2} style={{ minWidth: 0 }}>
            <Group gap={6} wrap="wrap" align="center">
              <Flag size={14} />
              <UserLink id={report.reporterUserId} name={report.reporterUserName} />
              <BodyLabel>→</BodyLabel>
              <UserLink id={report.reportedUserId} name={report.reportedUserName} />
              {report.reportedIsBlocked && (
                <Badge color="red" variant="light">
                  Заблокирован
                </Badge>
              )}
              {!isPending && (
                <Badge color="gray" variant="light">
                  Разобрана
                </Badge>
              )}
            </Group>
            <CaptionLabel>
              По карточке: {report.deceasedFullName} ·{' '}
              {formatDateTime(report.createdAtUtc)}
            </CaptionLabel>
          </Stack>
        </Group>

        <div
          style={{
            padding: '10px 12px',
            borderRadius: 10,
            background: 'var(--mantine-color-gray-light)',
            whiteSpace: 'pre-wrap',
          }}
        >
          <BodyLabel>{report.reason}</BodyLabel>
        </div>

        {!isPending && report.resolutionNote && (
          <CaptionLabel>Решение: {report.resolutionNote}</CaptionLabel>
        )}

        {isPending && (
          <Group gap="sm" justify="flex-end">
            <GhostButton
              leftSection={<Check size={16} />}
              onClick={onResolve}
              disabled={busy}
            >
              Закрыть без действий
            </GhostButton>
            {!report.reportedIsBlocked && (
              <PrimaryButton
                leftSection={<Ban size={16} />}
                color="red"
                onClick={onBlock}
                disabled={busy}
              >
                Заблокировать
              </PrimaryButton>
            )}
          </Group>
        )}
      </Stack>
    </CloudCard>
  );
}
