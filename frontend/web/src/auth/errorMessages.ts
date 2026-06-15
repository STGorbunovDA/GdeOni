import { AxiosError } from 'axios';
import { ApiError } from '../api/client';

/**
 * F4. Превращает axios/ApiError в человекочитаемую строку для UI.
 * Известные коды бэка маппим явно — иначе показываем сам errorMessage.
 */
const KNOWN_CODES: Record<string, string> = {
  'auth.invalid_credentials':
    'Неверный email или пароль.',
  'auth.user_blocked':
    'Пользователь заблокирован. Обратитесь в поддержку.',
  'user.email.already_exists':
    'Пользователь с таким email уже существует.',
  'user.password.too_short': 'Пароль слишком короткий.',
  'user.password.too_long': 'Пароль слишком длинный.',
  'user.email.invalid': 'Невалидный email.',
  'rate_limiting.too_many_requests':
    'Слишком много попыток. Подождите минуту и попробуйте снова.',
};

export function formatError(error: unknown): string {
  if (error instanceof ApiError) {
    return KNOWN_CODES[error.code] ?? error.message;
  }
  if (error instanceof AxiosError) {
    if (error.response?.status === 429) {
      return KNOWN_CODES['rate_limiting.too_many_requests'];
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Не удалось соединиться с сервером. Проверьте подключение.';
    }
    return `Сетевая ошибка: ${error.message}`;
  }
  return 'Неизвестная ошибка. Попробуйте ещё раз.';
}
