// Single source of truth for the picture upload formats, shared by the add file picker
// (PicturesEditor) and the replace file picker (EditPictureDialog).

// Formats the browser accepts and that the server supports for pictures.
export const ACCEPTED_PICTURE_TYPES = 'image/jpeg,image/png,image/tiff,image/bmp';

// The server rejects files above its size limit. JPEGs can usually be shrunk by lowering the
// export quality, whereas lossless formats (PNG, BMP, TIFF) need a smaller resolution instead.
export function isLosslessImage(file: File): boolean {
  return /^image\/(png|bmp|tiff)$/.test(file.type) || /\.(png|bmp|tiff?)$/i.test(file.name);
}
