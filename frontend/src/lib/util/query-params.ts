import { queryParameters, ssp } from 'sveltekit-search-params';

import type { ConditionalPick } from 'type-fest';
import type { EncodeAndDecodeOptions } from 'sveltekit-search-params/sveltekit-search-params';
import type { PrimitiveRecord } from './types';
import type { StandardEnum } from '$lib/type.utils';
import type { Writable } from 'svelte/store';

// Require default values
type QueryParamOptions<T> = Required<EncodeAndDecodeOptions<T>>;
type QueryParamsOptions<T> = { [Key in keyof T]: QueryParamOptions<T[Key]> };

// A more type-smart version of ssp that requires defaults to be provided
export const queryParam = ssp as {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  [Property in keyof typeof ssp]: (typeof ssp)[Property] extends (defaultValue: any) => EncodeAndDecodeOptions<infer P>
  ? <V extends P | undefined>(defaultValue: V) => QueryParamOptions<
    NonNullable<V> extends never // default is undefined, that's obviously too specific to be the parameter type, so we hand onto P
    ? P | V : V>
  : (typeof ssp)[Property] extends typeof ssp.array
  ? typeof ssp.array // special case we can worry about if we ever need it
  : never;
}

export function getSearchParams<T extends Record<string, unknown>>(options: QueryParamsOptions<T>)
  : { queryParams: Writable<T>, defaultQueryParams: T } {
  const defaultValues = getDefaults(options);
  for (const key in options) {
    const { encode, decode, defaultValue } = options[key];
    // Teach the encoder to exclude defaults from the URL
    options[key].encode = (value) => value === defaultValue ? undefined : encode.call(options[key], value);
    // Teach the decoder to return defaults
    options[key].decode = (urlValue) => {
      const value = decode.call(options[key], urlValue);
      return value === null ? defaultValue : value;
    };
    // get rid of defaults, so the sveltekit-search-params lib gives us an object right away
    delete (options[key] as EncodeAndDecodeOptions<T[typeof key]>).defaultValue;
  }

  return {
    queryParams: queryParameters<T>(options, { pushHistory: false }),
    defaultQueryParams: defaultValues,
  }
}

function getDefaults<T extends Record<string, unknown>>(
  options: QueryParamsOptions<T>): T {
  const defaultValues: Partial<T> = {};
  for (const key in options) {
    const option = options[key];
    defaultValues[key] = option.defaultValue;
  }
  return defaultValues as T;
}

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
 * @param defaultValue The value to return if the specified parameter is `null`
 */
export function getBoolSearchParam<T extends PrimitiveRecord>(key: keyof ConditionalPick<T, boolean> & string, params: URLSearchParams, defaultValue = false): boolean {
  const value = getSearchParam<T, boolean>(key, params);
  if (value === true.toString()) {
    return true;
  } else if (value === false.toString()) {
    return false;
  } else {
    return defaultValue;
  }
}

export function getSearchParam<T extends PrimitiveRecord, R>(
  key: keyof ConditionalPick<T, (R extends StandardEnum<unknown> ? R[keyof R] : R)> & string,
  params: URLSearchParams,
  defaultValue?: EnumOrString<R>): EnumOrString<R> | undefined {
  const value = params.get(key);
  return value ? value as EnumOrString<R> | undefined : defaultValue;
}

type EnumOrString<R> = R extends StandardEnum<unknown> ? R : string;
