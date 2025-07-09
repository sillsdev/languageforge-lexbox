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
  const msPerHour = 3_600_000;
  const msPerMinute = 60_000;
  const msPerSecond = 1_000;
  let distanceMs = (value.hours ?? 0) * msPerHour + (value.minutes ?? 0) * msPerMinute + (value.seconds ?? 0) * msPerSecond + (value.milliseconds ?? 0);
  let hours = distanceMs / msPerHour;
  if (smallestUnit === 'hours') return {hours};
  hours = Math.floor(hours);
  distanceMs -= hours * msPerHour;
  let minutes = distanceMs / msPerMinute;
  if (smallestUnit === 'minutes') return {hours, minutes};
  minutes = Math.floor(minutes);
  distanceMs -= minutes * msPerMinute;
  let seconds = distanceMs / msPerSecond;
  if (smallestUnit === 'seconds') return {hours, minutes, seconds};
  seconds = Math.floor(seconds);
  distanceMs -= seconds * msPerSecond;
  const milliseconds = distanceMs;
  return {hours, minutes, seconds, milliseconds};
}
