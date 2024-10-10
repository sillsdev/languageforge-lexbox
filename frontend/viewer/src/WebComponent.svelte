<script lang="ts">
  import { onDestroy, onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import { getSettings } from 'svelte-ux';

  //@ts-expect-error This is how we export our styles, so it should be available
  const stylesheetPromise = import('viewer/style')
    .then(() => [...document.querySelectorAll('style')].find((elem) => elem.textContent?.includes('LEXBOX-VIEWER-STYLES')));

  let stylesLoaded = false;

  export let projectName: string;
  const abortController = new AbortController();

  onMount(async () => {
    const shadowRoot = document.querySelector('lexbox-svelte')?.shadowRoot;
    if (!shadowRoot) throw new Error('Could not find shadow root');
    const stylesheet = await stylesheetPromise;
    if (!stylesheet) throw new Error('Could not find lexbox viewer stylesheet');
    shadowRoot.appendChild(stylesheet);
    stylesLoaded = true;

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
  });

  onDestroy(() => {
    return () => {
      abortController.abort();
    };
  })

  const { currentTheme } = getSettings();
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<div class="app contents" class:dark={$currentTheme.dark}>
  {#if stylesLoaded}
    <ProjectView {projectName} isConnected />
  {/if}
</div>
