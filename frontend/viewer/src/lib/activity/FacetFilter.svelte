<script lang="ts">
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Input} from '$lib/components/ui/input';
  import {Icon} from '$lib/components/ui/icon';

  // The shared shell of the activity facet filters (author, activity type): a summarizing trigger that
  // gains an inline ×-clear while active, a popover/drawer with an optional pinned search box, the rows
  // themselves (caller-rendered), and a pinned Reset footer. Selection semantics are the caller's; this
  // only standardizes the chrome so every facet looks and behaves identically.
  let {
    title,
    triggerLabel,
    active,
    onReset,
    clearLabel,
    showSearch = false,
    searchPlaceholder,
    search = $bindable(''),
    onOpen,
    isEmpty = false,
    emptyText,
    children,
  }: {
    title: string;
    triggerLabel: string;
    active: boolean;
    onReset: () => void;
    /** Aria-label of the × button that clears the facet without opening the popup. */
    clearLabel: string;
    showSearch?: boolean;
    searchPlaceholder?: string;
    search?: string;
    /** Called when the popup opens (after the search box is cleared) — e.g. to recompute expansion state. */
    onOpen?: () => void;
    /** Whether the search matched nothing; shows emptyText below the rows. */
    isEmpty?: boolean;
    emptyText?: string;
    children: Snippet;
  } = $props();

  let open = $state(false);
  $effect(() => {
    if (!open) return;
    search = '';
    onOpen?.();
  });
</script>

<div class="flex w-44 max-w-full grow items-center">
  <ResponsivePopup bind:open {title} contentProps={{class: 'w-80 p-0', align: 'start'}}>
    {#snippet trigger({props})}
      <Button {...props} variant="outline"
        class="min-w-0 grow justify-between font-normal {active ? 'rounded-e-none border-e-0 border-primary/50 font-medium' : ''}">
        <span class="truncate">{triggerLabel}</span>
        <Icon icon="i-mdi-chevron-down" class="size-4 shrink-0 opacity-50" />
      </Button>
    {/snippet}
    <div class="flex max-h-96 flex-col">
      {#if showSearch}
        <div class="p-2 pb-1">
          <Input type="search" placeholder={searchPlaceholder} bind:value={search} />
        </div>
      {/if}
      <div class="min-h-0 grow overflow-y-auto p-1" role="group" aria-label={title}>
        {@render children()}
        {#if isEmpty}
          <div class="p-3 text-center text-sm text-muted-foreground">{emptyText}</div>
        {/if}
      </div>
      {#if active}
        <div class="border-t p-1">
          <Button variant="ghost" size="sm" class="w-full" onclick={onReset}>{$t`Reset`}</Button>
        </div>
      {/if}
    </div>
  </ResponsivePopup>
  {#if active}
    <!-- Clear the whole facet without opening the popup (PatternFly active-filter convention). -->
    <Button variant="outline" size="icon" class="shrink-0 rounded-s-none border-primary/50"
      aria-label={clearLabel} onclick={onReset}>
      <Icon icon="i-mdi-close" class="size-4" />
    </Button>
  {/if}
</div>
