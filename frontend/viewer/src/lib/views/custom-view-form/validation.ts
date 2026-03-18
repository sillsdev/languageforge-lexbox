/** Validation helpers for the custom-view form. */

import type { IViewField, IViewWritingSystem } from '$lib/dotnet-types';

import type { ViewText } from '../view-text';
import { gt } from 'svelte-i18n-lingui';

export function validateForm(params: {
  name: string;
  entryFields: IViewField[];
  senseFields: IViewField[];
  exampleFields: IViewField[];
  vernacular?: IViewWritingSystem[];
  analysis?: IViewWritingSystem[];
}): ViewText | undefined {
  if (!params.name.trim()) {
    return gt`Name is required`;
  }
  const hasHeadword = params.entryFields.some((f) => f.fieldId === 'lexemeForm' || f.fieldId === 'citationForm');
  if (!hasHeadword) {
    return {
      lite: gt`Word or Display as is required`,
      classic: gt`Lexeme form or Citation form is required`
    }
  }
  if (params.vernacular && params.vernacular.length === 0) {
    return gt`Select at least one vernacular writing system`;
  }
  if (params.analysis && params.analysis.length === 0) {
    return gt`Select at least one analysis writing system`;
  }
}
