import { Alert, Badge, Group, Loader, Stack } from '@mantine/core';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { UserRound } from 'lucide-react';
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
import { shareApi, type ShareBundleItem } from '../../api/endpoints/shareApi';
import { formatError } from '../../auth/errorMessages';
import { formatDateOnly } from '../../utils/formatDate';

/**
 * D46. Экран получателя: открыл ссылку/QR `/s/{code}` → (после входа —
 * гарантирует ProtectedRoute) видит список карточек из подборки и
 * добавляет их к себе в отслеживание одной кнопкой.
 *
 * Роут в whitelist до RequireSubscription: список показываем и без
 * активной подписки. Сам импорт под подпиской — 403 subscription.required
 * уведёт на paywall (axios-интерсептор). Новый юзер на триале добавляет
 * сразу.
 */
export function ShareImportPage() {
  const navigate = useNavigate();
  const { code } = useParams<{ code: string }>();

  const bundleQuery = useQuery({
    queryKey: ['share-bundle', code],
    queryFn: () => shareApi.get(code!),
    enabled: !!code,
    retry: 0,
  });

  const importMutation = useMutation({
    mutationFn: () => shareApi.import(code!),
    onSuccess: (res) => {
      const msg =
        res.added > 0
          ? `Добавлено карточек: ${res.added}.`
          : 'Все карточки из подборки уже у вас в отслеживании.';
      notifications.show({ title: 'Готово', message: msg, color: 'blue' });
      navigate('/tracked', { replace: true });
    },
    onError: (e) => {
      // 403 subscription.required уводит на paywall интерсептором — сюда
      // долетают прочие ошибки.
      notifications.show({
        title: 'Не удалось добавить',
        message: formatError(e),
        color: 'red',
      });
    },
  });

  if (!code) {
    return <Centered>Ссылка неполная.</Centered>;
  }

  if (bundleQuery.isLoading) {
    return (
      <Stack align="center" py="xl">
        <Loader color="azure" />
      </Stack>
    );
  }

  if (bundleQuery.isError || !bundleQuery.data) {
    return (
      <Stack gap="lg" maw={560} mx="auto">
        <Alert color="red" variant="light">
          {bundleQuery.error
            ? formatError(bundleQuery.error)
            : 'Ссылка недействительна или срок её действия истёк.'}
        </Alert>
        <GhostButton onClick={() => navigate('/tracked')}>
          Перейти к отслеживаемым
        </GhostButton>
      </Stack>
    );
  }

  const items = bundleQuery.data.items;
  const newCount = items.filter((i) => i.trackingStatus === null).length;

  if (items.length === 0) {
    return (
      <Stack gap="lg" maw={560} mx="auto">
        <Alert color="yellow" variant="light">
          В этой подборке нет доступных карточек — возможно, их удалили.
        </Alert>
        <GhostButton onClick={() => navigate('/tracked')}>
          Перейти к отслеживаемым
        </GhostButton>
      </Stack>
    );
  }

  return (
    <Stack gap="lg" maw={560} mx="auto">
      <Stack gap="xs">
        <TitleLabel>Вам поделились карточками</TitleLabel>
        <CaptionLabel>
          {newCount > 0
            ? `Новых карточек: ${newCount} из ${items.length}. Добавим только тех, кого у вас ещё нет.`
            : 'Все карточки из подборки уже есть у вас в отслеживании.'}
        </CaptionLabel>
      </Stack>

      <Stack gap="sm">
        {items.map((item) => (
          <ShareRow key={item.deceasedId} item={item} />
        ))}
      </Stack>

      <Group grow>
        <GhostButton
          onClick={() => navigate('/tracked')}
          disabled={importMutation.isPending}
        >
          Отменить
        </GhostButton>
        <PrimaryButton
          onClick={() => importMutation.mutate()}
          loading={importMutation.isPending}
          disabled={newCount === 0}
        >
          {newCount === 0 ? 'Все уже у вас' : `Добавить (${newCount})`}
        </PrimaryButton>
      </Group>
    </Stack>
  );
}

function ShareRow({ item }: { item: ShareBundleItem }) {
  const life = `${item.birthDate ? formatDateOnly(item.birthDate) : '?'} — ${formatDateOnly(item.deathDate)}`;
  const place = [item.country, item.city, item.cemeteryName]
    .filter(Boolean)
    .join(', ');
  // Метка «уже в списке / в архиве» — карточку, которая уже есть у
  // получателя, импорт не трогает (см. ImportShareBundleUseCase).
  const badge = trackingBadge(item.trackingStatus);

  return (
    <CloudCard style={badge ? { opacity: 0.65 } : undefined}>
      <Group align="center" gap="md" wrap="nowrap">
        <div
          style={{
            width: 44,
            height: 44,
            flexShrink: 0,
            borderRadius: '50%',
            background: cloudColors.sky,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: cloudColors.azureDeep,
          }}
        >
          <UserRound size={22} strokeWidth={1.5} />
        </div>
        <Stack gap={2} style={{ flex: 1, minWidth: 0 }}>
          <SubTitleLabel>{item.fullName}</SubTitleLabel>
          <CaptionLabel>{life}</CaptionLabel>
          {place && <CaptionLabel>{place}</CaptionLabel>}
        </Stack>
        {badge && (
          <Badge color={badge.color} variant="light" style={{ flexShrink: 0 }}>
            {badge.text}
          </Badge>
        )}
      </Group>
    </CloudCard>
  );
}

function Centered({ children }: { children: React.ReactNode }) {
  return (
    <Stack align="center" py="xl">
      <BodyLabel>{children}</BodyLabel>
    </Stack>
  );
}

/** Метка статуса карточки у получателя (null-статус — метки нет, будет добавлена). */
function trackingBadge(
  status: string | null,
): { text: string; color: string } | null {
  if (!status) return null;
  if (status === 'Archived') return { text: 'В архиве', color: 'gray' };
  return { text: 'Уже отслеживаете', color: 'azure' };
}
