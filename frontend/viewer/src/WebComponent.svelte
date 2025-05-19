<script context="module" lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';

  setupServiceProvider();
</script>
<script lang="ts">
  import { onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import {mode, theme} from 'mode-watcher';
  import css from './app.postcss?inline';
  import {DotnetService, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
  import ProjectLoader from './ProjectLoader.svelte';
  import {initProjectContext} from '$lib/project-context.svelte';

  let loading = true;

  export let projectName: string;
  export let api: IMiniLcmJsInvokable;
  export let about: string | undefined;
  initProjectContext({api, projectName, projectCode: projectName});

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
    };
  });
  const serviceProvider = window.lexbox.ServiceProvider;
  serviceProvider.setService(DotnetService.FwLiteConfig, {
    appVersion: 'lexbox-viewer',
    feedbackUrl: '',
    os: FwLitePlatform.Web,
    useDevAssets: true, //has no effect, but is required
  });
  serviceProvider.setService(DotnetService.JsEventListener, {
    nextEventAsync: () => new Promise((_) => {}),
  });
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<svelte:element this={'style'}>
  {css}
</svelte:element>

<div class="app contents" class:dark={mode.current === 'dark'} data-theme={theme.current}>
  <ProjectLoader readyToLoadProject={!loading} {projectName} let:onProjectLoaded>
    <ProjectView isConnected showHomeButton={false} {about}  onloaded={onProjectLoaded} />
  </ProjectLoader>
</div>
