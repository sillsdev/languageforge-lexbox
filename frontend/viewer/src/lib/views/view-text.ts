import type {ViewType} from './view-data';

export type ViewText = string | {lite: string, classic: string};

export function viewText(classicText: string, liteText?: string): ViewText {
  if (liteText) {
    return {lite: liteText, classic: classicText};
  }
  return classicText;
}

export function pickViewText(classicText: string, liteText: string, type: ViewType): string
export function pickViewText(viewText: ViewText, type: ViewType): string
export function pickViewText(viewText: ViewText, typeOrLite: string, typeOrNothing?: ViewType): string {
  if (typeOrNothing) {
    return pickViewTextInternal(viewText as string, typeOrLite, typeOrNothing);
  } else if (typeof viewText === 'string') {
    return viewText;
  }
  return pickViewTextInternal(viewText.classic, viewText.lite, typeOrLite as ViewType);
}

function pickViewTextInternal(classicText: string, liteText: string | undefined, type: ViewType): string {
  if (liteText && type === 'fw-lite') {
    return liteText;
  }
  return classicText;
}

export {
  viewText as vt,
  pickViewText as pt,
};
