import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {usePlatformFeaturesService} from '$lib/services/platform-features-service';

export type DownloadPictureResult = {success: true} | {success: false; errorMessage?: string};

export async function downloadPictureFile(api: IMiniLcmJsInvokable, mediaUri: string): Promise<DownloadPictureResult> {
  const file = await api.getFileStream(mediaUri, true);
  if (!file.stream) return {success: false, errorMessage: file.errorMessage ?? undefined};
  const blob = await new Response(await file.stream.stream()).blob();
  const fileName = file.fileName ?? 'picture';
  // The Apple WebViews ignore <a download>, so hand the file to the native share sheet there.
  const {service: platform} = usePlatformFeaturesService();
  if (await platform.supportsShareFile()) {
    await platform.shareFile(blob, fileName, blob.type || undefined);
    return {success: true};
  }
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  // Release the object URL on the next tick, once the browser has captured the blob.
  setTimeout(() => URL.revokeObjectURL(url), 0);
  return {success: true};
}
