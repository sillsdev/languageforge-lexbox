import {ViewBase, type ICustomView} from '$lib/dotnet-types';
import {describe, expect, it} from 'vitest';

import {validateForm} from './validation';

const baseParams: ICustomView = {
  id: 'test-view',
  name: 'My View',
  base: ViewBase.FwLite,
  entryFields: [
    {fieldId: 'lexemeForm'},
    {fieldId: 'citationForm'},
  ],
  senseFields: [{fieldId: 'gloss'}],
  exampleFields: [],
  vernacular: [{wsId: 'en'}],
  analysis: [{wsId: 'fr'}],
};

describe('validateForm', () => {
  it('returns error for empty name', () => {
    const result = validateForm({...baseParams, name: '  '});
    expect(result).toBe('Name is required');
  });

  it('returns error when neither lexemeForm nor citationForm is selected', () => {
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

  it('passes when only lexemeForm is selected', () => {
    const result = validateForm({
      ...baseParams,
      entryFields: [
        {fieldId: 'lexemeForm'},
      ],
    });
    expect(result).toBeUndefined();
  });

  it('passes when only citationForm is selected', () => {
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

  it('allows vernacular to be undefined', () => {
    const result = validateForm({...baseParams, vernacular: undefined});
    expect(result).toBeUndefined();
  });

  it('allows analysis to be undefined', () => {
    const result = validateForm({...baseParams, analysis: undefined});
    expect(result).toBeUndefined();
  });

  it('returns undefined for valid input', () => {
    const result = validateForm(baseParams);
    expect(result).toBeUndefined();
  });
});
