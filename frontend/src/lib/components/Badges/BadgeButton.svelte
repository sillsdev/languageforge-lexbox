<script lang="ts">
  import type { IconString } from '$lib/icons';
  import Badge, { type BadgeVariant } from './Badge.svelte';

  // Add more as necessary. Should be as limited as possible to maximize consistency.
  type BadgeButtonType = 'badge-success' | '';
  export let type: BadgeButtonType = '';
  export let icon: IconString | undefined = undefined;
  export let disabled = false;

  $: _type = type as unknown as BadgeVariant;
</script>

<button on:click {disabled} class="button-badge">
  <Badge type={_type} {icon}>
    <slot />
  </Badge>
</button>
