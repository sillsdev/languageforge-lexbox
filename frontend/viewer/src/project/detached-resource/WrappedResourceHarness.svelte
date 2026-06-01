<script lang="ts">
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {untrack} from 'svelte';
  import type {WrappedHarnessControls} from './detached-api-resource-test-types';
  import {initProjectContext} from '../project-context.svelte';
  import WrappedResourceConsumer from './WrappedResourceConsumer.svelte';

  const props: {
    fetchData: () => Promise<string[]>;
    lookupKey: string;
    onReady: (controls: WrappedHarnessControls) => void;
  } = $props();

  const projectName = 'Test Project';
  const projectCode = 'test-project';

  const projectContext = initProjectContext({
    api: {} as IMiniLcmJsInvokable,
    projectName,
    projectCode,
  });

  let firstConsumer = $state(false);
  let secondConsumer = $state(false);

  const controls: WrappedHarnessControls = {
    showConsumer: () => {
      firstConsumer = true;
    },
    destroyConsumer: () => {
      firstConsumer = false;
    },
    showSecondConsumer: () => {
      secondConsumer = true;
    },
    swapApi: (api) => {
      projectContext.setup({
        api,
        projectName,
        projectCode,
      });
    },
  };

  untrack(() => props.onReady(controls));
</script>

{#if firstConsumer}
  <div data-testid="first-consumer">
    <WrappedResourceConsumer fetchData={props.fetchData} lookupKey={props.lookupKey} />
  </div>
{/if}
{#if secondConsumer}
  <div data-testid="second-consumer">
    <WrappedResourceConsumer fetchData={props.fetchData} lookupKey={props.lookupKey} />
  </div>
{/if}
