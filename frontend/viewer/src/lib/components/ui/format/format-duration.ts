import {fromStore} from 'svelte/store';
import {locale} from 'svelte-i18n-lingui';
import '@formatjs/intl-durationformat/polyfill';

const currentLocale = fromStore(locale);
type Duration = Pick<Intl.DurationLike, 'days' | 'hours' | 'minutes' | 'seconds' | 'milliseconds'>;

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
  let distanceMs = value.days ? value.days * msPerHour * 24 : 0;
  distanceMs += (value.hours ?? 0) * msPerHour + (value.minutes ?? 0) * msPerMinute + (value.seconds ?? 0) * msPerSecond + (value.milliseconds ?? 0);
  let days = distanceMs / (msPerHour * 24);
  days = Math.floor(days);
  distanceMs -= days * msPerHour * 24;
  let hours = distanceMs / msPerHour;
  hours = Math.floor(hours);
  if (smallestUnit === 'hours') return {days, hours};
  distanceMs -= hours * msPerHour;
  let minutes = distanceMs / msPerMinute;
  minutes = Math.floor(minutes);
  if (smallestUnit === 'minutes') return {days, hours, minutes};
  distanceMs -= minutes * msPerMinute;
  let seconds = distanceMs / msPerSecond;
  seconds = Math.floor(seconds);
  if (smallestUnit === 'seconds') return {days, hours, minutes, seconds};
  distanceMs -= seconds * msPerSecond;
  const milliseconds = Math.floor(distanceMs);
  return {days, hours, minutes, seconds, milliseconds};
}
