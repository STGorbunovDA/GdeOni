import { useState } from 'react';
import {
  Anchor,
  Button,
  Checkbox,
  Modal,
  Stack,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { legalApi } from '../../api/endpoints/legalApi';
import { usersApi } from '../../api/endpoints/authApi';
import { BodyLabel, CaptionLabel } from '../ui';
import { cloudColors } from '../../design/theme';
import { formatError } from '../../auth/errorMessages';

/**
 * F24 / D19. Блокирующая модалка «Мы обновили правила».
 *
 * Показывается когда бэк выставил <c>HasOutdatedLegalAcceptance=true</c>
 * (юзер зарегистрировался под старой версией, а юрист выкатил новую).
 * Без крестика, без close-по-Esc — юзер обязан подтвердить, иначе
 * весь UI заблокирован.
 *
 * Проверка идёт на верхнем уровне AppLayout — модалка живёт для всех
 * авторизованных роутов автоматически. Показ по <c>/users/me</c>,
 * accept дёргает POST /accept-legal с текущими версиями и потом
 * инвалидирует me.
 */
export function OutdatedLegalModal() {
  const queryClient = useQueryClient();
  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => usersApi.me(),
  });

  const shouldShow = meQuery.data?.hasOutdatedLegalAcceptance === true;

  const legalQuery = useQuery({
    queryKey: ['legal', 'metadata'],
    queryFn: async () => {
      const [privacy, terms] = await Promise.all([
        legalApi.getPrivacyPolicy(),
        legalApi.getTermsOfUse(),
      ]);
      return { privacy, terms };
    },
    enabled: shouldShow,
    staleTime: Infinity,
  });

  const [checked, setChecked] = useState(false);
  const [busy, setBusy] = useState(false);

  async function handleAccept() {
    if (!legalQuery.data) return;
    setBusy(true);
    try {
      await legalApi.accept({
        privacyPolicyVersion: legalQuery.data.privacy.version,
        termsVersion: legalQuery.data.terms.version,
      });
      await queryClient.invalidateQueries({ queryKey: ['me'] });
      setChecked(false);
    } catch (e) {
      notifications.show({
        title: 'Не удалось подтвердить',
        message: formatError(e),
        color: 'red',
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      opened={shouldShow}
      onClose={() => {
        /* Блокирующая — закрыть нельзя, кнопка «Принимаю». */
      }}
      title="Мы обновили правила"
      centered
      withCloseButton={false}
      closeOnClickOutside={false}
      closeOnEscape={false}
      size="md"
    >
      <Stack gap="md">
        <BodyLabel>
          Мы обновили Политику конфиденциальности и Условия использования.
          Пожалуйста, прочтите новую редакцию и подтвердите согласие, чтобы
          продолжить пользоваться приложением.
        </BodyLabel>

        <CaptionLabel>
          <Anchor
            component={Link}
            to="/legal/privacy"
            target="_blank"
            c={cloudColors.azureDeep}
          >
            Открыть Политику конфиденциальности
          </Anchor>
          {' · '}
          <Anchor
            component={Link}
            to="/legal/terms"
            target="_blank"
            c={cloudColors.azureDeep}
          >
            Открыть Условия использования
          </Anchor>
        </CaptionLabel>

        <Checkbox
          label="Я прочитал(а) и принимаю обновлённые документы"
          checked={checked}
          onChange={(e) => setChecked(e.currentTarget.checked)}
          disabled={legalQuery.isLoading || busy}
        />

        <Button
          onClick={handleAccept}
          loading={busy}
          disabled={!checked || legalQuery.isLoading}
          radius={24}
          size="md"
          fw={700}
          fullWidth
        >
          Принимаю и продолжаю
        </Button>
      </Stack>
    </Modal>
  );
}
