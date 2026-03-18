import type {View} from './view-data';
import {ViewBase} from '$lib/dotnet-types';
import {derived} from 'svelte/store';
import {t} from 'svelte-i18n-lingui';

export type ViewText = string | {lite: string, classic: string};

export const translateViewText = derived(t, ($t) => (viewT: ViewText): ViewText => {
  if (typeof viewT === 'string') {
    return $t(viewT);
  }
  return {
    classic: $t(viewT.classic),
    lite: $t(viewT.lite),
  };
});

export function viewText(classicText: string, liteText?: string): ViewText {
  if (liteText) {
    return {lite: liteText, classic: classicText};
  }
  return classicText;
}

export function pickViewText(classicText: string, liteText: string, type: ViewBase | View): string
export function pickViewText(viewText: ViewText, type: ViewBase | View): string
export function pickViewText(viewText: ViewText, typeOrLite: string | View, typeOrNothing?: ViewBase | View): string {
  const base = pickViewBase(typeOrNothing ? typeOrNothing : typeOrLite as ViewBase | View);
  if (typeOrNothing) {
    return pickViewTextInternal(viewText as string, typeOrLite as string, base);
  } else if (typeof viewText !== 'string') {
    return pickViewTextInternal(viewText.classic, viewText.lite, base);
  }
  return viewText;
}

function pickViewTextInternal(classicText: string, liteText: string | undefined, base: ViewBase): string {
  if (liteText && base === ViewBase.FwLite) {
    return liteText;
  }
  return classicText;
}

function pickViewBase(type: ViewBase | View): ViewBase {
  if (typeof type === 'string') {
    return type;
  }
  return type.base;
}

export {
  viewText as vt,
  pickViewText as pt,
  translateViewText as tvt,
};
