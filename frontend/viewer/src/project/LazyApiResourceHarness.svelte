<script lang="ts">
  import {initProjectContext} from './project-context.svelte';
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {ResourceReturn} from 'runed';

  type HarnessControls = {
    resource: ResourceReturn<string[], unknown, true>;
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

  const fetchData = () => props.fetchData();
  const resource = projectContext.lazyApiResource([], () => fetchData());

  let showConsumer = $state(true);

  const controls: HarnessControls = {
    resource,
    destroyConsumer: () => {
      showConsumer = false;
    },
    swapApi: (api) => {
      projectContext.setup({
        api,
        projectName,
        projectCode,
      });
    },
  };

  const notifyReady = () => props.onReady(controls);
  notifyReady();
</script>

{#if showConsumer}
  <span data-testid="consumer-count">{resource.current.length}</span>
{/if}
