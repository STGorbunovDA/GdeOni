import { Container, Group, Stack, TextInput, Textarea } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, Map, Search, User } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { cloudColors } from '../design/theme';

/**
 * F2. Демо всех элементов дизайн-системы разом. Используется для
 * ручной визуальной проверки темы и компонентов-обёрток. В продакшне
 * не нужна — но и не мешает (просто отдельный роут /style-demo).
 */
export function StyleDemoPage() {
  const navigate = useNavigate();
  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <Group>
          <GhostButton
            onClick={() => navigate('/tracked')}
            leftSection={<ChevronLeft size={16} />}
          >
            Назад
          </GhostButton>
        </Group>

        <Stack gap="xs">
          <TitleLabel>F2 — Cloud Design System</TitleLabel>
          <CaptionLabel>
            Визуальная проверка темы: цвета, типографика, обёртки.
          </CaptionLabel>
        </Stack>

        {/* Палитра */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Палитра</SubTitleLabel>
            <Group gap="sm">
              {Object.entries(cloudColors).map(([name, hex]) => (
                <Stack key={name} gap={4} align="center">
                  <div
                    style={{
                      width: 64,
                      height: 64,
                      borderRadius: 12,
                      backgroundColor: hex,
                      border: '1px solid #E0EAF2',
                    }}
                  />
                  <CaptionLabel>{name}</CaptionLabel>
                  <CaptionLabel>{hex}</CaptionLabel>
                </Stack>
              ))}
            </Group>
          </Stack>
        </CloudCard>

        {/* Типографика */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Типографика</SubTitleLabel>
            <TitleLabel>TitleLabel — 32px bold</TitleLabel>
            <SubTitleLabel>SubTitleLabel — 20px bold</SubTitleLabel>
            <BodyLabel>
              BodyLabel — 15px regular с line-height 1.4. Текст разной длины,
              который должен переноситься красиво и читаться без напряжения.
            </BodyLabel>
            <CaptionLabel>CaptionLabel — 13px regular #6B7C8C для подписей и метаинформации.</CaptionLabel>
          </Stack>
        </CloudCard>

        {/* Кнопки */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Кнопки</SubTitleLabel>

            <Group gap="md">
              <PrimaryButton>Primary</PrimaryButton>
              <PrimaryButton disabled>Primary disabled</PrimaryButton>
              <PrimaryButton leftSection={<Map size={16} />}>
                С иконкой
              </PrimaryButton>
            </Group>

            <Group gap="md">
              <GhostButton>Ghost</GhostButton>
              <GhostButton disabled>Ghost disabled</GhostButton>
              <GhostButton leftSection={<Search size={16} />}>
                С иконкой
              </GhostButton>
            </Group>

            <CaptionLabel>
              Hover/pressed/disabled состояния Mantine генерирует автоматически
              из Azure-палитры в theme.ts.
            </CaptionLabel>
          </Stack>
        </CloudCard>

        {/* Формы (Mantine native, окрашены через theme) */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Поля формы</SubTitleLabel>
            <TextInput
              label="Email"
              placeholder="you@example.com"
              leftSection={<User size={16} />}
            />
            <TextInput
              label="Имя"
              placeholder="Иван"
              description="Подсказка под полем."
            />
            <Textarea
              label="Биография"
              placeholder="Краткое описание..."
              minRows={3}
              autosize
            />
            <CaptionLabel>
              Реальные формы — в F4 (логин/регистрация). Здесь только
              проверяем, что theme проникает в Mantine-инпуты.
            </CaptionLabel>
          </Stack>
        </CloudCard>

        {/* Карточки-облака разных размеров */}
        <CloudCard>
          <Stack gap="md">
            <SubTitleLabel>Карточки</SubTitleLabel>
            <Group align="flex-start" gap="md" wrap="wrap">
              <CloudCard style={{ flex: 1, minWidth: 200 }}>
                <Stack gap="xs">
                  <BodyLabel>Внутри ещё одна CloudCard.</BodyLabel>
                  <CaptionLabel>Карточки вкладываются.</CaptionLabel>
                </Stack>
              </CloudCard>
              <CloudCard style={{ flex: 1, minWidth: 200 }}>
                <Stack gap="xs">
                  <BodyLabel>И вот ещё одна.</BodyLabel>
                  <PrimaryButton size="xs">Действие</PrimaryButton>
                </Stack>
              </CloudCard>
            </Group>
          </Stack>
        </CloudCard>
      </Stack>
    </Container>
  );
}
