<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import {ComposableInput} from '$lib/components/ui/input';
  import {t} from 'svelte-i18n-lingui';
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
  import PublicationSelect from './filter/PublicationSelect.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import type {ISemanticDomain, IPartOfSpeech, IPublication} from '$lib/dotnet-types';
  import {MorphType} from '$lib/dotnet-types';
  import {Switch} from '$lib/components/ui/switch';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {Button} from '$lib/components/ui/button';

  const stats = useProjectStats();
  const currentView = useCurrentView();
  const wsService = useWritingSystemService();

  let {
    search = $bindable(),
    gridifyFilter = $bindable(undefined),
    semanticDomain = $bindable(),
    partOfSpeech = $bindable(),
    publication = $bindable(),
  }: {
    search: string;
    gridifyFilter?: string;
    semanticDomain?: ISemanticDomain;
    partOfSpeech?: IPartOfSpeech;
    publication?: IPublication;
  } = $props();

  let missingField = $state<MissingOption | null>(null);
  let selectedField = $state<SelectedField | null>(null);
  let selectedWs = $state<string[]>(wsService.vernacularNoAudio.map(ws => ws.wsId));
  let fieldFilterValue = $state('');
  let filterOp = $state<Op>('contains')
  let includeSubDomains = $state(false);
  let userFilterActive = $state(false);

  const LITE_MORPHEME_TYPES = new Set([
    MorphType.Root, MorphType.BoundRoot,
    MorphType.Stem, MorphType.BoundStem,
    MorphType.Particle,
    MorphType.Phrase, MorphType.DiscontiguousPhrase,
  ]);

  $effect(() => {
    let newFilter: string[] = [];
    switch (missingField?.id) {
      case 'examples': newFilter.push('Senses.ExampleSentences=null'); break;
      case 'senses': newFilter.push('Senses=null'); break;
      case 'partOfSpeech': newFilter.push('Senses.PartOfSpeechId='); break;
      case 'semanticDomains': newFilter.push('Senses.SemanticDomains=null'); break;
      case 'publication': newFilter.push('PublishIn=null'); break;
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

    if (publication) {
      newFilter.push(`PublishIn.Id=${publication.id}`);
    }

    // all user selected filters should be before this line!
    userFilterActive = newFilter.length > 0;

    if ($currentView.type === 'fw-lite') {
      const morphTypeFilters = Array.from(LITE_MORPHEME_TYPES).map(mt => `MorphType=${mt}`);
      newFilter.push('(' + morphTypeFilters.join('|') + ')');
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

<div class="flex items-center gap-0.5">
  <Sidebar.Trigger icon="i-mdi-menu" class="aspect-square p-0" />
  <ComposableInput bind:value={search} inputProps={{ 'aria-label': $t`Filter` }} {placeholder} autofocus class="px-1 items-center overflow-x-hidden h-12 md:h-10">
    {#snippet after()}
      <ResponsivePopup
        bind:open={filtersExpanded}
        title={$t`Filters`}
        contentProps={{
          side: 'right', align: 'start', sideOffset: 10, alignOffset: -4,
          class: 'md:w-96'
        }}
      >
        {#snippet trigger({ props })}
          <Button {...props} variant="ghost"
            size={IsMobile.value ? 'sm-icon' : 'xs-icon'}
            icon={userFilterActive ? 'i-mdi-filter' : 'i-mdi-filter-outline'}
            aria-label={$t`Toggle filters`} />
        {/snippet}
        <div class="space-y-4">
          <div class="flex flex-col">
            <Label class="p-2">{$t`Specific field`}</Label>
            <div class="flex flex-col gap-2 items-stretch">
              <div class="flex flex-row gap-2 flex-1">
                <!-- Field Picker -->
                <FieldSelect bind:value={selectedField} />
                <!-- Writing System Picker -->
                <WsSelect bind:value={selectedWs} wsType={selectedField?.ws} />
              </div>
              <div class="flex flex-row gap-2 flex-1">
                <OpFilter bind:value={filterOp} />
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
            <Switch
              class="mt-1.5"
              disabled={!semanticDomain}
              bind:checked={includeSubDomains}
              label={$t`Include subdomains`}
            />
          </div>
          <div class="flex flex-col">
            <Label class="p-2">{pt($t`Grammatical info.`, $t`Part of speech`, $currentView)}</Label>
            <PartOfSpeechSelect bind:value={partOfSpeech} />
          </div>
          <div class="flex flex-col">
            <Label class="p-2">{$t`Publication`}</Label>
            <PublicationSelect bind:value={publication} />
          </div>
          <div class="flex flex-col">
            <Label class="p-2">{$t`Incomplete entries`}</Label>
            <MissingSelect bind:value={missingField} />
          </div>
        </div>
      </ResponsivePopup>
    {/snippet}
  </ComposableInput>
</div>
