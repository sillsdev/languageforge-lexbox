import {usePlatformFeaturesService} from '$lib/services/platform-features-service';

/**
 * Hands a file to the user to keep. The MAUI WebViews (iOS, Mac Catalyst, Android) ignore <a download>, so there
 * the file goes to the native share sheet (Save to Files, Save Image, AirDrop...); everywhere else it's a normal download.
 */
export async function saveFile(blob: Blob, fileName: string): Promise<void> {
  const {service: platform} = usePlatformFeaturesService();
  if (await platform.supportsShareFile()) {
    await platform.shareFile(blob, fileName, blob.type || undefined);
    return;
  }
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  // Release the object URL on the next tick, once the browser has captured the blob.
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
