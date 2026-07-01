import { AxiosError } from 'axios';
import { ApiError } from '../api/client';

/**
 * F4. Превращает axios/ApiError в человекочитаемую строку для UI.
 *
 * Важный нюанс: при HTTP 4xx/5xx axios выбрасывает AxiosError ДО того,
 * как unwrap() дойдёт до response.data. Поэтому здесь мы сами лезем в
 * error.response.data и достаём errorCode из envelope бэка.
 *
 * Коды берутся из backend/src/GdeOni.Domain/Shared/Errors.User.cs и
 * Errors.cs — должны точь-в-точь совпадать с теми, что бэк возвращает.
 */
const KNOWN_CODES: Record<string, string> = {
  // Auth
  'user.invalid.credentials': 'Неверный email или пароль.',
  'auth.unauthorized': 'Сессия истекла. Войдите снова.',

  // User registration / profile
  'user.email.already.exists': 'Пользователь с таким email уже существует.',
  'user.user_name.already.exists': 'Это имя пользователя уже занято.',
  'user.email.invalid': 'Невалидный email.',
  'user.email.required': 'Введите email.',
  'user.email.too_long': 'Слишком длинный email.',
  'user.password.required': 'Введите пароль.',
  'user.password.too_short': 'Пароль слишком короткий (минимум 8 символов).',
  'user.password.too_long': 'Пароль слишком длинный (максимум 128 символов).',
  'user.user_name.too_long': 'Слишком длинное имя пользователя.',
  'user.full_name.too_long': 'Слишком длинное полное имя.',
  'user.current_password.invalid': 'Текущий пароль введён неверно.',

  // Authorization
  'user.forbidden': 'У вас нет доступа к этому действию.',

  // D19 legal
  'user.privacy_policy.not_accepted': 'Необходимо принять Политику конфиденциальности.',
  'user.terms.not_accepted': 'Необходимо принять Условия использования.',

  // F17.4 moderation conflicts (409)
  'deceased_memory.already.rejected': 'Воспоминание уже скрыто.',
  'deceased_memory.already.approved': 'Воспоминание уже видно всем.',
  'deceased_media.already.rejected': 'Файл уже скрыт.',
  'deceased_media.already.approved': 'Файл уже видно всем.',

  // F17.6 subscription / complimentary management
  'subscription.revoke.self_forbidden': 'Себе нельзя снимать подписку.',
  'subscription.manage.super_admin_forbidden':
    'Admin не может управлять подпиской SuperAdmin.',
  'complimentary.grant.self.forbidden':
    'Себе нельзя выдавать бесплатный доступ.',
  'complimentary.manage.super_admin.forbidden':
    'Admin не может управлять комплиментом SuperAdmin.',
  'complimentary.until.in_past':
    'Дата окончания не может быть в прошлом.',
  'complimentary.note.too_long':
    'Причина слишком длинная (максимум 500 символов).',
  'subscription.trial_duration.invalid':
    'Некорректная длительность Trial.',

  // F17.10 block guards (403)
  'user.block.self.forbidden': 'Себя блокировать нельзя.',
  'user.block.super_admin.forbidden':
    'Супер-админа заблокировать нельзя.',
  'user.block.peer_admin.forbidden':
    'Admin не может заблокировать другого Admin.',
  'user.account.blocked':
    'Аккаунт заблокирован. Обратитесь к администратору.',

  // F17.11 delete guards (403)
  'user.delete.self.forbidden': 'Себя удалить нельзя.',
  'user.delete.super_admin.forbidden':
    'Супер-админа удалить нельзя.',
  'user.delete.peer_admin.forbidden':
    'Admin не может удалить другого Admin.',
  'user.delete.has_content':
    'Не удалось переуступить контент пользователя.',

  // F17.14 support tickets
  'support_ticket.title.required': 'Введите заголовок обращения.',
  'support_ticket.description.required': 'Введите текст обращения.',
  'support_ticket.resolution_note.required':
    'При переводе в «Решено» нужна заметка о решении.',
  'support_ticket.already.resolved': 'Обращение уже решено.',
  'support_ticket.already.accepted': 'Вы уже закрепили решение.',
  'support_ticket.view.forbidden': 'Это обращение не ваше.',
  'support_ticket.modify.forbidden':
    'Изменить это обращение может только его автор.',

  // Generic
  'validation.failed': 'Проверьте корректность заполненных полей.',
};

/** Извлекает errorCode из бэк-envelope в AxiosError, если он там есть. */
function extractEnvelopeError(
  error: AxiosError,
): { code: string; message: string } | null {
  const data = error.response?.data as
    | { errorCode?: string | null; errorMessage?: string | null }
    | undefined;
  if (data?.errorCode) {
    return {
      code: data.errorCode,
      message: data.errorMessage ?? data.errorCode,
    };
  }
  return null;
}

export function formatError(error: unknown): string {
  if (error instanceof ApiError) {
    return KNOWN_CODES[error.code] ?? error.message;
  }

  if (error instanceof AxiosError) {
    // Сначала пытаемся достать errorCode из envelope — это даёт человеческое
    // сообщение даже при HTTP 4xx (когда axios выбросил исключение).
    const envelope = extractEnvelopeError(error);
    if (envelope) {
      return KNOWN_CODES[envelope.code] ?? envelope.message;
    }

    // Особые статусы без envelope.
    if (error.response?.status === 429) {
      return 'Слишком много попыток. Подождите минуту и попробуйте снова.';
    }
    if (error.response?.status === 401) {
      return 'Неверный email или пароль.';
    }
    if (error.response?.status === 403) {
      return 'У вас нет доступа к этому действию.';
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Не удалось соединиться с сервером. Проверьте, что бэкенд запущен.';
    }
    return `Ошибка сети (HTTP ${error.response?.status ?? '?'}).`;
  }

  // Обычный Error (например, из requestGeolocationOnce, mutationFn,
  // ручных throw new Error('...')) — отдаём его собственное сообщение
  // вместо generic 'Неизвестная ошибка'. Иначе разработчик аккуратно
  // составил месседж, а юзер видит бесполезное 'неизвестная ошибка'.
  if (error instanceof Error && error.message) {
    return error.message;
  }

  return 'Неизвестная ошибка. Попробуйте ещё раз.';
}
