import {locale} from 'svelte-i18n-lingui';
import {fromStore} from 'svelte/store';

const currentLocale = fromStore(locale);
type Duration = {
  hours: number,
  minutes: number,
  seconds: number,
  milliseconds: number,
};
export function formatDuration(value: Partial<Duration>, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds', options?: {
  style?: 'long' | 'short' | 'narrow' | 'digital',
  hoursDisplay?: 'auto' | 'always',
  minutesDisplay?: 'auto' | 'always',
  secondsDisplay?: 'auto' | 'always',
  millisecondsDisplay?: 'auto' | 'always'
}) {

  // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment,@typescript-eslint/no-unsafe-call
  const formatter = new Intl.DurationFormat(currentLocale.current, options);//has been polyfilled in main.ts
  // eslint-disable-next-line @typescript-eslint/no-unsafe-return,@typescript-eslint/no-unsafe-call,@typescript-eslint/no-unsafe-member-access
  return formatter.format(normalizeDuration(value, smallestUnit));
}

export function normalizeDuration(value: Partial<Duration>, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds'): Partial<Duration>
export function normalizeDuration(value: Partial<Duration>, smallestUnit?: 'seconds'): Omit<Duration, 'milliseconds'>
export function normalizeDuration(value: Partial<Duration>): Duration
export function normalizeDuration(value: Partial<Duration>, smallestUnit?: 'hours' | 'minutes' | 'seconds' | 'milliseconds'): Partial<Duration> {
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
