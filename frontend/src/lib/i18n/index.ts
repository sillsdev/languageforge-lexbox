import type enType from './locales/en.json';
import en from '$locales/en';
import {get} from "svelte/store";
import {init, addMessages, waitLocale, getLocaleFromNavigator, t as translate} from 'svelte-intl-precompile';
import type {NestedKeyOf} from "$lib/type.utils";

export async function loadI18n() {
    addMessages('en', en);
    init({
        fallbackLocale: 'en',
        initialLocale: getLocaleFromNavigator() || 'en',
    });
    await waitLocale();
}

type InterpolationValues = Record<string, string | number | Date>;
export default function t(key: NestedKeyOf<typeof enType>, values?: InterpolationValues): string {
    return get(translate).call(null, {
        id: key,
        values
    });
}
