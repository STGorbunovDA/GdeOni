import { useEffect, useRef, useState } from 'react';
import { ActionIcon, Group, Popover, TextInput } from '@mantine/core';
import { DatePicker } from '@mantine/dates';
import { Calendar, X } from 'lucide-react';
import { formatRuDate, maskDateInput, parseRuDate } from '../utils/formatDate';

export type DateMaskInputProps = {
  label?: string;
  placeholder?: string;
  value: Date | null;
  onChange: (value: Date | null) => void;
  error?: string;
  required?: boolean;
  clearable?: boolean;
  minDate?: Date;
  maxDate?: Date;
};

/**
 * Поле даты с МАСКОЙ ввода: набираешь цифры — точки в формате ДД.ММ.ГГГГ
 * ставятся сами, плюс есть календарь по кнопке. Значение наружу — Date | null.
 *
 * Работает одинаково в любом браузере (iOS Safari / Android Chrome / десктоп):
 * маска — это обработка строки в onChange, а не поведение <input type="date">,
 * которое на мобильных ведёт себя по-разному. Заменяет Mantine DateInput,
 * у которого маски (авто-точек) нет.
 */
export function DateMaskInput({
  label,
  placeholder = 'дд.мм.гггг',
  value,
  onChange,
  error,
  required,
  clearable = true,
  minDate,
  maxDate,
}: DateMaskInputProps) {
  const [text, setText] = useState<string>(() => formatRuDate(value));
  const [calendarOpen, setCalendarOpen] = useState(false);
  const focusedRef = useRef(false);

  // Синхронизация текста с внешним value (префилл после загрузки, выбор из
  // календаря, очистка). Пока пользователь печатает (focus) — не трогаем,
  // чтобы не затереть промежуточный ввод при onChange(null).
  useEffect(() => {
    if (focusedRef.current) return;
    setText(formatRuDate(value));
  }, [value]);

  function handleText(raw: string) {
    const masked = maskDateInput(raw);
    setText(masked);
    // Полная валидная дата → Date; недобор/битая/пустая → null (текст при
    // этом остаётся, чтобы человек мог дописать).
    onChange(parseRuDate(masked));
  }

  function pickFromCalendar(picked: Date | null) {
    setText(formatRuDate(picked));
    onChange(picked);
    setCalendarOpen(false);
  }

  const showClear = clearable && text.length > 0;

  return (
    <TextInput
      label={label}
      required={required}
      placeholder={placeholder}
      value={text}
      inputMode="numeric"
      autoComplete="off"
      error={error}
      onFocus={() => {
        focusedRef.current = true;
      }}
      onBlur={() => {
        focusedRef.current = false;
        // Приводим показанное к валидному значению: недобор/битая дата
        // (value === null) → очищаем, чтобы текст совпадал с состоянием.
        setText(formatRuDate(value));
      }}
      onChange={(e) => handleText(e.currentTarget.value)}
      rightSectionPointerEvents="all"
      rightSectionWidth={showClear ? 66 : 40}
      rightSection={
        <Group gap={2} wrap="nowrap">
          {showClear && (
            <ActionIcon
              variant="subtle"
              color="gray"
              size="sm"
              aria-label="Очистить дату"
              onClick={() => {
                setText('');
                onChange(null);
              }}
            >
              <X size={14} />
            </ActionIcon>
          )}
          <Popover
            opened={calendarOpen}
            onChange={setCalendarOpen}
            position="bottom-end"
            withArrow
            shadow="md"
          >
            <Popover.Target>
              <ActionIcon
                variant="subtle"
                color="gray"
                size="sm"
                aria-label="Открыть календарь"
                onClick={() => setCalendarOpen((o) => !o)}
              >
                <Calendar size={16} />
              </ActionIcon>
            </Popover.Target>
            <Popover.Dropdown p="xs">
              <DatePicker
                value={value}
                onChange={pickFromCalendar}
                defaultDate={value ?? maxDate ?? undefined}
                minDate={minDate}
                maxDate={maxDate}
              />
            </Popover.Dropdown>
          </Popover>
        </Group>
      }
    />
  );
}
