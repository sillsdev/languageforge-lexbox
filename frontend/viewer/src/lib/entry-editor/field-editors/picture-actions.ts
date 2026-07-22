import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';

export type DownloadPictureResult = {success: true} | {success: false; errorMessage?: string};

/**
 * Downloads the image behind a picture's mediaUri, saved under the filename the media server
 * reports for it. getFileStream reports failure via the response (not an exception); the caller
 * decides how to surface `errorMessage`, keeping notification/translation in the component.
 */
export async function downloadPictureFile(api: IMiniLcmJsInvokable, mediaUri: string): Promise<DownloadPictureResult> {
  const file = await api.getFileStream(mediaUri, true);
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
