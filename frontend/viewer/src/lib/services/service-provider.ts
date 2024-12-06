import type {LexboxApiClient} from './lexbox-api';
import {openSearch} from '../search-bar/search';
import {ProjectService} from './projects-service';
import {DotnetService} from '../dotnet-types/generated-types/FwLiteShared/Services/DotnetService';

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
  [LexboxService.LexboxApi]: LexboxApiClient,
  [DotnetService.CombinedProjectsService]: ProjectService,
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
  return window.lexbox.ServiceProvider.getService(LexboxService.LexboxApi);
}

export function useProjectsService(): ProjectService {
  return window.lexbox.ServiceProvider.getService(DotnetService.CombinedProjectsService);
}
window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, new ProjectService());
