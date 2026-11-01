<script lang="ts">
  import type {Snippet} from 'svelte';
  import type {IEntry} from '$lib/dotnet-types';
  import {T, t} from 'svelte-i18n-lingui';
  import {Badge} from '$lib/components/ui/badge';
  import {Icon} from '$lib/components/ui/icon';
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useWritingSystemService} from '$project/data';
  import {bestHeadword, type ChangeBadge, type CrumbText} from './change-header';
  import {addedTint} from '$lib/entry-editor/diff-view/diff-classes';
  import {cn} from '$lib/utils';
  import EntryActionsButton from './EntryActionsButton.svelte';

  let {label, crumbs = [], identity, entry, related, badge, now}: {
    /** The change's name, e.g. "Edit entry", for assistive tech */
    label: string;
    /** The change's parents by their current name */
    crumbs?: CrumbText[];
    /** The entity as the change left it, usually a before/after anchor */
    identity: Snippet;
    /** The entry the actions open; none when it is deleted */
    entry?: IEntry;
    related?: {entryId: string; label: string};
    badge?: ChangeBadge;
    /** The entry's current state, when its headword is no longer the one the change left */
    now?: IEntry;
  } = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

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
        <span class="leading-6">{crumb.text}<HomographNumber homographNumber={crumb.homographNumber ?? 0} /></span>
        {@render separator()}
      {/each}
    </span>
  {/if}

  <span class="text-lg font-semibold min-w-0 max-w-full">
    {@render identity()}
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
      <T msg="Now called: #"><span class="font-medium text-foreground/80 wrap-break-word">{bestHeadword(writingSystemService, viewService.currentView, now, $t`Untitled`)}</span></T>
    </p>
  {/if}

  {#if entry}
    <div class="ms-auto">
      <EntryActionsButton entryId={entry.id} {related} />
    </div>
  {/if}
</div>
