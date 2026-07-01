import type {
  TicketKind,
  TicketSeverity,
  TicketSource,
  TicketStatus,
} from '../../api/endpoints/supportApi';

export const KIND_LABELS: Record<TicketKind, string> = {
  Payment: 'Платёж',
  Bug: 'Баг',
  Complaint: 'Жалоба',
  Question: 'Вопрос',
  Other: 'Другое',
  Photo: 'Фото',
};

export const STATUS_LABELS: Record<TicketStatus, string> = {
  Open: 'Открыто',
  InProgress: 'В работе',
  Resolved: 'Решено',
};

export const STATUS_COLORS: Record<TicketStatus, string> = {
  Open: 'azure',
  InProgress: 'yellow',
  Resolved: 'green',
};

export const SEVERITY_LABELS: Record<TicketSeverity, string> = {
  Normal: 'Обычный',
  Urgent: 'Срочный',
};

export const SEVERITY_COLORS: Record<TicketSeverity, string> = {
  Normal: 'gray',
  Urgent: 'red',
};

export const SOURCE_LABELS: Record<TicketSource, string> = {
  Manual: 'От юзера',
  Auto: 'Авто',
};

export const SOURCE_COLORS: Record<TicketSource, string> = {
  Manual: 'azure',
  Auto: 'grape',
};

export const KIND_OPTIONS: { value: TicketKind; label: string }[] = [
  { value: 'Payment', label: KIND_LABELS.Payment },
  { value: 'Bug', label: KIND_LABELS.Bug },
  { value: 'Complaint', label: KIND_LABELS.Complaint },
  { value: 'Question', label: KIND_LABELS.Question },
  { value: 'Other', label: KIND_LABELS.Other },
  { value: 'Photo', label: KIND_LABELS.Photo },
];

export const STATUS_OPTIONS: { value: TicketStatus; label: string }[] = [
  { value: 'Open', label: STATUS_LABELS.Open },
  { value: 'InProgress', label: STATUS_LABELS.InProgress },
  { value: 'Resolved', label: STATUS_LABELS.Resolved },
];

export const SEVERITY_OPTIONS: { value: TicketSeverity; label: string }[] = [
  { value: 'Normal', label: SEVERITY_LABELS.Normal },
  { value: 'Urgent', label: SEVERITY_LABELS.Urgent },
];

export const SOURCE_OPTIONS: { value: TicketSource; label: string }[] = [
  { value: 'Manual', label: SOURCE_LABELS.Manual },
  { value: 'Auto', label: SOURCE_LABELS.Auto },
];
