import type {LexboxServiceProvider, LexboxServiceRegistry} from './service-provider';

import type {DotNetServiceProvider} from './service-provider-dotnet';

declare global {
  interface Lexbox {
    /* eslint-disable @typescript-eslint/naming-convention */
    DotNetServiceProvider?: DotNetServiceProvider;
    FwLiteProvider?: LexboxServiceRegistry;
    ServiceProvider: LexboxServiceProvider;
    Search: { openSearch: (search: string) => void };
    /* eslint-enable @typescript-eslint/naming-convention */
  }

  interface Window {
    lexbox: Lexbox;
  }
}
