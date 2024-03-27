import { buildRegionalLocaleRegex, pickBestLocale } from '.';
import { describe, expect, it } from 'vitest';

describe('buildRegionalLocaleRegex', () => {
  it('should find all regional locales for available locales', () => {
    const acceptLanguageHeader = 'en;q=0.9,fr-CA;q=0.8,fr;q=0.7,de;q=0.6,en-US   ;q=0.5,en-GB,es-419,ja-JP';

    let supportedRegionalLocales = buildRegionalLocaleRegex(['en', 'es', 'de']);
    let matchedLocales = [...acceptLanguageHeader.matchAll(supportedRegionalLocales)].map(match => match[0]);
    expect(matchedLocales).toEqual(['en-US', 'en-GB', 'es-419']);

    supportedRegionalLocales = buildRegionalLocaleRegex(['fr', 'es']);
    matchedLocales = [...acceptLanguageHeader.matchAll(supportedRegionalLocales)].map(match => match[0]);
    expect(matchedLocales).toEqual(['fr-CA', 'es-419']);
  });
});

describe('pickBestLocale', () => {
  it('should return en by default', () => {
    expect(pickBestLocale()).toBe('en');
  });

  it('should return user locale if acceptLanguageHeader is not provided', () => {
    expect(pickBestLocale('fr')).toBe('fr');
  });

  it('should return user locale if acceptLanguageHeader does not provide a regional/more specific locale', () => {
    expect(pickBestLocale('fr', 'en,en-GB,es')).toBe('fr');
  });

  it('should return user locale if acceptLanguageHeader does not provide a regional/more specific locale with a higher quality rating', () => {
    expect(pickBestLocale('fr', 'fr,fr-CA;q=0.8,en-GB')).toBe('fr');
  });

  it('should return regional locale from acceptLanguageHeader if it has a higher quality rating than the regionless locale', () => {
    expect(pickBestLocale('fr', 'fr-CA,fr;q=0.8,en-GB')).toBe('fr-CA');
  });

  it('should return supported locale from acceptLanguageHeader with highest quality rating if no user locale is provided', () => {
    expect(pickBestLocale(undefined, 'fr-CA,fr;q=0.8,en-GB')).toBe('fr-CA');
  });

  it('should return en if no user locale is provided and acceptLanguageHeader does not have any supported locales', () => {
    expect(pickBestLocale(undefined, 'ja-JP')).toBe('en');
  });
});
