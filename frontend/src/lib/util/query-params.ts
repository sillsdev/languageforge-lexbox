import type {StandardEnum, StringifyValues} from '$lib/type.utils';
import {createSearchParamsSchema, type SearchParamsOptions, useSearchParams} from 'runed/kit';

import type {ConditionalPick} from 'type-fest';
import type {PrimitiveRecord} from './types';

// The phantom `T` only carries the value type (`UserType`, etc.) through to
// `getSearchParams<T>`; runed's runtime just sees 'string'/'boolean'/'number'.
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
 * Build a URL-backed reactive params object (via runed's `useSearchParams`) plus a
 * plain-object snapshot of the defaults. Mutate it directly
 * (`queryParamValues.userSearch = 'abc'`); reads are $state-backed, so `bind:value`
 * and `$derived` both work. Unset params are always `undefined`.
 */
export function getSearchParams<T extends Record<string, unknown>>(
  config: ParamConfig<T>,
  options?: SearchParamsOptions,
): QueryParams<T> {
  // `as any`: ParamSpec carries a phantom `T` for typing only; runed wants its exact
  // SchemaTypeConfig discriminated union, which doesn't accept the wider phantom-bearing shape.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const schema = createSearchParamsSchema(config as any);
  const params = useSearchParams(schema, {
    pushHistory: false,
    noScroll: true,
    ...options,
  });

  // runed is asymmetric about unset params: reads return `null`, but only a write of
  // `undefined` unsets one (a written `null` gets stringified to "null"). Adapt both
  // directions to `undefined` so consumers and their `X | undefined` types never see that.
  const raw = params as Record<string, unknown>;
  const queryParamValues = {} as T;
  const defaults: Partial<T> = {};
  for (const key in config) {
    defaults[key] = config[key].default as T[typeof key];
    Object.defineProperty(queryParamValues, key, {
      enumerable: true,
      get: () => raw[key] ?? undefined,
      set: (value: unknown) => {
        raw[key] = value ?? undefined;
      },
    });
  }

  return {
    queryParamValues,
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
