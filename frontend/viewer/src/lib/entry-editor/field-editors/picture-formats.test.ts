import {describe, expect, it} from 'vitest';
import {uniqueUploadFilename} from './picture-formats';

describe('uniqueUploadFilename', () => {
  it.each(['image.jpg', 'image.jpeg', 'IMAGE.JPG'])('replaces the generic iOS camera name %s', (name) => {
    const result = uniqueUploadFilename(name);
    expect(result).not.toBe(name);
    expect(result).toMatch(/^[0-9a-f-]{36}\.jpe?g$/i);
    expect(result.slice(36)).toBe(name.slice(5));
  });

  it('gives each capture a different name', () => {
    expect(uniqueUploadFilename('image.jpg')).not.toBe(uniqueUploadFilename('image.jpg'));
  });

  it.each(['IMG_0076.jpeg', 'image.png', 'my-image.jpg', 'image.jpg.png'])('keeps other names like %s', (name) => {
    expect(uniqueUploadFilename(name)).toBe(name);
  });
});
