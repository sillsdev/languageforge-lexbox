<script lang="ts">
  import { onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import { getSettings } from 'svelte-ux';
  import css from './app.postcss?inline';

  let loading = true;

  export let projectName: string;

  onMount(() => {
    const shadowRoot = document.querySelector('lexbox-svelte')?.shadowRoot;
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

    loading = false;

    return () => {
      abortController.abort();
    }
  });

  const { currentTheme } = getSettings();
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<svelte:element this="style">
  {css}
</svelte:element>

<div class="app contents" class:dark={$currentTheme.dark}>
  <ProjectView {projectName} isConnected {loading} showHomeButton={false} />
</div>
