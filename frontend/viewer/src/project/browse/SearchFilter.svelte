<script lang="ts">
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import { ComposableInput } from '$lib/components/ui/input';
  import { t } from 'svelte-i18n-lingui';
  import {Switch} from '$lib/components/ui/switch';
  import {Toggle} from '$lib/components/ui/toggle';
  import {cn} from '$lib/utils';
  import {mergeProps} from 'bits-ui';
  import {useProjectStats} from '$lib/project-stats';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {formatNumber} from '$lib/components/ui/format';
  import ViewT from '$lib/views/ViewT.svelte';

  const stats = useProjectStats();
  const currentView = useCurrentView();

  let {
    search = $bindable(),
    gridifyFilter = $bindable(undefined),
  }: {
    search: string;
    gridifyFilter: string | undefined;
  } = $props();
  let missingExamples = $state(false);
  let missingSenses = $state(false);
  let missingPartOfSpeech = $state(false);
  let missingSemanticDomains = $state(false);
  $effect(() => {
    let newFilter: string[] = [];
    if (missingExamples) {
      newFilter.push('Senses.ExampleSentences=null')
    }
    if (missingSenses) {
      newFilter.push('Senses=null')
    }
    if (missingPartOfSpeech) {
      newFilter.push('Senses.PartOfSpeechId=null')
    }
    if (missingSemanticDomains) {
      newFilter.push('Senses.SemanticDomains=null')
    }
    gridifyFilter = newFilter.join(', ');
  });

  let filtersExpanded = $state(false);
</script>

{#snippet placeholder()}
  {#if stats.current?.totalEntryCount !== undefined}
    <ViewT view={$currentView} classic={$t`Filter # entries`} lite={$t`Filter # words`}>
      <span class="font-bold">
        {formatNumber(stats.current.totalEntryCount)}
      </span>
    </ViewT>
  {:else}
    {pt($t`Filter entries`, $t`Filter words`, $currentView)}
  {/if}
{/snippet}

<Collapsible.Root bind:open={filtersExpanded} class={cn(filtersExpanded && 'bg-muted/50 rounded-b')}>
  <div class="relative">
    <ComposableInput bind:value={search} {placeholder} autofocus class="px-1">
      {#snippet before()}
        <Sidebar.Trigger icon="i-mdi-menu" iconProps={{ class: 'size-5' }} class="aspect-square p-0" size="xs" />
      {/snippet}
      {#snippet after()}
        <Collapsible.Trigger>
          {#snippet child({props})}
            <Toggle {...mergeProps(props, { class: 'aspect-square' })} aria-label={$t`Toggle filters`} size="xs">
              <Icon icon={gridifyFilter ? 'i-mdi-filter' : 'i-mdi-filter-outline'} class="size-5" />
            </Toggle>
          {/snippet}
        </Collapsible.Trigger>
      {/snippet}
    </ComposableInput>
  </div>
  <Collapsible.Content class="p-2 mb-2 space-y-2">
    <Switch bind:checked={missingExamples} label={$t`Missing Examples`} />
    <Switch bind:checked={missingSenses} label={$t`Missing Senses`} />
    <Switch bind:checked={missingPartOfSpeech} label={$t`Missing Part of Speech`} />
    <Switch bind:checked={missingSemanticDomains} label={$t`Missing Semantic Domains`} />
  </Collapsible.Content>
</Collapsible.Root>
