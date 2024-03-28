/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { type ISense } from './i-sense';
import { type IMultiString } from './i-multi-string';
import { type IExampleSentence } from './i-example-sentence';

export class Sense implements ISense {
    id: string;
    definition: IMultiString = {'values':{}};
    gloss: IMultiString = {'values':{}};
    partOfSpeech: string = '';
    semanticDomain: string[] = [];
    exampleSentences: IExampleSentence[] = [];
}
