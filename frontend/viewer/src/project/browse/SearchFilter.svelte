<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import {ComposableInput} from '$lib/components/ui/input';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectStats, useWritingSystemService} from '$project/data';
  import {pt, tvt} from '$lib/views/view-text';
  import {ViewBase} from '$lib/dotnet-types';
  import {entityConfig} from '$lib/views/entity-config';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {formatNumber} from '$lib/components/ui/format';
  import ViewT from '$lib/views/ViewT.svelte';
  import {Input} from '$lib/components/ui/input';
  import OpFilter, {type Op} from './filter/OpFilter.svelte';
  import WsSelect from './filter/WsSelect.svelte';
  import FieldSelect, {type FilterField, type SelectedField} from './filter/FieldSelect.svelte';
  import ActiveFilterChips, {type FilterChip} from './filter/ActiveFilterChips.svelte';
  import MissingSelect, {type MissingOption} from './filter/MissingSelect.svelte';
  import SemanticDomainSelect from './filter/SemanticDomainSelect.svelte';
  import PartOfSpeechSelect from './filter/PartOfSpeechSelect.svelte';
  import PublicationSelect from './filter/PublicationSelect.svelte';
  import MorphTypeSelect from './filter/MorphTypeSelect.svelte';
  import {sameFilter, toGridify, viewDefaultMorphTypeFilter, type MorphTypeFilter} from './filter/morph-type-filter';
  import {watch} from 'runed';
  import {Icon} from '$lib/components/ui/icon';
  import Label from '$lib/components/ui/label/label.svelte';
  import type {ISemanticDomain, IPartOfSpeech, IPublication} from '$lib/dotnet-types';
  import {Switch} from '$lib/components/ui/switch';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {Button, XButton} from '$lib/components/ui/button';
  import Hotkey from '$lib/components/hotkey/hotkey.svelte';
  import {useFeatures} from '$lib/services/feature-service';

  const uid = $props.id();
  const features = useFeatures();
  const stats = useProjectStats();
  const viewService = useViewService();
  const wsService = useWritingSystemService();

  let {
    search = $bindable(),
    gridifyFilter = $bindable(undefined),
    semanticDomain = $bindable(),
    partOfSpeech = $bindable(),
    publication = $bindable(),
    unreadComments = $bindable(false),
  }: {
    search: string;
    gridifyFilter?: string;
    semanticDomain?: ISemanticDomain;
    partOfSpeech?: IPartOfSpeech;
    publication?: IPublication;
    unreadComments?: boolean;
  } = $props();

  let inputRef = $state<HTMLInputElement | null>(null);
  let filterButton = $state<HTMLElement | null>(null);
  let morphTypeTrigger = $state<HTMLElement | null>(null);
  let missingField = $state<MissingOption | null>(null);
  let selectedField = $state<SelectedField | null>(null);
  let selectedWs = $state<string[]>(wsService.vernacularNoAudio.map(ws => ws.wsId));
  let fieldFilterValue = $state('');
  let filterOp = $state<Op>('contains')
  let includeSubDomains = $state(false);
  let userFilterActive = $state(false);
  let filterActive = $state(false);
  let hasComments = $state(false);
  let openComments = $state(false);

  function focusSearch() {
    inputRef?.focus();
    inputRef?.select();
  }

  function viewDefaultMorphTypes(): MorphTypeFilter {
    return viewDefaultMorphTypeFilter(viewService.currentView.base);
  }

  // Views act as presets: switching view base replaces the morph type filter, even one the user cleared
  let morphTypes = $state<MorphTypeFilter>(viewDefaultMorphTypes());
  watch(() => viewService.currentView.base, () => {
    morphTypes = viewDefaultMorphTypes();
  }, {lazy: true});

  const morphTypesAtDefault = $derived(sameFilter(morphTypes, viewDefaultMorphTypes()));
  const showLiteMorphHint = $derived(morphTypesAtDefault && viewService.currentView.base === ViewBase.FwLite);

  $effect(() => {
    let newFilter: string[] = [];
    switch (missingField?.id) {
      case 'examples': newFilter.push('Senses.ExampleSentences=null'); break;
      case 'translations': newFilter.push('Senses.ExampleSentences.Translations=null'); break;
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

    if (hasComments) {
      newFilter.push('CommentThreads!=null');
    }

    if (unreadComments) {
      newFilter.push('UnreadComments!=null');
    }

    if (openComments) {
      newFilter.push('OpenCommentThreads!=null');
    }

    // all user selected filters should be before this line!
    userFilterActive = newFilter.length > 0;

    const morphTypeFilter = toGridify(morphTypes);
    if (morphTypeFilter) newFilter.push(morphTypeFilter);

    filterActive = newFilter.length > 0;
    gridifyFilter = newFilter.join(', ');
  });


  function escapeGridifyValue(v: string) {
    //from https://alirezanet.github.io/Gridify/guide/filtering#escaping
    return v.replace(/([(),|\\]|\/i)/g, '\\$1');
  }

  const canReset = $derived(userFilterActive || !!search || fieldFilterValue !== '' || !morphTypesAtDefault);

  function resetFilters() {
    search = '';
    clearFilters();
  }

  function clearFilters() {
    missingField = null;
    // null tells FieldSelect to fall back to its default field; WsSelect then reacts to the
    // wsType change and repopulates selectedWs with the full writing-system set.
    selectedField = null;
    selectedWs = wsService.vernacularNoAudio.map(ws => ws.wsId);
    fieldFilterValue = '';
    filterOp = 'contains';
    includeSubDomains = false;
    semanticDomain = undefined;
    partOfSpeech = undefined;
    publication = undefined;
    hasComments = false;
    unreadComments = false;
    openComments = false;
    morphTypes = viewDefaultMorphTypes();
  }

  let filtersExpanded = $state(false);

  const fields: FilterField[] = $derived([
    { id: 'lexemeForm', label: pt($tvt(entityConfig.entry.lexemeForm.label), viewService.currentView), ws: 'vernacular-no-audio' },
    { id: 'citationForm', label: pt($tvt(entityConfig.entry.citationForm.label), viewService.currentView), ws: 'vernacular-no-audio' },
    { id: 'senses.gloss', label: $t(entityConfig.sense.gloss.label), ws: 'analysis-no-audio' },
  ]);

  function fieldChipLabel(field: string, value: string): string {
    switch (filterOp) {
      case 'starts-with': return $t`${field} starts with “${value}”`;
      case 'contains': return $t`${field} contains “${value}”`;
      case 'ends-with': return $t`${field} ends with “${value}”`;
      case 'equals': return $t`${field} is “${value}”`;
      case 'not-equals': return $t`${field} is not “${value}”`;
    }
  }

  const chips: FilterChip[] = $derived.by(() => {
    const result: FilterChip[] = [];
    if (selectedField && fieldFilterValue && selectedWs?.length > 0) {
      const field = fields.find(f => f.id === selectedField?.id)?.label ?? selectedField.id;
      result.push({key: 'field', label: fieldChipLabel(field, fieldFilterValue), remove: () => fieldFilterValue = ''});
    }
    if (semanticDomain) {
      const domain = `${semanticDomain.code} ${wsService.pickBestAlternative(semanticDomain.name, 'analysis')}`;
      result.push({
        key: 'semanticDomain',
        label: includeSubDomains ? $t`Semantic domain: ${domain} and subdomains` : $t`Semantic domain: ${domain}`,
        remove: () => { semanticDomain = undefined; includeSubDomains = false; },
      });
    }
    if (partOfSpeech) {
      const name = wsService.pickBestAlternative(partOfSpeech.name, 'analysis');
      result.push({
        key: 'partOfSpeech',
        label: pt($t`Grammatical info.: ${name}`, $t`Part of speech: ${name}`, viewService.currentView),
        remove: () => partOfSpeech = undefined,
      });
    }
    if (publication) {
      const name = wsService.pickBestAlternative(publication.name, 'analysis');
      result.push({key: 'publication', label: $t`Publication: ${name}`, remove: () => publication = undefined});
    }
    if (missingField) {
      result.push({key: 'missing', label: $t`Missing: ${missingField.label}`, remove: () => missingField = null});
    }
    if (hasComments) result.push({key: 'hasComments', label: $t`Has comments`, remove: () => hasComments = false});
    if (unreadComments) result.push({key: 'unreadComments', label: $t`Has unread comments`, remove: () => unreadComments = false});
    if (openComments) result.push({key: 'openComments', label: $t`Has unresolved comments`, remove: () => openComments = false});
    return result;
  });
</script>

<Hotkey key="f" onHotkey={focusSearch} />

{#snippet placeholder()}
  {#if stats.current?.totalEntryCount !== undefined}
    <ViewT view={viewService.currentView} classic={$t`Filter # entries`} lite={$t`Filter # words`}>
      <span class="font-bold">
        {formatNumber(stats.current.totalEntryCount)}
      </span>
    </ViewT>
  {:else}
    {pt($t`Filter entries`, $t`Filter words`, viewService.currentView)}
  {/if}
{/snippet}

<div class="flex items-center gap-0.5">
  <Sidebar.Trigger icon="i-mdi-menu" class="aspect-square p-0" />
  <ComposableInput bind:value={search} bind:inputRef inputProps={{ 'aria-label': $t`Filter` }} {placeholder} autofocus class="px-1 items-center overflow-x-hidden h-12 md:h-10">
    {#snippet after()}
      {#if search}
        <XButton onclick={() => search = ''} class="bg-transparent"/>
      {/if}
      <ResponsivePopup
        bind:open={filtersExpanded}
        title={$t`Filters`}
        contentProps={{
          side: 'right', align: 'start', sideOffset: 10, alignOffset: -4,
          class: 'md:w-96'
        }}
      >
        {#snippet titleActions()}
          <Button variant="ghost" size="sm" icon="i-mdi-filter-remove-outline" onclick={resetFilters} disabled={!canReset} title={$t`Reset to view defaults`}>
            {$t`Reset`}
          </Button>
        {/snippet}
        {#snippet trigger({ props })}
          <Button {...props} bind:ref={filterButton} variant="ghost"
            size={IsMobile.value ? 'icon-sm' : 'icon-xs'}
            icon={filterActive ? 'i-mdi-filter' : 'i-mdi-filter-outline'}
            aria-label={$t`Toggle filters`} />
        {/snippet}
        <div class="space-y-4">
          <div class="flex flex-col">
            <Label id="{uid}-morph-type-label" class="p-2">{$t`Morph type`}</Label>
            <MorphTypeSelect bind:value={morphTypes} bind:triggerRef={morphTypeTrigger} labelledBy="{uid}-morph-type-label"
              describedBy={showLiteMorphHint ? `${uid}-lite-morph-hint` : undefined} />
            {#if showLiteMorphHint}
              <p id="{uid}-lite-morph-hint" class="flex items-center gap-1 text-xs text-muted-foreground mt-1 px-1">
                <Icon icon="i-mdi-information-outline" class="size-3.5 shrink-0" />
                {$t`This view shows only whole words and phrases.`}
                <button type="button" class="underline hover:text-foreground" onclick={() => { morphTypes = 'all'; morphTypeTrigger?.focus(); }}>{$t`Show all`}</button>
              </p>
            {/if}
          </div>
          <div class="flex flex-col">
            <Label class="p-2">{$t`Specific field`}</Label>
            <div class="flex flex-col gap-2 items-stretch">
              <div class="flex flex-row gap-2 flex-1">
                <!-- Field Picker -->
                <FieldSelect bind:value={selectedField} {fields} />
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
            <Label class="p-2">{pt($t`Grammatical info.`, $t`Part of speech`, viewService.currentView)}</Label>
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
          {#if features.comments}
            <div class="flex flex-col">
              <Switch bind:checked={hasComments} label={$t`Has comments`} />
              <Switch class="mt-1.5" bind:checked={unreadComments} label={$t`Has unread comments`} />
              <Switch class="mt-1.5" bind:checked={openComments} label={$t`Has unresolved comments`} />
            </div>
          {/if}
        </div>
      </ResponsivePopup>
    {/snippet}
  </ComposableInput>
</div>
<ActiveFilterChips {chips}
  onShowMore={() => filtersExpanded = true}
  onClear={clearFilters}
  onEmptied={() => filterButton?.focus()} />
