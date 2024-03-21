/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { IMultiString } from "./i-multi-string";
import { ISense } from "./i-sense";

export interface IEntry {
    id: string;
    lexemeForm: IMultiString;
    citationForm: IMultiString;
    literalMeaning: IMultiString;
    senses: ISense[];
    note: IMultiString;
}
