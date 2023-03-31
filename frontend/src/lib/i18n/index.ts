import type enType from './locales/en.json';

// @ts-ignore there's an error here because this is a synthetic path
import en from '$locales/en';
import { derived } from "svelte/store";
import { init, addMessages, waitLocale, getLocaleFromNavigator, t as translate, register } from 'svelte-intl-precompile';
import type { NestedKeyOf } from "$lib/type.utils";

export async function loadI18n() {
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
    return (key: NestedKeyOf<typeof enType>, values?: InterpolationValues) => tFunc({
        id: key,
        values
    });
});
export default t;
