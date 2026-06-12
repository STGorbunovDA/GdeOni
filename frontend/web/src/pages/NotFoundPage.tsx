import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <div style={{ padding: 24, fontFamily: 'system-ui', textAlign: 'center', marginTop: 80 }}>
      <h1 style={{ fontSize: 48 }}>404</h1>
      <p style={{ color: '#666', marginBottom: 16 }}>Страница не найдена.</p>
      <Link to="/tracked" style={{ color: '#4A90E2' }}>
        На главную
      </Link>
    </div>
  );
}
