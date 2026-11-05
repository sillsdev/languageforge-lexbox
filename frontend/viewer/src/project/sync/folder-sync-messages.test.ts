import {describe, expect, it} from 'vitest';
import {shortFolderPath} from './folder-sync-messages';

describe('shortFolderPath', () => {
  it('keeps a short path as is', () => {
    expect(shortFolderPath(String.raw`E:\sena-3`)).toBe(String.raw`E:\sena-3`);
  });

  it('keeps the drive and the project folder of a long path', () => {
    expect(shortFolderPath(String.raw`C:\Users\someone\Documents\Dictionary work\shared\sena-3`))
      .toBe(String.raw`C:\…\sena-3`);
  });

  it('handles forward slashes', () => {
    expect(shortFolderPath('/media/someone/USB DRIVE/dictionary projects/sena-3')).toBe('/…/sena-3');
  });
});
