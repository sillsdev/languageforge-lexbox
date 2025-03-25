<script lang="ts">
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import { ComposableInput } from '$lib/components/ui/input';
  import { t } from 'svelte-i18n-lingui';
  import {Switch} from '$lib/components/ui/switch';
  import {Label} from '$lib/components/ui/label';
  import {Toggle} from '$lib/components/ui/toggle';
  import {cn} from '$lib/utils';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

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

<Collapsible.Root bind:open={filtersExpanded} class={cn(filtersExpanded && 'bg-[hsl(var(--sidebar-background))] rounded-b')}>
  <div class="relative">
    <ComposableInput bind:value={search} placeholder={$t`Filter`} autofocus class="px-1">
      {#snippet before()}
        <Sidebar.Trigger icon={IsMobile.value ? 'i-mdi-menu' : 'i-mdi-dock-left'} iconProps={{ class: 'size-5' }} class="aspect-square p-0" size="xs" />
      {/snippet}
      {#snippet after()}
        <Collapsible.Trigger>
          <Toggle aria-label={$t`Toggle filters`} class="aspect-square" size="xs">
            <Icon icon={gridifyFilter ? 'i-mdi-filter' : 'i-mdi-filter-outline'} class="size-5" />
          </Toggle>
        </Collapsible.Trigger>
      {/snippet}
    </ComposableInput>
  </div>
  <Collapsible.Content class="p-2 mb-2 space-y-2">
    <div class="flex items-center gap-2">
      <Switch id="missingExamples" bind:checked={missingExamples} />
      <Label class="cursor-pointer" for="missingExamples">{$t`Missing Examples`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingSenses" bind:checked={missingSenses} />
      <Label class="cursor-pointer" for="missingSenses">{$t`Missing Senses`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingPartOfSpeech" bind:checked={missingPartOfSpeech} />
      <Label class="cursor-pointer" for="missingPartOfSpeech">{$t`Missing Part of Speech`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingSemanticDomains" bind:checked={missingSemanticDomains} />
      <Label class="cursor-pointer" for="missingSemanticDomains">{$t`Missing Semantic Domains`}</Label>
    </div>
  </Collapsible.Content>
</Collapsible.Root>
