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
export enum GridifyConditionalOperatorForUrl {
  Contains = '%3D%2A', // =*
  EndsWith = '%24', // $
  Equal = '%3D', // =
  GreaterThan = '%3E', // >
  GreaterThanOrEqual = '%3E%3D', // >=
  LessThan = '%3C', // <
  LessThanOrEqual = '%3C%3D', // <=
  // eslint-disable-next-line @typescript-eslint/no-duplicate-enum-values
  Like = '%3D%2A', // =*, same as Contains
  NotContains = '%21%2A', // !*
  NotEndsWith = '%21%24', // !$
  NotEqual = '%21%3D', // !=
  // eslint-disable-next-line @typescript-eslint/no-duplicate-enum-values
  NotLike = '%21%2A', // !*, same as NotContains
  NotStartsWith = '%21%5E', // !^
  StartsWith = '%5E', // ^
}
