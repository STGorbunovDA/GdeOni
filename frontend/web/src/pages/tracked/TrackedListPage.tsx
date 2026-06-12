import { Link } from 'react-router-dom';
import { useAuthStore } from '../../auth/authStore';

/**
 * F1. Главная защищённая страница — сюда юзер попадает после логина.
 * Реальный список tracked-карточек — в F9.
 */
export function TrackedListPage() {
  const clear = useAuthStore((s) => s.clear);

  return (
    <div style={{ padding: 24, fontFamily: 'system-ui' }}>
      <h1>Отслеживаемые (F1 заглушка)</h1>
      <p style={{ color: '#666' }}>
        Реальный список появится в F9. Сейчас проверяем, что после "входа"
        нас сюда пускают, а без токена редиректит на /login.
      </p>

      <div style={{ marginTop: 24 }}>
        <h3>Навигация (заглушки):</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><Link to="/route">Маршрут</Link> (F14)</li>
          <li><Link to="/search">Поиск</Link> (F6)</li>
          <li><Link to="/at-grave">Добавить умершего</Link> (F8)</li>
          <li><Link to="/profile">Профиль</Link> (F16)</li>
          <li><Link to="/admin">Админка</Link> (F17)</li>
          <li><Link to="/tracked/00000000-0000-0000-0000-000000000000">Карточка (тест-id)</Link> (F11)</li>
        </ul>
      </div>

      <button
        onClick={clear}
        style={{
          marginTop: 24,
          padding: '8px 16px',
          background: 'transparent',
          color: '#C0392B',
          border: '1px solid #C0392B',
          borderRadius: 8,
          cursor: 'pointer',
        }}
      >
        Выйти (clear токенов)
      </button>
    </div>
  );
}
