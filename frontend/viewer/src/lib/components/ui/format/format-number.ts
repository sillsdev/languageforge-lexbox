import {i18n} from '@lingui/core';

export function formatNumber(value: number | undefined, options?: Intl.NumberFormatOptions, defaultValue = ''): string {
  if (value === undefined) return defaultValue;

  return i18n.number(value, options);
}
