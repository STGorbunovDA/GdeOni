import { useEffect, useState, type CSSProperties, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { mediaApi } from '../../api/endpoints/mediaApi';

/**
 * D47. Картинка за «вахтёром». Фото умерших и могил бэк отдаёт по
 * авторизованному пути (`/api/media/{id}/content`); тег <img> сам
 * Bearer-заголовок слать не умеет, поэтому качаем blob через axios
 * (токен + refresh при 401 через интерсепторы) и показываем через
 * object URL.
 *
 * Blob кэшируется TanStack Query по `src` (staleTime: Infinity) — скролл
 * списка и возврат на карточку не перекачивают. object URL создаём на
 * инстанс компонента и revoke'аем при размонтировании / смене src, чтобы
 * не течь памятью. Пока blob грузится / если 404 (скрытое медиа) — рисуем
 * `fallback` (иконка-заглушка).
 *
 * Абсолютные http(s)-URL (напр. presigned для документов) показываем
 * напрямую, без авторизованной загрузки.
 */
type AuthImageProps = {
  src: string | null | undefined;
  alt?: string;
  style?: CSSProperties;
  className?: string;
  onClick?: React.MouseEventHandler<HTMLImageElement>;
  /** Что рисовать, пока фото не готово / нет src / ошибка. */
  fallback?: ReactNode;
};

function isApiPath(src: string): boolean {
  return src.startsWith('/');
}

export function AuthImage({
  src,
  alt = '',
  style,
  className,
  onClick,
  fallback = null,
}: AuthImageProps) {
  // Абсолютный URL (presigned документ) — отдаём как есть, без загрузки.
  const direct = !!src && !isApiPath(src);

  const query = useQuery({
    queryKey: ['media-blob', src],
    queryFn: () => mediaApi.getBlob(src as string),
    enabled: !!src && !direct,
    staleTime: Infinity,
    gcTime: 30 * 60 * 1000,
    retry: 1,
  });

  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  useEffect(() => {
    const blob = query.data;
    if (!blob) {
      setObjectUrl(null);
      return;
    }
    const url = URL.createObjectURL(blob);
    setObjectUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [query.data]);

  const resolved = direct ? (src as string) : objectUrl;
  if (!resolved) return <>{fallback}</>;

  return (
    <img
      src={resolved}
      alt={alt}
      style={style}
      className={className}
      onClick={onClick}
    />
  );
}
