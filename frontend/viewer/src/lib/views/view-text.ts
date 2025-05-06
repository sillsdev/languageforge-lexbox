export type ViewText = string | {lite: string, classic: string};

export function viewText(classicText: string, liteText?: string): ViewText {
  if (liteText) {
    return {lite: liteText, classic: classicText};
  }
  return classicText;
}

export {viewText as vt};

export function pickViewText(viewText: ViewText, type: 'fw-lite' | 'fw-classic'): string {
  if (typeof viewText === 'string') {
    return viewText;
  }
  if (type === 'fw-lite') {
    return viewText.lite;
  }
  return viewText.classic;
}
