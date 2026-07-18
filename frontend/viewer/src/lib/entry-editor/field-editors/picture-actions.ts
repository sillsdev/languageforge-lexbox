import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';

export type DownloadPictureResult = {success: true} | {success: false; errorMessage?: string};

/**
 * Downloads the image behind a picture's mediaUri, saved under the filename the media server
 * reports for it. Shared by the picture field, the edit dialog, and the fullscreen viewer so the
 * "Download" action behaves identically wherever it's offered.
 *
 * getFileStream reports failure via the response (not an exception), so the caller decides how to
 * surface `errorMessage` — this keeps the notification/translation in the calling component.
 */
export async function downloadPicture(api: IMiniLcmJsInvokable, mediaUri: string): Promise<DownloadPictureResult> {
  const file = await api.getFileStream(mediaUri);
  if (!file.stream) return {success: false, errorMessage: file.errorMessage ?? undefined};
  const blob = await new Response(await file.stream.stream()).blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = file.fileName ?? 'picture';
  anchor.click();
  // Release the object URL on the next tick, once the browser has captured the blob.
  setTimeout(() => URL.revokeObjectURL(url), 0);
  return {success: true};
}
