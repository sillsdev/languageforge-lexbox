<script lang="ts">
  import {initProjectContext} from './project-context.svelte';
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {ResourceReturn} from 'runed';

  type HarnessControls = {
    resource: ResourceReturn<string[], unknown, true>;
    showConsumer: () => void;
    destroyConsumer: () => void;
    swapApi: (api: IMiniLcmJsInvokable) => void;
  };

  const props: {
    fetchData: () => Promise<string[]>;
    onReady: (controls: HarnessControls) => void;
  } = $props();

  const projectName = 'Test Project';
  const projectCode = 'test-project';

  const projectContext = initProjectContext({
    api: {} as IMiniLcmJsInvokable,
    projectName,
    projectCode,
  });

  const resource = projectContext.lazyApiResource([], () => props.fetchData());

  let consumer = $state(false);

  const controls: HarnessControls = {
    resource,
    showConsumer: () => {
      consumer = true;
    },
    destroyConsumer: () => {
      consumer = false;
    },
    swapApi: (api) => {
      projectContext.setup({
        api,
        projectName,
        projectCode,
      });
    },
  };

  // svelte-ignore state_referenced_locally
  props.onReady(controls);
</script>

{#if consumer}
  <span data-testid="consumer-count">{resource.current.length}</span>
{/if}
