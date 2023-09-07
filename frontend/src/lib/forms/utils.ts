import type { Translater } from '$lib/i18n';
import { z, type ZodDefault, type ZodType } from 'zod';

export function randomFieldId(): string {
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
    .regex(/^[a-zA-Z0-9-]+$/, $t('form.password.allowed_characters'));
}
