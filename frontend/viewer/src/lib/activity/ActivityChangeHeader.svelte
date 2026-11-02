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

  type EntryAction = {entryId: string; editLabel?: string; goToLabel?: string; headword?: string};

  let {label, crumbs = [], identity, actions = [], badge, now}: {
    /** The change's name, e.g. "Edit entry", for assistive tech */
    label: string;
    /** The change's parents by their current name */
    crumbs?: CrumbText[];
    /** The entity as the change left it, usually a before/after anchor */
    identity: Snippet;
    /** One button group per live entry the change touches */
    actions?: EntryAction[];
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

<!-- A container, so inline-size containment: a long crumb truncates instead of widening whatever lays the header out.
     The actions float top-right and the text flows beside, then under, them. They come first in the DOM only because a float must. -->
<div role="group" aria-label={label} class="@container flow-root min-w-0" data-testid="change-header">
  {#if actions.length}
    <!-- Stacked at equal width to leave the text room, side by side once the card is wide -->
    <div class="float-end ms-2 mb-1 flex flex-col @3xl:flex-row gap-1 [&>*]:w-full @3xl:[&>*]:w-auto">
      {#each actions as action (action.entryId)}
        <EntryActionsButton {...action} />
      {/each}
    </div>
  {/if}
    {#if crumbs.length}
      <!-- Current names, muted: the parent's context, not what changed. A block on narrow cards, so it truncates beside the actions instead of dropping below them. -->
      <span class="block @md:inline-block max-w-full align-bottom text-sm text-muted-foreground truncate" data-testid="change-crumb">
        {#each crumbs as crumb, i (i)}
          <span class="leading-6">{crumb.text}<HomographNumber homographNumber={crumb.homographNumber ?? 0} /></span>
          {@render separator()}
        {/each}
      </span>
    {/if}

    <span class="text-lg font-semibold me-1">
      {@render identity()}
    </span>

    {#if badge === 'added'}
      <!-- The badges' text says what the color does, so neither relies on the other -->
      <Badge variant="outline" class={cn(addedTint, 'border-transparent align-middle')}>{$t`Added`}</Badge>
    {:else if badge === 'deleted'}
      <Badge class="align-middle" variant="destructive">{$t`Deleted`}</Badge>
    {:else if badge === 'removed'}
      <Badge class="align-middle" variant="destructive">{$t`Removed`}</Badge>
    {:else if badge === 'moved'}
      <Badge class="align-middle" variant="outline">{$t`Moved`}</Badge>
    {/if}
    {#if now}
      <!-- Muted like the crumbs: the current name is context -->
      <p class="text-sm text-muted-foreground" data-testid="change-now">
        <T msg="Now called: #"><span class="font-medium text-foreground/80 wrap-break-word">{bestHeadword(writingSystemService, viewService.currentView, now, $t`Untitled`)}</span></T>
      </p>
    {/if}
</div>
