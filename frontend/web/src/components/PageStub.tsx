import { Link } from 'react-router-dom';

/**
 * F1. Универсальная заглушка страницы. Реальные страницы появятся
 * в F4-F17. Здесь — только маркер "роут работает" + ссылка обратно.
 */
type Props = {
  title: string;
  fBlock: string;
  back?: string;
};

export function PageStub({ title, fBlock, back = '/tracked' }: Props) {
  return (
    <div style={{ padding: 24, fontFamily: 'system-ui, sans-serif' }}>
      <h1 style={{ marginBottom: 8 }}>{title}</h1>
      <p style={{ color: '#666', marginBottom: 16 }}>
        Заглушка для блока <b>{fBlock}</b>. Реальная страница — в следующем этапе.
      </p>
      <Link to={back} style={{ color: '#4A90E2' }}>
        ← Назад
      </Link>
    </div>
  );
}
