import { RelationshipTypes } from '../api/endpoints/trackedDeceasedApi';

/**
 * Варианты «кем приходится» для селектов (добавление умершего, правка
 * отслеживания). Без legacy-объединённых значений Parent/Grandparent/Sibling —
 * они остаются только на старых карточках, в новых используются раздельные.
 */
export const RELATIONSHIP_OPTIONS = [
  { value: RelationshipTypes.Mother, label: 'Мама' },
  { value: RelationshipTypes.Father, label: 'Папа' },
  { value: RelationshipTypes.Grandfather, label: 'Дедушка' },
  { value: RelationshipTypes.Grandmother, label: 'Бабушка' },
  { value: RelationshipTypes.GreatGrandfather, label: 'Прадедушка' },
  { value: RelationshipTypes.GreatGrandmother, label: 'Прабабушка' },
  { value: RelationshipTypes.Child, label: 'Ребёнок' },
  { value: RelationshipTypes.Spouse, label: 'Супруг(а)' },
  { value: RelationshipTypes.Brother, label: 'Брат' },
  { value: RelationshipTypes.Sister, label: 'Сестра' },
  { value: RelationshipTypes.Relative, label: 'Родственник' },
  { value: RelationshipTypes.DistantRelative, label: 'Дальний родственник' },
  { value: RelationshipTypes.Friend, label: 'Друг' },
  { value: RelationshipTypes.Acquaintance, label: 'Знакомый' },
  { value: RelationshipTypes.Other, label: 'Другое' },
];
