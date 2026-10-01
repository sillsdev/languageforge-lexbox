import {describe, expect, it} from 'vitest';
import {uniqueUploadFilename} from './picture-formats';

describe('uniqueUploadFilename', () => {
  it.each(['image.jpg', 'image.jpeg', 'IMAGE.JPG'])('replaces the generic iOS camera name %s', (name) => {
    const result = uniqueUploadFilename(name);
    expect(result).not.toBe(name);
    expect(result).toMatch(/^picture_\d{8}-\d{4}_[0-9a-f]{4}\.jpe?g$/i);
    expect(result.split('.').pop()).toBe(name.split('.').pop());
  });

  it('gives each capture a different name', () => {
    expect(uniqueUploadFilename('image.jpg')).not.toBe(uniqueUploadFilename('image.jpg'));
  });

  it.each(['IMG_0076.jpeg', 'image.png', 'my-image.jpg', 'image.jpg.png'])('keeps other names like %s', (name) => {
    expect(uniqueUploadFilename(name)).toBe(name);
  });
});
