// Single source of truth for the picture upload formats, shared by the add file picker
// (PicturesEditor) and the replace file picker (EditPictureDialog).

import {generatedMediaFilename} from '$lib/utils/media-filename';

// Formats the browser accepts and that the server supports for pictures.
export const ACCEPTED_PICTURE_TYPES = 'image/jpeg,image/png,image/tiff,image/bmp';

// The server rejects files above its size limit. JPEGs can usually be shrunk by lowering the
// export quality, whereas lossless formats (PNG, BMP, TIFF) need a smaller resolution instead.
export function isLosslessImage(file: File): boolean {
  return /^image\/(png|bmp|tiff)$/.test(file.type) || /\.(png|bmp|tiff?)$/i.test(file.name);
}

export function isSupportedImageType(file: File): boolean {
  return /^image\/(jpeg|png|tiff|bmp)$/.test(file.type) || /\.(jpe?g|png|bmp|tiff?)$/i.test(file.name);
}

export function generatedPictureFilename(extension: string): string {
  return generatedMediaFilename({field: 'picture'}, extension);
}

const PICTURE_EXTENSION_BY_TYPE: Record<string, string> = {
  'image/jpeg': 'jpg',
  'image/png': 'png',
  'image/tiff': 'tiff',
  'image/bmp': 'bmp',
};

// A camera capture's reported name may carry no extension at all, and its content type is the
// authoritative answer anyway, so only fall back to the name when the type isn't one we accept.
export function pictureExtension(contentType: string, filename: string): string {
  return PICTURE_EXTENSION_BY_TYPE[contentType.toLowerCase()] ?? /\.([^.\\/]+)$/.exec(filename)?.[1] ?? 'jpg';
}

// iOS WebKit names every photo taken from the file picker's "Take Photo" option `image.jpg`, and
// saved files are deduplicated by filename, so a second photo would silently reuse the first one.
export function uniqueUploadFilename(filename: string): string {
  const match = /^image\.(jpe?g)$/i.exec(filename);
  return match ? generatedPictureFilename(match[1]) : filename;
}
