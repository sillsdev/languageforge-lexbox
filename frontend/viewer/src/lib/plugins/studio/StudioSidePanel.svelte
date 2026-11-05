<script lang="ts">
  import * as Tabs from '$lib/components/ui/tabs';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio, type StudioTab} from './plugin-studio.svelte';
  import AboutPanel from './panels/AboutPanel.svelte';
  import ChangeAiPanel from './panels/ChangeAiPanel.svelte';
  import DetailsPanel from './panels/DetailsPanel.svelte';
  import NewPluginAiPanel from './panels/NewPluginAiPanel.svelte';
  import SourcePanel from './panels/SourcePanel.svelte';

  const studio = useStudio();

  const tabLabels = $derived<Record<StudioTab, string>>({
    about: $t`About`,
    ai: $t`Ask AI`,
    source: studio.mode === 'new' ? $t`Paste or upload` : $t`Source`,
    details: $t`Details`,
  });
</script>

<Tabs.Root bind:value={studio.tab} class="min-h-0 grow gap-0">
  <Tabs.List class="m-3 mb-0 w-auto">
    {#each studio.tabs as tab (tab)}
      <Tabs.Trigger value={tab} class="flex-1">{tabLabels[tab]}</Tabs.Trigger>
    {/each}
  </Tabs.List>
  {#each studio.tabs as tab (tab)}
    <Tabs.Content value={tab} class="min-h-0 grow overflow-y-auto p-4">
      {#if studio.loadState === 'ready'}
        {#if tab === 'about' && studio.example}
          <AboutPanel example={studio.example} />
        {:else if tab === 'ai'}
          {#if studio.mode === 'new' && !studio.draft.html}
            <NewPluginAiPanel />
          {:else}
            <ChangeAiPanel />
          {/if}
        {:else if tab === 'source'}
          <SourcePanel />
        {:else if tab === 'details'}
          <DetailsPanel />
        {/if}
      {/if}
    </Tabs.Content>
  {/each}
</Tabs.Root>
