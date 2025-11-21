<script lang="ts">
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Sidebar from '$lib/components/ui/sidebar';
  import {Icon} from '$lib/components/ui/icon';
  import {ComposableInput} from '$lib/components/ui/input';
  import {t} from 'svelte-i18n-lingui';
  import {Toggle} from '$lib/components/ui/toggle';
  import {cn} from '$lib/utils';
  import {mergeProps} from 'bits-ui';
  import {useProjectStats, useWritingSystemService} from '$project/data';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {formatNumber} from '$lib/components/ui/format';
  import ViewT from '$lib/views/ViewT.svelte';
  import {Input} from '$lib/components/ui/input';
  import OpFilter, {type Op} from './filter/OpFilter.svelte';
  import WsSelect from './filter/WsSelect.svelte';
  import FieldSelect, {type SelectedField} from './filter/FieldSelect.svelte';
  import MissingSelect, {type MissingOption} from './filter/MissingSelect.svelte';
  import SemanticDomainSelect from './filter/SemanticDomainSelect.svelte';
  import PartOfSpeechSelect from './filter/PartOfSpeechSelect.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import type {ISemanticDomain, IPartOfSpeech} from '$lib/dotnet-types';
  import {Switch} from '$lib/components/ui/switch';

  const stats = useProjectStats();
  const currentView = useCurrentView();
  const wsService = useWritingSystemService();

  let {
    search = $bindable(),
    gridifyFilter = $bindable(undefined),
    semanticDomain = $bindable(),
    partOfSpeech = $bindable(),
  }: {
    search: string;
    gridifyFilter?: string;
    semanticDomain?: ISemanticDomain;
    partOfSpeech?: IPartOfSpeech;
  } = $props();

  let missingField = $state<MissingOption | null>(null);
  let selectedField = $state<SelectedField | null>(null);
  let selectedWs = $state<string[]>(wsService.vernacularNoAudio.map(ws => ws.wsId));
  let fieldFilterValue = $state('');
  let filterOp = $state<Op>('contains')
  let includeSubDomains = $state(false);

  $effect(() => {
    let newFilter: string[] = [];
    switch (missingField?.id) {
      case 'examples': newFilter.push('Senses.ExampleSentences=null'); break;
      case 'senses': newFilter.push('Senses=null'); break;
      case 'partOfSpeech': newFilter.push('Senses.PartOfSpeechId='); break;
      case 'semanticDomains': newFilter.push('Senses.SemanticDomains=null'); break;
    }

    if (selectedField && fieldFilterValue && selectedWs?.length > 0) {
      let op: string;
      switch (filterOp) {
        case 'starts-with': op = '^'; break;
        case 'contains': op = '=*'; break;
        case 'ends-with': op = '$'; break;
        case 'equals': op = '='; break;
        case 'not-equals': op = '!='; break;
      }
      let fieldFilter = [];
      let escapedValue = escapeGridifyValue(fieldFilterValue);
      for (let ws of selectedWs) {
        fieldFilter.push(`${selectedField.id}[${ws}]${op}${escapedValue}`);
      }
      //construct a filter like LexemeForm[en]=value|LexemeForm[fr]=value
      newFilter.push('(' + fieldFilter.join('|') + ')')
    }

    if (semanticDomain) {
      newFilter.push(`Senses.SemanticDomains.Code${includeSubDomains ? '^' : '='}${semanticDomain.code}`);
    }

    if (partOfSpeech) {
      newFilter.push(`Senses.PartOfSpeechId=${partOfSpeech.id}`);
    }

    gridifyFilter = newFilter.join(', ');
  });


  function escapeGridifyValue(v: string) {
    //from https://alirezanet.github.io/Gridify/guide/filtering#escaping
    return v.replace(/([(),|\\]|\/i)/g, '\\$1');
  }

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
  <Collapsible.Content class="p-2 mb-2 space-y-2 max-h-[calc(65vh-3rem)] overflow-y-auto">
    <div class="flex flex-col">
      <Label class="p-2">{$t`Specific field`}</Label>
      <div class="flex flex-col @md/list:flex-row gap-2 items-stretch">
        <div class="flex flex-row gap-2 flex-1">
          <!-- Field Picker -->
          <FieldSelect bind:value={selectedField} />
          <!-- Writing System Picker -->
          <WsSelect bind:value={selectedWs} wsType={selectedField?.ws} />
        </div>
        <!-- Text Box: on mobile, wraps to new line -->
        <div class="flex flex-row gap-2 flex-1">
          <OpFilter bind:value={filterOp}/>
          <Input
            bind:value={fieldFilterValue}
            placeholder={$t`Filter for...`}
            class="flex-1"
          />
        </div>
      </div>
    </div>
    <div class="flex flex-col">
      <Label class="p-2">{$t`Semantic domain`}</Label>
      <SemanticDomainSelect bind:value={semanticDomain} />
      <Switch class="mt-1.5" disabled={!semanticDomain} bind:checked={includeSubDomains} label={$t`Include subdomains`} />
    </div>
    <div class="flex flex-col">
      <Label class="p-2">{pt($t`Grammatical info.`, $t`Part of speech`, $currentView)}</Label>
      <PartOfSpeechSelect bind:value={partOfSpeech} />
    </div>
    <div class="flex flex-col">
      <Label class="p-2">{$t`Incomplete entries`}</Label>
      <MissingSelect bind:value={missingField} />
    </div>
  </Collapsible.Content>
</Collapsible.Root>
