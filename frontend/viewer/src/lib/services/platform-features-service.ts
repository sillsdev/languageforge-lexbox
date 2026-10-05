import type {
  IPlatformFeaturesService
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IPlatformFeaturesService';
import {DotnetService} from '$lib/dotnet-types';
import {tryUseService} from '$lib/services/service-provider';
import {SvelteMap} from 'svelte/reactivity';
import type {ICameraResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ICameraResult';

const cache = new SvelteMap<string, boolean>();
const features = ['supportsImageCapture', 'hasHardwareKeyboard'] as const satisfies (keyof Pick<IPlatformFeaturesService, {
  [K in keyof IPlatformFeaturesService]: IPlatformFeaturesService[K] extends () => Promise<boolean> ? K : never
}[keyof IPlatformFeaturesService]>)[];
type Features = typeof features[number];
export function usePlatformFeaturesService(): {service: IPlatformFeaturesService, features: Record<Features, boolean>} {
  const service = tryUseService(DotnetService.PlatformFeaturesService);

  if (!service) {
    return {
      service: {
        captureImage(): Promise<ICameraResult | undefined> {
          return Promise.resolve(undefined);
        },
        supportsImageCapture(): Promise<boolean> {
          return Promise.resolve(false);
        },
        copyToClipboard(): Promise<void> {
          return Promise.resolve();
        },
        supportsShareFile(): Promise<boolean> {
          return Promise.resolve(false);
        },
        shareFile(): Promise<void> {
          return Promise.reject(new Error('Native file sharing is not available'));
        },
        hasHardwareKeyboard(): Promise<boolean> {
          return Promise.resolve(true);
        }
      },
      features: Object.fromEntries(features.map((feature) => [feature, false])) as Record<Features, boolean>
    };
  }
  const featuresObj = {} as Record<Features, boolean>;
  for (const feature of features) {
    if (!cache.has(feature)) queryFeature(service, feature);
    Object.defineProperty(featuresObj, feature, {
      get: () => cache.get(feature) ?? false
    });
  }

  return {
    service,
    features: featuresObj
  };
}

function queryFeature(service: IPlatformFeaturesService, feature: Features) {
  cache.set(feature, false);
  void service[feature]().then((result) => {
    cache.set(feature, result);
  }).catch((err) => {
    // if the service call fails, we want to clear the cache so that we can try again later
    cache.delete(feature);
    throw err;
  });
}

/** Re-asks the platform for every feature; for tests that swap the service's answers after startup. */
export function refreshPlatformFeatures(): void {
  const service = tryUseService(DotnetService.PlatformFeaturesService);
  if (!service) return;
  for (const feature of features) queryFeature(service, feature);
}
