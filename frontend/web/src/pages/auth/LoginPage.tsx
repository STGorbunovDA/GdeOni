import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../auth/authStore';

/**
 * F1. Временная заглушка логина. Кнопка "Войти как тест-юзер"
 * проставляет dummy-токены в store, чтобы можно было проверить
 * редиректы ProtectedRoute. Реальная форма + API — в F4.
 */
export function LoginPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);

  function handleFakeLogin() {
    setTokens('fake-access-token-f1', 'fake-refresh-token-f1');
    navigate('/tracked', { replace: true });
  }

  return (
    <div style={{ padding: 24, maxWidth: 480, margin: '40px auto', fontFamily: 'system-ui' }}>
      <h1>Вход (F1 заглушка)</h1>
      <p style={{ color: '#666' }}>
        Реальная форма входа — в F4. Сейчас только проверяем, что роутинг
        и ProtectedRoute работают.
      </p>
      <button
        onClick={handleFakeLogin}
        style={{
          padding: '12px 20px',
          background: '#4A90E2',
          color: 'white',
          border: 'none',
          borderRadius: 8,
          fontSize: 16,
          cursor: 'pointer',
          marginTop: 16,
        }}
      >
        Войти как тест-юзер
      </button>
    </div>
  );
}
