/* eslint-disable @typescript-eslint/no-unsafe-argument */
/* eslint-disable @typescript-eslint/ban-ts-comment */
import { addMessages, getLocaleFromNavigator, init, register, t as translate, waitLocale } from 'svelte-intl-precompile';

import { derived } from 'svelte/store';
// @ts-ignore there's an error here because this is a synthetic path
import en from '$locales/en';

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
