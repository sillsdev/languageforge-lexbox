<script lang="ts">
  import { Icon, type IconString } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import { badgePadding, type BadgeSize, type BadgeVariant } from './Badge.svelte';
  export let disabled = false;
  export let actionIcon: IconString;
  export let variant: BadgeVariant = 'badge-neutral';
  export let size: BadgeSize = 'badge-lg';
  $: padding = badgePadding(size);
  $: iconHoverColor = variant === 'badge-neutral'
    ? 'group-hover:bg-base-200'
    : 'group-hover:bg-neutral/50';

  const dispatch = createEventDispatcher<{
    action: void;
  }>();

  function onAction(): void {
    if (!disabled) {
      dispatch('action');
    }
  }
</script>

<!-- eslint-disable-next-line svelte/valid-compile -->
<button {disabled} class="button-badge group transition" on:click={onAction} on:keypress={onAction}>
  <span class="badge {size} {variant} {padding} pr-0 whitespace-nowrap gap-1">
    <slot />

    {#if !disabled}
      <span
        class="flex justify-center p-1 mr-1 {iconHoverColor} transition rounded-full"
      >
        <Icon icon={actionIcon} />
      </span>
    {/if}
  </span>
</button>

<style>
  .badge {
    border-right: none;
  }
</style>
