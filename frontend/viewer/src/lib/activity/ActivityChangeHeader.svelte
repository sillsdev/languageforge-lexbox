<script lang="ts" module>
  import type {IEntry} from '$lib/dotnet-types';

  /** A parent of the change's entity, by its current name: plain text, or an entry that links to itself. */
  export type Crumb = {text: string} | {entry: IEntry};
</script>

<script lang="ts">
  import type {Snippet} from 'svelte';
  import {T, t} from 'svelte-i18n-lingui';
  import {Link} from 'svelte-routing';
  import {Badge} from '$lib/components/ui/badge';
  import {Icon} from '$lib/components/ui/icon';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import {homographSubscript} from '$lib/components/dictionary/HomographNumber.svelte';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {useWritingSystemService} from '$project/data';
  import type {ChangeBadge} from './change-header';
  import {addedTint} from '$lib/entry-editor/diff-view/diff-classes';
  import {cn} from '$lib/utils';
  import EntryActionsButton from './EntryActionsButton.svelte';

  let {label, crumbs = [], identity, linkTo, badge, now}: {
    /** The change's name, e.g. "Edit entry", for assistive tech */
    label: string;
    crumbs?: Crumb[];
    /** The entity as the change left it, usually a before/after anchor */
    identity: Snippet;
    /** The entry the identity links to; none for something deleted */
    linkTo?: IEntry;
    badge?: ChangeBadge;
    /** The entry's current state, when its headword is no longer the one the change left */
    now?: IEntry;
  } = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

  function bestHeadword(entry: IEntry): string {
    return (writingSystemService.viewBestHeadword(entry, viewService.currentView) || $t`Untitled`)
      + homographSubscript(entry.homographNumber);
  }

  function goToLabel(entry: IEntry): string {
    const headword = bestHeadword(entry);
    return pt($t`Go to entry: ${headword}`, $t`Go to word: ${headword}`, viewService.currentView);
  }

  function entryHref(entry: IEntry): string {
    return `./browse?${entryBrowseParams(entry.id)}`;
  }
</script>

{#snippet separator()}
  <Icon icon="i-mdi-chevron-right" class="size-4 align-middle" aria-hidden="true" />
{/snippet}

<!-- Inline-size containment: a long crumb truncates instead of widening whatever lays the header out -->
<div role="group" aria-label={label} class="flex flex-wrap items-center gap-x-1 gap-y-1 min-w-0 [contain:inline-size]" data-testid="change-header">
  {#if crumbs.length}
    <!-- Current names, muted: the parent's context, not what changed. Truncates rather than pushing the identity off a phone screen. -->
    <span class="text-sm text-muted-foreground max-w-full min-w-0 truncate" data-testid="change-crumb">
      {#each crumbs as crumb, i (i)}
        {#if 'entry' in crumb}
          <Link to={entryHref(crumb.entry)} aria-label={goToLabel(crumb.entry)}
                class="inline-block min-h-6 leading-6 align-bottom max-w-full truncate hover:underline">
            <!-- Muted like the other crumbs: writing system colors would compete with the identity -->
            <Headwords entry={crumb.entry} placeholder={$t`Untitled`} class="font-normal [&_span]:text-inherit" />
          </Link>
        {:else}
          <span class="leading-6">{crumb.text}</span>
        {/if}
        {@render separator()}
      {/each}
    </span>
  {/if}

  <span class="text-lg font-semibold min-w-0 max-w-full">
    {#if linkTo}
      <Link to={entryHref(linkTo)} aria-label={goToLabel(linkTo)}
            class="inline-block min-h-6 min-w-6 underline-offset-4 decoration-muted-foreground/50 hover:underline focus-visible:underline">
        {@render identity()}
      </Link>
    {:else}
      {@render identity()}
    {/if}
  </span>

  {#if badge === 'added'}
    <!-- The badges' text says what the color does, so neither relies on the other -->
    <Badge variant="outline" class={cn(addedTint, 'border-transparent')}>{$t`Added`}</Badge>
  {:else if badge === 'deleted'}
    <Badge variant="destructive">{$t`Deleted`}</Badge>
  {:else if badge === 'removed'}
    <Badge variant="destructive">{$t`Removed`}</Badge>
  {:else if badge === 'moved'}
    <Badge variant="outline">{$t`Moved`}</Badge>
  {/if}
  {#if now}
    <!-- Muted like the crumbs: the current name is context, and the identity links to the entry by that name -->
    <p class="basis-full order-last text-sm text-muted-foreground" data-testid="change-now">
      <T msg="Now called: #"><span class="font-medium text-foreground/80 wrap-break-word">{bestHeadword(now)}</span></T>
    </p>
  {/if}

  {#if linkTo}
    <div class="ms-auto">
      <EntryActionsButton entryId={linkTo.id} />
    </div>
  {/if}
</div>
