export type FwliteStoryParameters = {
  themePicker?: boolean,
  viewPicker?: boolean,
  resizable?: boolean,
  showValue?: boolean,
  value?: unknown,
};

export function fwliteStoryParameters(params: Partial<FwliteStoryParameters>): {fwlite: FwliteStoryParameters} {
  return {fwlite: params};
}
