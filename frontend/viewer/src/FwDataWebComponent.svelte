<script lang="ts">
  import {onMount} from 'svelte';
  import {getSettings} from 'svelte-ux';
  import {
    HttpTransportType,
    type IHttpConnectionOptions
  } from '@microsoft/signalr';
  import FwDataProjectView from './FwDataProjectView.svelte';
  import css from './app.postcss?inline';

  export let projectName: string;
  export let baseUrl: string;
  let signalrConnectionOptions: IHttpConnectionOptions = {withCredentials: false, transport: HttpTransportType.ServerSentEvents + HttpTransportType.LongPolling};
  onMount(() => {
    const shadowRoot = document.querySelector('fw-data-project-view')?.shadowRoot;
    if (!shadowRoot) throw new Error('Could not find shadow root');

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

<svelte:element this={'style'}>
  {css}
</svelte:element>

<div class="app contents" class:dark={$currentTheme.dark}>
  <FwDataProjectView {projectName} {baseUrl} {signalrConnectionOptions}></FwDataProjectView>
</div>
