<script lang="ts" module>
  export interface DuplicateSummary {
    count: number;
    capped: boolean;
    hasExactWordMatch: boolean;
    /** Matched headwords, strongest first, comma-joined for one-line display */
    previewHeadwords: string;
    /** One-line banner text, shared by this widget's header and the host's jump pill */
    message: string;
  }
</script>

<script lang="ts">
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {SortField} from '$lib/dotnet-types';
  import {resource, watch} from 'runed';
  import {SvelteMap} from 'svelte/reactivity';
  import {plural, t} from 'svelte-i18n-lingui';
  import {slide} from 'svelte/transition';
  import {navigate, useRouter} from 'svelte-routing';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as ButtonGroup from '$lib/components/ui/button-group';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Badge} from '$lib/components/ui/badge';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';
  import EditEntryDialog from './EditEntryDialog.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSaveHandler} from '$lib/services/save-event-service.svelte';
  import {useLexboxApi} from '$lib/services/service-provider';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {useWritingSystemService} from '$project/data';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
  import {
    classifyDuplicateCheckResults,
    duplicateResultContainerClass,
    getDuplicateCheckQueries,
    mergeSearchResults,
    trapEnter,
    type DuplicateCheckMatch,
    type DuplicateCheckQueries,
  } from './duplicate-check';
  import {cn} from '$lib/utils';

  interface Props {
    entry: IEntry;
    sense?: ISense;
    /** Called right before navigating to an existing entry, so the host dialog can close itself. */
    onNavigateToEntry?: (entry: IEntry) => void;
    /** True while an add-sense save is in flight — the host dialog should block submitting until it settles. */
    busy?: boolean;
    /** Set while there are matches, so the host can render an out-of-view indicator. */
    summary?: DuplicateSummary;
  }

  let {entry, sense, onNavigateToEntry, busy = $bindable(false), summary = $bindable()}: Props = $props();

  const lexboxApi = useLexboxApi();
  const multiWindowService = useMultiWindowService();
  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();
  const saveHandler = useSaveHandler();
  const {base} = useRouter();

  // Over-fetch: the backend is not queryable exactly how we want to use it, so we use the generic/forgiving query api
  // (which sorts best matches to the front) and then pull out the results that are most appropriate
  const FETCH_COUNT = 30;
  const INITIAL_DISPLAY_COUNT = 3;

  const queries = $derived(getDuplicateCheckQueries(entry, sense, writingSystemService));
  const hasQueries = $derived(queries.vernacular.length + queries.analysis.length > 0);

  // Use a cache as a quick way to prevent ALL redundant queries
  // (not just the user typing the same thing twice, but also querying vernacular again when only an analysis query changed, etc.)
  const searchCache = new SvelteMap<string, Promise<IEntry[]>>();
  function search(text: string, writingSystem: string): Promise<IEntry[]> {
    const key = `${writingSystem}:${text}`;
    let result = searchCache.get(key);
    if (!result) {
      result = lexboxApi.searchEntries(text, {
        offset: 0,
        count: FETCH_COUNT,
        order: {field: SortField.SearchRelevance, writingSystem, ascending: true},
      });
      searchCache.set(key, result);
      // don't cache a failure — a transient error must not poison the query for the dialog's life
      result.catch(() => searchCache.delete(key));
    }
    return result;
  }

  const duplicatesResource = resource(
    () => queries,
    async (
      queries,
      _prev,
      {signal},
    ): Promise<{candidates: IEntry[]; queries: DuplicateCheckQueries; capped: boolean} | undefined> => {
      // query.bare / analysis texts are already diacritic-stripped (see getDuplicateEntryQueries):
      // the backend only matches accent-insensitively for a diacritic-free query, and stripping
      // there is what surfaces accent variants for the client to classify. Each query is searched
      // sorted by the writing system it was typed in, so that WS's headword matches sort first.
      const searches = [
        ...queries.vernacular.map((query) => ({text: query.bare, writingSystem: query.wsId})),
        ...queries.analysis.map((text) => ({text, writingSystem: 'default'})),
      ];
      if (!searches.length) return undefined;
      const results = await Promise.all(searches.map((s) => search(s.text, s.writingSystem)));
      signal.throwIfAborted();
      return {
        candidates: mergeSearchResults(results),
        // remember what queries these candidates correspond to
        queries,
        capped: results.some((result) => result.length >= FETCH_COUNT),
      };
    },
    {debounce: DEFAULT_DEBOUNCE_TIME},
  );

  // Classification lives outside the resource so it can compare against the displayed headword:
  // writingSystemService.headword reads the lazy morph-types resource, so once that loads matches
  // re-classify (with the correct morph tokens) without re-searching.
  const matches = $derived.by(() => {
    const result = duplicatesResource.current;
    if (!result) return undefined;
    return classifyDuplicateCheckResults(result.candidates, result.queries, writingSystemService);
  });
  const hasExactWordMatch = $derived(!!matches?.some((match) => match.kind === 'same-word'));
  const previewHeadwords = $derived(
    [...new Set((matches ?? []).map((match) => writingSystemService.headword(match.entry)).filter(Boolean))].join(', '),
  );
  const summaryMessage = $derived.by(() => {
    if (hasExactWordMatch)
      return pt($t`This entry may already exist`, $t`This word may already exist`, viewService.currentView);
    if (matches?.length === 1)
      return pt($t`A similar entry already exists`, $t`A similar word already exists`, viewService.currentView);
    return pt($t`Similar entries already exist`, $t`Similar words already exist`, viewService.currentView);
  });
  $effect(() => {
    summary = matches?.length
      ? {
          count: matches.length,
          capped: !!duplicatesResource.current?.capped,
          hasExactWordMatch,
          previewHeadwords,
          message: summaryMessage,
        }
      : undefined;
  });

  let userToggled = $state(false);
  let expanded = $state(false);
  // Auto-open on an exact match, until the user takes over the strip. Kept as state (not a
  // $derived off userToggled): expand() sets userToggled, which would recompute a derived back
  // to closed — collapsing the strip the jump-pill just tried to open.
  $effect(() => {
    if (hasExactWordMatch && !userToggled) expanded = true;
  });

  /** Opens the match list, counting as a user toggle (the host's jump-pill calls this). */
  export function expand(): void {
    expanded = true;
    userToggled = true;
  }
  let displayCount = $state(INITIAL_DISPLAY_COUNT);
  let expandedEntryId = $state<string>();
  const displayedMatches = $derived(matches?.slice(0, displayCount) ?? []);

  watch(
    () => matches,
    (current) => {
      if (!current?.length) {
        expanded = false;
        userToggled = false;
        displayCount = INITIAL_DISPLAY_COUNT;
        expandedEntryId = undefined;
      }
    },
  );

  function kindLabel(match: DuplicateCheckMatch): string {
    switch (match.kind) {
      case 'same-word':
        // a lexeme-only match on an entry whose citation form differs must not claim "Same
        // headword" — the row displays that (different) citation form as the headword
        return match.field === 'lexeme'
          ? pt($t`Same lexeme form`, $t`Same word`, viewService.currentView)
          : pt($t`Same headword`, $t`Same word`, viewService.currentView);
      case 'similar-word':
        return pt($t`Similar headword`, $t`Similar word`, viewService.currentView);
      case 'similar-meaning':
        return pt($t`Similar gloss`, $t`Similar meaning`, viewService.currentView);
    }
  }

  function openEntry(target: IEntry): void {
    onNavigateToEntry?.(target);
    navigate(`${$base.uri}/browse?${entryBrowseParams(target.id)}`);
  }

  // "Edit" keeps the new-entry dialog open (unlike "Go to", which navigates away and discards it),
  // so the user can amend the existing entry and then decide what to do with their draft.
  let editEntryId = $state<string>();
  let editOpen = $state(false);
  function editEntry(target: IEntry): void {
    editEntryId = target.id;
    editOpen = true;
  }

  // Rescues the meaning the user already typed: instead of creating a duplicate entry,
  // it becomes a new sense of the existing one.
  const canAddSense = $derived(Boolean(sense && writingSystemService.firstDefOrGlossVal(sense)));

  async function addSenseToEntry(target: IEntry): Promise<void> {
    if (!sense || busy) return;
    busy = true;
    try {
      // fresh id: the dialog's sense id must never end up on two entries (e.g. add-sense then create)
      const senseSnapshot = {...$state.snapshot(sense), id: crypto.randomUUID(), entryId: target.id};
      await saveHandler.handleSave(() => lexboxApi.createSense(target.id, senseSnapshot));
    } finally {
      busy = false;
    }
    AppNotification.display(
      pt(
        $t`Sense added to "${writingSystemService.headword(target)}"`,
        $t`Meaning added to "${writingSystemService.headword(target)}"`,
        viewService.currentView,
      ),
      {type: 'success', timeout: 'short'},
    );
    openEntry(target);
  }
