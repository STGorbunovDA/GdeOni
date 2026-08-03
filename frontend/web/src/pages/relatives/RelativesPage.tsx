import { Alert, Group, Loader, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { UserRound } from 'lucide-react';
import {
  CaptionLabel,
  CloudCard,
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
  const query = useQuery({
    queryKey: ['relatives'],
    queryFn: () => relativesApi.myRelatives(),
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
        />
      ))}
    </Stack>
  );
}

function RelativeRow({ item }: { item: MyRelativeItem }) {
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
        {/* «Написать» + чат — Фаза 3. */}
      </Group>
    </CloudCard>
  );
}
