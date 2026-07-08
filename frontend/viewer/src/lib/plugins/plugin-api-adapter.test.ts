import {describe, expect, it} from 'vitest';
import {computeHeadword, toGridifyFilter} from './plugin-api-adapter';
import {PluginApiException} from './plugin-api-types';
import {type IEntry, type IWritingSystem, MorphTypeKind} from '$lib/dotnet-types';

const POS_ID = '86ff66f6-0774-407a-a0dc-3eeaf873daf7';

function ws(wsId: string, isAudio = false): IWritingSystem {
  return {wsId, isAudio} as unknown as IWritingSystem;
}

function entry(fields: Partial<IEntry>): IEntry {
  return {lexemeForm: {}, citationForm: {}, morphType: MorphTypeKind.Stem, ...fields} as IEntry;
}

describe('toGridifyFilter', () => {
  it('returns undefined for no filter or an empty filter', () => {
    expect(toGridifyFilter(undefined)).toBeUndefined();
    expect(toGridifyFilter({})).toBeUndefined();
  });

  it('translates existing fields', () => {
    expect(toGridifyFilter({semanticDomainCode: '2.1.1'}))
      .toEqual({gridifyFilter: 'Senses.SemanticDomains.Code=2.1.1'});
    expect(toGridifyFilter({partOfSpeechId: POS_ID}))
      .toEqual({gridifyFilter: `Senses.PartOfSpeechId=${POS_ID}`});
  });

  it('translates missingGlossWs', () => {
    expect(toGridifyFilter({missingGlossWs: 'en'}))
      .toEqual({gridifyFilter: '(Senses=null|Senses.Gloss[en]=)'});
  });

  it('translates missingExampleWs', () => {
    expect(toGridifyFilter({missingExampleWs: 'seh-fonipa'}))
      .toEqual({gridifyFilter: '(Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[seh-fonipa]=)'});
  });

  it('translates missingPartOfSpeech only when true', () => {
    expect(toGridifyFilter({missingPartOfSpeech: true}))
      .toEqual({gridifyFilter: 'Senses.PartOfSpeechId='});
    expect(toGridifyFilter({missingPartOfSpeech: false})).toBeUndefined();
  });

  it('joins multiple conditions with AND', () => {
    expect(toGridifyFilter({semanticDomainCode: '1', missingGlossWs: 'en', missingPartOfSpeech: true}))
      .toEqual({gridifyFilter: 'Senses.SemanticDomains.Code=1,(Senses=null|Senses.Gloss[en]=),Senses.PartOfSpeechId='});
  });

  it('rejects invalid writing system codes', () => {
    expect(() => toGridifyFilter({missingGlossWs: 'en; DROP'})).toThrow(PluginApiException);
    expect(() => toGridifyFilter({missingGlossWs: '1en'})).toThrow(/writing system/);
    expect(() => toGridifyFilter({missingExampleWs: ''})).toThrow(PluginApiException);
  });

  it('rejects invalid semantic domain codes and part of speech ids', () => {
    expect(() => toGridifyFilter({semanticDomainCode: 'abc'})).toThrow(PluginApiException);
    expect(() => toGridifyFilter({partOfSpeechId: 'not-a-guid'})).toThrow(PluginApiException);
  });
});

describe('computeHeadword', () => {
  const vernacular = [ws('seh'), ws('seh-fonipa')];
  const suffixTokens = {[MorphTypeKind.Suffix]: {prefix: '-', postfix: undefined}};

  it('prefers the (undecorated) citation form of the default writing system', () => {
    const result = computeHeadword(
      entry({citationForm: {seh: 'nyumba'}, lexemeForm: {seh: 'yumba'}, morphType: MorphTypeKind.Suffix}),
      vernacular, suffixTokens);
    expect(result).toBe('nyumba');
  });

  it('decorates the lexeme form with morph-type affix tokens when there is no citation form', () => {
    const result = computeHeadword(
      entry({lexemeForm: {seh: 's'}, morphType: MorphTypeKind.Suffix}), vernacular, suffixTokens);
    expect(result).toBe('-s');
  });

  it('does not decorate a plain stem', () => {
    expect(computeHeadword(entry({lexemeForm: {seh: 'yumba'}}), vernacular, suffixTokens)).toBe('yumba');
  });

  it('skips audio writing systems so a media reference never becomes the headword', () => {
    const result = computeHeadword(
      entry({lexemeForm: {'seh-audio': 'audio-ref.wav', seh: 'yumba'}}),
      [ws('seh-audio', true), ws('seh')], suffixTokens);
    expect(result).toBe('yumba');
  });

  it('returns an empty string when nothing is set', () => {
    expect(computeHeadword(entry({}), vernacular, suffixTokens)).toBe('');
  });
});
