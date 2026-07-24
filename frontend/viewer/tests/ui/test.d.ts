import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';

export { }; // for some reason this is required in order to make global changes

declare global {
  interface Window {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    __PLAYWRIGHT_UTILS__: {
      demoApi: IMiniLcmJsInvokable;
      /** Toggle demo write feature and refetch project features. */
      setWrite: (write: boolean) => Promise<void>;
    };
  }
}
