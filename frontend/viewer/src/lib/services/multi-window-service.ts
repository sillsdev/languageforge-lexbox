import {DotnetService} from '$lib/dotnet-types';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import type {IMultiWindowService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMultiWindowService';
import {MOBILE_BREAKPOINT} from '../../css-breakpoints';
import {entryBrowseParams} from '$lib/utils/search-params';

/* -10, because the window is likely wider than the viewport and we want the breakpoint to work */
const SM_VIEW_MAX_WIDTH = MOBILE_BREAKPOINT - 10;

export class MultiWindowService implements IMultiWindowService {

  constructor(private readonly _multiWindowService?: IMultiWindowService) {
  }

  async openNewWindow(url?: string, width?: number): Promise<void> {
    if (this._multiWindowService) {
      await this._multiWindowService.openNewWindow(url, width);
      return;
    }

    if (typeof window === 'undefined') return;

    // Fallback to opening a new browser window

    const targetUrl = url ?? location.href;
    // specifying width and height causes a new window to open instead of a tab
    // width is not always respected if height isn't also set
    // the fallbacks/defaults are somewhat arbitrary
    width = width ?? Math.max(window.innerWidth * 0.8, Math.min(window.innerWidth, 600));
    const height = Math.max(window.innerHeight * 0.8, Math.min(window.innerHeight, 400));
    window.open(targetUrl, '_blank', `width=${width},height=${height}`);
  }

  async openEntryInNewWindow(entryId: string) {
    const url = new URL(location.href);
    const [_, projectCode] = url.pathname.split('/').filter(Boolean);
    const browsePath = `/project/${projectCode}/browse`;

    await this.openNewWindow(`${browsePath}?${entryBrowseParams(entryId)}${url.hash}`, SM_VIEW_MAX_WIDTH);
  }
}

export function useMultiWindowService(): MultiWindowService | undefined {
  const multiWindowService = window.lexbox.ServiceProvider.tryGetService(DotnetService.MultiWindowService);
  if (multiWindowService) return new MultiWindowService(multiWindowService);

  const fwLiteConfig = window.lexbox.ServiceProvider.tryGetService(DotnetService.FwLiteConfig);
  const platform = fwLiteConfig?.os;

  // optimistically exclude only the platforms that we're confident don't support window.open()
  if (platform === FwLitePlatform.Android || platform === FwLitePlatform.iOS) {
    return undefined;
  }

  return new MultiWindowService();
}
