<script lang="ts">
  import Badge, { type BadgeType } from './Badge.svelte';

  // Add more as necessary. Should be as limited as possible to maximize consistency.
  type BadgeButtonType = 'badge-success' | '';
  export let type: BadgeButtonType = '';
  export let icon: `i-mdi-${string}` | undefined = undefined;
  export let disabled = false;

  $: _type = type as unknown as BadgeType;
</script>

<button on:click {disabled} class="button-badge">
  <Badge type={_type} {icon}>
    <slot />
  </Badge>
</button>
