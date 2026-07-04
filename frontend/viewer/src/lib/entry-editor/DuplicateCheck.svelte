<script lang="ts">
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {SortField} from '$lib/dotnet-types';
  import {resource, watch} from 'runed';
  import {t} from 'svelte-i18n-lingui';
  import {slide} from 'svelte/transition';
  import {navigate, useRouter} from 'svelte-routing';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import {Badge} from '$lib/components/ui/badge';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSaveHandler} from '$lib/services/save-event-service.svelte';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {useWritingSystemService} from '$project/data';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
  import {classifyDuplicates, duplicateQueries, mergeSearchResults, type DuplicateMatch, type DuplicateMatchKind} from './duplicate-check';

  interface Props {
    entry: IEntry;
    sense?: ISense;
    /** Called right before navigating to an existing entry, so the host dialog can close itself. */
    onNavigateToEntry?: (entry: IEntry) => void;
  }

  const {entry, sense, onNavigateToEntry}: Props = $props();

  const lexboxApi = useLexboxApi();
  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();
  const saveHandler = useSaveHandler();
  const {base} = useRouter();

  const FETCH_COUNT = 10;
  const INITIAL_DISPLAY_COUNT = 3;

  const vernacularWsIds = $derived(writingSystemService.vernacularNoAudio.map(ws => ws.wsId));
  const analysisWsIds = $derived(writingSystemService.analysisNoAudio.map(ws => ws.wsId));
  const queries = $derived(duplicateQueries(entry, sense, vernacularWsIds, analysisWsIds));

  const duplicatesResource = resource(
    // string key, so edits to unrelated fields don't retrigger the search
    () => JSON.stringify([queries.vernacular, queries.analysis]),
    async (_key, _prev, {signal}): Promise<{matches: DuplicateMatch[], capped: boolean} | undefined> => {
      const allQueries = [...queries.vernacular, ...queries.analysis];
      if (!allQueries.length) return undefined;
      const results = await Promise.all(allQueries.map(query => lexboxApi.searchEntries(query, {
        offset: 0,
        count: FETCH_COUNT,
        order: {field: SortField.SearchRelevance, writingSystem: 'default', ascending: true},
      })));
      // searchEntries can't take the abort signal over JSInterop and `resource` keeps whatever
      // resolves last, so discard results that a newer keystroke has already superseded
      if (signal.aborted) throw new DOMException('superseded duplicate search', 'AbortError');
      const candidates = mergeSearchResults(results);
      return {
        matches: classifyDuplicates(candidates, queries, vernacularWsIds, analysisWsIds),
        capped: results.some(result => result.length >= FETCH_COUNT),
      };
    },
    {debounce: DEFAULT_DEBOUNCE_TIME},
  );

  const matches = $derived(duplicatesResource.current?.matches);
  const hasExactWordMatch = $derived(!!matches?.some(match => match.kind === 'same-word'));

  let expanded = $state(false);
  let userToggled = $state(false);
  let displayCount = $state(INITIAL_DISPLAY_COUNT);
  const displayedMatches = $derived(matches?.slice(0, displayCount) ?? []);

  // Unfold automatically when the word itself already exists — that's the "stop and look" case.
  // A manual collapse/expand always wins afterwards.
  $effect(() => {
    if (hasExactWordMatch && !userToggled) expanded = true;
  });
  watch(() => matches, current => {
    if (!current?.length) {
      expanded = false;
      userToggled = false;
      displayCount = INITIAL_DISPLAY_COUNT;
    }
  });

  function kindLabel(kind: DuplicateMatchKind): string | undefined {
    switch (kind) {
      case 'same-word':
        return pt($t`Same headword`, $t`Same word`, viewService.currentView);
      case 'similar-word':
        return pt($t`Similar headword`, $t`Similar word`, viewService.currentView);
      case 'same-meaning':
        return pt($t`Similar gloss`, $t`Similar meaning`, viewService.currentView);
      case 'related':
        return undefined;
    }
  }

  function openEntry(target: IEntry): void {
    onNavigateToEntry?.(target);
    navigate(`${$base.uri}/browse?${entryBrowseParams(target.id)}`);
  }

  // Rescues the meaning the user already typed: instead of creating a duplicate entry,
  // it becomes a new sense of the existing one.
  const canAddSense = $derived(!!sense && !!writingSystemService.firstDefOrGlossVal(sense));
  let addingSense = $state(false);

  async function addSenseToEntry(target: IEntry): Promise<void> {
    if (!sense || addingSense) return;
    addingSense = true;
    try {
      const senseSnapshot = {...$state.snapshot(sense), entryId: target.id};
      await saveHandler.handleSave(() => lexboxApi.createSense(target.id, senseSnapshot));
    } finally {
      addingSense = false;
    }
    AppNotification.display(
      pt($t`Sense added to "${writingSystemService.headword(target)}"`,
        $t`Meaning added to "${writingSystemService.headword(target)}"`,
        viewService.currentView),
      {type: 'success', timeout: 'short'});
    openEntry(target);
  }

  // The host dialog submits on Enter; interacting with the duplicate list must never also create the entry
  function trapEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter') event.stopPropagation();
  }
