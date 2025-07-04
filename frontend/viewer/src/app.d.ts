export { }; // for some reason this is required in order to make global changes

declare global {
  function enableDevMode(): void;
  function enableShadcn(enable = true): void;

  // waiting on: https://github.com/microsoft/TypeScript/issues/60608
  // has been polyfilled in main.ts
  namespace Intl {
    interface DurationFormatOptions {
      style?: 'long' | 'short' | 'narrow' | 'digital';
      hoursDisplay?: 'auto' | 'always';
      minutesDisplay?: 'auto' | 'always';
      secondsDisplay?: 'auto' | 'always';
      millisecondsDisplay?: 'auto' | 'always';
      microsecondsDisplay?: 'auto' | 'always';
      nanosecondsDisplay?: 'auto' | 'always';
      fractionalDigits?: number;
    }

    interface DurationLike {
      years?: number;
      months?: number;
      weeks?: number;
      days?: number;
      hours?: number;
      minutes?: number;
      seconds?: number;
      milliseconds?: number;
      microseconds?: number;
      nanoseconds?: number;
    }

    class DurationFormat {
      constructor(locales?: string | string[], options?: DurationFormatOptions);
      format(duration: DurationLike): string;
      formatToParts(duration: DurationLike): Array<{
        type: string;
        value: string;
      }>;
      resolvedOptions(): Required<DurationFormatOptions> & {
        locale: string;
        numberingSystem: string;
      };
    }
  }
}
