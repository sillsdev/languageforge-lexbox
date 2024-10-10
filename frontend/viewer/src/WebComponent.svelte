<script lang="ts">
  import { onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import { fixBrokenNestedGlobalStyles } from './lib/utils/style-fix';
  import { getSettings } from 'svelte-ux';

  let loading = true;

  export let projectName: string;

  onMount(() => {
    const shadowRoot = document.querySelector('lexbox-svelte')?.shadowRoot;
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

    loading = false;

    return () => {
      abortController.abort();
    }
  });

  const { currentTheme } = getSettings();
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<style global lang="postcss">
  @import './app.postcss';
</style>

<div class="app contents" class:dark={$currentTheme.dark}>
  <ProjectView {projectName} isConnected {loading} showHomeButton={false} />
</div>
