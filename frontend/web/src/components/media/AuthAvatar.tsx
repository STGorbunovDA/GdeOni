import { UserRound } from 'lucide-react';
import { cloudColors } from '../../design/theme';
import { AuthImage } from './AuthImage';

/**
 * D47. Круглый аватар карточки умершего на базе AuthImage. Заменил
 * копипастные локальные `Avatar` по страницам: фото грузится через
 * «вахтёра» (авторизованно), при отсутствии/ошибке — иконка UserRound.
 * `src` — путь к фото (поле mainPhotoUrl/photoUrl из ответа бэка) или null.
 */
export function AuthAvatar({
  src,
  size = 56,
  iconSize,
}: {
  src: string | null | undefined;
  size?: number;
  iconSize?: number;
}) {
  const icon = iconSize ?? Math.round(size / 2);
  return (
    <div
      style={{
        width: size,
        height: size,
        flexShrink: 0,
        borderRadius: '50%',
        background: cloudColors.sky,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        color: cloudColors.azureDeep,
      }}
    >
      <AuthImage
        src={src}
        style={{
          width: size,
          height: size,
          objectFit: 'cover',
          display: 'block',
        }}
        fallback={<UserRound size={icon} strokeWidth={1.5} />}
      />
    </div>
  );
}
