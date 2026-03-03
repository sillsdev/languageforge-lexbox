import {on} from 'svelte/events';
import {onMount} from 'svelte';
import {useAppStorage} from '$lib/utils/app-storage.svelte';
import {useDebounce} from 'runed';

// Tracks the current URL so the backend can restore it on next app launch.
// We intercept pushState/replaceState + popstate to catch ALL URL changes,
// including direct history calls from QueryParamState that bypass svelte-routing's location store.
export function trackUrl() {
  const appStorage = useAppStorage();
  let lastSavedUrl: string | undefined = undefined;

  const saveUrl = useDebounce(async () => {
    const { pathname, search, hash } = document.location;
    const currentUrl = pathname + search + hash;

    if (currentUrl === lastSavedUrl) return;
    await appStorage.lastUrl.set(pathname + search + hash);
    lastSavedUrl = currentUrl;
  }) as () => void;

  onMount(() => {
    const origPushState = history.pushState.bind(history);
    const origReplaceState = history.replaceState.bind(history);
    history.pushState = function (...args) {
      origPushState.apply(this, args);
      saveUrl();
    };
    history.replaceState = function (...args) {
      origReplaceState.apply(this, args);
      saveUrl();
    };

    const unsubscribePopstate = on(window, 'popstate', saveUrl);

    return () => {
      unsubscribePopstate();
      history.pushState = origPushState;
      history.replaceState = origReplaceState;
    };
  });
}
