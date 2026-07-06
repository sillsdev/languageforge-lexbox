import {describe, expect, it} from 'vitest';
import {entityFieldIds} from './entity-config';

describe('entityFieldIds', () => {
  it('exposes the variant fields to custom views', () => {
    // custom-view field toggles enumerate these ids; dropping one silently removes it from the dialog
    expect(entityFieldIds('entry')).toEqual(expect.arrayContaining(['variantOf', 'variants']));
  });
});
