export enum WebViewType {
  AddWord = 'fw-lite-add-word.react',
  DictionarySelect = 'fw-lite-dictionary-select.react',
  FindRelatedWords = 'fw-lite-find-related-words.react',
  FindWord = 'fw-lite-find-word.react',
  Main = 'fw-lite-extension.react',
}

export enum ProjectSettingKey {
  FwDictionaryCode = 'fw-lite-extension.fwDictionaryCode',
  ProjectLanguage = 'platform.language',
  ProjectName = 'platform.name',
}

/** https://alirezanet.github.io/Gridify/guide/filtering#conditional-operators */
export enum GridifyConditionalOperator {
  Contains = '=*',
  EndsWith = '$',
  Equal = '=',
  GreaterThan = '>',
  GreaterThanOrEqual = '>=',
  LessThan = '<',
  LessThanOrEqual = '<=',
  // eslint-disable-next-line @typescript-eslint/no-duplicate-enum-values
  Like = '=*', // same as Contains
  NotContains = '!*',
  NotEndsWith = '!$',
  NotEqual = '!=',
  // eslint-disable-next-line @typescript-eslint/no-duplicate-enum-values
  NotLike = '!*', // same as NotContains
  NotStartsWith = '!^',
  StartsWith = '^',
}
