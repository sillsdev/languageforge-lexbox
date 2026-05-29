import type {StandardEnum, StringifyValues} from '$lib/type.utils';
import {createSearchParamsSchema, type SearchParamsOptions, useSearchParams} from 'runed/kit';

import type {ConditionalPick} from 'type-fest';
import type {PrimitiveRecord} from './types';

// `runed`'s createSearchParamsSchema input shape — kept here so call sites don't need to
// know about runed. `queryParam.X(default)` helpers below return entries that conform.
// The phantom `T` carries the value type (`UserType`, etc.) through to the returned proxy
// via `getSearchParams<T>`, even though runed's runtime only sees 'string'/'boolean'/'number'.
type ParamSpec<T> = (
  | {type: 'string'; default?: string | undefined}
  | {type: 'number'; default?: number | undefined}
  | {type: 'boolean'; default?: boolean | undefined}
) & {phantomT?: T};

type ParamConfig<T> = {[K in keyof T]: ParamSpec<T[K]>};

export type QueryParams<T> = {
  queryParamValues: T;
  defaultQueryParamValues: T;
};

export const queryParam = {
  string: <T extends string | undefined>(defaultValue: T) =>
    ({type: 'string', default: defaultValue}) as ParamSpec<T>,
  boolean: <T extends boolean | undefined>(defaultValue: T) =>
    ({type: 'boolean', default: defaultValue}) as ParamSpec<T>,
  number: <T extends number | undefined>(defaultValue: T) =>
    ({type: 'number', default: defaultValue}) as ParamSpec<T>,
};

/**
 * Build a URL-backed reactive params object via runed's `useSearchParams`.
 *
 * Returns the runed proxy (synchronous local-cache reads, debounced URL writes) plus a
 * separate plain-object snapshot of the defaults — components use the latter to decide
 * which filters count as "active".
 *
 * Call sites mutate the proxy directly: `queryParamValues.userSearch = 'abc'`. The proxy
 * is reactive ($state-backed under the hood), so `bind:value={queryParamValues.X}` and
 * `$derived(queryParamValues.X)` both work.
 */
export function getSearchParams<T extends Record<string, unknown>>(
  config: ParamConfig<T>,
  options?: SearchParamsOptions,
): QueryParams<T> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const schema = createSearchParamsSchema(config as any);
  // No runed-level debounce: URL writes are immediate. Per-input debouncing
  // (e.g. for a search input that drives server-side filtering) lives in
  // FilterBar/debouncedFilter, where it's explicit at the call site.
  const params = useSearchParams(schema, {
    pushHistory: false,
    noScroll: true,
    ...options,
  });

  const defaults: Partial<T> = {};
  for (const key in config) {
    defaults[key] = config[key].default as T[typeof key];
  }

  // runed's createSearchParamsSchema normalises `undefined` defaults to `null` on
  // reads (use-search-params.svelte.js:1016-1017). Our consumer code uses
  // `X | undefined` types and checks like `confidential === undefined`, so we
  // re-wrap reads to convert `null` back to `undefined`. Writes pass through
  // unchanged (proxy.set goes straight to runed's setter).
  const normalised = new Proxy(params, {
    get(target, prop, receiver) {
      const value = Reflect.get(target, prop, receiver);
      return value === null ? undefined : value;
    },
  });

  return {
    queryParamValues: normalised as unknown as T,
    defaultQueryParamValues: defaults as T,
  };
}

export function getSearchParamValues<T extends Record<string, unknown>>(): StringifyValues<T> {
  return Object.fromEntries(new URLSearchParams(location.search).entries()) as StringifyValues<T>;
}

/**
 * Converts a dictionary of any primitive-type values to a search params string.
 * new URLSearchParams(params) only allows string values, which seems unnecessarily strict, see e.g.:
 * https://github.com/microsoft/TypeScript-DOM-lib-generator/issues/1568#issuecomment-1587963141
 */
export function toSearchParams<T extends PrimitiveRecord>(params: T): string {
  //filter out null values
  const paramsWithoutNull = Object.fromEntries(Object.entries(params).filter(([_, v]) => v !== null));
  const searchParams = new URLSearchParams(paramsWithoutNull as unknown as Record<string, string>);
  return searchParams.toString();
}

/**
 * @param defaultValue The value to return if the specified parameter is `null`
 */
export function getBoolSearchParam<T extends PrimitiveRecord>(key: keyof ConditionalPick<T, boolean>, params: URLSearchParams, defaultValue = false): boolean {
  const value = getSearchParam<T, boolean>(key, params);
  if (value === true.toString()) {
    return true;
  } else if (value === false.toString()) {
    return false;
  } else {
    return defaultValue;
  }
}

export function getSearchParam<T extends PrimitiveRecord, R = string | undefined | null>(
  key: Exclude<keyof ConditionalPick<T, (R extends StandardEnum<unknown> ? R[keyof R] : R)>, number | symbol>,
  params: URLSearchParams): EnumOrString<R> | undefined {
  const value = params.get(key);
  return value as EnumOrString<R> | undefined;
}

type EnumOrString<R> = R extends StandardEnum<unknown> ? R
  : R extends (string | undefined) ? R
  : string;
