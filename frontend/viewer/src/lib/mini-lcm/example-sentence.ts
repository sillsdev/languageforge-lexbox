/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { IExampleSentence } from "./i-example-sentence";
import { IMultiString } from "./i-multi-string";

export class ExampleSentence implements IExampleSentence {
    id: string;
    sentence: IMultiString = {"values":{}};
    translation: IMultiString = {"values":{}};
    reference: string = "";
}
