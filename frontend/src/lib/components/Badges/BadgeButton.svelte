<script lang="ts">
  import type { IconString } from '$lib/icons';
  import Badge from './Badge.svelte';

  // Add more as necessary. Should be as limited as possible to maximize consistency.
  type BadgeButtonVariant = 'badge-success' | 'badge-warning' | 'badge-neutral' | undefined;
  export let variant: BadgeButtonVariant = undefined;
  export let icon: IconString | undefined = undefined;
  export let disabled = false;

  $: _type = type as unknown as BadgeVariant;
</script>

<button on:click {disabled} class="badge btn btn-sm !p-0">
  <Badge {variant} {icon}>
    <slot />
  </Badge>
</button>
