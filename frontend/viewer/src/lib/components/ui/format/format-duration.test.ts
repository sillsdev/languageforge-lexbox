import {describe, expect, it} from 'vitest';

import {normalizeDuration, formatDuration} from './format-duration';

describe('normalizeDuration', () => {
  describe('basic normalization without smallestUnit', () => {
    it('should normalize a simple duration with all units', () => {
      const result = normalizeDuration({
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500,
      });

      expect(result).toEqual({
        days: 0,
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500,
      });
    });

    it('should handle missing units by treating them as 0', () => {
      const result = normalizeDuration({
        hours: 1,
        seconds: 30,
      });

      expect(result).toEqual({
        days: 0,
        hours: 1,
        minutes: 0,
        seconds: 30,
        milliseconds: 0,
      });
    });

    it('should normalize overflow values correctly', () => {
      const result = normalizeDuration({
        minutes: 90, // 1 hour 30 minutes
        seconds: 120, // 2 minutes
        milliseconds: 2500, // 2 seconds 500 milliseconds
      });

      expect(result).toEqual({
        days: 0,
        hours: 1,
        minutes: 32,
        seconds: 2,
        milliseconds: 500,
      });
    });

    it('should handle large milliseconds values', () => {
      const result = normalizeDuration({
        milliseconds: 7265500, // 2 hours, 1 minute, 5 seconds, 500 milliseconds
      });

      expect(result).toEqual({
        days: 0,
        hours: 2,
        minutes: 1,
        seconds: 5,
        milliseconds: 500,
      });
    });

    it('should handle zero duration', () => {
      const result = normalizeDuration({});

      expect(result).toEqual({
        days: 0,
        hours: 0,
        minutes: 0,
        seconds: 0,
        milliseconds: 0,
      });
    });
  });

  //this prevents the exception `RangeError: 500.23 isn't a valid milliseconds duration because it isn't an integer`
  it('should floor fractional milliseconds', () => {
    const result = normalizeDuration({
      milliseconds: 500.23,
    });
    expect(result).toEqual({
      days: 0,
      hours: 0,
      minutes: 0,
      seconds: 0,
      milliseconds: 500,
    });
  });

  describe('normalization with smallestUnit = "hours"', () => {
    it('should return only hours', () => {
      const result = normalizeDuration(
        {
          hours: 2,
          minutes: 30,
          seconds: 45,
          milliseconds: 500,
        },
        'hours',
      );

      expect(result.hours).toEqual(2);
    });
  });

  describe('normalization with smallestUnit = "hours"', () => {
    it('should return only hours', () => {
      const result = normalizeDuration(
        {
          hours: 2,
          minutes: 30,
          seconds: 45,
          milliseconds: 500,
        },
        'hours',
      );

      expect(result.hours).toEqual(2);
    });
  });

  describe('normalization with smallestUnit = "minutes"', () => {
    it('should return only hours and minutes', () => {
      const result = normalizeDuration(
        {
          hours: 1,
          minutes: 30,
          seconds: 45,
          milliseconds: 0,
        },
        'minutes',
      );

      expect(result).toEqual({
        days: 0,
        hours: 1,
        minutes: 30,
      });
    });
  });

  describe('normalization with smallestUnit = "seconds"', () => {
    it('should only return hours, minutes, and seconds', () => {
      const result = normalizeDuration(
        {
          hours: 1,
          minutes: 30,
          seconds: 45,
          milliseconds: 500,
        },
        'seconds',
      );

      expect(result).toEqual({
        days: 0,
        hours: 1,
        minutes: 30,
        seconds: 45,
      });
    });
  });

  describe('normalization with smallestUnit = "milliseconds"', () => {
    it('should return all units including milliseconds', () => {
      const result = normalizeDuration(
        {
          hours: 1,
          minutes: 30,
          seconds: 45,
          milliseconds: 500,
        },
        'milliseconds',
      );

      expect(result).toEqual({
        days: 0,
        hours: 1,
        minutes: 30,
        seconds: 45,
        milliseconds: 500,
      });
    });
  });
});

