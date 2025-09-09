import { IEntry, IPartOfSpeech, ISemanticDomain, ISense } from 'fw-lite-extension';

export function domainText(domain: ISemanticDomain, lang = 'en'): string {
  return `${domain.code}: ${domain.name[lang] || domain.name.en}`;
}

export function entryGlossText(entry: IEntry): string {
  return entry.senses
    .map((s) => Object.values(s.gloss).filter(Boolean).join('; '))
    .filter(Boolean)
    .join(' | ');
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

export function senseDefinitionText(sense: ISense): string {
  return JSON.stringify(sense.definition);
}

export function senseGlossText(sense: ISense): string {
  return JSON.stringify(sense.gloss);
}
