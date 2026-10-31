import {describe, it, expect} from 'vitest';
import {changedFieldIds, changesOnly, showChangedFields, sortedIds, valueChanged} from './changed-fields';

describe('valueChanged', () => {
  it('treats empty values as missing', () => {
    expect(valueChanged(undefined, {})).toBe(false);
    expect(valueChanged({en: ''}, undefined)).toBe(false);
    expect(valueChanged([], undefined)).toBe(false);
    expect(valueChanged({en: {spans: []}}, {})).toBe(false);
  });

  it('compares rich text as plain text', () => {
    expect(valueChanged({en: {spans: [{text: 'a', ws: 'en'}]}}, {en: {spans: [{text: 'a', ws: 'en', bold: 'On'}]}})).toBe(false);
    expect(valueChanged({en: {spans: [{text: 'a'}]}}, {en: {spans: [{text: 'b'}]}})).toBe(true);
  });

  it('ignores key order', () => {
    expect(valueChanged({en: 'a', fr: 'b'}, {fr: 'b', en: 'a'})).toBe(false);
  });

  it('sees added, removed and reordered list items', () => {
    expect(valueChanged(undefined, [{id: '1'}])).toBe(true);
    expect(valueChanged([{id: '1'}, {id: '2'}], [{id: '2'}, {id: '1'}])).toBe(true);
  });
});

describe('showChangedFields', () => {
  const fields = [
    {fieldId: 'lexemeForm' as const, show: true},
    {fieldId: 'literalMeaning' as const, show: false},
    {fieldId: 'publishIn' as const, show: false},
  ];

  it('shows a hidden field only when its value changed', () => {
    const before = {literalMeaning: {en: {spans: [{text: 'old'}]}}, publishIn: [{id: 'p'}]};
    const after = {literalMeaning: {en: {spans: [{text: 'new'}]}}, publishIn: [{id: 'p'}]};
    expect(showChangedFields(fields, before, after).map((f) => f.show)).toEqual([true, true, false]);
  });

  it('compares a field the way compareAs reduces it', () => {
    const before = {publishIn: [{id: 'p', name: {en: 'Old'}}, {id: 'q', name: {}}]};
    const after = {publishIn: [{id: 'q', name: {}}, {id: 'p', name: {en: 'New'}}]};
    expect(showChangedFields(fields, before, after).map((f) => f.show)).toEqual([true, false, true]);
    expect(showChangedFields(fields, before, after, {publishIn: sortedIds}).map((f) => f.show)).toEqual([true, false, false]);
    expect(showChangedFields(fields, before, {publishIn: [{id: 'p', name: {}}]}, {publishIn: sortedIds}).map((f) => f.show))
      .toEqual([true, false, true]);
  });

  it('shows a hidden field with content on a new entry', () => {
    expect(showChangedFields(fields, undefined, {publishIn: [{id: 'p'}], literalMeaning: {}}).map((f) => f.show))
      .toEqual([true, false, true]);
  });
});

describe('changedFieldIds', () => {
  const fields = [
    {fieldId: 'lexemeForm' as const, show: true},
    {fieldId: 'note' as const, show: true},
    {fieldId: 'publishIn' as const, show: false},
  ];

  it('lists the changed fields, shown or not', () => {
    const before = {lexemeForm: {en: 'a'}, note: {en: {spans: [{text: 'n'}]}}, publishIn: [{id: 'p'}]};
    const after = {lexemeForm: {en: 'b'}, note: {en: {spans: [{text: 'n'}]}}, publishIn: []};
    expect([...changedFieldIds(fields, before, after)]).toEqual(['lexemeForm', 'publishIn']);
  });

  it('ignores formatting-only rich text edits', () => {
    const before = {note: {en: {spans: [{text: 'n', ws: 'en'}]}}};
    const after = {note: {en: {spans: [{text: 'n', ws: 'en', bold: 'On'}]}}};
    expect(changedFieldIds(fields, before, after).size).toBe(0);
  });

  it('compares a field the way compareAs reduces it', () => {
    const before = {publishIn: [{id: 'p', name: 'Old'}, {id: 'q'}]};
    const after = {publishIn: [{id: 'q'}, {id: 'p', name: 'New'}]};
    expect([...changedFieldIds(fields, before, after)]).toEqual(['publishIn']);
    expect(changedFieldIds(fields, before, after, {publishIn: sortedIds}).size).toBe(0);
  });

  it('sees every non-empty field of a new or deleted entity as changed', () => {
    const entity = {lexemeForm: {en: 'a'}, note: {}, publishIn: [{id: 'p'}]};
    expect([...changedFieldIds(fields, undefined, entity)]).toEqual(['lexemeForm', 'publishIn']);
    expect([...changedFieldIds(fields, entity, undefined)]).toEqual(['lexemeForm', 'publishIn']);
  });
});

describe('changesOnly', () => {
  const fields = [
    {fieldId: 'lexemeForm' as const, show: true},
    {fieldId: 'citationForm' as const, show: true},
    {fieldId: 'note' as const, show: true},
    {fieldId: 'publishIn' as const, show: false},
  ];

  it('hides the unchanged fields and counts the ones with a value', () => {
    const after = {lexemeForm: {en: 'b'}, citationForm: {en: 'c'}, note: {}, publishIn: [{id: 'p'}]};
    const {fields: shown, unchanged} = changesOnly(fields, new Set(['lexemeForm'] as const), after);
    expect(shown.map((f) => f.show)).toEqual([true, false, false, false]);
    // citationForm: the empty note and the field the view hides aren't worth revealing
    expect(unchanged).toBe(1);
  });
});
