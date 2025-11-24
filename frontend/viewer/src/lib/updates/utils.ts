import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import type {IFwLiteRelease} from '$lib/dotnet-types/generated-types/LexCore/Entities/IFwLiteRelease';
import {openUrl} from '$lib/services/url-opener';
import {useFwLiteConfig} from '$lib/services/service-provider';

const updateUrls: Partial<Record<FwLitePlatform, string>> = {
  [FwLitePlatform.Android]: 'https://play.google.com/store/apps/details?id=org.sil.FwLiteMaui',
};

export async function openReleaseUrl(release: IFwLiteRelease) {
  const fwliteConfig = useFwLiteConfig();
  const url = updateUrls[fwliteConfig.os] ?? release.url;
  await openUrl(url);
}
