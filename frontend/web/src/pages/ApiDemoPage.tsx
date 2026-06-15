import { useState } from 'react';
import { Code, Container, Group, Stack } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  GhostButton,
  PrimaryButton,
  SubTitleLabel,
  TitleLabel,
} from '../components/ui';
import { apiClient, ApiError } from '../api/client';
import { authApi, userApi } from '../api/endpoints/authApi';
import { useAuthStore } from '../auth/authStore';
import { API_BASE_URL } from '../api/config';

/**
 * F3. Demo для ручной проверки HTTP-клиента и refresh-интерсептора.
 * Публичный роут /api-demo. После F4 удалим (тогда login flow покроет
 * все сценарии естественно).
 *
 * Сценарии:
 *  1. GET /api/users/me — без токена → 401 → refresh-попытка → редирект /login.
 *  2. POST /api/auth/login с валидными creds → токены → setSession.
 *  3. Симулировать просроченный access (сломать его в store) → запрос
 *     должен пройти после refresh.
 *  4. Сломать refresh тоже → запрос упадёт + редирект /login.
 */
export function ApiDemoPage() {
  const navigate = useNavigate();
  const [log, setLog] = useState<string[]>([]);
  const [email, setEmail] = useState('admin@gdeoni.local');
  const [password, setPassword] = useState('SuperAdmin123!');

  const role = useAuthStore((s) => s.role);
  const access = useAuthStore((s) => s.accessToken);
  const refresh = useAuthStore((s) => s.refreshToken);
  const setSession = useAuthStore((s) => s.setSession);
  const clear = useAuthStore((s) => s.clear);

  function addLog(msg: string) {
    setLog((prev) => [
      `${new Date().toLocaleTimeString()} — ${msg}`,
      ...prev,
    ].slice(0, 30));
  }

  async function callMe() {
    try {
      const me = await userApi.me();
      addLog(
        `GET /api/users/me → 200 — ${me.email} (${me.role})`,
      );
    } catch (e) {
      if (e instanceof ApiError) {
        addLog(`GET /api/users/me → ApiError(${e.code}): ${e.message}`);
      } else {
        addLog(`GET /api/users/me → ${String(e)}`);
      }
    }
  }

  async function callLogin() {
    try {
      const tokens = await authApi.login(email, password);
      // Роль здесь хардкодим 'User' — в F4 будет дёргаться /users/me и
      // роль возьмётся оттуда. Сейчас цель — посмотреть, как login flow
      // дёргает бэк через axios + проходит CORS.
      setSession(tokens.accessToken, tokens.refreshToken, 'User');
      addLog(
        `POST /api/auth/login → 200, токены сохранены в store.`,
      );
    } catch (e) {
      if (e instanceof ApiError) {
        addLog(`POST /api/auth/login → ApiError(${e.code}): ${e.message}`);
      } else {
        addLog(`POST /api/auth/login → ${String(e)}`);
      }
    }
  }

  function breakAccess() {
    if (!access || !refresh) {
      addLog('Нет токенов — сначала залогинься.');
      return;
    }
    setSession('this-is-broken-jwt', refresh, role ?? 'User');
    addLog('Access сломан. Следующий запрос должен запустить refresh.');
  }

  function breakRefresh() {
    if (!access) {
      addLog('Нет токенов — сначала залогинься.');
      return;
    }
    setSession(access, 'this-is-broken-refresh', role ?? 'User');
    addLog(
      'Refresh сломан. Если access тоже сломан — запрос упадёт + редирект /login.',
    );
  }

  async function callRaw401() {
    try {
      await apiClient.get('/api/admin/support-tickets', {
        params: { page: 1, pageSize: 1 },
      });
      addLog('GET /api/admin/support-tickets → 200.');
    } catch (e) {
      if (e instanceof ApiError) {
        addLog(`/api/admin/... → ApiError(${e.code})`);
      } else {
        addLog(`/api/admin/... → ${String(e)}`);
      }
    }
  }

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
          <TitleLabel>API Demo (F3)</TitleLabel>
          <CaptionLabel>
            Ручная проверка axios + interceptors. Backend ожидается на
            {' '}<Code>{API_BASE_URL}</Code>. После F4 эту страницу
            уберём — login flow покроет все сценарии.
          </CaptionLabel>
        </Stack>

        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>Состояние store</SubTitleLabel>
            <BodyLabel>
              Роль: <Code>{role ?? 'null'}</Code>
            </BodyLabel>
            <BodyLabel>
              Access: <Code>{access ? `${access.slice(0, 24)}…` : 'null'}</Code>
            </BodyLabel>
            <BodyLabel>
              Refresh: <Code>{refresh ? `${refresh.slice(0, 24)}…` : 'null'}</Code>
            </BodyLabel>
            <Group>
              <GhostButton onClick={() => clear()}>
                Очистить store
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>

        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>1. Логин</SubTitleLabel>
            <BodyLabel>
              POST /api/auth/login. Введи реальные credentials юзера из БД
              (например созданного через регистрацию в mobile или seed
              SuperAdmin). После успеха токены упадут в Zustand.
            </BodyLabel>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="email"
              style={{
                padding: 10,
                border: '1px solid #E0EAF2',
                borderRadius: 8,
                fontSize: 14,
              }}
            />
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="password"
              style={{
                padding: 10,
                border: '1px solid #E0EAF2',
                borderRadius: 8,
                fontSize: 14,
              }}
            />
            <Group>
              <PrimaryButton onClick={callLogin}>Login</PrimaryButton>
            </Group>
          </Stack>
        </CloudCard>

        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>2. Защищённый запрос</SubTitleLabel>
            <BodyLabel>
              GET /api/users/me. Если access валидный → 200. Если access
              сломан → 401 → refresh → ретрай → 200. Если оба сломаны →
              чистка store + редирект /login.
            </BodyLabel>
            <Group>
              <PrimaryButton onClick={callMe}>
                GET /api/users/me
              </PrimaryButton>
              <GhostButton onClick={callRaw401}>
                GET /api/admin/support-tickets
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>

        <CloudCard>
          <Stack gap="sm">
            <SubTitleLabel>3. Симуляция</SubTitleLabel>
            <BodyLabel>
              Эти кнопки портят соответствующий токен в store. После них
              нажми "GET /api/users/me" сверху и смотри, что произойдёт.
            </BodyLabel>
            <Group>
              <GhostButton onClick={breakAccess}>
                Сломать access (refresh должен сработать)
              </GhostButton>
              <GhostButton onClick={breakRefresh}>
                Сломать refresh (logout + редирект)
              </GhostButton>
            </Group>
          </Stack>
        </CloudCard>

        <CloudCard>
          <Stack gap="xs">
            <SubTitleLabel>Лог</SubTitleLabel>
            {log.length === 0 ? (
              <CaptionLabel>Лог пуст — нажми любую кнопку выше.</CaptionLabel>
            ) : (
              log.map((line, idx) => (
                <CaptionLabel key={idx}>{line}</CaptionLabel>
              ))
            )}
          </Stack>
        </CloudCard>
      </Stack>
    </Container>
  );
}
