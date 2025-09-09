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
  import Select from '$lib/components/field-editors/select.svelte';
  import { Input } from '$lib/components/ui/input';
  import {watch} from 'runed';
  import OpFilter, {type Op} from './filter/OpFilter.svelte';
  import WsSelect from './filter/WsSelect.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';

  const stats = useProjectStats();
  const currentView = useCurrentView();
  const wsService = useWritingSystemService();

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

  let fields: Array<{id: string, label: string, ws: 'vernacular-no-audio' | 'analysis-no-audio'}> = $derived([
    { id: 'lexemeForm', label: pt($t`Lexeme Form`, $t`Word`, $currentView), ws: 'vernacular-no-audio' },
    { id: 'citationForm', label: pt($t`Citation Form`, $t`Display as`, $currentView), ws: 'vernacular-no-audio' },
    { id: 'senses.gloss', label: $t`Gloss`, ws: 'analysis-no-audio' },
  ]);

  // svelte-ignore state_referenced_locally
  let selectedField = $state(fields[0]);
  let selectedWs = $state<string[]>(wsService.vernacularNoAudio.map(ws => ws.wsId));
  watch(() => fields, fields => {
    //updates selected field when selected view changes
    selectedField = fields.find(f => f.id === selectedField.id) ?? fields[0];
  });
  let fieldFilterValue = $state('');
  let filterOp = $state<Op>('contains')

  $effect(() => {
    let newFilter: string[] = [];
    if (missingExamples) {
      newFilter.push('Senses.ExampleSentences=null')
    }
    if (missingSenses) {
      newFilter.push('Senses=null')
    }
    if (missingPartOfSpeech) {
      newFilter.push('Senses.PartOfSpeechId=')
    }
    if (missingSemanticDomains) {
      newFilter.push('Senses.SemanticDomains=null')
    }
    if (fieldFilterValue && selectedWs?.length > 0) {
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
  <Collapsible.Content class="p-2 mb-2 space-y-2">
    <div class="flex flex-col @md/list:flex-row gap-2 items-stretch">
      <div class="flex flex-row gap-2 flex-1">
        <!-- Field Picker -->
        <Select
          bind:value={selectedField}
          options={fields}
          idSelector="id"
          labelSelector="label"
          placeholder={$t`Field`}
          class="flex-1"
        />
        <!-- Writing System Picker -->
        <WsSelect bind:value={selectedWs} wsType={selectedField.ws} />
      </div>
      <!-- Text Box: on mobile, wraps to new line -->
      <div class="flex flex-row gap-2 flex-1">
        <OpFilter bind:value={filterOp}/>
        <Input
          bind:value={fieldFilterValue}
          placeholder={$t`Filter for`}
          class="flex-1"
        />
      </div>
    </div>
    <Switch bind:checked={missingExamples} label={$t`Missing Examples`} />
    <Switch bind:checked={missingSenses} label={$t`Missing Senses`} />
    <Switch bind:checked={missingPartOfSpeech} label={$t`Missing Part of Speech`} />
    <Switch bind:checked={missingSemanticDomains} label={$t`Missing Semantic Domains`} />
  </Collapsible.Content>
</Collapsible.Root>
