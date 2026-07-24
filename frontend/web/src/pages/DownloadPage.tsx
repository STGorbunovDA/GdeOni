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
import { Cloud, Download, Globe, Smartphone } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { cloudColors } from '../design/theme';
import { APK_DOWNLOAD_URL } from '../hooks/useAppVersion';

/**
 * F27. Публичная страница `/download`.
 *  - Landing для новых юзеров, куда ведёт mobile BlockingUpdatePage
 *    и ссылки «скачать приложение» из веба (логин, профиль).
 *  - Кнопка «Скачать APK» ведёт прямо на файл (APK_DOWNLOAD_URL →
 *    /apk/latest.apk), nginx отдаёт его как attachment.
 *  - Anonymous-роут (вне ProtectedRoute) — юзер не обязан быть залогинен.
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

        {/* Android APK — скачивание + инструкция по установке */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Smartphone size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Android-приложение</SubTitleLabel>
            </Group>
            <BodyLabel>
              Приложение для телефонов на Android. Все функции те же, что
              в браузере, плюс напоминания о памятных датах приходят прямо
              на телефон.
            </BodyLabel>
            <Group>
              <Button
                component="a"
                href={APK_DOWNLOAD_URL}
                leftSection={<Download size={18} />}
                radius={24}
                fw={700}
                size="lg"
              >
                Скачать APK
              </Button>
            </Group>

            <Divider />

            <SubTitleLabel>Как установить</SubTitleLabel>
            <List type="ordered" spacing="xs">
              <List.Item>
                <BodyLabel>Нажмите «Скачать APK» — файл сохранится в телефон.</BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Откройте скачанный файл. Android предупредит про «установку
                  из неизвестных источников» — это нормально для приложений
                  не из Google Play.
                </BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>
                  Разрешите установку для браузера (телефон сам предложит
                  открыть настройки) и подтвердите.
                </BodyLabel>
              </List.Item>
              <List.Item>
                <BodyLabel>Готово — иконка «ГдеОни» появится на экране.</BodyLabel>
              </List.Item>
            </List>
            <CaptionLabel>
              Приложение не в Google Play, потому что распространяется
              напрямую. Файл безопасен — это официальная сборка «ГдеОни».
            </CaptionLabel>
          </Stack>
        </CloudCard>

        {/* Web-версия */}
        <CloudCard>
          <Stack gap="md">
            <Group gap={8}>
              <Globe size={24} color={cloudColors.azureDeep} />
              <SubTitleLabel>Веб-версия</SubTitleLabel>
            </Group>
            <BodyLabel>
              Не хотите ставить APK — заходите в браузере. Все функции, кроме
              фоновых напоминаний о годовщинах, работают одинаково.
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
                Открыть веб-версию
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
