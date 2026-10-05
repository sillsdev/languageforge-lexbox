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
//Stands in when no host registered the service (the plain browser app); its answers are what the cache reports.
export const fallbackPlatformFeaturesService: IPlatformFeaturesService = {
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
};

export function usePlatformFeaturesService(): {service: IPlatformFeaturesService, features: Record<Features, boolean>} {
  const service = tryUseService(DotnetService.PlatformFeaturesService) ?? fallbackPlatformFeaturesService;
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

/** Forgets every cached answer and asks again; for tests that change the service's answers after startup. */
export function refreshPlatformFeatures(): void {
  cache.clear();
  usePlatformFeaturesService();
}
