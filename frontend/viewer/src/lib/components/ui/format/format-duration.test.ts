import {describe, expect, it} from 'vitest';

import {normalizeDuration, formatDuration} from './format-duration';

describe('normalizeDuration', () => {
  describe('basic normalization without smallestUnit', () => {
    it('should normalize a simple duration with all units', () => {
      const result = normalizeDuration({
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      });

      expect(result).toEqual({
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      });
    });

    it('should handle missing units by treating them as 0', () => {
      const result = normalizeDuration({
        hours: 1,
        seconds: 30
      });

      expect(result).toEqual({
        hours: 1,
        minutes: 0,
        seconds: 30,
        milliseconds: 0
      });
    });

    it('should normalize overflow values correctly', () => {
      const result = normalizeDuration({
        minutes: 90,  // 1 hour 30 minutes
        seconds: 120, // 2 minutes
        milliseconds: 2500 // 2 seconds 500 milliseconds
      });

      expect(result).toEqual({
        hours: 1,
        minutes: 32,
        seconds: 2,
        milliseconds: 500
      });
    });

    it('should handle large milliseconds values', () => {
      const result = normalizeDuration({
        milliseconds: 7265500 // 2 hours, 1 minute, 5 seconds, 500 milliseconds
      });

      expect(result).toEqual({
        hours: 2,
        minutes: 1,
        seconds: 5,
        milliseconds: 500
      });
    });

    it('should handle zero duration', () => {
      const result = normalizeDuration({});

      expect(result).toEqual({
        hours: 0,
        minutes: 0,
        seconds: 0,
        milliseconds: 0
      });
    });
  });

  //this prevents the exception `RangeError: 500.23 isn't a valid milliseconds duration because it isn't an integer`
  it('should floor fractional milliseconds', () => {
    const result = normalizeDuration({
      milliseconds: 500.23
    });
    expect(result).toEqual({
      hours: 0,
      minutes: 0,
      seconds: 0,
      milliseconds: 500
    });
  });

  describe('normalization with smallestUnit = "hours"', () => {
    it('should return only hours', () => {
      const result = normalizeDuration({
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      }, 'hours');

      expect(result.hours).toEqual(2)
    });
  });

  describe('normalization with smallestUnit = "minutes"', () => {
    it('should return only hours and minutes', () => {
      const result = normalizeDuration({
        hours: 1,
        minutes: 30,
        seconds: 45,
        milliseconds: 0
      }, 'minutes');

      expect(result).toEqual({
        hours: 1,
        minutes: 30
      });
    });
  });

  describe('normalization with smallestUnit = "seconds"', () => {
    it('should only return hours, minutes, and seconds', () => {
      const result = normalizeDuration({
        hours: 1,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      }, 'seconds');

      expect(result).toEqual({
        hours: 1,
        minutes: 30,
        seconds: 45
      });
    });
  });

  describe('normalization with smallestUnit = "milliseconds"', () => {
    it('should return all units including milliseconds', () => {
      const result = normalizeDuration({
        hours: 1,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      }, 'milliseconds');

      expect(result).toEqual({
        hours: 1,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      });
    });
  });
});

describe('formatDuration', () => {
  it('formats durations', () => {
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500})).toEqual('1 hr, 30 min, 45 sec, 500 ms');
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45})).toEqual('1 hr, 30 min, 45 sec');
    expect(formatDuration({hours: 1, minutes: 30})).toEqual('1 hr, 30 min');
    expect(formatDuration({hours: 1})).toEqual('1 hr');
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
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'seconds')).toEqual('1 hr, 30 min, 45 sec');
    expect(formatDuration({hours: 1, minutes: 30, seconds: 45, milliseconds: 500}, 'milliseconds')).toEqual('1 hr, 30 min, 45 sec, 500 ms');
  });
});
