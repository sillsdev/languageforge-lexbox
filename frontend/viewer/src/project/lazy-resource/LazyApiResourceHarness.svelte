<script lang="ts">
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {ResourceReturn} from 'runed';
  import type {HarnessControls} from './lazy-api-resource-test-types';
  import {initProjectContext} from '../project-context.svelte';
  import LazyApiResourceConsumer from './LazyApiResourceConsumer.svelte';

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

  let consumer = $state(false);
  let resource: ResourceReturn<string[], unknown, true> | undefined = $state(undefined);

  function onResource(res: ResourceReturn<string[], unknown, true>) {
    resource = res;
  }

  const controls: HarnessControls = {
    get resource() {
      return resource!;
    },
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
  <LazyApiResourceConsumer fetchData={props.fetchData} {onResource} />
{/if}
