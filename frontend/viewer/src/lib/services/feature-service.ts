import {DotnetService, type IMiniLcmFeatures} from '$lib/dotnet-types';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import {hasFlag} from '$lib/feature-flags/feature-flags.svelte';
import {useProjectContext} from '$project/project-context.svelte';

export type LexboxFeatures = IMiniLcmFeatures;

function isAndroid(): boolean {
  return window.lexbox.ServiceProvider.tryGetService(DotnetService.FwLiteConfig)?.os === FwLitePlatform.Android;
}

export function useFeatures(): LexboxFeatures {
  const context = useProjectContext();
  //need to do this as returning context.features would not be reactive
  return {
    get history() {
      return !!context.features.history;
    },
    get feedback() {
      return !!context.features.feedback;
    },
    get openWithFlex() {
      return !!context.features.openWithFlex;
    },
    get sync() {
      return !!context.features.sync;
    },
    get write() {
      return !!context.features.write;
    },
    get audio() {
      return !!context.features.audio;
    },
    get comments() {
      return !!context.features.comments;
    },
    get commentWrite() {
      return !!context.features.commentWrite;
    },
    get customViews() {
      return !!context.features.customViews;
    },
    get fieldWorksSync() {
      return !!context.features.fieldWorksSync;
    },
    get plugins() {
      // Off on Android: the Blazor WebView bootstrap there forwards every window message to
      // JSInterop without checking its source, so a plugin frame could reach the app (#2917).
      return !!context.features.plugins && hasFlag('plugins') && !isAndroid();
    },
  } satisfies Required<IMiniLcmFeatures>;
}
