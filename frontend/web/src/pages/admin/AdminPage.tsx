import { Link } from 'react-router-dom';

/**
 * F1. Заглушка админ-меню. Реальные карточки разделов — в F17.
 */
export function AdminPage() {
  return (
    <div style={{ padding: 24, fontFamily: 'system-ui' }}>
      <h1>Админка (F1 заглушка)</h1>
      <p style={{ color: '#666' }}>
        Полные разделы — в F17. Сейчас только проверяем роутинг.
      </p>

      <ul style={{ lineHeight: 1.8 }}>
        <li><Link to="/admin/all">Все карточки</Link> (F17.1)</li>
        <li><Link to="/admin/users">Пользователи</Link> (F17.7)</li>
        <li><Link to="/tracked">← Назад в обычную часть</Link></li>
      </ul>
    </div>
  );
}
