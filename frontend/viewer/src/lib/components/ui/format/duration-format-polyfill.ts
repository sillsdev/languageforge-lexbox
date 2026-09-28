import {DurationFormat} from '@formatjs/intl-durationformat';

/**
 * WebKit's native Intl.DurationFormat (Safari, iOS/macOS WKWebView) mangles digital style when leading units
 * are hidden, e.g. 4.43s renders as ":, 04.43" instead of "04.43". The stock formatjs polyfill only installs
 * when Intl.DurationFormat is missing, so probe the native one and replace it if it gets this wrong.
 */
export function durationFormatWorks(ctor: typeof Intl.DurationFormat | undefined): boolean {
  if (typeof ctor !== 'function') return false;
  const formatter = new ctor('en', {
    style: 'digital',
    hoursDisplay: 'auto',
    minutesDisplay: 'auto',
    secondsDisplay: 'always',
    fractionalDigits: 2,
  });
  return formatter.format({seconds: 4, milliseconds: 430}) === '04.43';
}

if (!durationFormatWorks(Intl.DurationFormat)) {
  Object.defineProperty(Intl, 'DurationFormat', {value: DurationFormat, configurable: true, writable: true});
}
