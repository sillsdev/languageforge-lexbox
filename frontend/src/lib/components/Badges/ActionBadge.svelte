<script lang="ts">
  import { Icon, type IconString } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  export let disabled = false;
  export let actionIcon: IconString;
  export let variant: 'btn-neutral' | 'btn-primary' | 'btn-secondary' = 'btn-neutral';
  $: iconHoverColor = variant === 'btn-neutral' ? 'group-hover:bg-base-200' : 'group-hover:bg-neutral/50';

  const dispatch = createEventDispatcher<{
    action: void;
  }>();

  function onAction(): void {
    if (!disabled) {
      dispatch('action');
    }
  }
</script>

<button
  {disabled}
  on:click={onAction}
  on:keypress={onAction}
  class="btn badge !pr-1 {variant} group transition whitespace-nowrap gap-1"
>
  <slot />

  {#if !disabled}
    <button class="btn btn-circle btn-xs btn-ghost transition {iconHoverColor}">
      <Icon icon={actionIcon} />
    </button>
  {/if}
</button>
