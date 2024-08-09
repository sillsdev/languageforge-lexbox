<script lang="ts">
  import {onMount} from 'svelte';
  import {fixBrokenNestedGlobalStyles} from './lib/utils/style-fix';
  import {getSettings} from 'svelte-ux';
  import {
    HttpTransportType,
    type IHttpConnectionOptions
  } from '@microsoft/signalr';
  import FwDataProjectView from './FwDataProjectView.svelte';

  export let projectName: string;
  export let baseUrl: string;
  let signalrConnectionOptions: IHttpConnectionOptions = {withCredentials: false, transport: HttpTransportType.LongPolling};
  onMount(() => {
    const shadowRoot = document.querySelector('fw-data-project-view')?.shadowRoot;
    if (!shadowRoot) throw new Error('Could not find shadow root');
    fixBrokenNestedGlobalStyles(shadowRoot);

    const abortController = new AbortController();
    window.addEventListener('popstate', () => {
      if (!location.hash) return;

      const hashTarget = shadowRoot.querySelector(location.hash);
      if (hashTarget) {
        hashTarget.scrollIntoView({
          behavior: 'smooth',
        });
      }
    }, {
      signal: abortController.signal,
    });

    return () => {
      abortController.abort();
    }
  });

  const { currentTheme } = getSettings();
</script>

<svelte:options customElement={{ tag: 'fw-data-project-view' }} />

<style global lang="postcss">
  @import './app.postcss';
</style>

<div class="app contents" class:dark={$currentTheme.dark}>
  <FwDataProjectView {projectName} {baseUrl} {signalrConnectionOptions}></FwDataProjectView>
</div>
