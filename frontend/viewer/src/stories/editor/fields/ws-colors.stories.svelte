<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {onMount} from 'svelte';

  const {Story} = defineMeta({});

</script>

<script lang="ts">
  import {useWritingSystemService, type WritingSystemService} from '$project/data';
  import {allWsEntry, writingSystems} from '$project/demo/demo-entry-data';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';

  let wsService = $state<WritingSystemService>();
    onMount(() => {
      wsService = useWritingSystemService();
    });
</script>

<Story name="All writing system colors">
  {#snippet template()}
    <div class="flex flex-col gap-8 p-4">
      <h2 class="text-lg font-bold">Dictionary Entry — Light & Dark</h2>
      <div class="grid grid-cols-2 gap-4">
        <div class="light rounded border p-4" data-theme="blue">
          <h3 class="font-semibold mb-2">Light</h3>
          <DictionaryEntry entry={allWsEntry} />
        </div>
        <div class="dark rounded border p-4 bg-background text-foreground" data-theme="blue">
          <h3 class="font-semibold mb-2">Dark</h3>
          <DictionaryEntry entry={allWsEntry} />
        </div>
      </div>

      <hr class="border-t" />

      <h2 class="text-lg font-bold">Color Swatches — Light & Dark</h2>
      <div class="grid grid-cols-2 gap-4">
        <div class="light rounded border p-4" data-theme="blue">
          <h3 class="font-semibold mb-2">Light</h3>
          <div class="flex flex-col gap-2">
            <h4 class="text-sm font-medium">Vernacular</h4>
            {#each writingSystems.vernacular as ws (ws.wsId)}
              {@const color = wsService?.wsColor(ws.wsId, 'vernacular')}
              <span class={color}>
                {color}
              </span>
            {/each}
            <h4 class="text-sm font-medium mt-2">Analysis</h4>
            {#each writingSystems.analysis as ws (ws.wsId)}
              {@const color = wsService?.wsColor(ws.wsId, 'analysis')}
              <span class={color}>
                {color}
              </span>
            {/each}
          </div>
        </div>
        <div class="dark rounded border p-4 bg-background text-foreground" data-theme="blue">
          <h3 class="font-semibold mb-2">Dark</h3>
          <div class="flex flex-col gap-2">
            <h4 class="text-sm font-medium">Vernacular</h4>
            {#each writingSystems.vernacular as ws (ws.wsId)}
              {@const color = wsService?.wsColor(ws.wsId, 'vernacular')}
              <span class={color}>
                {color}
              </span>
            {/each}
            <h4 class="text-sm font-medium mt-2">Analysis</h4>
            {#each writingSystems.analysis as ws (ws.wsId)}
              {@const color = wsService?.wsColor(ws.wsId, 'analysis')}
              <span class={color}>
                {color}
              </span>
            {/each}
          </div>
        </div>
      </div>
    </div>
  {/snippet}
</Story>
