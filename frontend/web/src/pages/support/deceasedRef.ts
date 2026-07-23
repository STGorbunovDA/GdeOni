/**
 * D34 / F34. Распознаёт ссылку на карточку умершего внутри Description
 * тикета. Шаблон проставляется автоматически при переходе с карточки
 * умершего (см. supportLink в DeceasedDetailsPage) — маркер
 * «ID карточки: {guid}». Если юзер не удалил эту строку при
 * редактировании — админ получит кнопку быстрого перехода.
 *
 * Зеркало SupportDeceasedRefParser на mobile — формат маркера обязан
 * совпадать. Меняя regex, обнови обе платформы одновременно.
 */
const DECEASED_ID_REGEX =
  /ID карточки:\s*([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/;

export function extractDeceasedRefId(description: string | null | undefined): string | null {
  if (!description) return null;
  const match = description.match(DECEASED_ID_REGEX);
  return match ? match[1].toLowerCase() : null;
}
