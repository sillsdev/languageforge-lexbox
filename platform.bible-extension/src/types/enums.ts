export enum WebViewType {
  AddWord = 'dictionary-add-word.react',
  DictionarySelect = 'dictionary-select.react',
  FindRelatedWords = 'dictionary-find-related-words.react',
  FindWord = 'dictionary-find-word.react',
  Main = 'lexicon.react',
}

export enum ProjectSettingKey {
  AnalysisLanguage = 'lexicon.analysisLanguage',
  DictionaryCode = 'lexicon.dictionaryCode',
  ProjectLanguage = 'platform.language',
  ProjectLanguageTag = 'platform.languageTag',
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
  Like = '=*', // same as Contains
  NotContains = '!*',
  NotEndsWith = '!$',
  NotEqual = '!=',
  NotLike = '!*', // same as NotContains
  NotStartsWith = '!^',
  StartsWith = '^',
}
