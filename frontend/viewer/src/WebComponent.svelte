<script context="module" lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';

  setupServiceProvider();
</script>
<script lang="ts">
  import { onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import { getSettings } from 'svelte-ux';
  import css from './app.postcss?inline';
  import {DotnetService} from '$lib/dotnet-types';
  import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
  import ProjectLoader from './ProjectLoader.svelte';

  let loading = true;

  export let projectName: string;
  export let about: string | undefined;

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
  window.lexbox.ServiceProvider.setService(DotnetService.FwLiteConfig, {
    appVersion: 'lexbox-viewer',
    feedbackUrl: '',
    os: FwLitePlatform.Web,
    useDevAssets: true,//has no effect, but is required
  });
  const { currentTheme } = getSettings();
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<svelte:element this={'style'}>
  {css}
</svelte:element>

<div class="app contents" class:dark={$currentTheme.dark}>
  <ProjectLoader readyToLoadProject={!loading} {projectName} let:onProjectLoaded>
    <ProjectView {projectName} isConnected showHomeButton={false} {about}  on:loaded={e => onProjectLoaded(e.detail)} />
  </ProjectLoader>
</div>
