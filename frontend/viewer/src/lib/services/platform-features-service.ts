import type {
  IPlatformFeaturesService
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IPlatformFeaturesService';
import {DotnetService} from '$lib/dotnet-types';
import {tryUseService} from '$lib/services/service-provider';
import {SvelteMap} from 'svelte/reactivity';
import type {ICameraResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ICameraResult';

const cache = new SvelteMap<string, boolean>();
const features = ['supportsImageCapture'] as const satisfies (keyof Pick<IPlatformFeaturesService, {
  [K in keyof IPlatformFeaturesService]: IPlatformFeaturesService[K] extends () => Promise<boolean> ? K : never
}[keyof IPlatformFeaturesService]>)[];
type Features = typeof features[number];
export function usePlatformFeaturesService(): {service: IPlatformFeaturesService, features: Record<Features, boolean>} {
  const service = tryUseService(DotnetService.PlatformFeaturesService);

  if (!service) {
    return {
      service: {
        captureImage(): Promise<ICameraResult> {
          //type gen isn't working correctly, this should be Promise<ICameraResult | null>
          return Promise.resolve(null) as unknown as Promise<ICameraResult>;
        },
        supportsImageCapture(): Promise<boolean> {
          return Promise.resolve(false);
        }
      },
      features: Object.fromEntries(features.map((feature) => [feature, false])) as Record<Features, boolean>
    };
  }
  const featuresObj = {} as Record<Features, boolean>;
  for (const feature of features) {
    if (!cache.has(feature)) {
      cache.set(feature, false);
      void service[feature]().then((result) => {
        cache.set(feature, result);
      }).catch((err) => {
        // if the service call fails, we want to clear the cache so that we can try again later
        cache.delete(feature);
        throw err;
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
