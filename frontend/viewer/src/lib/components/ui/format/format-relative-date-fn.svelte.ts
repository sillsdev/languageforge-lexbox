import {SvelteDate} from 'svelte/reactivity';
import {formatDuration, type SmallestUnit} from './format-duration';
import {fromStore} from 'svelte/store';
import {gt} from 'svelte-i18n-lingui';
import {locale} from 'svelte-i18n-lingui';

const currentLocale = fromStore(locale);
type Config = {
  defaultValue: string;
  now: Date;
  maxUnits?: number;
  smallestUnit?: SmallestUnit;
};

export function formatRelativeDate(
  value: Date | string | undefined | null,
  options?: Intl.DurationFormatOptions,
  config: Config = {defaultValue: '', now: new SvelteDate()},
): string {
  if (!value) return config.defaultValue;
  void currentLocale.current; // invalidate when the current locale changes

  const targetDate = typeof value === 'string' ? new SvelteDate(value) : value;
  const diffMs = targetDate.getTime() - config.now.getTime();
  const isPast = diffMs <= 0;
  const absDiffMs = Math.abs(diffMs);

  const duration = formatDuration({milliseconds: absDiffMs}, config.smallestUnit, options, config.maxUnits);
  // DurationFormat omits zero fields, so diffs < smallestUnit format as ""
  if (!duration) {
    // digital style has to stay numeric, so force-display a zero there
    if (options?.style !== 'digital') return gt`just now`;
    const unit = config.smallestUnit ?? 'seconds';
    const zero = formatDuration({[unit]: 0}, unit, {...options, [`${unit}Display`]: 'always'});
    return isPast ? gt`${zero} ago` : gt`in ${zero}`;
  }

  return isPast ? gt`${duration} ago` : gt`in ${duration}`;
}
