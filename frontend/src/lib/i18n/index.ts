/* eslint-disable @typescript-eslint/no-unsafe-argument */
/* eslint-disable @typescript-eslint/ban-ts-comment */

import type { DeepPathsToType, DeepPaths, DeepPathsToString, StoreType } from '$lib/type.utils';
import { addMessages, getLocaleFromNavigator, init, register, t as translate, waitLocale } from 'svelte-intl-precompile';

import type I18n from '../i18n/locales/en.json';
import { derived, type Readable } from 'svelte/store';
// @ts-ignore there's an error here because this is a synthetic path
import en from '$locales/en';
import type { Get } from 'type-fest';

export async function loadI18n(): Promise<void> {
  addMessages('en', en);
  //dynamically load the es translation at runtime if the user's browser is set to spanish
  // @ts-ignore there's an error here because this is a synthetic path
  register('es', () => import('$locales/es'));
  init({
    fallbackLocale: 'en',
    initialLocale: getLocaleFromNavigator() || 'en',
  });
  await waitLocale();
}

type InterpolationValues = Record<string, string | number | Date>;
const t = derived(translate, tFunc => {
  return (key: string, values?: InterpolationValues) => tFunc({
    id: key,
    values
  });
});
export default t;

export type Translater = StoreType<typeof t>;

export function tScoped<Scope extends I18nScope>(scope: Scope): Readable<(key: DeepPathsToString<I18nShape<Scope>>, values?: InterpolationValues) => string> {
  // I can't quite figure out why this needs to be cast
  return tTypeScoped<I18nShape<Scope>>(scope as I18nShapeKey<I18nShape<Scope>>);
}

export function tTypeScoped<Shape extends object>(scope: I18nShapeKey<Shape>): Readable<(key: DeepPathsToString<Shape>, values?: InterpolationValues) => string> {
  return derived(t, tFunc => (key: DeepPathsToString<Shape>, values?: InterpolationValues) =>
    tFunc(`${String(scope)}.${String(key)}`, values));
}

type I18nKey = DeepPaths<typeof I18n>;
type I18nScope = DeepPathsToType<typeof I18n, I18nKey, object>;
type I18nShape<Scope extends I18nScope> = Get<typeof I18n, Scope>;
export type I18nShapeKey<Shape> = DeepPathsToType<typeof I18n, I18nKey, Shape>;
