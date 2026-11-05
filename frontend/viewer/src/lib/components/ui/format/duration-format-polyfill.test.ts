import {describe, expect, it} from 'vitest';
import {DurationFormat} from '@formatjs/intl-durationformat';

import {durationFormatWorks} from './duration-format-polyfill';

describe('durationFormatWorks', () => {
  it('accepts the formatjs polyfill', () => {
    expect(durationFormatWorks(DurationFormat as unknown as typeof Intl.DurationFormat)).toBe(true);
  });

  it('rejects a missing implementation', () => {
    expect(durationFormatWorks(undefined)).toBe(false);
  });

  it('rejects an implementation with the WebKit digital-style bug', () => {
    class WebKitLikeDurationFormat {
      format() {
        return ':, 04.43';
      }
    }
    expect(durationFormatWorks(WebKitLikeDurationFormat as unknown as typeof Intl.DurationFormat)).toBe(false);
  });

  it('leaves Intl.DurationFormat producing correct digital output', () => {
    const formatter = new Intl.DurationFormat('en', {
      style: 'digital',
      hoursDisplay: 'auto',
      minutesDisplay: 'auto',
      secondsDisplay: 'always',
      fractionalDigits: 2,
    });
    expect(formatter.format({seconds: 4, milliseconds: 430})).toBe('04.43');
  });
});
