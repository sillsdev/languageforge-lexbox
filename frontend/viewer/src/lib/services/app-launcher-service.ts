import {DotnetService} from '$lib/dotnet-types';
import type {IAppLauncher} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IAppLauncher';

export function useAppLauncherService(): IAppLauncher | undefined {
  return window.lexbox.ServiceProvider.tryGetService(DotnetService.AppLauncher);
}