describe('formatDuration', () => {
  it('formats durations', () => {
    expect(formatDuration({days: 1, hours: 1, minutes: 30, seconds: 45, milliseconds: 500})).toEqual(
      '1 day, 1 hr, 30 min, 45 sec, 500 ms',
    );
    expect(formatDuration({days: 1, hours: 1, minutes: 30, seconds: 45})).toEqual('1 day, 1 hr, 30 min, 45 sec');
    expect(formatDuration({days: 1, hours: 1, minutes: 30})).toEqual('1 day, 1 hr, 30 min');
    expect(formatDuration({days: 1, hours: 1})).toEqual('1 day, 1 hr');
    expect(formatDuration({days: 1})).toEqual('1 day');
  });

  it('formats fractional durations', () => {
    expect(formatDuration({milliseconds: 500.23})).toEqual('500 ms');
    expect(formatDuration({seconds: 45.23})).toEqual('45 sec, 230 ms');
    expect(formatDuration({minutes: 30.23})).toEqual('30 min, 13 sec, 800 ms');
    expect(formatDuration({hours: 2.23})).toEqual('2 hr, 13 min, 48 sec');
  });

  it('formats durations with smallest unit', () => {
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'hours')).toEqual('1 hr');
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'minutes')).toEqual('1 hr, 30 min');
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'seconds')).toEqual(
      '1 hr, 30 min, 45 sec',
    );
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'milliseconds')).toEqual(
      '1 hr, 30 min, 45 sec, 500 ms',
    );
  });

  describe('maxUnits parameter', () => {
    it('should limit units for durations starting with days', () => {
      expect(
        formatDuration({days: 7, hours: 10, minutes: 17, seconds: 30, milliseconds: 500}, undefined, {}, 2),
      ).toEqual('7 days, 10 hr');
      expect(formatDuration({days: 1, hours: 2, minutes: 30}, undefined, {}, 1)).toEqual('1 day');
    });

    it('should limit units for durations starting with minutes', () => {
      expect(formatDuration({days: 0, hours: 0, minutes: 5, seconds: 30, milliseconds: 0}, undefined, {}, 2)).toEqual(
        '5 min, 30 sec',
      );
      expect(formatDuration({days: 0, hours: 0, minutes: 5, seconds: 30, milliseconds: 250}, undefined, {}, 1)).toEqual(
        '5 min',
      );
    });

    it('should limit units for durations starting with seconds', () => {
      expect(formatDuration({days: 0, hours: 0, minutes: 0, seconds: 45, milliseconds: 500}, undefined, {}, 2)).toEqual(
        '45 sec, 500 ms',
      );
      expect(formatDuration({days: 0, hours: 0, minutes: 0, seconds: 45, milliseconds: 500}, undefined, {}, 1)).toEqual(
        '45 sec',
      );
    });

    it('should limit units for durations starting with milliseconds', () => {
      expect(formatDuration({days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500}, undefined, {}, 1)).toEqual(
        '500 ms',
      );
    });

    it('should work with mixed scenarios', () => {
      expect(formatDuration({days: 0, hours: 0, minutes: 2, seconds: 15, milliseconds: 250}, undefined, {}, 3)).toEqual(
        '2 min, 15 sec, 250 ms',
      );
      expect(formatDuration({days: 0, hours: 1, minutes: 30, seconds: 45}, undefined, {}, 2)).toEqual('1 hr, 30 min');
    });

    it('should handle gaps where it starts with days, has no hours and has minutes', () => {
      // Duration: 1 day, 0 hours, 5 minutes - this creates a gap at hours
      const durationWithGap = {days: 1, hours: 0, minutes: 5, seconds: 0, milliseconds: 0};

      // When maxUnits is 2, it should only display days (since hours is 0, we skip to the next non-zero unit but hit the limit)
      expect(formatDuration(durationWithGap, undefined, {}, 2)).toEqual('1 day');

      // When maxUnits is 3, it should display days and minutes (skipping the empty hours)
      expect(formatDuration(durationWithGap, undefined, {}, 3)).toEqual('1 day, 5 min');

      // Additional gap scenarios
      expect(formatDuration({days: 2, hours: 0, minutes: 10, seconds: 0}, undefined, {}, 2)).toEqual('2 days');
      expect(formatDuration({days: 2, hours: 0, minutes: 10, seconds: 0}, undefined, {}, 3)).toEqual('2 days, 10 min');
      expect(formatDuration({days: 1, hours: 0, minutes: 0, seconds: 30}, undefined, {}, 3)).toEqual('1 day');
      expect(formatDuration({days: 1, hours: 0, minutes: 0, seconds: 30}, undefined, {}, 4)).toEqual('1 day, 30 sec');
    });
  });
});
