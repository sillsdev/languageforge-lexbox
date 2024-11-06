/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { IComplexFormComponent } from './complex-form-component';
import type { ComplexFormType } from './complex-form-type';
import { type IMultiString } from './i-multi-string';
import { type ISense } from './i-sense';

export interface IEntry {
    id: string;
    lexemeForm: IMultiString;
    citationForm: IMultiString;
    complexForms: IComplexFormComponent[];
    complexFormTypes: ComplexFormType[];
    components: IComplexFormComponent[];
    literalMeaning: IMultiString;
    senses: ISense[];
    note: IMultiString;
}