</script>

<div class="min-h-9 flex flex-col justify-center" aria-live="polite">
  {#if !matches?.length}
    {#if duplicatesResource.loading || matches}
      <div class="flex items-center gap-2 px-1 text-sm text-muted-foreground" transition:slide={{duration: 150}}>
        {#if duplicatesResource.loading}
          <Loading class="size-4" />
          {pt($t`Checking for similar entries…`, $t`Checking for similar words…`, viewService.currentView)}
        {:else}
          <Icon icon="i-mdi-check-circle-outline" class="size-4 text-green-600 dark:text-green-500" />
          {pt($t`No similar entries found`, $t`Looks like a new word`, viewService.currentView)}
        {/if}
      </div>
    {/if}
  {:else}
    <Collapsible.Root
      bind:open={expanded}
      onOpenChange={() => userToggled = true}
      class="rounded-md border border-amber-600/40 bg-amber-500/10 dark:border-amber-400/40"
      >
      <Collapsible.Trigger class="w-full flex items-center gap-2 px-3 py-2 text-sm cursor-pointer" onkeydown={trapEnter}>
        <Icon icon="i-mdi-alert-circle-outline" class="size-5 shrink-0 text-amber-600 dark:text-amber-400" />
        <span class="grow text-start font-medium">
          {#if hasExactWordMatch}
            {pt($t`This entry may already exist`, $t`This word may already exist`, viewService.currentView)}
          {:else}
            {pt($t`Similar entries already exist`, $t`Similar words already exist`, viewService.currentView)}
          {/if}
        </span>
        {#if duplicatesResource.loading}
          <Loading class="size-4" />
        {/if}
        <Badge variant="secondary">{matches.length}{duplicatesResource.current?.capped ? '+' : ''}</Badge>
        <Icon icon={expanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'} class="size-5 shrink-0" />
      </Collapsible.Trigger>
      <Collapsible.Content>
        <ul class="px-1.5 pb-1.5 space-y-1 max-h-56 overflow-y-auto overscroll-contain">
          {#each displayedMatches as match (match.entry.id)}
            {@const badge = kindLabel(match.kind)}
            {@const goToLabel = pt($t`Go to entry`, $t`Go to word`, viewService.currentView)}
            {@const headword = writingSystemService.headword(match.entry)}
            <li class="flex items-stretch gap-1">
              <button
                type="button"
                class="grow min-w-0 flex items-center gap-2 rounded bg-background/80 hover:bg-accent px-2.5 py-2 text-start"
                title={goToLabel}
                aria-label={headword ? `${goToLabel}: ${headword}` : goToLabel}
                onkeydown={trapEnter}
                onclick={() => openEntry(match.entry)}>
                <div class="grow min-w-0 line-clamp-2 text-sm">
                  <DictionaryEntry entry={match.entry} hideExamples />
                </div>
                {#if badge}
                  <Badge variant="outline" class="shrink-0 border-amber-600/50 dark:border-amber-400/50 whitespace-nowrap">
                    {badge}
                  </Badge>
                {/if}
                <Icon icon="i-mdi-chevron-right" class="size-4 shrink-0 text-muted-foreground" />
              </button>
              {#if canAddSense}
                {@const addSenseLabel = pt($t`Add sense to this entry`, $t`Add meaning to this word`, viewService.currentView)}
                <Button
                  variant="ghost"
                  size="icon"
                  icon="i-mdi-playlist-plus"
                  class="self-center shrink-0"
                  title={addSenseLabel}
                  aria-label={addSenseLabel}
                  disabled={addingSense}
                  onkeydown={trapEnter}
                  onclick={() => addSenseToEntry(match.entry)} />
              {/if}
            </li>
          {/each}
          {#if matches.length > displayedMatches.length}
            {@const remainingEntries = matches.length - displayedMatches.length}
            <li>
              <Button variant="ghost" size="sm" class="w-full text-muted-foreground"
                onkeydown={trapEnter}
                onclick={() => displayCount = matches.length}>
                {$t`Show ${remainingEntries} more...`}
              </Button>
            </li>
          {/if}
        </ul>
      </Collapsible.Content>
    </Collapsible.Root>
  {/if}
</div>
