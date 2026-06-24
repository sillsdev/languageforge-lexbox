import type {LexboxServiceProvider, LexboxServiceRegistry} from './service-provider';

import type {DotNetServiceProvider} from './service-provider-dotnet';

declare global {
  interface Lexbox {
    /* eslint-disable @typescript-eslint/naming-convention */
    DotNetServiceProvider?: DotNetServiceProvider;
    FwLiteProvider?: LexboxServiceRegistry;
    ServiceProvider: LexboxServiceProvider;
    Search: { openSearch: (search: string) => void };
    IsDotnetHosted?: boolean;
    // Expose svelte-routing navigate for native hosts (MAUI) and other integrations
    SvelteNavigate?: (url: string, options?: { replace?: boolean }) => void;
    /* eslint-enable @typescript-eslint/naming-convention */
  }

  interface Window {
    lexbox: Lexbox;
  }
}
