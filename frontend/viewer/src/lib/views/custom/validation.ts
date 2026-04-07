import type {ICustomView} from '$lib/dotnet-types';
import type {ViewText} from '../view-text';
import {gt} from 'svelte-i18n-lingui';

export function validateForm(view: ICustomView): ViewText | undefined {
  if (!view.name.trim()) {
    return gt`Name is required`;
  }
  const hasHeadword = view.entryFields.some((f) => f.fieldId === 'lexemeForm' || f.fieldId === 'citationForm');
  if (!hasHeadword) {
    return {
      lite: gt`Word or Display as is required`,
      classic: gt`Lexeme form or Citation form is required`
    }
  }
  if (view.vernacular && view.vernacular.length === 0) {
    return gt`Select at least one vernacular writing system`;
  }
  if (view.analysis && view.analysis.length === 0) {
    return gt`Select at least one analysis writing system`;
  }
}
