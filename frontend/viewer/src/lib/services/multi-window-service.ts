import {DotnetService} from '$lib/dotnet-types';
import type {IMultiWindowService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMultiWindowService';
import {ViewerSearchParam} from '$lib/utils/search-params';

/* -10, because the Windows window width might not match the browser width 🤷 */
const SM_VIEW_MAX_WIDTH = 800 - 10;

export class MultiWindowService implements IMultiWindowService {

  constructor(private readonly _multiWindowService: IMultiWindowService) {
  }

  async openNewWindow(url?: string, width?: number): Promise<void> {
    await this._multiWindowService.openNewWindow(url, width);
  }

  async openEntryInNewWindow(entryId: string) {
    const url = new URL(location.href);
    url.searchParams.set(ViewerSearchParam.EntryId, entryId);
    await this.openNewWindow(url.pathname + url.search + url.hash, SM_VIEW_MAX_WIDTH);
  }
}

export function useMultiWindowService(): MultiWindowService | undefined {
  const multiWindowService = window.lexbox.ServiceProvider.tryGetService(DotnetService.MultiWindowService);
  return multiWindowService ? new MultiWindowService(multiWindowService) : undefined;
}
