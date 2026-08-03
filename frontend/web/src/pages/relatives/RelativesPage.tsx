import { useEffect } from 'react';
import { Alert, Group, Loader, Stack } from '@mantine/core';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { MessageSquare, UserRound } from 'lucide-react';
import { RELATIVES_SUMMARY_KEY } from '../../hooks/useRelativesSummary';
import {
  CaptionLabel,
  CloudCard,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';
import {
  relativesApi,
  type MyRelativeItem,
} from '../../api/endpoints/relativesApi';
import { relationshipDisplay } from '../../utils/relationshipDisplay';
import { formatError } from '../../auth/errorMessages';
import { formatDateOnly } from '../../utils/formatDate';

/**
 * Функция «Родственники» (Фаза 2). По карточкам, которые отслеживает
 * пользователь, показывает других отслеживающих с семейной/близкой связью:
 * ник + кем приходится умершему. Почты нет. Кнопка «Написать» и чат —
 * Фаза 3.
 */
export function RelativesPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['relatives'],
    queryFn: () => relativesApi.myRelatives(),
  });

  // Заход на вкладку = «увидел новых родственников»: сбрасываем is_new на
  // бэке и инвалидируем сводку, чтобы попап «События» и бейдж их не показывали.
  useEffect(() => {
    relativesApi
      .markRelativesSeen()
      .then(() =>
        queryClient.invalidateQueries({ queryKey: RELATIVES_SUMMARY_KEY }),
      )
      .catch(() => {
        // best-effort: не удалось отметить — не мешаем показу списка.
      });
  }, [queryClient]);

  // «Написать»: открываем/получаем диалог и переходим в чат.
  const startMutation = useMutation({
    mutationFn: (item: MyRelativeItem) =>
      relativesApi.startConversation(item.deceasedId, item.relativeUserId),
    onSuccess: (conv) => navigate(`/relatives/chat/${conv.conversationId}`),
    onError: (e) =>
      notifications.show({
        title: 'Не удалось открыть переписку',
        message: formatError(e),
        color: 'red',
      }),
  });

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <TitleLabel>Родственники</TitleLabel>
        <CaptionLabel>
          Люди, которые отслеживают те же карточки, что и вы, и указали своё
          родство с умершим. Почта не показывается — связаться можно внутри
          приложения.
        </CaptionLabel>
      </Stack>

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
            Пока никого не нашли. Как только кто-то ещё начнёт отслеживать те же
            карточки и укажет родство — он появится здесь.
          </CaptionLabel>
        </CloudCard>
      )}

      {query.data?.map((item) => (
        <RelativeRow
          key={`${item.deceasedId}:${item.relativeUserId}`}
          item={item}
          onWrite={() => startMutation.mutate(item)}
          writing={
            startMutation.isPending &&
            startMutation.variables?.deceasedId === item.deceasedId &&
            startMutation.variables?.relativeUserId === item.relativeUserId
          }
        />
      ))}
    </Stack>
  );
}

function RelativeRow({
  item,
  onWrite,
  writing,
}: {
  item: MyRelativeItem;
  onWrite: () => void;
  writing: boolean;
}) {
  const life = `${item.birthDate ? formatDateOnly(item.birthDate) : '?'} — ${formatDateOnly(item.deathDate)}`;
  return (
    <CloudCard>
      <Group align="center" gap="md" wrap="nowrap">
        <div
          style={{
            width: 48,
            height: 48,
            flexShrink: 0,
            borderRadius: '50%',
            background: cloudColors.sky,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: cloudColors.azureDeep,
          }}
        >
          <UserRound size={24} strokeWidth={1.5} />
        </div>
        <Stack gap={2} style={{ flex: 1, minWidth: 0 }}>
          <SubTitleLabel>{item.relativeUserName}</SubTitleLabel>
          <CaptionLabel>
            {relationshipDisplay(item.relationshipType)} · {item.deceasedFullName}
          </CaptionLabel>
          <CaptionLabel>{life}</CaptionLabel>
        </Stack>
        <PrimaryButton
          leftSection={<MessageSquare size={16} />}
          onClick={onWrite}
          loading={writing}
        >
          Написать
        </PrimaryButton>
      </Group>
    </CloudCard>
  );
}
