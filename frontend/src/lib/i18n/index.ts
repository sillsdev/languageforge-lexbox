import type { DeepPathsToType, DeepPaths, DeepPathsToString } from '$lib/type.utils';
// eslint-disable-next-line no-restricted-imports
import { date as _date, number as _number, getLocaleFromAcceptLanguageHeader, getLocaleFromNavigator, init, t as translate, waitLocale } from 'svelte-intl-precompile';

import type I18nShape from '../i18n/locales/en.json';
import {
  derived,
  writable,
  type Readable,
  type Writable,
  type Unsubscriber,
  type Subscriber,
  type Invalidator
} from 'svelte/store';
import { availableLocales, registerAll } from '$locales';
import type { Get } from 'type-fest';
import { defineContext } from '$lib/util/context';
import { browser } from '$app/environment';

export function getLanguageCodeFromNavigator(): string | undefined {
  // Keep the language code. Discard the country code.
  return getLocaleFromNavigator()?.split('-')[0];
}

export function pickBestLocale(userLocale?: string, acceptLanguageHeader?: string | null): string {
  if (userLocale) return userLocale;
  if (acceptLanguageHeader) {
    // replaceAll works around: https://github.com/cibernox/precompile-intl-runtime/issues/45
    const acceptLanguageLocale = getLocaleFromAcceptLanguageHeader(acceptLanguageHeader.replaceAll(' ', ''), availableLocales);
    if (acceptLanguageLocale) return acceptLanguageLocale;
  }
  return getLanguageCodeFromNavigator() ?? 'en';
}

let loaded = false;

/**
 * Initializes our i18n dependency. Also ensures that necessary translations are loaded.
 * @param locale The active locale that needs to be loaded.
 */
export async function loadI18n(locale?: string): Promise<void> {
  if (!loaded) {
    registerAll();
    init({ fallbackLocale: 'en' });
    if (!browser) await Promise.all(availableLocales.map(waitLocale));
    loaded = true;
  }

  if (browser) {
    await waitLocale(locale); // ensure the current locale (which can change) is loaded
  }
}

type InterpolationValues = Record<string, string | number | Date>;

export const LOCALE_CONTEXT_KEY = Symbol('i18n-locale');

function buildI18n(localeStore: Readable<string>): I18n {
  return derived([translate, localeStore], ([tFunc, locale]) => {
    return (key: I18nKey | I18nScope, valuesOrSubPath?: InterpolationValues | string, _values?: InterpolationValues) => {
      let id: string = key;
      let values = _values;

      if (typeof valuesOrSubPath === 'string') {
        id = `${key}.${valuesOrSubPath}`;
      } else {
        values = valuesOrSubPath;
      }

      return tFunc({
        id,
        values,
        locale,
      });
    }
  });
}

const { use: useLocale, init: initLocale } = defineContext<Readable<string>, [Readable<string>]>(
  (userLocale: Readable<string>) => userLocale,
  { key: LOCALE_CONTEXT_KEY });

// We have to return what we do, because the calling page can't $t the t below, because that triggers a subscription on it before it's ready.
export function initI18n(locale: string): { t: I18n, locale: Writable<string> } {
  const localeStore = writable(locale);
  return { t: buildI18n(initLocale(localeStore)), locale: localeStore };
}

export const locale: Readable<string> = {
  subscribe(run: Subscriber<string>, invalidate?: Invalidator<string>): Unsubscriber {
    return useLocale().subscribe(run, invalidate);
  }
};

const t: I18n = buildI18n(locale);
export default t;

const NULL_LABEL = '–';

export const number = withLocale(_number, (numberFunc, value, options) => numberFunc(Number(value), options));

export const date = withLocale(_date, (dateFunc, value, options) => dateFunc(new Date(value), {
  dateStyle: 'short',
  timeStyle: 'short',
  ...options,
}));

function withLocale<T, O extends { locale?: string, nullLabel?: string }>(
  store: Readable<(value: T, options?: O) => string>,
  formatter: (func: (value: T, options?: O) => string, value: T | string, options: O) => string
): Readable<(value: T | string | null | undefined, options?: O & { nullLabel?: string }) => string> {
  return derived([store, locale], ([storeFunc, locale]) =>
    (value: T | string | null | undefined, options?: O & { nullLabel?: string }) => {
      if (value === null || value === undefined) {
        return options?.nullLabel ?? NULL_LABEL;
      } else {
        return formatter(storeFunc, value, {...options, locale} as O);
      }
    }
  );
}

export function tScoped<Scope extends I18nScope>(scope: Scope): Readable<(key: DeepPathsToString<I18nScopedShape<Scope>>, values?: InterpolationValues) => string> {
  // I can't quite figure out why this needs to be cast
  return tTypeScoped<I18nScopedShape<Scope>>(scope as I18nShapeKey<I18nScopedShape<Scope>>);
}

export function tTypeScoped<Shape extends object>(scope: I18nShapeKey<Shape>): Readable<(key: DeepPathsToString<Shape>, values?: InterpolationValues) => string> {
  return derived(t, tFunc => (key: DeepPathsToString<Shape>, values?: InterpolationValues) =>
    tFunc(`${String(scope)}.${String(key)}` as I18nKey, values));
}

export type Translater = {
  (scope: I18nScope, subPath: string, values?: InterpolationValues): string;
  (key: I18nKey, values?: InterpolationValues): string;
}
type I18n = Readable<Translater>;
export type I18nKey = DeepPaths<typeof I18nShape>;
type I18nScope = DeepPathsToType<typeof I18nShape, I18nKey, object>;
type I18nScopedShape<Scope extends I18nScope> = Get<typeof I18nShape, Scope>;
export type I18nShapeKey<Shape> = DeepPathsToType<typeof I18nShape, I18nKey, Shape>;
