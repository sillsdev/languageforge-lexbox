const NULL_LABEL = '–';

export function formatCount(value: number | bigint | undefined | null, locale: string, nullLabel?: string, options?: Intl.NumberFormatOptions): string {
  if (value === undefined || value === null) return nullLabel ?? NULL_LABEL;
  return Intl.NumberFormat(locale, options).format(value);
}

export function formatDate(value: string | Date | number | null | undefined, locale: string, nullLabel?: string, options?: Intl.DateTimeFormatOptions): string {
  if (value === undefined || value === null) return nullLabel ?? NULL_LABEL;
  const formatter = Intl.DateTimeFormat(locale, options);
  return formatter.format(new Date(value));
}
