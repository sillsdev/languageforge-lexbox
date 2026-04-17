export enum WebViewType {
  AddWord = 'lexicon-add-word.react',
  DictionarySelect = 'lexicon-dictionary-select.react',
  FindRelatedWords = 'lexicon-find-related-words.react',
  FindWord = 'lexicon-find-word.react',
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
