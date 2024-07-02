/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { type IMultiString } from './i-multi-string';
import { type IExampleSentence } from './i-example-sentence';
import type { SemanticDomain } from './semantic-domain';

export interface ISense {
    id: string;
    definition: IMultiString;
    gloss: IMultiString;
    partOfSpeechId: string;
    semanticDomains: SemanticDomain[];
    exampleSentences: IExampleSentence[];
}
