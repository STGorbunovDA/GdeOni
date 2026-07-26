import {
  Anchor,
  Button,
  Container,
  Divider,
  Group,
  List,
  Stack,
} from '@mantine/core';
import { Link } from 'react-router-dom';
import { Cloud, Globe, Smartphone } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { cloudColors } from '../design/theme';
import { InstallPwaButton } from '../components/pwa/InstallPwaButton';

/**
 * F27 / PWA. Публичная страница `/download` — как поставить «ГдеОни» на телефон.
 * Единственный способ — установить сайт как приложение (PWA): работает и на
 * Android, и на iPhone, App Store/Google Play не нужны. Anonymous-роут.
 */
export function DownloadPage() {
  return (
    <Container size="sm" pt={64} pb={64}>
      <Stack gap="xl">
        <Stack gap="sm" align="center">
          <Cloud size={56} color={cloudColors.azureDeep} />
          <TitleLabel>ГдеОни</TitleLabel>
          <BodyLabel style={{ textAlign: 'center', maxWidth: 480 }}>
            Каталог мест захоронений с GPS-координатами. Помогает быстро
            находить могилы близких и делиться местом с родственниками.
          </BodyLabel>
        </Stack>

        {/* Основной способ — установить сайт как приложение (PWA). */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Smartphone size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Установить на телефон</SubTitleLabel>
            </Group>
            <BodyLabel>
              Это сам сайт, добавленный на главный экран: открывается на весь
              экран, как обычное приложение, и обновляется само вместе с сайтом.
              Работает и на Android, и на iPhone — ничего скачивать из магазинов
              не нужно.
            </BodyLabel>
            <Group>
              <InstallPwaButton label="Установить на смартфон" size="lg" />
            </Group>

            <Divider />

            <SubTitleLabel>Android</SubTitleLabel>
            <List type="ordered" spacing="xs">
              <List.Item>
                <BodyLabel>Откройте gdeoni.ru в Chrome.</BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Нажмите всплывающую плашку «Установить приложение» внизу —
                  или меню ⋮ (три точки справа вверху) → «Установить приложение».
                </BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Подтвердите — иконка «ГдеОни» появится на экране.
                </BodyLabel>
              </List.Item>
            </List>

            <SubTitleLabel>iPhone</SubTitleLabel>
            <List type="ordered" spacing="xs">
              <List.Item>
                <BodyLabel>
                  Откройте gdeoni.ru в <b>Safari</b> (именно Safari, в Chrome на
                  iPhone этого пункта нет).
                </BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Нажмите «Поделиться» — квадрат со стрелкой вверх внизу экрана.
                </BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Пролистайте вниз → «На экран „Домой"» → «Добавить».
                </BodyLabel>
              </List.Item>
            </List>
          </Stack>
        </CloudCard>

        {/* Web-версия */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Globe size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Просто открыть в браузере</SubTitleLabel>
            </Group>
            <BodyLabel>
              Ничего устанавливать не обязательно — заходите на сайт с телефона
              или компьютера. Все функции те же.
            </BodyLabel>
            <Group>
              <Button
                component={Link}
                to="/login"
                variant="default"
                radius={24}
                fw={700}
                size="md"
              >
                Открыть в браузере
              </Button>
            </Group>
          </Stack>
        </CloudCard>

        {/* Контакты / legal */}
        <Stack gap={4} align="center">
          <CaptionLabel>
            Вопросы —{' '}
            <Anchor
              href="mailto:bous07@mail.ru"
              c={cloudColors.azureDeep}
            >
              bous07@mail.ru
            </Anchor>
          </CaptionLabel>
          <CaptionLabel>
            <Anchor component={Link} to="/legal/privacy" c={cloudColors.azureDeep}>
              Политика конфиденциальности
            </Anchor>
            {' · '}
            <Anchor component={Link} to="/legal/terms" c={cloudColors.azureDeep}>
              Условия использования
            </Anchor>
          </CaptionLabel>
        </Stack>
      </Stack>
    </Container>
  );
}
