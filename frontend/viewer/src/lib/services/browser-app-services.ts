import {DotnetService} from '$lib/dotnet-types';
import type {IFwEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IFwEvent';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';
import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IPreferencesService';

const localStoragePreferencesService: IPreferencesService = {
  get(key: string): Promise<string | null> {
    return Promise.resolve(localStorage.getItem(key));
  },
  set(key: string, value: string): Promise<void> {
    localStorage.setItem(key, value);
    return Promise.resolve();
  },
  remove(key: string): Promise<void> {
    localStorage.removeItem(key);
    return Promise.resolve();
  },
};

const noopJsEventListener: IJsEventListener = {
  nextEventAsync(): Promise<IFwEvent> {
    // Never resolves — no events to deliver in a browser-only context
    return new Promise<IFwEvent>(() => {});
  },
  lastEvent(): Promise<IFwEvent | null> {
    return Promise.resolve(null);
  },
};

/**
 * Registers app-level services that dotnet would normally provide.
 * Only call this when running without a dotnet host (e.g. Vite dev server, demo, tests).
 */
export function setupBrowserAppServices(): void {
  window.lexbox.ServiceProvider.setService(DotnetService.PreferencesService, localStoragePreferencesService);
  window.lexbox.ServiceProvider.setService(DotnetService.JsEventListener, noopJsEventListener);
}
