import { Group, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  TitleLabel,
} from './ui';

/**
 * F1 / F2 / F2.1. Универсальная заглушка страницы. Реальные страницы
 * появятся в F4-F17. Здесь — только маркер "роут работает" + ссылка
 * "назад" + примечание про текущий F-блок.
 *
 * F2.1: убран собственный Container — AppShell.Main уже даёт padding.
 * Для страниц вне layout (NotFoundPage) Container ставится в самой странице.
 */
type Props = {
  title: string;
  fBlock: string;
  back?: string;
};

export function PageStub({ title, fBlock, back = '/tracked' }: Props) {
  const navigate = useNavigate();

  return (
    <Stack gap="lg">
      <Group>
        <GhostButton
          onClick={() => navigate(back)}
          leftSection={<ChevronLeft size={16} />}
        >
          Назад
        </GhostButton>
      </Group>

      <CloudCard>
        <Stack gap="sm">
          <TitleLabel>{title}</TitleLabel>
          <BodyLabel>
            Заглушка для блока <b>{fBlock}</b>. Реальная страница
            появится в следующем этапе.
          </BodyLabel>
          <CaptionLabel>
            Если ты видишь эту страницу с Cloud-стилем и sidebar слева —
            F2 + F2.1 работают.
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Stack>
  );
}
