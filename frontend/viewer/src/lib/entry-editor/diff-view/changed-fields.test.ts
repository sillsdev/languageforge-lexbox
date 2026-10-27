import {describe, it, expect} from 'vitest';
import {showChangedFields, valueChanged} from './changed-fields';

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

  it('shows a hidden field with content on a new entry', () => {
    expect(showChangedFields(fields, undefined, {publishIn: [{id: 'p'}], literalMeaning: {}}).map((f) => f.show))
      .toEqual([true, false, true]);
  });
});
