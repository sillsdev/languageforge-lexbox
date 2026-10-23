import {describe, expect, it} from 'vitest';
import {pictureExtension, uniqueUploadFilename} from './picture-formats';

describe('pictureExtension', () => {
  it('prefers the content type over the reported name', () => {
    expect(pictureExtension('image/png', 'shot.jpg')).toBe('png');
  });

  it('falls back to the name for a type we do not accept', () => {
    expect(pictureExtension('application/octet-stream', 'shot.heic')).toBe('heic');
  });

  it('falls back to jpg when the name carries no extension', () => {
    expect(pictureExtension('application/octet-stream', 'capture')).toBe('jpg');
  });
});

describe('uniqueUploadFilename', () => {
  it.each(['image.jpg', 'image.jpeg', 'IMAGE.JPG'])('replaces the generic iOS camera name %s', (name) => {
    const result = uniqueUploadFilename(name);
    expect(result).not.toBe(name);
    expect(result).toMatch(/^picture_\d{8}-\d{4}_[0-9a-f]{8}\.(jpe?g|JPE?G)$/);
    expect(result.split('.').pop()).toBe(name.split('.').pop());
  });

  it('gives each capture a different name', () => {
    expect(uniqueUploadFilename('image.jpg')).not.toBe(uniqueUploadFilename('image.jpg'));
  });

  it.each(['IMG_0076.jpeg', 'image.png', 'my-image.jpg', 'image.jpg.png'])('keeps other names like %s', (name) => {
    expect(uniqueUploadFilename(name)).toBe(name);
  });
});
