import type {View, ViewType} from './view-data';

export type ViewText = string | {lite: string, classic: string};

export function viewText(classicText: string, liteText?: string): ViewText {
  if (liteText) {
    return {lite: liteText, classic: classicText};
  }
  return classicText;
}

export function pickViewText(classicText: string, liteText: string, type: ViewType | View): string
export function pickViewText(viewText: ViewText, type: ViewType | View): string
export function pickViewText(viewText: ViewText, typeOrLite: string | View, typeOrNothing?: ViewType | View): string {
  const type = pickViewType(typeOrNothing ? typeOrNothing : typeOrLite as ViewType | View);
  if (typeOrNothing) {
    return pickViewTextInternal(viewText as string, typeOrLite as string, type);
  } else if (typeof viewText === 'string') {
    return viewText;
  }
  return pickViewTextInternal(viewText.classic, viewText.lite, type);
}

function pickViewTextInternal(classicText: string, liteText: string | undefined, type: ViewType): string {
  if (liteText && type === 'fw-lite') {
    return liteText;
  }
  return classicText;
}

function pickViewType(type: ViewType | View): ViewType {
  if (typeof type === 'string') {
    return type;
  }
  return type.type;
}

export {
  viewText as vt,
  pickViewText as pt,
};
