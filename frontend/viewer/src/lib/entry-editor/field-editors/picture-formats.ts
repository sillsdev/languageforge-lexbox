// Single source of truth for the picture upload formats, shared by the add file picker
// (PicturesEditor) and the replace file picker (EditPictureDialog).

import {randomId} from '$lib/utils';

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

// iOS WebKit names every photo taken from the file picker's "Take Photo" option `image.jpg`. That
// name tells nobody anything, and every such photo would otherwise end up as image-1.jpg,
// image-2.jpg… in LinkedFiles, so give it a random name instead.
export function uniqueUploadFilename(filename: string): string {
  const match = /^image(\.jpe?g)$/i.exec(filename);
  return match ? `${randomId()}${match[1]}` : filename;
}
