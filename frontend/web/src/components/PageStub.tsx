import { Container, Group, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  TitleLabel,
} from './ui';

type Props = {
  title: string;
  fBlock: string;
  back?: string;
};

export function PageStub({ title, fBlock, back = '/tracked' }: Props) {
  const navigate = useNavigate();

  return (
    <Container size="md" py="xl">
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
              Если ты видишь эту страницу с Cloud-стилем (мягким облачным
              фоном и закруглённой карточкой) — значит, F2 theme работает.
            </CaptionLabel>
          </Stack>
        </CloudCard>
      </Stack>
    </Container>
  );
}
