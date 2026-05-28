import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {ResourceReturn} from 'runed';

export interface HarnessControls {
  resource: ResourceReturn<string[], unknown, true>;
  showConsumer: () => void;
  destroyConsumer: () => void;
  swapApi: (api: IMiniLcmJsInvokable) => void;
}
