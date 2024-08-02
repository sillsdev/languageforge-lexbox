<script lang="ts">
  import { Badge } from '../Badges';
  import type { BadgeVariant } from '../Badges/Badge.svelte';

  export let tag: string;
  export let isActive: boolean;
  export let isDefault: boolean;

  let variant: BadgeVariant = 'badge-info';
  $: variant = isActive ? (isDefault ? 'badge-primary' : 'badge-info') : 'badge-neutral';

</script>

<Badge {variant}>
  {tag}
</Badge>
