import {fromStore} from 'svelte/store';
import {locale} from 'svelte-i18n-lingui';

const stateLocale = fromStore(locale);

export function formatNumber(value: number | undefined, options?: Intl.NumberFormatOptions, defaultValue = ''): string {
  if (value === undefined) return defaultValue;

  return new Intl.NumberFormat(stateLocale.current, {
    style: 'decimal',
    useGrouping: true,
    ...options,
  }).format(value);
}
