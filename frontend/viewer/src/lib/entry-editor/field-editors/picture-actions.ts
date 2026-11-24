import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {saveFile} from '$lib/utils/save-file';

export type DownloadPictureResult = {success: true} | {success: false; errorMessage?: string};

export async function downloadPictureFile(api: IMiniLcmJsInvokable, mediaUri: string): Promise<DownloadPictureResult> {
  const file = await api.getFileStream(mediaUri, true);
  if (!file.stream) return {success: false, errorMessage: file.errorMessage ?? undefined};
  const blob = await new Response(await file.stream.stream()).blob();
  await saveFile(blob, file.fileName ?? 'picture');
  return {success: true};
}
