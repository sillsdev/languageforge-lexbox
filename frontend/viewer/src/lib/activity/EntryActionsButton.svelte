<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Link} from 'svelte-routing';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import * as ButtonGroup from '$lib/components/ui/button-group';
  import {Icon} from '$lib/components/ui/icon';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';

  let {entryId, editLabel, goToLabel, headword}: {
    entryId: string;
    /** Defaults to Edit Entry/Word */
    editLabel?: string;
    /** Defaults to Go to Entry/Word */
    goToLabel?: string;
    /** Appended to the accessible names, so cards' buttons can be told apart */
    headword?: string;
  } = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();
  const features = useFeatures();

  let editOpen = $state(false);
  let editMounted = $state(false);
  const editText = $derived(editLabel ?? pt($t`Edit Entry`, $t`Edit Word`, viewService.currentView));
  const goToText = $derived(goToLabel ?? pt($t`Go to Entry`, $t`Go to Word`, viewService.currentView));

  function withHeadword(text: string): string | undefined {
    return headword ? `${text}: ${headword}` : undefined;
  }
</script>

{#if editMounted}
  <!-- Mounted on first open, since it fetches its entry eagerly and every card has this button. Kept mounted, so closing still animates and returns focus. -->
  <EditEntryDialog bind:open={editOpen} {entryId} />
{/if}

{#snippet goToLink(props: Record<string, unknown>)}
  <Link {...props} to="./browse?{entryBrowseParams(entryId)}" aria-label={withHeadword(goToText)}>
    <Icon icon="i-mdi-link" />
    {goToText}
  </Link>
{/snippet}

<!-- Accessible names start with the visible text (WCAG label in name) -->
<ButtonGroup.Root>
  {#if features.write}
    <Button variant="secondary" size="xs" icon="i-mdi-pencil-outline" class="flex-1 justify-start" onclick={() => { editMounted = true; editOpen = true; }}
            aria-label={withHeadword(editText)}>
      {editText}
    </Button>
  {:else}
    {@render goToLink({class: buttonVariants({variant: 'secondary', size: 'xs', class: 'flex-1 justify-start'})})}
  {/if}
  {#if features.write || multiWindowService}
    <ButtonGroup.Separator class="bg-background" />
    <DropdownMenu.Root>
      <DropdownMenu.Trigger>
        {#snippet child({props})}
          <Button {...props} variant="secondary" size="xs" class="w-7"
                  icon="i-mdi-chevron-down" aria-label={headword ? $t`More options for ${headword}` : $t`More options`} />
        {/snippet}
      </DropdownMenu.Trigger>
      <DropdownMenu.Content align="end">
        {#if features.write}
          <DropdownMenu.Item class="cursor-pointer">
            {#snippet child({props})}
              {@render goToLink(props)}
            {/snippet}
          </DropdownMenu.Item>
        {/if}
        {#if multiWindowService}
          <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entryId)}>
            <Icon icon="i-mdi-open-in-new" />
            {$t`Open in new Window`}
          </DropdownMenu.Item>
        {/if}
      </DropdownMenu.Content>
    </DropdownMenu.Root>
  {/if}
</ButtonGroup.Root>
