import './service-declaration';
import {openSearch} from '../search-bar/search';
import {DotnetService, type IAuthService, type ICombinedProjectsService} from '../dotnet-types';
import type {IImportFwdataService} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/IImportFwdataService';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {useEventBus} from './event-bus';
import type {IFwLiteConfig} from '$lib/dotnet-types/generated-types/FwLiteShared/IFwLiteConfig';
import type {
  IProjectServicesProvider
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IProjectServicesProvider';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {IAppLauncher} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IAppLauncher';
import type {
  ITroubleshootingService
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ITroubleshootingService';
import type {ITestingService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ITestingService';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';

export type ServiceKey = keyof LexboxServiceRegistry;
export type LexboxServiceRegistry = {
  [DotnetService.MiniLcmApi]: IMiniLcmJsInvokable,
  [DotnetService.CombinedProjectsService]: ICombinedProjectsService,
  [DotnetService.AuthService]: IAuthService,
  [DotnetService.ImportFwdataService]: IImportFwdataService,
  [DotnetService.FwLiteConfig]: IFwLiteConfig,
  [DotnetService.ProjectServicesProvider]: IProjectServicesProvider,
  [DotnetService.HistoryService]: IHistoryServiceJsInvokable,
  [DotnetService.AppLauncher]: IAppLauncher,
  [DotnetService.TroubleshootingService]: ITroubleshootingService,
  [DotnetService.TestingService]: ITestingService,
  [DotnetService.JsEventListener]: IJsEventListener,
};

export const SERVICE_KEYS = Object.values(DotnetService);

export class LexboxServiceProvider {
  private services: LexboxServiceRegistry = {} as LexboxServiceRegistry;

  public setService<K extends ServiceKey>(key: K, service: LexboxServiceRegistry[K]): void {
    this.validateServiceKey(key);
    this.services[key] = service;
  }

  public removeService<K extends ServiceKey>(key: K): void {
    this.validateServiceKey(key);
    delete this.services[key];
  }

  public getService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] {
    this.validateServiceKey(key);
    const service = globalThis.window.lexbox.DotNetServiceProvider?.getService(key) ?? this.services[key];
    if (!service) throw new Error(`Lexbox service '${key}' not found`);
    return service;
  }
  public tryGetService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] | undefined {
    this.validateServiceKey(key);
    return globalThis.window.lexbox.DotNetServiceProvider?.getService(key) ?? this.services[key];
  }

  private validateServiceKey(key: ServiceKey): void {
    if (!SERVICE_KEYS.includes(key)) {
      throw new Error(`Invalid service key: ${key}. Valid values are: ${SERVICE_KEYS.join(', ')}`);
    }
  }
}

export function setupServiceProvider() {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  const lexbox = { ServiceProvider: new LexboxServiceProvider(), Search: {openSearch: openSearch} }
  if (!window.lexbox) {
    window.lexbox = lexbox;
  } else {
    window.lexbox = {...window.lexbox, ...lexbox};
  }
}

setupServiceProvider();

export function useLexboxApi(): IMiniLcmJsInvokable {
  return window.lexbox.ServiceProvider.getService(DotnetService.MiniLcmApi);
}

export function useMiniLcmApi(): IMiniLcmJsInvokable {
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

export function useFwLiteConfig(): IFwLiteConfig {
  return window.lexbox.ServiceProvider.getService(DotnetService.FwLiteConfig);
}

export function useProjectServicesProvider(): IProjectServicesProvider {
  return window.lexbox.ServiceProvider.getService(DotnetService.ProjectServicesProvider);
}

export function useAppLauncher(): IAppLauncher | undefined {
  return window.lexbox.ServiceProvider.tryGetService(DotnetService.AppLauncher);
}

export function useTroubleshootingService(): ITroubleshootingService | undefined {
  return window.lexbox.ServiceProvider.tryGetService(DotnetService.TroubleshootingService);
}

export function useService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] {
  return window.lexbox.ServiceProvider.getService(key);
}

export function tryUseService<K extends ServiceKey>(key: K): LexboxServiceRegistry[K] | undefined {
  return window.lexbox.ServiceProvider.tryGetService(key);
}
