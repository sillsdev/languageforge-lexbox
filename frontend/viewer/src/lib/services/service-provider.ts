import type {LexboxApiClient} from './lexbox-api';
import {openSearch} from '../search-bar/search';
import {ProjectService} from './projects-service';
import {DotnetService, type ICombinedProjectsService, type IAuthService} from '../dotnet-types';
import type {IImportFwdataService} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/IImportFwdataService';

declare global {

  interface Lexbox {
    /* eslint-disable @typescript-eslint/naming-convention */
    ServiceProvider: LexboxServiceProvider;
    Search: {openSearch: (search: string) => void};
    /* eslint-enable @typescript-eslint/naming-convention */
  }

  interface Window {
    lexbox: Lexbox;
  }
}

export enum LexboxService {
  LexboxApi = 'LexboxApi'
}
export type ServiceKey = keyof LexboxServiceRegistry;

export type LexboxServiceRegistry = {
  [DotnetService.MiniLcmApi]: LexboxApiClient,
  [DotnetService.CombinedProjectsService]: ICombinedProjectsService,
  [DotnetService.AuthService]: IAuthService,
  [DotnetService.ImportFwdataService]: IImportFwdataService,
};

export const SERVICE_KEYS = [...Object.values(LexboxService), ...Object.values(DotnetService)];

export class LexboxServiceProvider {
  private services: LexboxServiceRegistry = {} as LexboxServiceRegistry;

  public setService<K extends ServiceKey>(key: K, service: LexboxServiceRegistry[K]): void {
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  public getService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] {
    this.validateServiceKey(key);
    const service = globalThis.window.lexbox.DotNetServiceProvider?.getService(key) ?? this.services[key];
    if (!service) throw new Error(`Lexbox service '${key}' not found`);
    return service;
  }

  private validateServiceKey(key: ServiceKey): void {
    if (!SERVICE_KEYS.includes(key)) {
      throw new Error(`Invalid service key: ${key}. Valid values are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}

{
  // eslint-disable-next-line @typescript-eslint/naming-convention
  const lexbox = {ServiceProvider: new LexboxServiceProvider(), Search: {openSearch: openSearch}}
  if (!window.lexbox) {
    window.lexbox = lexbox;
  } else {
    window.lexbox = {...window.lexbox, ...lexbox};
  }
}

export function useLexboxApi(): LexboxApiClient {
  return window.lexbox.ServiceProvider.getService(DotnetService.MiniLcmApi);
}

export function useProjectsService(): ICombinedProjectsService {
  return window.lexbox.ServiceProvider.getService(DotnetService.CombinedProjectsService);
}
export function useAuthService(): IAuthService {
  return window.lexbox.ServiceProvider.getService(DotnetService.AuthService);
}
export function useImportFwdataService(): IImportFwdataService {
  return window.lexbox.ServiceProvider.getService(DotnetService.ImportFwdataService);
}
