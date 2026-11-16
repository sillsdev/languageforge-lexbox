import {describe, expect, it} from 'vitest';
import {generatedMediaFilename} from './media-filename';

const now = new Date(2026, 8, 28, 13, 57);

describe('generatedMediaFilename', () => {
  it('joins field, ws, timestamp and a short random suffix', () => {
    expect(generatedMediaFilename({field: 'lexemeForm', ws: 'sen'}, 'webm', now)).toMatch(
      /^lexemeForm_sen_20260928-1357_[0-9a-f]{8}\.webm$/,
    );
  });

  it('pads single-digit months, days, hours and minutes', () => {
    expect(generatedMediaFilename({field: 'gloss'}, 'wav', new Date(2026, 0, 5, 9, 5))).toMatch(/^gloss_20260105-0905_/);
  });

  it.each(['en-Zxxx-x-audio', 'en-zxxx-x-audio', 'en-Zxxx-x-audio-var'])('strips the FLEx audio tag from %s', (ws) => {
    expect(generatedMediaFilename({field: 'gloss', ws}, 'wav', now)).toMatch(/^gloss_en(-var)?_20260928-1357_[0-9a-f]{8}\.wav$/);
  });

  it('leaves a ws that merely resembles the audio tag alone', () => {
    expect(generatedMediaFilename({field: 'gloss', ws: 'en-Latn-x-audiophile'}, 'wav', now)).toMatch(
      /^gloss_en-Latn-x-audiophile_/,
    );
  });

  it('skips missing parts and a leading dot on the extension', () => {
    expect(generatedMediaFilename({field: 'picture'}, '.jpg', now)).toMatch(/^picture_20260928-1357_[0-9a-f]{8}\.jpg$/);
  });

  it('still starts with the timestamp when nothing identifies the field', () => {
    expect(generatedMediaFilename({}, 'jpg', now)).toMatch(/^20260928-1357_[0-9a-f]{8}\.jpg$/);
  });

  it('gives each call a different name', () => {
    expect(generatedMediaFilename({}, 'jpg', now)).not.toBe(generatedMediaFilename({}, 'jpg', now));
  });
});
