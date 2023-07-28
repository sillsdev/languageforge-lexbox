import type { ConditionalPick } from 'type-fest';
import type { PrimitiveRecord } from './types';

/**
 * Converts a dictionary of any primitive-type values to a search params string.
 * new URLSearchParams(params) only allows string values, which seems unnecessarily strict, see e.g.:
 * https://github.com/microsoft/TypeScript-DOM-lib-generator/issues/1568#issuecomment-1587963141
 */
export function toSearchParams(params: PrimitiveRecord): string {
  const searchParams = new URLSearchParams(params as unknown as Record<string, string>);
  return searchParams.toString();
}

/**
 * @param defaultValue The value to return of the specified parameter is `null`
 */
export function getBoolSearchParam<T extends PrimitiveRecord>(key: keyof ConditionalPick<T, boolean | null> & string, params: URLSearchParams, defaultValue = false): boolean {
  const value = params.get(key);
  if (value === true.toString()) {
    return true;
  } else if (value === false.toString()) {
    return false;
  } else {
    return defaultValue;
  }
}
