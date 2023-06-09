import type { ZodDefault, ZodType } from 'zod';

export function randomFieldId(): string {
  return crypto.randomUUID().split('-').at(-1) as string;
}

export function tryParse<T, ValidT>(zodType: ZodDefault<ZodType<ValidT>>, value: T, callback?: (parsedValue: ValidT) => void): ValidT | undefined
export function tryParse<T, ValidT>(zodType: ZodType<ValidT>, value: T, callback?: (parsedValue: ValidT) => void): ValidT | undefined
export function tryParse<T, ValidT>(zodType: ZodType<ValidT>, value: T, callback?: (parsedValue: ValidT) => void): ValidT | undefined {
  const result = zodType.safeParse(value);
  if (result.success) {
    callback?.(result.data);
    return result.data;
  }
  return undefined;
}
