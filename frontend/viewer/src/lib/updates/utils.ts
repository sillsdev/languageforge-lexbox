import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import type {IFwLiteRelease} from '$lib/dotnet-types/generated-types/LexCore/Entities/IFwLiteRelease';
import {openUrl} from '$lib/services/url-opener';
import {useFwLiteConfig} from '$lib/services/service-provider';

const updateUrls: Partial<Record<FwLitePlatform, string>> = {
  [FwLitePlatform.Android]: 'https://play.google.com/store/apps/details?id=org.sil.FwLiteMaui',
};

export const releaseNotesUrl = 'https://community.software.sil.org/t/10807';
// For Android this could be a link to the Play Store page,
// but the "Download Update" button in the app already goes there.
export const downloadPageUrl = 'https://lexbox.org/fw-lite';

/**
 * will start download directly or open the appropriate store page
 */
export async function openReleaseUrl(release: IFwLiteRelease) {
  const url = getReleaseUrl(release);
  await openUrl(url);
}

/**
 * returns a url directly to the update package if possible, otherwise the release page
 */
export function getReleaseUrl(release: IFwLiteRelease): string {
  const fwliteConfig = useFwLiteConfig();
  return updateUrls[fwliteConfig.os] ?? release.url;
}
