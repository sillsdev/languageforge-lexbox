import { browser } from '$app/environment';
import { type Translater } from '$lib/i18n';
import { z, type ZodDefault, type ZodType } from 'zod';

export function randomFormId(): string {
  return crypto.randomUUID().split('-').at(-1) as string;
}
export function tryParse<T, ValidT>(zodType: ZodDefault<ZodType<ValidT>>, value: T): ValidT | undefined
export function tryParse<T, ValidT>(zodType: ZodType<ValidT>, value: T): ValidT | undefined
export function tryParse<T, ValidT>(zodType: ZodType<ValidT>, value: T): ValidT | undefined {
  const result = zodType.safeParse(value);
  return result.success ? result.data : undefined;
}

export function passwordFormRules($t: Translater): z.ZodString {
  return z.string()
    .min(4, $t('form.password.too_short'))
    .regex(/^[^&%:+]+$/, $t('form.password.forbidden_characters'));
}

export function emptyString(): z.ZodString {
  return z.string().length(0);
}

/**
 * Debouncind a refine serves 2 purposes:
 * 1) Debouncing, of course, to avoid (e.g.) making too many requests
 * 2) Ensuring the validation result is consistent. It seems that the last resolve doesn't always "win".
 * By making them all resolve to the same value, it doesn't matter which one wins.
 */
export function debouncedRefine<T>(refine: (value: T) => Promise<boolean>, debounceTime = 300): (value: T) => Promise<boolean> {
  // consumers don't expect debounced refines to get called server-side, but they sometimes do,
  // which can cause problems, e.g. calling fetch with a relative path kills the server
  if (!browser) return () => Promise.resolve(true);

  let timeout: ReturnType<typeof setTimeout>;
  let openResolves: ((value: boolean) => void)[] = [];
  return (value: T) => {
    clearTimeout(timeout);
    return new Promise((resolve) => {
      openResolves.push(resolve);
      timeout = setTimeout(() => {
        void refine(value).then((result) => {
          for (const openResolve of openResolves) openResolve(result);
          openResolves = [];
        });
      }, debounceTime);
    });
  };
}
