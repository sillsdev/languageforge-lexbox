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

export function isEmail(value: string): boolean {
  return !!tryParse(z.string().email(), value);
}
