<script context="module" lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';

  setupServiceProvider();
</script>
<script lang="ts">
  import { onMount } from 'svelte';
  import ProjectView from './ProjectView.svelte';
  import {mode, theme} from 'mode-watcher';
  import css from './app.css?inline';
  import {DotnetService, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import ProjectLoader from './ProjectLoader.svelte';
  import {initProjectContext} from '$project/project-context.svelte';
  import {mockFwLiteConfig} from '$project/demo/in-memory-demo-api';
  import type {IFwEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IFwEvent';

  let connecting = true;
  let loading = true;

  export let projectName: string;
  export let api: IMiniLcmJsInvokable;
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

    connecting = false;

    return () => {
      abortController.abort();
    };
  });
  const serviceProvider = window.lexbox.ServiceProvider;
  serviceProvider.setService(DotnetService.FwLiteConfig, mockFwLiteConfig);
  serviceProvider.setService(DotnetService.JsEventListener, {
    nextEventAsync: () => new Promise((_) => {}),
    lastEvent: () => Promise.resolve<IFwEvent | null>(null)
  });
</script>

<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<svelte:element this={'style'}>
  {css}
</svelte:element>

<div class="app contents" class:dark={mode.current === 'dark'} data-theme={theme.current}>
  <ProjectLoader readyToLoadProject={!connecting} {loading} {projectName}>
    <ProjectView onloaded={() => loading = false} />
  </ProjectLoader>
</div>
