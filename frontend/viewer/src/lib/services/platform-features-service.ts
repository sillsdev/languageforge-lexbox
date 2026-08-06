import type {
  IPlatformFeaturesService
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IPlatformFeaturesService';
import {DotnetService} from '$lib/dotnet-types';
import {useService} from '$lib/services/service-provider';
import {SvelteMap} from 'svelte/reactivity';

const cache = new SvelteMap<string, boolean>();
const features = ['supportsImageCapture'] as const satisfies (keyof Pick<IPlatformFeaturesService, {
  [K in keyof IPlatformFeaturesService]: IPlatformFeaturesService[K] extends () => Promise<boolean> ? K : never
}[keyof IPlatformFeaturesService]>)[];
type Features = typeof features[number];
export function usePlatformFeaturesService(): {service: IPlatformFeaturesService, features: Record<Features, boolean>} {
  const service = useService(DotnetService.PlatformFeaturesService);
  const featuresObj = {} as Record<Features, boolean>;
  for (const feature of features) {
    if (!cache.has(feature)) {
      cache.set(feature, false);
      void service[feature]().then((result) => {
        cache.set(feature, result);
      });
    }
    Object.defineProperty(featuresObj, feature, {
      get: () => cache.get(feature) ?? false
    });
  }

  return {
    service,
    features: featuresObj
  };
}
