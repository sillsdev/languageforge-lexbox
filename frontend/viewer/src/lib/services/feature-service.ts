import type {IMiniLcmFeatures} from '$lib/dotnet-types';
import {useProjectContext} from '$project/project-context.svelte';

export type LexboxFeatures = IMiniLcmFeatures;

export function useFeatures(): LexboxFeatures {
  const context = useProjectContext();
  //need to do this as returning context.features would not be reactive
  return {
    get history() {
      return context.features.history;
    },
    get feedback() {
      return context.features.feedback;
    },
    get openWithFlex() {
      return context.features.openWithFlex;
    },
    get sync() {
      return context.features.sync;
    },
    get write() {
      return context.features.write;
    },
    get audio() {
      return context.features.audio;
    }
  };
}
