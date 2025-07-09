import {describe, expect, it} from 'vitest';

import {normalizeDuration} from './format-duration';

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

  describe('normalization with smallestUnit = "hours"', () => {
    it('should return only hours', () => {
      const result = normalizeDuration({
        hours: 2,
        minutes: 30,
        seconds: 45,
        milliseconds: 500
      }, 'hours');

      // 2 + 30/60 + 45/3600 + 500/3600000 = 2 + 0.5 + 0.0125 + 0.0001388... ≈ 2.5126
      expect(result.hours).toBeCloseTo(2.5126, 4);
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
        minutes: 30.75
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
        seconds: 45.5 // 45 + 500/1000
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
