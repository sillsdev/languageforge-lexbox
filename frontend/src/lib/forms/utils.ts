import { type Translater } from '$lib/i18n';
import { makeAsyncDebouncer } from '$lib/util/time';
import { writable, type Readable } from 'svelte/store';
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

export type LexboxFieldValidator = {
  validate: () => void;
  error: Readable<string | string[] | undefined>;
  valid: () => Promise<boolean>;
}


export function concatAll<T>(...values: (T | T[] | undefined)[]): T[] | undefined {
  let mergedResult: T[] | undefined;
  for (const value of values) {
    if (value === undefined) continue;
    mergedResult ??= [];
    mergedResult.push(...(Array.isArray(value) ? value : [value]));
  }
  return mergedResult;
}

export function buildFieldValidator<T extends z.ZodType>(schema: T, valueGetter: () => z.infer<T>, debounce: number | boolean = false): LexboxFieldValidator  {
  const error = writable<string | string[] | undefined>();

  const debouncer = makeAsyncDebouncer(
    (value: z.infer<T>) => schema.safeParseAsync(value),
    (result) => {
      if (result.success) {
        error.set(undefined);
      } else {
        error.set(result.error.flatten().formErrors);
      }
    }, debounce);

  return {
    validate: () => {
      void debouncer.debounce(valueGetter());
    },
    error: error as Readable<string | string[] | undefined>,
    valid: async () => {
      const result = await debouncer.flush(valueGetter());
      return result.success;
    }
  }
}
