import {DotnetService} from '$lib/dotnet-types';
import type {IJsInvokableLogger} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IJsInvokableLogger';

export function useJsInvokableLogger(): IJsInvokableLogger | undefined {
  return window.lexbox.ServiceProvider.tryGetService(DotnetService.JsInvokableLogger);
}
