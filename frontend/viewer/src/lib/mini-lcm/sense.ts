/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import {type ISense} from './i-sense';
import {type IMultiString} from './i-multi-string';
import {type IExampleSentence} from './i-example-sentence';

export class Sense implements ISense {

  constructor(id: string) {
    this.id = id;
  }

  id: string;
  definition: IMultiString = {};
  gloss: IMultiString = {};
  partOfSpeech: string = '';
  semanticDomain: string[] = [];
  exampleSentences: IExampleSentence[] = [];
}
