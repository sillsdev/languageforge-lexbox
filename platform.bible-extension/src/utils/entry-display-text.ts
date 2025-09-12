import type { IEntry, IPartOfSpeech, ISemanticDomain, ISense } from 'fw-lite-extension';

export function domainText(domain: ISemanticDomain, lang = 'en'): string {
  return `${domain.code}: ${domain.name[lang] || domain.name.en}`;
}

export function entryGlossText(entry: IEntry, lang = 'en'): string {
  return entry.senses.map((s) => senseGlossText(s, lang)).join(' | ');
}

export function entryHeadwordText(entry: IEntry, lang = 'en'): string {
  return (
    entry.citationForm[lang] ||
    entry.lexemeForm[lang] ||
    Object.values(entry.citationForm).filter(Boolean)[0] ||
    Object.values(entry.lexemeForm).filter(Boolean)[0] ||
    ''
  );
}

export function partOfSpeechText(partOfSpeech: IPartOfSpeech, lang = 'en'): string {
  return partOfSpeech.name[lang] || partOfSpeech.name.en;
}

export function senseDefinitionText(sense: ISense, lang = 'en'): string {
  return sense.definition[lang] || Object.values(sense.definition).join('; ');
}

export function senseGlossText(sense: ISense, lang = 'en'): string {
  return sense.gloss[lang] || Object.values(sense.gloss).join('; ');
}
