import type {IExampleSentence, ISense} from '$lib/dotnet-types';
import {ids, sortedIds} from './changed-fields';

/** How each diff reduces a field to what it renders, shared with anything that must agree on "changed". */
export const entryCompareAs = {complexFormTypes: ids, publishIn: sortedIds};

export const senseCompareAs = {
  semanticDomains: sortedIds,
  pictures: (pictures: ISense['pictures']) => pictures.map(({id, mediaUri, caption}) => ({id, mediaUri, caption})),
};

export const exampleCompareAs = {
  translations: (translations: IExampleSentence['translations']) => Object.fromEntries(translations.map((tr) => [tr.id, tr.text])),
};
