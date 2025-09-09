import {formatDuration} from './format-duration';
import {locale} from 'svelte-i18n-lingui';
import {fromStore} from 'svelte/store';
import {gt} from 'svelte-i18n-lingui';

const currentLocale = fromStore(locale);

export function formatRelativeDate(value: Date | string | undefined | null, options?: Intl.DurationFormatOptions, defaultValue = ''): string {
  if (!value) return defaultValue;
  void currentLocale.current; // invalidate when the current locale changes

  const targetDate = typeof value === 'string' ? new Date(value) : value;
  const now = new Date();
  const diffMs = targetDate.getTime() - now.getTime();
  const isPast = diffMs < 0;
  const absDiffMs = Math.abs(diffMs);

  const duration = formatDuration({milliseconds: absDiffMs}, undefined, options);
  if (!duration) return defaultValue;

  return isPast ? gt`${duration} ago` : gt`in ${duration}`;
}
