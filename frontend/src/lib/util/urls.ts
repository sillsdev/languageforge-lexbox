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
