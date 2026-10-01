import {describe, expect, it} from 'vitest';
import {generatedMediaFilename} from './media-filename';

const now = new Date(2026, 8, 28, 13, 57);

describe('generatedMediaFilename', () => {
  it('joins field, ws, timestamp and a short random suffix', () => {
    expect(generatedMediaFilename({field: 'lexemeForm', ws: 'sen'}, 'webm', now)).toMatch(
      /^lexemeForm_sen_20260928-1357_[0-9a-f]{4}\.webm$/,
    );
  });

  it('strips the FLEx audio tag from the ws', () => {
    expect(generatedMediaFilename({field: 'gloss', ws: 'en-Zxxx-x-audio'}, 'wav', now)).toMatch(/^gloss_en_/);
  });

  it('skips missing parts and a leading dot on the extension', () => {
    expect(generatedMediaFilename({field: 'picture'}, '.jpg', now)).toMatch(/^picture_20260928-1357_[0-9a-f]{4}\.jpg$/);
  });

  it('gives each call a different name', () => {
    expect(generatedMediaFilename({}, 'jpg', now)).not.toBe(generatedMediaFilename({}, 'jpg', now));
  });
});
