import {describe, expect, it} from 'vitest';

import type {IViewField, IViewWritingSystem} from '$lib/dotnet-types';
import {validateForm} from './validation';

const baseParams = {
  name: 'My View',
  entryFields: [
    {fieldId: 'lexemeForm'},
    {fieldId: 'citationForm'},
  ] as IViewField[],
  senseFields: [{fieldId: 'gloss'}] as IViewField[],
  exampleFields: [] as IViewField[],
  vernacular: [{wsId: 'en'}] as IViewWritingSystem[],
  analysis: [{wsId: 'fr'}] as IViewWritingSystem[],
};

describe('validateForm', () => {
  it('returns error for empty name', () => {
    const result = validateForm({...baseParams, name: '  '});
    expect(result).toBe('Name is required');
  });

  it('returns error when neither lexemeForm nor citationForm is present', () => {
    const result = validateForm({
      ...baseParams,
      entryFields: [
        {fieldId: 'note'},
      ],
    });
    expect(result).toEqual({
      lite: 'Word or Display as is required',
      classic: 'Lexeme form or Citation form is required',
    });
  });

  it('passes when only citationForm is present', () => {
    const result = validateForm({
      ...baseParams,
      entryFields: [
        {fieldId: 'citationForm'},
      ],
    });
    expect(result).toBeUndefined();
  });

  it('allows sense and example fields to be empty', () => {
    const result = validateForm({
      ...baseParams,
      senseFields: [],
      exampleFields: [],
    });
    expect(result).toBeUndefined();
  });

  it('returns error for custom vernacular with no selected items', () => {
    const result = validateForm({...baseParams, vernacular: []});
    expect(result).toBe('Select at least one vernacular writing system');
  });

  it('returns error for custom analysis with no selected items', () => {
    const result = validateForm({...baseParams, analysis: []});
    expect(result).toBe('Select at least one analysis writing system');
  });

  it('returns undefined for valid input', () => {
    const result = validateForm(baseParams);
    expect(result).toBeUndefined();
  });
});
