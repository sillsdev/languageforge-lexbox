/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import type { IComplexFormComponent } from './i-complex-form-component';

export class ComplexFormComponent implements IComplexFormComponent {
  constructor(id: string, complexFormEntryId: string, componentEntryId: string) {
    this.id = id;
    this.complexFormEntryId = complexFormEntryId;
    this.componentEntryId = componentEntryId;
  }
  id: string;
  complexFormEntryId: string;
  complexFormHeadword?: string;
  componentEntryId: string;
  componentSenseId?: string;
  componentHeadword?: string;
}
