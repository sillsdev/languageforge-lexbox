import {fromStore} from 'svelte/store';
import {locale} from 'svelte-i18n-lingui';

const currentLocale = fromStore(locale);
type Duration = Pick<Intl.DurationLike, 'hours' | 'minutes' | 'seconds' | 'milliseconds'>;

export function formatDuration(value: Duration, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds', options?: Intl.DurationFormatOptions) {
  const formatter = new Intl.DurationFormat(currentLocale.current, options);//has been polyfilled in main.ts
  return formatter.format(normalizeDuration(value, smallestUnit));
}

export function normalizeDuration(value: Duration, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds'): Duration
export function normalizeDuration(value: Duration, smallestUnit: 'seconds'): Omit<Duration, 'milliseconds'>
export function normalizeDuration(value: Duration): Duration
export function normalizeDuration(value: Duration, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds'): Duration {
  let distanceMs = (value.hours ?? 0) * 3600000 + (value.minutes ?? 0) * 60000 + (value.seconds ?? 0) * 1000 + (value.milliseconds ?? 0);
  const hours = Math.floor(distanceMs / 3600000);
  if (smallestUnit === 'hours') return {hours};
  distanceMs -= hours * 3600000;
  const minutes = Math.floor(distanceMs / 60000);
  if (smallestUnit === 'minutes') return {hours, minutes};
  distanceMs -= minutes * 60000;
  const seconds = Math.floor(distanceMs / 1000);
  if (smallestUnit === 'seconds') return {hours, minutes, seconds};
  distanceMs -= seconds * 1000;
  const milliseconds = Math.floor(distanceMs);
  return {hours, minutes, seconds, milliseconds};
}
