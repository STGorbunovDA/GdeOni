/**
 * F9. Зеркало mobile RelationshipDisplayConverter — превращает
 * технические имена RelationshipType ('Parent', 'Friend', ...) в
 * читаемые русские подписи для UI ("Родитель", "Друг", ...).
 *
 * Имена константные, лежат в trackedDeceasedApi.RelationshipTypes;
 * та же таблица используется и в F8 AtGravePage (Select).
 *
 * Если придёт неизвестное значение — возвращаем его как есть,
 * чтобы не терять данные из-за рассинхрона backend↔web.
 */
const DISPLAY: Record<string, string> = {
  Parent: 'Родитель',
  Grandparent: 'Бабушка/дедушка',
  GreatGrandfather: 'Прадедушка',
  GreatGrandmother: 'Прабабушка',
  Child: 'Ребёнок',
  Spouse: 'Супруг(а)',
  Sibling: 'Брат/сестра',
  Relative: 'Родственник',
  DistantRelative: 'Дальний родственник',
  Friend: 'Друг',
  Acquaintance: 'Знакомый',
  Other: 'Другое',
};

export function relationshipDisplay(value: string): string {
  return DISPLAY[value] ?? value;
}