</script>

<div class="min-h-9 flex flex-col justify-center w-full" aria-live="polite">
  {#if !matches?.length}
    {#if (duplicatesResource.loading && hasQueries) || matches || duplicatesResource.error}
      <div class="flex items-center gap-2 px-1 text-sm text-muted-foreground" transition:slide={{duration: 150}}>
        {#if duplicatesResource.loading}
          <Loading class="size-4" />
          {pt($t`Checking for similar entries…`, $t`Checking for similar words…`, viewService.currentView)}
        {:else if duplicatesResource.error}
          <!-- inline, not AppNotification.error: this search re-fires per typing pause, and a
            failure toast per pause would bury the dialog; the strip already owns a status line -->
          <Icon icon="i-mdi-alert-outline" class="size-4" />
          {pt($t`Could not check for similar entries`, $t`Could not check for similar words`, viewService.currentView)}
        {:else}
          <Icon icon="i-mdi-check-circle-outline" class="size-4 text-green-600 dark:text-green-500" />
          {pt($t`No similar entries found`, $t`Looks like a new word`, viewService.currentView)}
        {/if}
      </div>
    {/if}
  {:else}
    <Collapsible.Root
      bind:open={expanded}
      onOpenChange={() => (userToggled = true)}
      class={cn('rounded-md border', duplicateResultContainerClass(hasExactWordMatch))}>
      <Collapsible.Trigger
        class="w-full flex items-center gap-2 px-3 py-2 text-sm cursor-pointer"
        onkeydown={trapEnter}
      >
        {#if hasExactWordMatch}
          <Icon icon="i-mdi-alert-circle-outline" class="size-5 shrink-0 text-amber-600 dark:text-amber-400" />
        {:else}
          <Icon icon="i-mdi-information-outline" class="size-5 shrink-0 text-muted-foreground" />
        {/if}
        <span class="grow min-w-0 truncate text-start font-medium">
          {summaryMessage}
          <!-- If expanded, then the preview headwords are just noise -->
          {#if !expanded && previewHeadwords}
            <span class="text-muted-foreground font-normal">— {previewHeadwords}</span>
          {/if}
        </span>
        {#if duplicatesResource.loading}
          <Loading class="size-4" />
        {/if}
        <Badge variant="secondary">{matches.length}{duplicatesResource.current?.capped ? '+' : ''}</Badge>
        <Icon icon={expanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'} class="size-5 shrink-0" />
      </Collapsible.Trigger>
      <Collapsible.Content>
        <ul class="px-1.5 pb-1.5 space-y-1.5 max-h-56 overflow-y-auto">
          {#each displayedMatches as match (match.entry.id)}
            {@const badge = kindLabel(match)}
            {@const isExpanded = expandedEntryId === match.entry.id}
            <li class="rounded bg-background/80">
              <button
                type="button"
                class="w-full flex items-center gap-2 {isExpanded
                  ? 'rounded-t'
                  : 'rounded'} hover:bg-accent px-2.5 py-2 text-start"
                aria-expanded={isExpanded}
                onkeydown={trapEnter}
                onclick={() => (expandedEntryId = isExpanded ? undefined : match.entry.id)}
              >
                <div class="grow min-w-0 text-sm {isExpanded ? '' : 'line-clamp-1'}">
                  <DictionaryEntry entry={match.entry} inline={!isExpanded} hideExamples={!isExpanded} />
                </div>
                {#if badge}
                  <Badge
                    variant="outline"
                    class="shrink-0 self-start whitespace-nowrap {match.kind === 'same-word'
                      ? 'border-amber-600/50 dark:border-amber-400/50'
                      : ''}"
                  >
                    {badge}
                  </Badge>
                {/if}
                <Icon
                  icon={isExpanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'}
                  class="size-4 shrink-0 self-start mt-0.5 text-muted-foreground"
                />
              </button>
              {#if isExpanded}
                <div class="flex flex-wrap justify-end gap-1.5 px-2.5 pt-1 pb-2" transition:slide={{duration: 150}}>
                  {#if canAddSense && (match.kind === 'same-word' || match.kind === 'similar-word')}
                    {@const addSenseLabel = pt($t`Add sense`, $t`Add meaning`, viewService.currentView)}
                    {@const addSenseHint = pt(
                      $t`Add sense to this entry`,
                      $t`Add meaning to this word`,
                      viewService.currentView,
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      icon="i-mdi-playlist-plus"
                      title={addSenseHint}
                      aria-label={addSenseHint}
                      disabled={busy}
                      onkeydown={trapEnter}
                      onclick={() => addSenseToEntry(match.entry)}
                    >
                      {addSenseLabel}
                    </Button>
                  {/if}
                  <ButtonGroup.Root>
                    <Button
                      variant="outline"
                      size="sm"
                      icon="i-mdi-book-arrow-right-outline"
                      disabled={busy}
                      onkeydown={trapEnter}
                      onclick={() => openEntry(match.entry)}
                    >
                      {pt($t`Go to entry`, $t`Go to word`, viewService.currentView)}
                    </Button>
                    <ResponsiveMenu.Root>
                      <ResponsiveMenu.Trigger>
                        {#snippet child({props})}
                          <Button
                            {...props}
                            variant="outline"
                            size="icon-sm"
                            icon="i-mdi-chevron-down"
                            disabled={busy}
                            aria-label={$t`More actions`}
                          />
                        {/snippet}
                      </ResponsiveMenu.Trigger>
                      <ResponsiveMenu.Content>
                        {#if multiWindowService}
                          <ResponsiveMenu.Item
                            icon="i-mdi-open-in-new"
                            onSelect={() => void multiWindowService.openEntryInNewWindow(match.entry.id)}
                          >
                            {$t`Open in new Window`}
                          </ResponsiveMenu.Item>
                        {/if}
                        <ResponsiveMenu.Item icon="i-mdi-pencil-outline" onSelect={() => editEntry(match.entry)}>
                          {pt($t`Edit entry`, $t`Edit word`, viewService.currentView)}
                        </ResponsiveMenu.Item>
                      </ResponsiveMenu.Content>
                    </ResponsiveMenu.Root>
                  </ButtonGroup.Root>
                </div>
              {/if}
            </li>
          {/each}
          {#if matches.length > displayedMatches.length}
            {@const remainingEntries = matches.length - displayedMatches.length}
            <li>
              <Button
                variant="ghost"
                size="sm"
                class="w-full text-muted-foreground"
                onkeydown={trapEnter}
                onclick={() => (displayCount = matches.length)}
              >
                {$plural(remainingEntries, {one: 'Show # more...', other: 'Show # more...'})}
              </Button>
            </li>
          {/if}
        </ul>
      </Collapsible.Content>
    </Collapsible.Root>
  {/if}
</div>

<EditEntryDialog bind:open={editOpen} entryId={editEntryId} />
