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

  $: pr = disabled ? '!pr-0' : '!pr-1';
  $: br = disabled ? 'border-r-0' : '';
</script>

<button
  on:click={onAction}
  on:keypress={onAction}
  class="btn badge {variant} group transition whitespace-nowrap gap-1 {pr} {br}" class:pointer-events-none={disabled}
>
  <slot />

  {#if !disabled}
    <button class="btn btn-circle btn-xs btn-ghost transition {iconHoverColor}">
      <Icon icon={actionIcon} />
    </button>
  {/if}
</button>
