import {i18n} from '@lingui/core';
import {fromStore} from 'svelte/store';
import {locale} from 'svelte-i18n-lingui';

const currentLocale = fromStore(locale);
export function formatNumber(value: number | undefined, options?: Intl.NumberFormatOptions, defaultValue = ''): string {
  if (value === undefined) return defaultValue;
  void currentLocale.current; //invalidate when the current locale changes

  return i18n.number(value, options);
}
